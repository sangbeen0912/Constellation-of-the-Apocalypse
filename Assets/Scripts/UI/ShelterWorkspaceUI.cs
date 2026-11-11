using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace Apocalypse
{
    public sealed partial class GameApp
    {
        string shelterFacility="의무실";
        int upgradeQuantity=1;
        RectTransform WorkPanel(Transform parent,string name,Vector2 min,Vector2 max)
        {return Box(parent,name,min,max,new Color(.095f,.115f,.16f));}
        Button WorkspaceTab(Transform parent,string label,bool selected,Action action)
        {
            var b=MakeButton(parent,label,action,true,selected);
            b.GetComponent<LayoutElement>().preferredHeight=62;
            return b;
        }
        void RenderShelterWorkspace(Transform body)
        {
            var s=Game.S;
            if(Array.IndexOf(ArtCatalog.FacilityNames,tab)>=0)shelterFacility=tab;
            bool clinic=shelterFacility=="의무실"&&s.phase!=Phase.Report;
            Icon(body,6,new Vector2(.025f,.925f),new Vector2(.055f,.985f),ink);
            Positioned(body,s.phase==Phase.Report?"쉘터 · 귀환":"쉘터 · 오늘의 정비",new Vector2(.065f,.925f),new Vector2(.695f,.995f),32,ink).fontStyle=FontStyle.Bold;
            FixedArt(body,Facility(Array.IndexOf(ArtCatalog.FacilityNames,shelterFacility)),clinic?new Vector2(.025f,.73f):new Vector2(.575f,.925f),clinic?new Vector2(.69f,.92f):new Vector2(.69f,.985f));
            var tabs=Row(Box(body,"Facility tabs",new Vector2(.025f,.635f),new Vector2(.69f,.715f),Color.clear),62);
            foreach(string facility in ArtCatalog.FacilityNames)
            {
                string selected=facility;var b=WorkspaceTab(tabs,facility,shelterFacility==facility,()=>{shelterFacility=selected;tab=selected;Render();});
                b.name="Facility tab "+facility;b.interactable=s.phase==Phase.Morning;
            }
            // Only the clinic uses the banner layout. Other facilities keep room for their own controls.
            if(!clinic)
            {
                var tabRect=(RectTransform)tabs.parent;tabRect.anchorMin=new Vector2(.025f,.825f);tabRect.anchorMax=new Vector2(.69f,.91f);
            }
            var contentArea=ScreenArea(body,"Shelter work area",new Vector2(.025f,.025f),new Vector2(.69f,clinic?.62f:.80f));
            if(s.phase==Phase.Report)
            {
                Label(Stack(contentArea,25),"오늘의 귀환 기록",30,65,gold);Label(Stack(ScreenArea(contentArea,"Report text",new Vector2(0,.16f),new Vector2(1,.82f)),25),s.report,26,330);
            }
            else if(shelterFacility=="의무실")RenderClinicScreen(contentArea);
            else if(shelterFacility=="예배방")RenderChapelScreen(contentArea);
            else if(shelterFacility=="보급창고")RenderWarehouseScreen(contentArea);
            else RenderPersonnelScreen(contentArea,false);
            var departure=WorkPanel(body,"Departure workspace",new Vector2(.715f,.02f),new Vector2(.985f,.985f));
            RenderDepartureWorkspace(departure);
        }
        Text sealCountText,enemyScalingText,riftProgressText;
        readonly SealGlyph[] sealMarks=new SealGlyph[7];readonly SealStatusHint[] sealHints=new SealStatusHint[7];readonly int[] sealDays=new int[7];
        int shownSeals=-1,shownRift=-2,shownProgress=-1;
        void RenderSealSummary(Transform side)
        {
            sealCountText=Label(side,"",21,28,gold);
            var marks=Row(side,28);marks.GetComponent<HorizontalLayoutGroup>().spacing=8;
            for(int i=0;i<Game.S.demons.Count;i++)
            {
                var go=new GameObject("Seal "+Game.S.demons[i].sin,typeof(RectTransform),typeof(CanvasRenderer),typeof(SealGlyph));go.transform.SetParent(marks,false);
                sealMarks[i]=go.GetComponent<SealGlyph>();sealHints[i]=go.AddComponent<SealStatusHint>();sealHints[i].font=font;sealDays[i]=-1;
            }
            enemyScalingText=Label(side,"",18,26,muted);riftProgressText=Label(side,"",20,28,gold);
            shownSeals=-1;shownRift=-2;shownProgress=-1;RefreshSealSummary();
        }
        void RefreshSealSummary()
        {
            if(sealCountText==null)return;var s=Game.S;int count=Game.ActiveSealCount;
            if(shownSeals!=count)
            {
                shownSeals=count;sealCountText.text="악마 봉인 "+count+" / 7";
                enemyScalingText.text="체력 "+Rules.Round(Game.EnemyHealthMultiplier*100)+"% · 공격력 "+Rules.Round(Game.EnemyAttackMultiplier*100)+"%";
            }
            for(int i=0;i<s.demons.Count;i++)if(sealMarks[i]!=null&&sealDays[i]!=s.demons[i].remaining)
            {
                var d=s.demons[i];sealDays[i]=d.remaining;sealMarks[i].color=d.remaining>0?gold:new Color(.25f,.30f,.32f);
                sealHints[i].message=Rules.Sins[(int)d.sin]+(d.remaining>0?" · 봉인 "+d.remaining+"일 남음":" · 봉인되지 않음");
            }
            int progress=s.activeRift<0?0:s.demons[s.activeRift].progress;
            if(shownRift!=s.activeRift||shownProgress!=progress){shownRift=s.activeRift;shownProgress=progress;riftProgressText.text="봉쇄 돌파 "+progress+" / "+Rules.RiftProgressRequired;}
        }
        void RenderDepartureWorkspace(Transform parent)
        {
            var s=Game.S;bool morning=s.phase==Phase.Morning;
            Positioned(parent,s.phase==Phase.Report?"정비를 마쳤습니다":"오늘의 활동",new Vector2(.06f,.88f),new Vector2(.95f,.97f),29,ink);
            DrawAreaArt(parent,goal==Goal.Rift?Area.Rift:s.exploration.selected,new Vector2(.06f,.64f),new Vector2(.94f,.86f));
            if(s.phase==Phase.Report)
            {
                Positioned(parent,"쉘터에서 다시 준비한 뒤\n다음 날의 탐사를 시작합니다.",new Vector2(.06f,.40f),new Vector2(.94f,.60f),24,muted);
                PlacedButton(parent,"다음 날 아침",new Vector2(.06f,.08f),new Vector2(.94f,.19f),()=>Act(()=>Game.NextDay()),true,true);return;
            }
            var choices=Row(Box(parent,"Activity selection",new Vector2(.06f,.53f),new Vector2(.94f,.62f),Color.clear),62);
            foreach(Goal option in new[]{Goal.Explore,Goal.Rift})
            {
                Goal selected=option;
                var b=MakeButton(choices,option==Goal.Explore?"탐사":"공략",()=>{goal=selected;choosingExploration=selected==Goal.Explore;Render();},morning&&(option==Goal.Explore||s.activeRift>=0&&s.day>s.avatar.selectedDay),goal==option);
                if(goal==option)DistinctAction(b,new Color(.24f,.39f,.55f),ink,new Color(.64f,.79f,.90f),tick:true);
            }
            if(goal==Goal.Explore&&choosingExploration)
            {
                var destinations=Stack(Box(parent,"Inline destinations",new Vector2(.06f,.27f),new Vector2(.94f,.51f),Color.clear),0);
                foreach(Area area in new[]{Area.Residence,Area.Subway})
                {Area selected=area;WorkspaceTab(destinations,AreaRules.Name(area),s.exploration.selected==area,()=>Act(()=>Game.SelectExplorationArea(selected)));}
            }
            else
            {
                string text=goal==Goal.Rift?(s.activeRift<0?"열린 균열 없음":Rules.Sins[s.activeRift]+" 군단\n봉쇄 돌파 "+s.demons[s.activeRift].progress+" / "+Rules.RiftProgressRequired):"탐사를 누르면\n출발 장소를 고를 수 있습니다.";
                Positioned(parent,text,new Vector2(.06f,.29f),new Vector2(.94f,.50f),24,muted);
            }
            bool ready=morning&&(goal==Goal.Rift||choosingExploration)&&s.hours<Rules.DayHours;
            var start=PlacedButton(parent,goal==Goal.Rift?"공략 출발":"탐사 출발",new Vector2(.06f,.14f),new Vector2(.94f,.25f),Depart,ready,true);
            DistinctAction(start,new Color(.88f,.70f,.38f),new Color(.065f,.05f,.025f),new Color(1,.84f,.54f),forward:true);
            int waiting=s.relics.Count(r=>r.awaitingRestoration);
            if(waiting>0)PlacedButton(parent,"복원 대기 · "+waiting+"개",new Vector2(.06f,.025f),new Vector2(.94f,.11f),()=>{shelterFacility="예배방";tab="예배방";chapelSection="성유물 복원";restorationId=null;Render();},morning);
            else Positioned(parent,"남은 "+Math.Max(0,Rules.DayHours-s.hours).ToString("0.0")+"시간",new Vector2(.06f,.025f),new Vector2(.94f,.11f),23,muted);
        }
    }
}

