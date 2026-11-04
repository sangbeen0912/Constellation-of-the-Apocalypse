using System;
using System.Collections.Generic;
using System.Linq;
namespace Apocalypse
{
    public sealed partial class GameEngine
    {
        void MigrateCurrentRules()
        {
            if(S.legacyRelics==null)S.legacyRelics=new List<LegacyRelicRecord>();
            if(S.recruitedRare==null)S.recruitedRare=new List<string>();
            if(S.additionalSupplies==null)S.additionalSupplies=new List<SupplyStack>();
            if(S.relics==null)S.relics=new List<Relic>();
            if(S.rulesRevision<Rules.RulesRevision)
            {
                // Old random relics have no equivalent in the eight unique heirlooms.
                // Archive ownership rather than invent a conversion or erase the old save.
                foreach(var relic in S.relics)
                    S.legacyRelics.Add(new LegacyRelicRecord{id=relic.id,name=relic.name,previousAttackBonus=relic.attackBonus});
                S.relics.Clear();
                if(S.avatar!=null){S.avatar.relics.Clear();S.avatar.relicHealthBonus=S.avatar.relicAttackBonus=0;}
                foreach(var npc in S.candidates){npc.relics.Clear();npc.relicHealthBonus=npc.relicAttackBonus=0;}
                if(S.pending!=null&&string.IsNullOrEmpty(S.pending.constellationId))
                {
                    var template=Content.activityEvents.FirstOrDefault(e=>e.title==S.pending.title);
                    if(template!=null)ApplyTemplate(S.pending,template);
                    else if(S.pending.title=="균열의 마지막 문" && (S.activeRift<0||!CanReachDemon((Sin)S.activeRift)))
                        ApplyTemplate(S.pending,Content.activityEvents.First(e=>e.id=="rift-blockade"));
                }
                S.eventTarget=Math.Max(Math.Min(S.eventSlots,3),DailyEventCount());
                S.eventSlots=Math.Min(S.eventSlots,S.eventTarget);
                S.rulesRevision=Rules.RulesRevision;
                Log("10월30일 통합 규칙 적용 · 균열50점 / 봉인100·120일 / 전승 성유물");
            }
            foreach(var relic in S.relics)if(relic.history==null)relic.history=new List<RelicChapter>();
            SyncRelicStats();
        }
        public int DailyEventCount()
        {
            var weights=B.dailyEventCountWeights;
            if(weights!=null&&weights.Length==3&&weights.All(n=>n>=0)&&weights.Sum()>0)return Rng.Weighted(weights)+1;
            return Math.Max(1,Math.Min(3,B.dailyEventCountFallback));
        }
        public TreatmentPreview PreviewTreatment(bool mental,bool sleep=false,bool useItem=false)
        {
            var p=new TreatmentPreview{hours=sleep?10:useItem?2:6};
            var a=S.avatar;
            if(a==null||S.phase!=Phase.Morning){p.reason="아침에 화신체가 있어야 합니다.";return p;}
            if(sleep&&useItem){p.reason="잠자기에는 아이템을 사용할 수 없습니다.";return p;}
            if(useItem&&(mental?S.syringes:S.medicine)<1){p.reason=mental?"정신 회복 주사기 부족":"응급 의료 키트 부족";return p;}
            if(24-S.hours<p.hours){p.reason="시간 "+p.hours+"시간 필요 · "+(p.hours-(24-S.hours)).ToString("0.0")+"시간 부족";return p;}
            p.healthAfter=sleep||!mental?Rules.Round(Math.Min(a.MaxHealth,a.health+a.MaxHealth*.7*(!sleep&&a.Has("hua")?1+B.huaHeal:1))):a.health;
            p.sanityAfter=sleep||mental?Rules.Round(Math.Min(100,a.sanity+70)):a.sanity;
            p.health=p.healthAfter-a.health;p.sanity=p.sanityAfter-a.sanity;p.available=true;return p;
        }
        public bool ShouldReturn()
        {
            var a=S.avatar;if(a==null)return false;
            double personality=B.returnPersonalityOffsets!=null&&B.returnPersonalityOffsets.Length==4?B.returnPersonalityOffsets[(int)a.personality]:0;
            double risk=S.goal==Goal.Rift?ActiveSealCount*B.returnRiskPerSeal:0;
            return a.health/a.MaxHealth<=B.returnHealthRatio+personality+risk || a.hunger<B.returnHunger ||
                a.sanity<B.returnSanity || S.hours>=B.returnHour || 24-S.hours<B.minBattleHours;
        }
        public bool RequestReturn(string reason)
        {
            if(S.phase!=Phase.Running||S.avatar==null)return false;
            S.returnRequested=true;S.returnReason=reason;
            // A living normal return still receives at least one general choice.
            if(S.eventSlots==0&&S.hours<24){ShowGeneralEvent();return true;}
            ContinueReturn();return true;
        }
        void ContinueReturn()
        {
            if(EncounterConstellation())return;
            Log(S.returnReason+" 쉘터에 정상 귀환했다.");EndDay();
        }
        public bool EncounterConstellation()
        {
            if(S.avatar==null||S.phase!=Phase.Running||string.IsNullOrEmpty(S.scheduledConstellation))return false;
            if(CheckDeath(false,null))return false;
            string id=S.scheduledConstellation;S.scheduledConstellation=null;
            if(S.permanent.unlocked.Contains(id))return false;
            var c=Content.Find(id);
            S.pending=new ChoiceEvent{id=Id(c.eventId),constellationId=id,title=c.title,body=c.story,choices=new[]{"이야기 확인 · 영구 해금"},tags=new Sin[0]};
            S.phase=Phase.Event;SavePoint();
            if(S.controlLocked)Resolve(S.pending.id,0);
            return true;
        }
        public List<ActivityEvent> EligibleEvents(Goal location)
        {
            if(location==Goal.Rift&&(S.activeRift<0||S.avatar==null||S.day==S.avatar.selectedDay))return new List<ActivityEvent>();
            bool safe=S.avatar!=null&&S.day==S.avatar.selectedDay;
            return Content.activityEvents.Where(e=>e.location==location &&
                (string.IsNullOrEmpty(e.relicId)||!S.relics.Any(r=>r.id==e.relicId)) &&
                (string.IsNullOrEmpty(e.rareNpcId)||(!S.recruitedRare.Contains(e.rareNpcId)&&S.candidates.Count+(S.avatar==null?0:1)<Rules.MaxNpc)) &&
                (!e.divinityEvent||S.divineEventDay!=S.day) &&
                (!safe||e.options.Any(o=>o.normal==0&&o.special==0&&!o.boss))).ToList();
        }
        public ActivityEvent DrawGeneralEvent(Goal location)
        {
            var eligible=EligibleEvents(location);return eligible.Count==0?null:Rng.Pick(eligible);
        }
        void ApplyTemplate(ChoiceEvent destination,ActivityEvent template)
        {
            destination.templateId=template.id;destination.title=template.title;destination.body=template.body;destination.scene=template.scene;
            destination.options=template.options.Select(o=>new EventOption{text=o.text,relicId=o.relicId,rareNpcId=o.rareNpcId,chance=o.chance,hours=o.hours,hunger=o.hunger,sanity=o.sanity,food=o.food,kits=o.kits,syringes=o.syringes,normal=o.normal,special=o.special,recruit=o.recruit,discoverFood=o.discoverFood,divinity=o.divinity,returnHome=o.returnHome,boss=o.boss}).ToArray();
            var cost=(B.eventCosts??new EventCost[0]).FirstOrDefault(c=>c.id==template.id);
            if(cost!=null)for(int i=0;i<destination.options.Length;i++)
            {
                var o=destination.options[i];
                if(cost.hours!=null&&i<cost.hours.Length)o.hours=cost.hours[i];
                if(cost.hunger!=null&&i<cost.hunger.Length)o.hunger=cost.hunger[i];
                if(cost.sanity!=null&&i<cost.sanity.Length)o.sanity=cost.sanity[i];
                if(cost.food!=null&&i<cost.food.Length)o.food=cost.food[i];
                if(cost.kits!=null&&i<cost.kits.Length)o.kits=cost.kits[i];
                if(cost.normal!=null&&i<cost.normal.Length)o.normal=cost.normal[i];
                if(cost.special!=null&&i<cost.special.Length)o.special=cost.special[i];
            }
            destination.choices=destination.options.Select(o=>o.text).ToArray();destination.tags=template.tags;
        }
        public bool PresentGeneralEvent(string templateId)
        {
            if(S.avatar==null||S.phase!=Phase.Running||S.eventSlots>=3)return false;
            var template=EligibleEvents(S.goal).FirstOrDefault(e=>e.id==templateId);if(template==null)return false;
            var pending=new ChoiceEvent{id=Id(template.id)};ApplyTemplate(pending,template);
            S.pending=pending;S.phase=Phase.Event;S.eventSlots++;
            S.nextImportantHour=S.hours+(24-S.hours)/Math.Max(1,S.eventTarget-S.eventSlots+1);
            if(template.divinityEvent)S.divineEventDay=S.day;
            SavePoint();if(S.controlLocked)Resolve(pending.id,AutomaticChoice(pending));return true;
        }
        void ShowGeneralEvent()
        {
            var template=DrawGeneralEvent(S.goal);
            if(template==null){S.eventTarget=S.eventSlots;return;}
            PresentGeneralEvent(template.id);
        }
        void ShowBossEvent()
        {
            S.resolvedEvents.Add("boss-offer:"+S.day);
            S.pending=new ChoiceEvent{id=Id("boss"),title="균열의 마지막 문",body="군단의 봉쇄를 돌파했다. 마지막 문 너머, 악마 본체에 도전할 수 있다.",
                choices=new[]{"악마 본체에 도전한다","다음 공략을 준비한다"},
                options=new[]{new EventOption{text="악마 본체에 도전한다",boss=true},new EventOption{text="다음 공략을 준비한다"}},tags=new[]{Sin.Pride,Sin.Wrath}};
            S.phase=Phase.Event;SavePoint();if(S.controlLocked)Resolve(S.pending.id,AutomaticChoice(S.pending));
        }
        public bool CanChoose(ChoiceEvent e,int index,out string reason)
        {
            reason="";
            if(e==null||e.choices==null||index<0||index>=e.choices.Length||S.avatar==null){reason="선택할 수 없습니다.";return false;}
            if(e.options==null||e.options.Length!=e.choices.Length)return true;
            var o=e.options[index];
            if(o.hours>24-S.hours+.000001){reason="필요 "+o.hours+"시간 · "+(o.hours-(24-S.hours)).ToString("0.0")+"시간 부족";return false;}
            if(S.food<o.food){reason="식량 "+o.food+"개 필요 · "+(o.food-S.food)+"개 부족";return false;}
            if(S.medicine<o.kits){reason="응급 의료 키트 "+o.kits+"개 필요";return false;}
            if(S.syringes<o.syringes){reason="정신 회복 주사기 "+o.syringes+"개 필요";return false;}
            if((o.normal>0||o.special>0||o.boss)&&S.day==S.avatar.selectedDay){reason="계약 첫날에는 전투하지 않습니다.";return false;}
            if(o.boss&&(S.goal!=Goal.Rift||S.activeRift<0||!CanReachDemon((Sin)S.activeRift))){reason="균열 공략과 진행도 50점이 필요합니다.";return false;}
            if(!string.IsNullOrEmpty(o.rareNpcId)&&S.candidates.Count+1>=Rules.MaxNpc){reason="활성 인원에 빈자리가 없습니다.";return false;}
            return true;
        }
        public int AutomaticChoice(ChoiceEvent e)
        {
            var available=Enumerable.Range(0,e.choices.Length).Where(i=>CanChoose(e,i,out _)).ToList();if(available.Count==0)return -1;
            var a=S.avatar;
            if(a.health<a.MaxHealth*.4||a.hunger<30||a.sanity<30||a.personality==Temperament.Cautious)
            {
                int safe=available.LastOrDefault(i=>e.options==null||e.options[i].normal==0&&e.options[i].special==0&&!e.options[i].boss);
                if(available.Contains(safe))return safe;
            }
            return a.personality==Temperament.Curious?available[Rng.Int(0,available.Count)]:available[0];
        }
        public bool Resolve(string eventId,int choice)
        {
            var e=S.pending;
            if(S.phase!=Phase.Event||e==null||e.id!=eventId||S.resolvedEvents.Contains(e.id)||choice<0||choice>=e.choices.Length)return false;
            LockControl();
            if(!S.controlLocked&&!CanChoose(e,choice,out _))return false;
            if(string.IsNullOrEmpty(e.constellationId))
            {
                if(S.controlLocked)choice=AutomaticChoice(e);
                else if(choice>=0&&choice<e.choices.Length&&S.avatar.personality==Temperament.Defiant&&Rng.Next()<B.refusalChance)
                {Log("화신체가 제안을 거부하고 스스로 판단한다.");choice=AutomaticChoice(e);}
            }
            if(!CanChoose(e,choice,out _))return false;
            S.resolvedEvents.Add(e.id);S.pending=null;S.phase=Phase.Running;
            if(!string.IsNullOrEmpty(e.constellationId))
            {
                if(!S.permanent.unlocked.Contains(e.constellationId))S.permanent.unlocked.Add(e.constellationId);
                var definition=Content.Find(e.constellationId);
                if(!S.permanent.completedStories.Contains(definition.eventId))S.permanent.completedStories.Add(definition.eventId);
                if(!S.permanent.stories.Any(x=>x.constellationId==e.constellationId))S.permanent.stories.Add(new AcquiredStory{constellationId=e.constellationId,title=e.title??definition.title,body=e.body??definition.story,day=S.day});
                Log(definition.name+" · 성좌 영구 해금");
                if(B.constellationStoryHours>0&&!SpendTime(Activity.Rest,B.constellationStoryHours))return true;
            }
            else
            {
                Log(e.title+" · "+e.choices[choice]);
                var option=e.options!=null&&e.options.Length==e.choices.Length?e.options[choice]:new EventOption();
                // Rechecked above before making the one-time transaction.
                S.food-=option.food;S.medicine-=option.kits;S.syringes-=option.syringes;
                // Listed event costs are the complete event cost, not a hidden Search hunger surcharge.
                S.hours+=option.hours;
                if(option.hunger>0){S.avatar.hunger=Rules.Clamp(S.avatar.hunger-option.hunger);LockControl();if(CheckDeath(false,null))return true;AutoEat();}
                if(option.sanity>0){Stress(option.sanity,e.tags,true);if(CheckDeath(false,null))return true;}
                bool success=true;
                string previousScene=combatScene,previousTitle=combatEventTitle;
                combatScene=e.scene??Content.activityEvents.FirstOrDefault(t=>t.id==e.templateId)?.scene;combatEventTitle=e.title;
                try
                {
                    if(option.boss)success=Battle(EnemyKind.Demon,1,(Sin)S.activeRift);
                    else if(option.normal>0||option.special>0)success=BattleFormation(option.normal,option.special,S.activeRift>=0?(Sin)S.activeRift:Sin.Wrath,true);
                }
                finally{combatScene=previousScene;combatEventTitle=previousTitle;}
                if(S.avatar==null||S.phase==Phase.Death)return true;
                if(!string.IsNullOrEmpty(option.chance))
                {double chance=option.chance=="recruit"?B.scoutSuccess+(S.avatar.Has("cleopatra")?B.cleopatraScout:0):option.chance=="food"?B.foodDiscoveryChance:B.divineSuccessChance;success&=Rng.Next()<chance;}
                if(success)
                {
                    if(option.discoverFood)GrantFoundFood();
                    if(option.divinity){int amount=Rng.Int(3,11);Reward(e.id+":divinity",amount);Log("위험을 견딘 기도 · 신위 +"+amount);}
                    if(option.recruit)Recruit();
                    rewardRelicId=option.relicId;rewardRareId=option.rareNpcId;
                    try
                    {
                        if(!string.IsNullOrEmpty(option.rareNpcId))RecruitRare(option.rareNpcId);
                        if(!string.IsNullOrEmpty(option.relicId))AcquireRelic(option.relicId);
                    }
                    finally {rewardRelicId=null;rewardRareId=null;}
                }
                else Log("조건을 완수하지 못해 이벤트 보상은 얻지 못했다.");
                if(option.returnHome)S.returnRequested=true;
            }
            if(S.returnRequested&&S.phase==Phase.Running)ContinueReturn();
            SavePoint();return true;
        }
        public int RollFoundFoodAmount(){return Rng.Weighted(new[]{60,30,10})+1;}
        void GrantFoundFood()
        {
            int amount=RollFoundFoodAmount();if(S.avatar!=null&&S.avatar.Has("leonardo"))amount=Rules.Round(amount*(1+B.leonardoLoot));
            S.food+=amount;Log("물자 수색 · 식량 +"+amount);
        }
        public void SearchFood(){if(Rng.Next()<B.foodDiscoveryChance)GrantFoundFood();else Log("물자 수색 · 식량을 발견하지 못했다.");}
        public Character CreateRare(string id)
        {
            var d=Content.rareNpcs.First(n=>n.id==id);
            return new Character{id=Id("rare-"+id),name=d.name,species=d.species,baseHealth=d.health,baseAttack=d.attack,health=d.health,personality=(Temperament)Rng.Int(0,4)};
        }
        public bool RecruitRare(string id)
        {
            if(rewardRareId!=id||S.goal!=Goal.Explore||S.recruitedRare.Contains(id)||S.candidates.Count+(S.avatar==null?0:1)>=Rules.MaxNpc)return false;
            var npc=CreateRare(id);S.candidates.Add(npc);S.recruitedRare.Add(id);Log(npc.name+" · 희귀 생존자 합류");return true;
        }
    }
}

