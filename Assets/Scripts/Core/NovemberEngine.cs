using System;
using System.Collections.Generic;
using System.Linq;
namespace Apocalypse
{
    public sealed partial class GameEngine
    {
        void InitializeNovember()
        {
            if(S.exploration==null)S.exploration=new ExplorationState();
            if(S.combat!=null&&string.IsNullOrEmpty(S.combat.id))S.combat=null;
            if(S.combat!=null&&S.combat.queuedMoment!=null&&S.combat.queuedMoment.kind!=CombatMomentKind.Recovery)S.combat.queuedMoment=null;
            if(S.combat!=null)foreach(var e in S.combat.enemies){if(e.kind!=EnemyKind.Named)e.named=null;else if(e.named!=null)e.named=S.permanent.named.FirstOrDefault(n=>n.id==e.named.id)??e.named;}
            if(S.pending!=null&&string.IsNullOrEmpty(S.pending.id))S.pending=null;
            if(S.plot==null)S.plot=new PlotState();
            if(S.uniqueRecruits==null)S.uniqueRecruits=new List<string>();
            if(B.november==null)B.november=new NovemberConfig();
            if(S.rulesRevision<4)
            {
                // Enum value 2 now means Cold, so legacy Curious converts without rerolling.
                foreach(var c in S.candidates)if((int)c.personality>5)c.personality=Temperament.Cold;
                if(S.avatar!=null&&(int)S.avatar.personality>5)S.avatar.personality=Temperament.Cold;
                if(S.phase==Phase.Selection&&S.avatar==null&&S.candidates.Count>0)Select(S.candidates[0].id);
                S.exploration.current=S.phase==Phase.Running||S.phase==Phase.Event?(S.goal==Goal.Rift?Area.Rift:Area.Residence):Area.Shelter;
                if(S.pending!=null&&string.IsNullOrEmpty(S.pending.constellationId))
                {
                    var template=Content.activityEvents.FirstOrDefault(x=>x.id==S.pending.templateId);
                    if(template==null){Log("개편된 옛 사건을 종료하고 현재 위치에서 탐사를 재개합니다.");S.pending=null;S.phase=Phase.Running;}
                }
                S.eventTarget=Math.Max(S.eventSlots,3);S.rulesRevision=4;
                Log("11월4일 규칙 적용 · 지역 탐사 / 행동별 전투 / 호기심→냉철함");
            }
            if(S.rulesRevision<5)
            {
                if(S.hours>Rules.DayHours){Log("이전 규칙의 경과 "+S.hours.ToString("0.0")+"시간을 기록하고 오늘 활동을 마감합니다.");S.hours=Rules.DayHours;}
                if(!pendingCostsUpdated&&S.pending!=null&&S.pending.options!=null)foreach(var o in S.pending.options)o.hours=Rules.ActionHours(o.hours);
                S.nextImportantHour=Math.Min(S.nextImportantHour,Rules.DayHours);
                S.constellationMeetingHour=Math.Min(S.constellationMeetingHour,Rules.DayHours);
                S.rulesRevision=5;
            }
        }
        public bool SelectExplorationArea(Area area)
        {
            if(S.phase!=Phase.Morning||(area!=Area.Residence&&area!=Area.Subway))return false;
            S.exploration.selected=area;SavePoint();return true;
        }
        public bool IsTransit(ChoiceEvent e){return e!=null&&!string.IsNullOrEmpty(e.templateId)&&e.templateId.StartsWith("TRN-");}
        public bool OfferTransit()
        {
            if(S.phase!=Phase.Running||S.goal!=Goal.Explore||S.exploration.current!=Area.Subway)return false;
            S.exploration.resumeTransit=false;
            var choices=new List<Area>(6);
            foreach(var a in AreaRules.Destinations)if(!S.exploration.rejected.Contains((int)a))choices.Add(a);
            if(choices.Count==0){S.exploration.rejected.Clear();choices.AddRange(AreaRules.Destinations);}
            var dest=Rng.Pick(choices);S.exploration.destination=(int)dest;S.exploration.routeOffered=true;
            string id="TRN-"+(Array.IndexOf(AreaRules.Destinations,dest)+1).ToString("00");
            var t=Content.activityEvents.First(x=>x.id==id);var e=new ChoiceEvent{id=Id(id)};ApplyTemplate(e,t);
            S.pending=e;S.phase=Phase.Event;SavePoint();return true;
        }
        bool ResolveTransit(int choice)
        {
            var e=S.pending;if(!IsTransit(e)||choice<0||choice>1)return false;
            var dest=(Area)S.exploration.destination;double hours=choice==0?AreaRules.TravelHours[(int)dest]:1;
            if(hours>Rules.DayHours-S.hours)return false;
            S.pending=null;S.phase=Phase.Running;
            if(!SpendTime(Activity.Travel,hours))return true;
            if(choice==0)
            {
                S.exploration.current=dest;S.exploration.destination=-1;S.exploration.rejected.Clear();
                Log("도보 이동 · "+AreaRules.Name(dest)+" 도착 · "+hours+"시간");
            }
            else
            {
                S.exploration.rejected.Add((int)dest);while(S.exploration.rejected.Count>2)S.exploration.rejected.RemoveAt(0);
                Log("다른 노선 탐색 · 1시간");
                if(S.hours>=Rules.DayHours)RequestReturn("탐사 시간이 끝나");
                else if(S.eventSlots<S.eventTarget&&Rng.Next()<B.november.rerollEventChance)
                {
                    S.exploration.resumeTransit=true;
                    var next=DrawGeneralEvent(Goal.Explore);
                    if(next==null||!PresentGeneralEvent(next.id))OfferTransit();
                }
                else OfferTransit();
            }
            SavePoint();return true;
        }
        public bool ReturnToSubway()
        {
            if(S.phase!=Phase.Running||S.combat!=null||S.exploration.current<Area.Museum||S.exploration.current>Area.Heritage||Rules.DayHours-S.hours<.5)return false;
            if(!SpendTime(Activity.Travel,.5))return true;
            S.exploration.current=Area.Subway;S.exploration.routeOffered=false;Log("지하철역 복귀 · 30분");SavePoint();return true;
        }
        public bool ReturnToShelter()
        {
            if(S.combat!=null||S.avatar==null||S.phase!=Phase.Running&&!(S.phase==Phase.Event&&IsTransit(S.pending)))return false;
            S.pending=null;S.phase=Phase.Running;S.exploration.destination=-1;S.exploration.rejected.Clear();S.exploration.resumeTransit=false;
            return RequestReturn("탐사를 마치고");
        }
        bool TemplateAffordable(ActivityEvent e)
        {
            var costs=B.eventCosts;EventCost cost=null;if(costs!=null)foreach(var c in costs)if(c.id==e.id){cost=c;break;}
            for(int i=0;i<e.options.Length;i++)
            {
                var o=e.options[i];double hours=cost!=null&&cost.hours!=null&&i<cost.hours.Length?cost.hours[i]:(o.defaultHours?B.november.defaultEventHours:o.hours);
                int food=cost!=null&&cost.food!=null&&i<cost.food.Length?cost.food[i]:o.food;
                int kits=cost!=null&&cost.kits!=null&&i<cost.kits.Length?cost.kits[i]:o.kits;
                hours=Rules.ActionHours(hours);
                if(hours>Rules.DayHours-S.hours||food>S.food||kits>S.medicine||o.syringes>S.syringes)continue;
                if(S.day==S.avatar.selectedDay&&(o.normal>0||o.special>0||o.boss))continue;
                return true;
            }
            return false;
        }
        bool PlotEligible(ActivityEvent e)
        {
            if(e.prerequisites!=null)foreach(string flag in e.prerequisites)if(!S.plot.completed.Contains(flag))return false;
            if(e.anyPrerequisites!=null&&e.anyPrerequisites.Length>0&&!e.anyPrerequisites.Any(f=>S.plot.completed.Contains(f)))return false;
            if(!e.repeat&&S.plot.completed.Contains(e.id))return false;
            if(!string.IsNullOrEmpty(e.rareNpcId)&&S.recruitedRare.Contains(e.rareNpcId))return false;
            if(e.options!=null&&e.options.Any(o=>!string.IsNullOrEmpty(o.uniqueNpcId)&&S.uniqueRecruits.Contains(o.uniqueNpcId)))return false;
            return true;
        }
        static void AddFlag(List<string> flags,string value){if(!string.IsNullOrEmpty(value)&&!flags.Contains(value))flags.Add(value);}
        void CompleteEventOption(ChoiceEvent e,int choice,bool won)
        {
            if(S.avatar==null||e.options==null||choice<0||choice>=e.options.Length)return;
            var o=e.options[choice];bool success=won;
            if(success&&!string.IsNullOrEmpty(o.chance))
            {
                double chance=o.chance=="recruit"?B.scoutSuccess+(S.avatar.Has("cleopatra")?B.cleopatraScout:0):o.chance=="food"?FoodDiscoveryChance:o.chance=="medical"?B.november.supplyDiscovery:B.divineSuccessChance;
                success=Rng.Next()<chance;
            }
            if(!success){Log("조건 미완료 · 사건 보상 없음");return;}
            if(o.injury&&Rng.Next()<B.november.injuryChance)
            {
                S.avatar.health=Rules.Clamp(S.avatar.health-(o.damage>0?o.damage:B.november.injuryHp),S.avatar.MaxHealth);
                Log("불안정한 경로 · 체력 손실");if(CheckDeath(false,null))return;
            }
            bool recruited=false;
            if(o.recruit&&S.candidates.Count+1<Rules.MaxNpc)
            {
                if(string.IsNullOrEmpty(o.uniqueNpcId)||!S.uniqueRecruits.Contains(o.uniqueNpcId))
                {
                    var npc=CreateCharacter(false);S.candidates.Add(npc);recruited=true;
                    AddFlag(S.uniqueRecruits,o.uniqueNpcId);Log(npc.name+" · 전투 가능한 생존자 합류");
                }
            }
            rewardRelicId=o.relicId;rewardRareId=o.rareNpcId;
            try
            {
                if(!string.IsNullOrEmpty(o.rareNpcId))recruited=RecruitRare(o.rareNpcId);
                if(!string.IsNullOrEmpty(o.relicId))AcquireRelic(o.relicId);
            }
            finally{rewardRelicId=null;rewardRareId=null;}
            if(o.discoverFood)GrantFoundFood();
            if(o.medicalLoot){if(Rng.Int(0,2)==0){S.medicine++;Log("응급 의료 키트 +1");}else{S.syringes++;Log("정신 회복 주사기 +1");}}
            if(o.divinity&&S.divineEventDay!=S.day)
            {
                int amount=Rng.Int(3,11);if(Reward(e.id+":divinity",amount)){S.divineEventDay=S.day;Log("신위 사건 성공 · +"+amount);}
            }
            bool recruitment=o.recruit||!string.IsNullOrEmpty(o.rareNpcId);
            if(!recruitment||recruited)
            {
                AddFlag(S.plot.completed,"branch:"+e.templateId+":"+choice);
                if(o.flags!=null)foreach(string flag in o.flags)AddFlag(S.plot.completed,flag);
                if(o.completes)AddFlag(S.plot.completed,e.templateId);
            }
            if(o.partialFlags!=null)foreach(string flag in o.partialFlags)AddFlag(S.plot.partial,flag);
            if(o.returnHome)S.returnRequested=true;
            if(!string.IsNullOrEmpty(o.outcome))Log(o.outcome);
        }
        bool Branch(string id,int choice){return S.plot.completed.Contains("branch:"+id+":"+choice);}
        public string PlotContext(ActivityEvent e)
        {
            if(e.id=="MUS-14"&&S.plot.partial.Contains("restorer_partial_note"))return "이전에 필사한 단서를 원본 기록과 대조합니다.";
            if(e.id=="HOM-03"&&Branch("HOM-02",1))return "관리실 장부에 표시한 통로를 따라 조사합니다.";
            if(e.id=="HOM-03"&&Branch("HOM-02",0))return "회색 문에서 찾은 의식의 흔적과 명부를 대조합니다.";
            if(e.id=="HOM-10"&&S.plot.completed.Contains("HOM-03"))return "명부에서 추적한 경호원의 무전 기록을 대조합니다.";
            if(e.id=="HOM-09"&&S.plot.completed.Contains("HOM-06"))return "셔터 너머의 발톱 흔적이 이곳으로 이어집니다.";
            if(e.id=="SUB-N04"&&Branch("SUB-N03",0))return "객차에서 발견한 관제실 열쇠를 사용합니다.";
            if(e.id=="SUB-N04"&&Branch("SUB-N03",1))return "객차 아래에서 찾은 우회 통로를 이용합니다.";
            if(e.id=="SUB-N08"&&S.recruitedRare.Contains("nerissa"))return "네리사의 경고가 이 수로의 흔적과 이어집니다.";
            if(e.id=="MUS-14"&&S.plot.completed.Contains("MUS-10"))return "복원사의 작업대에서 보았던 번호가 반복됩니다.";
            if(e.id=="SAN-13"&&S.plot.completed.Contains("SAN-03"))return "지하 예배실의 일곱 문과 같은 표식입니다.";
            if(e.area==Area.Residence&&Branch("HOM-04",0))return "이전에 보강한 옥상 지름길을 따라 도착했습니다.";
            if(S.plot.viewed.Contains(e.id))return "이전에 남긴 기록을 보존한 채 다시 조사합니다.";
            return "";
        }
        public double FoodDiscoveryChance { get { return S.exploration.current==Area.Mart?B.november.martFood:S.exploration.current==Area.Residence?B.november.residenceFood:B.november.otherFood; } }
    }
}
