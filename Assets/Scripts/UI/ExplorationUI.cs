using System;
using UnityEngine;
using UnityEngine.UI;
namespace Apocalypse
{
    public sealed partial class GameApp
    {
        static readonly string[] PersonalityNames={"용감함","신중함","냉철함","고집스러움","호전적","겁이 많음"};
        Texture2D areaAtlas;
        Text explorationStatus,timeStatus,controlStatus;
        Button subwayReturnButton;
        bool choosingExploration,transitWalking;
        float walkSeconds;
        RectTransform walkPortrait;
        RectTransform transitLayer;
        Text walkCaption;
        string walkText,walkArrival;
        public bool TransitWalkVisible { get { return transitWalking; } }
        void Depart(){Act(()=>{choosingExploration=false;tab="하루 기록";Game.StartDay(goal);});}
        void RenderExplorationPicker(Transform body)
        {
            Positioned(body,"탐사 목적지",new Vector2(.025f,.88f),new Vector2(.975f,.98f),34,gold);
            Positioned(body,"오늘 탐사를 시작할 장소를 선택하세요.",new Vector2(.025f,.80f),new Vector2(.975f,.88f),24,muted);
            for(int i=0;i<2;i++)
            {
                Area area=i==0?Area.Residence:Area.Subway;float x=.025f+i*.485f;
                var card=Box(body,"Exploration destination "+area,new Vector2(x,.20f),new Vector2(x+.46f,.78f),panel);Plate(card,panel);
                DrawAreaArt(card,area,new Vector2(.02f,.40f),new Vector2(.98f,.98f));
                Positioned(card,AreaRules.Name(area),new Vector2(.04f,.27f),new Vector2(.96f,.39f),29,gold);
                Positioned(card,area==Area.Residence?"주변 주택에서 식량과 생존 흔적을 찾습니다.":"폐선과 연결 통로를 걸어 다른 지역의 길을 찾습니다.",new Vector2(.04f,.04f),new Vector2(.96f,.27f),23,ink);
                PlacedButton(body,AreaRules.Name(area),new Vector2(x,.08f),new Vector2(x+.46f,.18f),()=>Act(()=>Game.SelectExplorationArea(area)),true,Game.S.exploration.selected==area);
            }
            // The final action stays in the same lower-right position as preparation.
            PlacedButton(body,"탐사 출발",new Vector2(.72f,.005f),new Vector2(.97f,.07f),Depart,true,true);
        }
        void ResolveEventChoice(string id,int choice)
        {
            Act(()=>
            {
                bool transit=Game.IsTransit(Game.S.pending);
                if(!Game.Resolve(id,choice)||!transit||Game.S.avatar==null||Game.S.phase==Phase.Death||Game.S.phase==Phase.Report)return;
                transitWalking=true;walkSeconds=0;
                string[] lines={"무너진 승강장을 지나 정비 통로로 들어섰다.","물에 잠긴 선로를 피해 좁은 보행로를 따라 걸었다.","낡은 노선도를 비추며 다른 연결 통로를 찾았다."};
                walkText=choice==1?lines[(Game.S.day+Game.S.exploration.rejected.Count)%lines.Length]:"발견한 표지를 따라 폐선의 연결 통로를 걸었다.";
                walkArrival=Game.IsTransit(Game.S.pending)?Game.S.pending.title:Game.S.phase==Phase.Event?"통로에서 새로운 흔적을 발견했다.":AreaRules.Name(Game.S.exploration.current)+"에 도착했다.";
            });
        }
        void FinishTransitWalk()
        {
            if(!transitWalking)return;
            if(transitLayer!=null){transitLayer.gameObject.SetActive(false);Destroy(transitLayer.gameObject);transitLayer=null;}
            transitWalking=false;walkPortrait=null;walkCaption=null;clock=0;fieldContentKey=null;Render();
        }
        bool UpdateTransitWalk()
        {
            if(!transitWalking)return false;
            walkSeconds+=Time.unscaledDeltaTime;
            if(walkPortrait!=null){walkPortrait.anchoredPosition=new Vector2(Mathf.Lerp(12,-12,Mathf.Clamp01(walkSeconds/1.6f)),Mathf.Sin(walkSeconds*12)*3);}
            if(walkCaption!=null)walkCaption.text=walkSeconds<1.6f?walkText:walkArrival;
            if(walkSeconds>=2.4f||Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.Space))FinishTransitWalk();
            return true;
        }
        void RenderTransitWalk(Transform root)
        {
            if(!transitWalking||transitLayer!=null)return;
            foreach(var button in canvas.GetComponentsInChildren<Button>())button.interactable=false;
            var shield=Box(fieldStage!=null?fieldStage:root,"Transit walking",Vector2.zero,Vector2.one,panel);
            transitLayer=shield;
            var stage=Box(shield,"Walking passage",Vector2.zero,Vector2.one,panel);
            DrawAreaArt(stage,Area.Subway,Vector2.zero,Vector2.one);
            var shade=Box(stage,"Walking caption",new Vector2(.025f,.025f),new Vector2(.975f,.40f),new Color(.02f,.03f,.04f,.95f));
            Positioned(shade,"도보 탐색",new Vector2(.025f,.64f),new Vector2(.97f,.95f),26,gold);
            walkCaption=Positioned(shade,walkSeconds<1.6f?walkText:walkArrival,new Vector2(.025f,.30f),new Vector2(.97f,.65f),24,ink);
            Positioned(shade,"폐선 연결 통로를 따라 이동합니다.",new Vector2(.025f,.015f),new Vector2(.70f,.22f),19,muted);
            walkPortrait=stage.GetComponentInChildren<RawImage>().rectTransform;walkPortrait.localScale=Vector3.one*1.06f;
            var skip=PlacedButton(shade,"넘기기",new Vector2(.72f,.025f),new Vector2(.98f,.30f),FinishTransitWalk,true,true);
            if(UnityEngine.EventSystems.EventSystem.current!=null)UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(skip.gameObject);
        }
        readonly Text[] resourceTexts=new Text[4];
        sealed class HudMeter{public string name;public Text text;public RectTransform fill;public double value=double.NaN,max=double.NaN;}
        readonly Text[] lastResourceLabels=new Text[4];
        readonly int[] lastResourceValues=new int[4];
        Text lastHudClock,lastHudControl;
        double lastHudHours=double.NaN;
        bool lastHudLock;
        void RefreshResource(int index,string caption,int value)
        {
            var label=resourceTexts[index];if(label==null)return;
            if(lastResourceLabels[index]==label&&lastResourceValues[index]==value)return;
            lastResourceLabels[index]=label;lastResourceValues[index]=value;label.text=caption+value;
        }
        readonly System.Collections.Generic.List<HudMeter> hudMeters=new System.Collections.Generic.List<HudMeter>(3);
        void RefreshRunningHud()
        {
            var s=Game.S;var a=s.avatar;if(a==null)return;
            RefreshSealSummary();
            RefreshResource(1,"신위  ",s.divinity);
            RefreshResource(2,"식량  ",s.food);
            RefreshResource(3,"의료품  ",s.medicine);
            foreach(var m in hudMeters){double value=m.name=="체력"?a.health:m.name=="정신력"?a.sanity:a.hunger,max=m.name=="체력"?a.MaxHealth:100;if(m.value==value&&m.max==max)continue;m.value=value;m.max=max;m.text.text=m.name+"  "+Rules.Round(value)+" / "+Rules.Round(max);m.fill.anchorMax=new Vector2((float)(value/max),1);}
            if(timeStatus!=null&&(lastHudClock!=timeStatus||lastHudHours!=s.hours)){lastHudClock=timeStatus;lastHudHours=s.hours;timeStatus.text=Rules.Clock(s.hours)+" · 남은 "+Math.Max(0,Rules.DayHours-s.hours).ToString("0.0")+"시간";}
            if(controlStatus!=null&&(lastHudControl!=controlStatus||lastHudLock!=s.controlLocked)){lastHudControl=controlStatus;lastHudLock=s.controlLocked;controlStatus.text=s.controlLocked?"개입권 잠김":"개입 가능";controlStatus.color=s.controlLocked?new Color(.95f,.45f,.35f):muted;}
        }
        void DrawAreaArt(Transform parent,Area area,Vector2 min,Vector2 max)
        {
            int index=area==Area.Subway?0:area==Area.Museum?1:area==Area.Mart?2:area==Area.Military?3:area==Area.ThemePark?4:area==Area.Heritage?5:-1;
            if(index<0){FixedArt(parent,area==Area.Rift?gate:area==Area.Sanctuary?Facility(1):city,min,max);return;}
            if(areaAtlas==null)areaAtlas=Resources.Load<Texture2D>("Art/Environments/november-areas-v1");
            var frame=Box(parent,"Area art "+area,min,max,Color.clear);frame.gameObject.AddComponent<RectMask2D>();
            var go=new GameObject("Area artwork",typeof(RectTransform),typeof(RawImage),typeof(AspectRatioFitter));go.transform.SetParent(frame,false);
            var image=go.GetComponent<RawImage>();image.texture=areaAtlas;image.raycastTarget=false;image.uvRect=new Rect(index%3/3f,1-(index/3+1)/2f,1/3f,.5f);
            var fit=go.GetComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;fit.aspectRatio=areaAtlas==null?16f/9:areaAtlas.width*2f/(areaAtlas.height*3);
        }
        void RenderExplorationControls(Transform body)
        {
            explorationStatus=Label(body,"",25,44,gold);RefreshExplorationStatus();
            if(Game.S.phase!=Phase.Running)return;
        }
        void RenderFieldActions(Transform body)
        {
            var row=Box(body,"Field actions",new Vector2(.01f,.005f),new Vector2(.99f,.105f),panel);
            var controls=Stack(row,8);var buttons=Row(controls,56);
            MakeButton(buttons,"쉘터로 돌아가기 · 0시간",()=>Act(()=>Game.ReturnToShelter()));
            if(Game.S.exploration.current>=Area.Museum&&Game.S.exploration.current<=Area.Heritage)
                subwayReturnButton=MakeButton(buttons,"지하철역으로 돌아가기 · 30분",()=>Act(()=>Game.ReturnToSubway()),Rules.DayHours-Game.S.hours>=.5);
        }
        void RefreshExplorationStatus()
        {
            if(subwayReturnButton!=null)subwayReturnButton.interactable=Rules.DayHours-Game.S.hours>=.5;
            if(explorationStatus!=null)explorationStatus.text=AreaRules.Name(Game.S.exploration.current)+"  ·  남은 "+(Rules.DayHours-Game.S.hours).ToString("0.0")+"시간  ·  사건 "+Game.S.eventSlots+" / "+Game.S.eventTarget;
        }
        void RenderTransitExit(Transform body)
        {
            if(Game.IsTransit(Game.S.pending))MakeButton(body,"쉘터로 돌아가기 · 0시간",()=>Act(()=>Game.ReturnToShelter()),true,true).GetComponent<LayoutElement>().preferredHeight=58;
        }
    }
}
