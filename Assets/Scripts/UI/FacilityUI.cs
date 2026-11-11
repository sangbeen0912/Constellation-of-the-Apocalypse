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
            if(Game.IsTransit(e))return index==0?AreaRules.TravelHours[Game.S.exploration.destination]+"시간":"1시간";
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
            var row=Row(body,104);row.GetComponent<HorizontalLayoutGroup>().enabled=false;
            FixedArt(row,ArtCatalog.Relic(relic.id),new Vector2(.01f,.03f),new Vector2(.17f,.97f));
            Positioned(row,relic.name,new Vector2(.20f,.65f),new Vector2(.99f,1),26,gold);
            Positioned(row,"최대 체력 +"+relic.healthBonus+" · 공격력 +"+relic.attackBonus,new Vector2(.20f,.32f),new Vector2(.99f,.65f),22,ink);
            Positioned(row,relic.awaitingRestoration?"복원 대기":"전승 기록 "+relic.history.Count+"장",new Vector2(.20f,0),new Vector2(.99f,.32f),21,muted);
        }
        string ChapterText(RelicChapter h)
        {
            return h.name+" · "+h.days+"일 생존\n일반 좀비 "+h.normalKills+" / 특수 "+h.specialKills+" / 네임드 "+h.namedKills+" / 악마 "+h.demonKills+" 처치";
        }
        void RenderRestoration(Transform body)
        {
            var relic=Game.S.relics.FirstOrDefault(r=>r.id==restorationId);if(relic==null){restorationId=null;return;}
            body.GetComponent<VerticalLayoutGroup>().spacing=6;
            RelicCard(body,relic);body.GetChild(body.childCount-1).GetComponent<LayoutElement>().preferredHeight=82;var preview=Game.PreviewRestoration(relic.id);
            Label(body,relic.awaitingRestoration?"복원 비용 신위 100 · 보유 "+Game.S.divinity:"전승이 복원되었습니다.",24,34,gold);
            Label(body,"이번 성장 · 최대 체력 +"+preview.health+" / 공격력 +"+preview.attack+"\n복원 후 · 최대 체력 +"+(relic.healthBonus+preview.health)+" / 공격력 +"+(relic.attackBonus+preview.attack),24,64);
            var views=Row(body,52);
            MakeButton(views,"마지막 사용자의 기록",()=>{restorationStep=1;Render();});
            MakeButton(views,"이전 전승 기록",()=>{restorationStep=2;Render();});
            MakeButton(views,"이번 복원 결과",()=>{restorationStep=3;Render();});
            if(restorationStep==1)
            {var h=relic.history.LastOrDefault();Label(body,h==null?"아직 전승 기록이 없습니다.":ChapterText(h),22,74);}
            else if(restorationStep==2)
            {
                int count=Math.Max(0,relic.history.Count-1);historyChapter=Math.Max(0,Math.Min(historyChapter,Math.Max(0,count-1)));
                var history=Row(body,74);history.GetComponent<HorizontalLayoutGroup>().enabled=false;
                var words=Positioned(history,count==0?"이전 전승 기록이 없습니다.":ChapterText(relic.history[historyChapter]),Vector2.zero,new Vector2(count>1?.70f:1,1),22,ink);
                if(count>1){PlacedButton(history,"이전",new Vector2(.72f,.05f),new Vector2(.85f,.95f),()=>{historyChapter--;Render();},historyChapter>0);PlacedButton(history,"다음",new Vector2(.87f,.05f),new Vector2(1,.95f),()=>{historyChapter++;Render();},historyChapter<count-1);}
            }
            else Label(body,"다음 전승 이월 · 생존 "+preview.carryDays+"일 / 좀비 "+preview.carryZombies+"마리\n이력은 선택해서 읽을 수 있습니다.",22,74,muted);
            MakeButton(body,"복원 확정 · 신위 100",()=>Act(()=>{if(Game.RestoreRelic(relic.id))restorationStep=4;}),Game.S.phase==Phase.Morning&&Game.S.divinity>=Rules.RelicCost&&relic.awaitingRestoration,true).GetComponent<LayoutElement>().preferredHeight=58;
        }
    }
}

