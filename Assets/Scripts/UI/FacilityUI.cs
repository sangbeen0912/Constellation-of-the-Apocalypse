using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace Apocalypse
{
    public sealed partial class GameApp
    {
        string restorationId,historyId;
        int restorationStep,historyChapter;
        static string SpeciesName(Species species){return new[]{"인간","생체 전투 기계","어인","반시체","수인"}[(int)species];}
        string Passive(Character c){return content.rareNpcs.FirstOrDefault(n=>n.species==c.species)?.passive??"";}
        string OptionCost(ChoiceEvent e,int index)
        {
            if(Game.IsTransit(e))return index==0?AreaRules.TravelHours[Game.S.exploration.destination]+"시간":"2시간";
            if(e.options==null||index>=e.options.Length)return "";
            var o=e.options[index];var parts=new System.Collections.Generic.List<string>();
            if(o.hours>0)parts.Add(o.hours+"시간");if(o.food>0)parts.Add("식량 "+o.food);
            if(o.kits>0)parts.Add("키트 "+o.kits);if(o.syringes>0)parts.Add("주사기 "+o.syringes);
            if(o.hunger>0)parts.Add("허기 -"+o.hunger);if(o.sanity>0)parts.Add("정신력 -"+o.sanity);
            return string.Join(" / ",parts);
        }
        void EventArtwork(Transform body,ChoiceEvent e)
        {
            var template=content.activityEvents.FirstOrDefault(t=>t.id==e.templateId);
            if(template!=null&&!string.IsNullOrEmpty(template.relicId)){Art(body,ArtCatalog.Relic(template.relicId),200,true);return;}
            if(template!=null&&!string.IsNullOrEmpty(template.rareNpcId))
            {
                var npc=content.rareNpcs.First(n=>n.id==template.rareNpcId);
                Art(body,ArtCatalog.Portrait(npc.name),200,true);
                Label(body,npc.name+" · "+SpeciesName(npc.species)+" · 체력 "+npc.health+" / 공격력 "+npc.attack,25,45,gold);
                Label(body,npc.passive,23,75);return;
            }
            var frame=ScreenArea(body,"Area illustration",Vector2.zero,new Vector2(1,.58f));frame.gameObject.AddComponent<LayoutElement>().preferredHeight=300;
            DrawAreaArt(frame,Game.S.exploration.current,Vector2.zero,Vector2.one);
        }
        void RelicCard(Transform body,Relic relic)
        {
            var row=Row(body,116);Art(row,ArtCatalog.Relic(relic.id),108,true);
            var detail=Stack(row,6);
            Label(detail,relic.name,28,40,gold);
            Label(detail,"최대 체력 +"+relic.healthBonus+"    공격력 +"+relic.attackBonus,23,36);
            Label(detail,relic.awaitingRestoration?"복원 대기":"전승 기록 "+relic.history.Count+"장",21,30,muted);
        }
        string ChapterText(RelicChapter h)
        {
            return h.name+" · "+h.days+"일 생존\n일반 좀비 "+h.normalKills+" / 특수 "+h.specialKills+" / 네임드 "+h.namedKills+" / 악마 "+h.demonKills+" 처치";
        }
        void RenderRestoration(Transform body)
        {
            var relic=Game.S.relics.FirstOrDefault(r=>r.id==restorationId);
            if(relic==null){restorationId=null;return;}
            RelicCard(body,relic);
            var preview=Game.PreviewRestoration(relic.id);
            if(restorationStep==0)
            {
                Label(body,"복원 비용  신위100    보유 "+Game.S.divinity,25,55);
                Label(body,"역대 사용자의 업적을 확인하고 이번 성장을 미리 볼 수 있습니다.",23,65,muted);
                MakeButton(body,"마지막 사용자의 기록",()=>{restorationStep=1;Render();});
            }
            else if(restorationStep==1)
            {
                var last=relic.history.LastOrDefault();
                if(last!=null){Label(body,"제"+relic.history.Count+"장 — "+last.name+"의 기록",30,48,gold);Label(body,ChapterText(last),24,80);}
                MakeButton(body,"이전 전승 기록",()=>{restorationStep=2;Render();});
            }
            else if(restorationStep==2)
            {
                int count=Math.Max(0,relic.history.Count-1);historyChapter=Math.Max(0,Math.Min(historyChapter,Math.Max(0,count-1)));
                if(count>0){Label(body,"제"+(historyChapter+1)+"장 — "+relic.history[historyChapter].name,27,44,gold);Label(body,ChapterText(relic.history[historyChapter]),23,80);
                    PageBar(body,historyChapter,count,p=>{historyChapter=p;Render();});}
                if(relic.history.Count<=1)Label(body,"첫 번째 전승입니다.",24,60,muted);
                MakeButton(body,"이번 복원 결과",()=>{restorationStep=3;Render();});
            }
            else if(restorationStep==3)
            {
                Label(body,"이번 성장  최대 체력 +"+preview.health+" / 공격력 +"+preview.attack,30,65,gold);
                Label(body,"복원 후  최대 체력 +"+(relic.healthBonus+preview.health)+" / 공격력 +"+(relic.attackBonus+preview.attack)+"\n다음 전승에 이월  생존 "+preview.carryDays+"일 / 좀비 "+preview.carryZombies+"마리",25,115);
                MakeButton(body,"복원 확정 · 신위 100",()=>Act(()=>{if(Game.RestoreRelic(relic.id))restorationStep=4;}),Game.S.phase==Phase.Morning&&Game.S.divinity>=Rules.RelicCost&&relic.awaitingRestoration);
            }
            else
            {
                Label(body,"전승이 복원되었습니다.",32,70,gold);
                Label(body,"누적 최대 체력 +"+relic.healthBonus+" / 공격력 +"+relic.attackBonus+"\n이월 생존 "+(relic.days-relic.paidDays)+"일 / 좀비 "+(relic.Zombies-relic.paidZombies)+"마리",25,110);
            }
            if(restorationStep<3)MakeButton(body,"연출 건너뛰고 복원 미리보기",()=>{restorationStep=3;Render();});
        }
    }
}

