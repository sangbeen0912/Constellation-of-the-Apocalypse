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
        readonly Text[] resourceTexts=new Text[4];
        sealed class HudMeter{public string name;public Text text;public RectTransform fill;}
        readonly System.Collections.Generic.List<HudMeter> hudMeters=new System.Collections.Generic.List<HudMeter>(3);
        void RefreshRunningHud()
        {
            var s=Game.S;var a=s.avatar;if(a==null)return;
            if(resourceTexts[1]!=null)resourceTexts[1].text="신위  "+s.divinity;
            if(resourceTexts[2]!=null)resourceTexts[2].text="식량  "+s.food;
            if(resourceTexts[3]!=null)resourceTexts[3].text="의료품  "+s.medicine;
            foreach(var m in hudMeters){double value=m.name=="체력"?a.health:m.name=="정신력"?a.sanity:a.hunger,max=m.name=="체력"?a.MaxHealth:100;m.text.text=m.name+"  "+Rules.Round(value)+" / "+Rules.Round(max);m.fill.anchorMax=new Vector2((float)(value/max),1);}
            if(timeStatus!=null)timeStatus.text=s.hours.ToString("0.0")+" / 24시간";
            if(controlStatus!=null){controlStatus.text=s.controlLocked?"개입권 잠김":"개입 가능";controlStatus.color=s.controlLocked?new Color(.95f,.45f,.35f):muted;}
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
            var row=Row(body,46);
            MakeButton(row,"쉘터로 돌아가기 · 0시간",()=>Act(()=>Game.ReturnToShelter()));
            if(Game.S.exploration.current>=Area.Museum&&Game.S.exploration.current<=Area.Heritage)
                subwayReturnButton=MakeButton(row,"지하철역으로 돌아가기 · 1시간",()=>Act(()=>Game.ReturnToSubway()),24-Game.S.hours>=1);
        }
        void RefreshExplorationStatus()
        {
            if(subwayReturnButton!=null)subwayReturnButton.interactable=24-Game.S.hours>=1;
            if(explorationStatus!=null)explorationStatus.text=AreaRules.Name(Game.S.exploration.current)+"  ·  남은 "+(24-Game.S.hours).ToString("0.0")+"시간  ·  사건 "+Game.S.eventSlots+" / "+Game.S.eventTarget;
        }
        void RenderTransitExit(Transform body)
        {
            if(Game.IsTransit(Game.S.pending))PlacedButton(body,"쉘터로 돌아가기 · 0시간",new Vector2(.035f,.035f),new Vector2(.435f,.105f),()=>Act(()=>Game.ReturnToShelter()),true,true);
        }
    }
}
