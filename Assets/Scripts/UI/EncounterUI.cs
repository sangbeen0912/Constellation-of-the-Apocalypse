using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Apocalypse
{
    public sealed partial class GameApp
    {
        RectTransform fieldMain,fieldStage,fieldBackdrop,fieldPane,fieldButtons;
        Text fieldTime,fieldActivity;
        string fieldContentKey,fieldArtKey;
        readonly System.Collections.Generic.List<Button> encounterButtonBuffer=new System.Collections.Generic.List<Button>(32);
        Text lastFieldClock;
        double lastFieldHours=double.NaN;
        Area lastFieldArea;
        bool lastFieldCombat;
        void RefreshFieldTime(bool combat)
        {
            var s=Game.S;if(fieldTime==null)return;
            if(lastFieldClock==fieldTime&&lastFieldHours==s.hours&&lastFieldArea==s.exploration.current&&lastFieldCombat==combat)return;
            lastFieldClock=fieldTime;lastFieldHours=s.hours;lastFieldArea=s.exploration.current;lastFieldCombat=combat;
            double remaining=Math.Max(0,Rules.DayHours-s.hours);
            fieldTime.text=combat?Rules.Clock(s.hours)+"   남은 "+remaining.ToString("0.0")+"시간 · 전투":
                Rules.Clock(s.hours)+"   남은 "+(int)remaining+"시간 "+Math.Round(remaining%1*60).ToString("00")+"분   ·   "+AreaRules.Name(s.exploration.current);
        }
        bool IsFieldPhase { get { return Game!=null&&(Game.S.phase==Phase.Running||Game.S.phase==Phase.Event||Game.S.phase==Phase.Combat||liveBattle!=null); } }
        void RenderEncounter(Transform main)
        {
            fieldMain=(RectTransform)main;fieldContentKey=fieldArtKey=null;
            fieldTime=Positioned(main,"",new Vector2(.02f,.925f),new Vector2(.98f,.995f),31,gold);
            fieldStage=Box(main,"Location stage",new Vector2(.015f,.29f),new Vector2(.675f,.915f),panel);
            fieldStage.gameObject.AddComponent<RectMask2D>();
            fieldBackdrop=Box(fieldStage,"Location backdrop",Vector2.zero,Vector2.one,Color.clear);
            var logs=Box(main,"Day observation",new Vector2(.015f,.09f),new Vector2(.675f,.28f),panel);
            logBody=Scroller(logs);logRows.Clear();logSources.Clear();Label(logBody,"오늘의 기록",23,30,gold);
            fieldButtons=Box(main,"Field actions",new Vector2(.015f,.01f),new Vector2(.675f,.08f),panel);
            var buttons=Row(Stack(fieldButtons,3),48);
            MakeButton(buttons,"쉘터로 돌아가기 · 0시간",()=>Act(()=>Game.ReturnToShelter()));
            subwayReturnButton=MakeButton(buttons,"지하철역으로 돌아가기 · 30분",()=>Act(()=>Game.ReturnToSubway()));
            fieldPane=Box(main,"Event and command pane",new Vector2(.69f,.01f),new Vector2(.985f,.915f),panel);
            Canvas.ForceUpdateCanvases();RefreshEncounter();
        }
        void ClearChildren(Transform parent)
        {
            foreach(Transform child in parent){foreach(var image in child.GetComponentsInChildren<RawImage>(true))image.texture=null;child.gameObject.SetActive(false);Destroy(child.gameObject);}
        }
        void RefreshEncounter()
        {
            if(fieldMain==null)return;
            var s=Game.S;double remaining=Math.Max(0,Rules.DayHours-s.hours);
            RefreshFieldTime(false);
            RefreshRunningHud();
            bool canTravel=s.phase==Phase.Running&&!BattleVisible;
            encounterButtonBuffer.Clear();fieldButtons.GetComponentsInChildren(false,encounterButtonBuffer);
            foreach(var button in encounterButtonBuffer)button.interactable=canTravel;
            subwayReturnButton.gameObject.SetActive(s.exploration.current>=Area.Museum&&s.exploration.current<=Area.Heritage);
            subwayReturnButton.interactable=canTravel&&remaining>=.5;
            encounterButtonBuffer.Clear();canvas.GetComponentsInChildren(false,encounterButtonBuffer);
            foreach(var button in encounterButtonBuffer)
                if(!button.transform.IsChildOf(fieldMain))button.interactable=canTravel;
            string art=s.pending!=null&&!string.IsNullOrEmpty(s.pending.constellationId)?"star:"+s.pending.constellationId:
                (Game.S.combat!=null?Game.S.combat.scene:s.pending!=null?s.pending.scene:null)=="interior"?"interior":((int)s.exploration.current).ToString();
            if(art!=fieldArtKey)
            {
                ClearChildren(fieldBackdrop);fieldArtKey=art;
                if(art!="interior"&&battleInterior!=null){Resources.UnloadAsset(battleInterior);battleInterior=null;}
                if(art.StartsWith("star:")){var texture=constellationArt.Use(art.Substring(5));constellationArtUsed=texture!=null;FixedArt(fieldBackdrop,texture,Vector2.zero,Vector2.one);foreach(var fit in fieldBackdrop.GetComponentsInChildren<AspectRatioFitter>())fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;}
                else
                {
                    DetachIllustration();constellationArt.Dispose();
                    if(art=="interior"){if(battleInterior==null)battleInterior=Resources.Load<Texture2D>("Art/Environments/battle-interior-v1");FixedArt(fieldBackdrop,battleInterior,Vector2.zero,Vector2.one);}
                    else DrawAreaArt(fieldBackdrop,s.exploration.current,Vector2.zero,Vector2.one);
                }
            }
            if(BattleVisible){RenderBattleScene();RefreshLogRows();return;}
            string key=s.phase==Phase.Event?s.pending.id:"running";
            if(key!=fieldContentKey)
            {
                fieldContentKey=key;ClearChildren(fieldPane);
                if(s.phase==Phase.Event)RenderEncounterChoices();
                else
                {
                    var stack=Stack(fieldPane,18);Label(stack,"탐사 진행 중",29,55,gold);
                    fieldActivity=Label(stack,"화신체가 주변을 살피고 있습니다.",23,200,ink);
                    Label(stack,"사건이나 전투 명령이 생기면 이곳에서 선택할 수 있습니다.",21,120,muted);
                }
            }
            if(s.phase==Phase.Running&&fieldActivity!=null&&s.logs.Count>0)fieldActivity.text=s.logs[s.logs.Count-1];
            if(s.phase!=Phase.Running)visibleLogs=s.logs.Count;
            RefreshLogRows();
        }
        void RenderEncounterChoices()
        {
            var e=Game.S.pending;
            var textArea=Box(fieldPane,"Event narrative",new Vector2(.015f,.44f),new Vector2(.985f,.99f),Color.clear);
            var story=Scroller(textArea);Label(story,e.title,28,90,gold);
            if(!string.IsNullOrEmpty(e.context))Label(story,e.context,20,80,muted);
            var body=Label(story,e.body,22,0);body.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,Math.Max(100,fieldPane.rect.width-80));body.GetComponent<LayoutElement>().preferredHeight=Math.Max(180,body.preferredHeight+20);
            if(!string.IsNullOrEmpty(e.constellationId))
            {
                var c=content.Find(e.constellationId);Label(story,c.ability,24,65,gold);Label(story,c.description,21,180);Label(story,"소속  "+c.affiliation,20,60,muted);
            }
            var choices=Stack(Box(fieldPane,"Event choices",new Vector2(.015f,.01f),new Vector2(.985f,.43f),Color.clear),8);
            Button primary=null;
            for(int i=0;i<e.choices.Length;i++)
            {
                int choice=i;string id=e.id;bool possible=Game.CanChoose(e,i,out string reason);string cost=OptionCost(e,i);
                var button=MakeButton(choices,e.choices[i]+(cost.Length>0?" · "+cost:"")+(possible?"":"\n"+reason),()=>ResolveEventChoice(id,choice),possible&&!Game.S.controlLocked,true);
                button.GetComponent<LayoutElement>().preferredHeight=e.choices.Length==1?80:76;
                button.GetComponentInChildren<Text>().fontSize=20;
                if(primary==null&&button.interactable)primary=button;
            }
            RenderTransitExit(choices);
            if(primary!=null&&EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(primary.gameObject);
        }
    }
}
