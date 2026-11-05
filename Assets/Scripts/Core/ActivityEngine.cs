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
            if(S.rulesRevision<3)
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
                        ApplyTemplate(S.pending,Content.activityEvents.First(e=>e.id=="RF-005"));
                }
                S.eventTarget=Math.Max(Math.Min(S.eventSlots,3),DailyEventCount());
                S.eventSlots=Math.Min(S.eventSlots,S.eventTarget);
                S.rulesRevision=3;
                Log("10월30일 통합 규칙 적용 · 균열50점 / 봉인100·120일 / 전승 성유물");
            }
            foreach(var relic in S.relics)if(relic.history==null)relic.history=new List<RelicChapter>();
            SyncRelicStats();
        }
        public int DailyEventCount()
        {
            var w=B.november.dailyWeights;
            return w!=null&&w.Length==2&&w[0]>=0&&w[1]>=0&&w[0]+w[1]>0?3+Rng.Weighted(w):3;
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
            double personality=B.returnPersonalityOffsets!=null&&B.returnPersonalityOffsets.Length==6?B.returnPersonalityOffsets[(int)a.personality]:0;
            double risk=S.goal==Goal.Rift?ActiveSealCount*B.returnRiskPerSeal:0;
            return a.health/a.MaxHealth<=B.returnHealthRatio+personality+risk || a.hunger<B.returnHunger ||
                a.sanity<B.returnSanity || 24-S.hours<B.minBattleHours;
        }
        public bool RequestReturn(string reason)
        {
            if(S.phase!=Phase.Running||S.avatar==null)return false;
            S.returnRequested=true;S.returnReason=reason;
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
                e.area==(location==Goal.Rift?Area.Rift:S.exploration.current==Area.Shelter?Area.Residence:S.exploration.current) && !e.id.StartsWith("TRN-") && PlotEligible(e) && TemplateAffordable(e) &&
                (string.IsNullOrEmpty(e.relicId)||!S.relics.Any(r=>r.id==e.relicId)) &&
                (string.IsNullOrEmpty(e.rareNpcId)||(!S.recruitedRare.Contains(e.rareNpcId)&&S.candidates.Count+(S.avatar==null?0:1)<Rules.MaxNpc)) &&
                
                (!safe||e.options.Any(o=>o.normal==0&&o.special==0&&!o.boss))).ToList();
        }
        public ActivityEvent DrawGeneralEvent(Goal location)
        {
            var eligible=EligibleEvents(location);if(eligible.Count==0)return null;
            if(location==Goal.Explore&&S.exploration.current==Area.Residence)
            {
                // Preserve the original pool order; append the additional food tickets.
                var food=eligible.Where(e=>e.options.Any(o=>o.discoverFood)).ToArray();
                for(int weight=1;weight<Math.Max(1,B.november.residenceFoodEventWeight);weight++)eligible.AddRange(food);
            }
            return Rng.Pick(eligible);
        }
        void ApplyTemplate(ChoiceEvent destination,ActivityEvent template)
        {
            destination.templateId=template.id;destination.title=template.title;destination.body=template.body;destination.scene=template.scene;destination.context=PlotContext(template);
            destination.options=template.options.Select(o=>o.Copy()).ToArray();
            foreach(var o in destination.options){if(o.defaultHours)o.hours=B.november.defaultEventHours;if(o.mentalRisk)o.sanity=B.november.mentalCost;if(o.optionalEncounter)o.encounterChance*=B.november.optionalEncounterChance;}
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
            if(S.avatar==null||S.phase!=Phase.Running||S.eventSlots>=Math.Max(3,S.eventTarget))return false;
            var template=EligibleEvents(S.goal).FirstOrDefault(e=>e.id==templateId);if(template==null)return false;
            var pending=new ChoiceEvent{id=Id(template.id)};ApplyTemplate(pending,template);
            S.pending=pending;S.phase=Phase.Event;S.eventSlots++;AddFlag(S.plot.viewed,template.id);
            S.nextImportantHour=S.hours+(24-S.hours)/Math.Max(1,S.eventTarget-S.eventSlots+1);
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
            if(IsTransit(e))
            {
                if(index<0||index>1){reason="선택할 수 없습니다.";return false;}
                double required=index==0?AreaRules.TravelHours[S.exploration.destination]:2;
                if(required>24-S.hours){reason="남은 시간 부족";return false;}return true;
            }
            if(e==null||e.choices==null||index<0||index>=e.choices.Length||S.avatar==null){reason="선택할 수 없습니다.";return false;}
            if(e.options==null||e.options.Length!=e.choices.Length)return true;
            var o=e.options[index];
            if(o.hours>24-S.hours+.000001){reason="필요 "+o.hours+"시간 · "+(o.hours-(24-S.hours)).ToString("0.0")+"시간 부족";return false;}
            if(S.food<o.food){reason="식량 "+o.food+"개 필요 · "+(o.food-S.food)+"개 부족";return false;}
            if(S.medicine<o.kits){reason="응급 의료 키트 "+o.kits+"개 필요";return false;}
            if(S.syringes<o.syringes){reason="정신 회복 주사기 "+o.syringes+"개 필요";return false;}
            if((o.normal>0||o.special>0||o.boss)&&S.day==S.avatar.selectedDay){reason="계약 첫날에는 전투하지 않습니다.";return false;}
            if(o.boss&&(S.goal!=Goal.Rift||S.activeRift<0||!CanReachDemon((Sin)S.activeRift))){reason="균열 공략과 진행도 50점이 필요합니다.";return false;}
            if((o.recruit||!string.IsNullOrEmpty(o.rareNpcId))&&S.candidates.Count+1>=Rules.MaxNpc){reason="활성 인원에 빈자리가 없습니다.";return false;}
            return true;
        }
        public int AutomaticChoice(ChoiceEvent e)
        {
            if(e==null||e.choices==null||S.avatar==null)return -1;
            var a=S.avatar;int best=-1;double bestScore=double.NegativeInfinity;
            for(int i=0;i<e.choices.Length;i++)
            {
                if(!CanChoose(e,i,out _))continue;
                if(IsTransit(e)||e.options==null||e.options.Length!=e.choices.Length)return i;
                var o=e.options[i];double reward=0,risk=0;
                bool fight=o.normal>0||o.special>0||o.boss;
                if(o.relicId!=null&&o.relicId.Length>0)reward+=80;
                if(o.rareNpcId!=null&&o.rareNpcId.Length>0)reward+=60;
                if(o.recruit)reward+=35;if(o.divinity&&S.divineEventDay!=S.day)reward+=8;
                if(o.discoverFood)reward+=S.food<80?30:8;if(o.medicalLoot)reward+=8;
                if(o.completes)reward+=12;if(o.flags!=null)reward+=o.flags.Length*3;
                if(o.chance=="recruit")reward*=Math.Min(1,B.scoutSuccess+(a.Has("cleopatra")?B.cleopatraScout:0));
                if(o.chance=="food")reward*=FoodDiscoveryChance;
                if(fight)
                {
                    Sin sin=S.activeRift>=0?(Sin)S.activeRift:Sin.Wrath;
                    var normal=GetEnemyStats(EnemyKind.Normal,null,sin);var special=GetEnemyStats(EnemyKind.Special,null,sin);var demon=GetEnemyStats(EnemyKind.Demon,null,sin);
                    double hp=o.boss?demon.health:o.normal*normal.health+o.special*special.health;
                    double attack=o.boss?demon.attack:o.normal*normal.attack+o.special*special.attack;
                    risk=hp/Math.Max(1,a.Attack+S.equipment)*attack*.5/a.MaxHealth*100*o.encounterChance;
                    reward+=o.boss?100:o.special*15+o.normal;
                }
                double score;
                if(a.personality==Temperament.Defiant)score=-i;
                else if(a.personality==Temperament.Fearful)score=-(fight?1000:0)-(o.injury?200:0)-o.sanity*5-o.hours;
                else if(a.personality==Temperament.Cautious||a.health<a.MaxHealth*.2||a.hunger<30||a.sanity<30)
                    score=-(fight?500:0)-(o.injury?100:0)-o.sanity*3-o.hours;
                else if(a.personality==Temperament.Aggressive)score=(fight?500:0)+reward-o.hours;
                else if(a.personality==Temperament.Cold)score=reward-risk-o.hours*2-o.sanity-(o.injury?B.november.injuryChance*B.november.injuryHp:0);
                else score=reward-risk*.5-o.hours-o.sanity;
                if(score>bestScore||score==bestScore&&i>best){best=i;bestScore=score;}
            }
            return best;
        }
        public bool Resolve(string eventId,int choice)
        {
            var e=S.pending;
            if(S.phase!=Phase.Event||e==null||e.id!=eventId||S.resolvedEvents.Contains(e.id))return false;
            LockControl();
            if(S.controlLocked)choice=AutomaticChoice(e);
            if(!CanChoose(e,choice,out _))
            {
                if(IsTransit(e)&&S.controlLocked)ReturnToShelter();
                return false;
            }
            if(IsTransit(e))return ResolveTransit(choice);
            if(string.IsNullOrEmpty(e.constellationId)&&!S.controlLocked&&S.avatar.personality==Temperament.Defiant&&Rng.Next()<B.refusalChance)
                choice=AutomaticChoice(e);
            if(!CanChoose(e,choice,out _))return false;
            S.resolvedEvents.Add(e.id);S.pending=null;S.phase=Phase.Running;
            if(!string.IsNullOrEmpty(e.constellationId))
            {
                AddFlag(S.permanent.unlocked,e.constellationId);
                var definition=Content.Find(e.constellationId);AddFlag(S.permanent.completedStories,definition.eventId);
                if(!S.permanent.stories.Any(x=>x.constellationId==e.constellationId))
                    S.permanent.stories.Add(new AcquiredStory{constellationId=e.constellationId,title=e.title??definition.title,body=e.body??definition.story,day=S.day});
                Log(definition.name+" · 성좌 영구 해금");
            }
            else
            {
                var o=e.options[choice];Log(e.title+" · "+o.text);
                S.food-=o.food;S.medicine-=o.kits;S.syringes-=o.syringes;S.hours+=o.hours;
                if(o.hunger>0){S.avatar.hunger=Rules.Clamp(S.avatar.hunger-o.hunger);LockControl();if(CheckDeath(false,null))return true;AutoEat();}
                if(o.sanity>0){Stress(o.sanity,e.tags,true);if(CheckDeath(false,null))return true;}
                bool won=true;
                string previousScene=combatScene,previousTitle=combatEventTitle;
                combatScene=e.scene;combatEventTitle=e.title;battleSource=InteractiveCombat?e:null;battleChoice=choice;
                try
                {
                    if(o.boss)won=Battle(EnemyKind.Demon,1,(Sin)S.activeRift);
                    else if(o.normal>0||o.special>0)
                    {
                        if(o.encounterChance>=1||Rng.Next()<o.encounterChance)won=BattleFormation(o.normal,o.special,S.activeRift>=0?(Sin)S.activeRift:Sin.Wrath,true);
                        else{Log("적을 마주치지 않고 조사를 마쳤다.");if(o.noEncounterSanity>0)Stress(o.noEncounterSanity,e.tags,true);}
                    }
                }
                finally{combatScene=previousScene;combatEventTitle=previousTitle;battleSource=null;}
                if(S.phase==Phase.Combat||S.avatar==null||CheckDeath(false,null))return true;
                CompleteEventOption(e,choice,won);
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
        public void SearchFood(){if(Rng.Next()<FoodDiscoveryChance)GrantFoundFood();else Log("물자 수색 · 식량을 발견하지 못했다.");}
        public Character CreateRare(string id)
        {
            var d=Content.rareNpcs.First(n=>n.id==id);
            return new Character{id=Id("rare-"+id),name=d.name,species=d.species,baseHealth=d.health,baseAttack=d.attack,health=d.health,personality=(Temperament)Rng.Int(0,6)};
        }
        public bool RecruitRare(string id)
        {
            if(rewardRareId!=id||S.goal!=Goal.Explore||S.recruitedRare.Contains(id)||S.candidates.Count+(S.avatar==null?0:1)>=Rules.MaxNpc)return false;
            var npc=CreateRare(id);S.candidates.Add(npc);S.recruitedRare.Add(id);Log(npc.name+" · 희귀 생존자 합류");return true;
        }
    }
}

