using UnityEngine;
using UnityEngine.UI;
namespace Apocalypse
{
    public sealed partial class GameApp
    {
        PixelPlate Plate(RectTransform parent,Color color,bool button=false,bool selected=false)
        {
            var go=new GameObject("Stepped pixel plate",typeof(RectTransform),typeof(CanvasRenderer),typeof(LayoutElement),typeof(PixelPlate));
            go.transform.SetParent(parent,false);go.transform.SetAsFirstSibling();go.GetComponent<LayoutElement>().ignoreLayout=true;
            var rect=go.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var plate=go.GetComponent<PixelPlate>();plate.color=color;plate.button=button;plate.selected=selected;plate.raycastTarget=button;return plate;
        }
        void RenderDossier(Transform side)
        {
            var a=Game.S.avatar;
            Label(side,"화신체",18,26,muted);
            var row=Row(side,128);row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth=false;
            var portrait=new GameObject("Dossier portrait",typeof(RectTransform),typeof(LayoutElement));portrait.transform.SetParent(row,false);
            var le=portrait.GetComponent<LayoutElement>();le.preferredWidth=100;le.minWidth=100;le.flexibleWidth=0;
            Art(Stack(portrait.transform,0),Portrait(a),110,true);
            var identity=Stack(row,6);identity.gameObject.AddComponent<LayoutElement>().flexibleWidth=1;
            identity.GetComponent<VerticalLayoutGroup>().spacing=2;
            Label(identity,a.name,25,34,gold);Label(identity,Personality(a),19,58,muted);
            Label(identity,"공격력 "+a.Attack,21,28,ink);
            Meter(side,"체력",a.health,a.MaxHealth,new Color(.66f,.29f,.25f));
            Meter(side,"정신력",a.sanity,100,new Color(.39f,.57f,.63f));
            Meter(side,"허기",a.hunger,100,new Color(.65f,.58f,.33f));
            controlStatus=Label(side,Game.S.controlLocked?"개입권 잠김":"개입 가능",19,26,Game.S.controlLocked?new Color(.95f,.45f,.35f):muted);
            Label(side,"관측 메뉴",18,26,gold);
        }
        void RenderResourceHeader(Transform parent)
        {
            var s=Game.S;string[] text={"DAY  "+s.day.ToString("00"),"신위  "+s.divinity,"식량  "+s.food,"의료품  "+s.medicine};
            for(int i=0;i<4;i++)
            {
                float x=.42f+i*.139f;
                var tile=Box(parent,"Resource "+i,new Vector2(x,.926f),new Vector2(x+.132f,.981f),panel);
                Plate(tile,panel,false,i==0);
                resourceTexts[i]=Positioned(tile,text[i],new Vector2(.10f,.08f),new Vector2(.94f,.92f),24,i==0?gold:ink);
            }
        }
        Button PlacedButton(Transform parent,string label,Vector2 min,Vector2 max,System.Action action,bool enabled=true,bool accent=false)
        {
            var frame=ScreenArea(parent,"Action "+label,min,max);
            var button=MakeButton(frame,label,action,enabled,accent);var rect=button.GetComponent<RectTransform>();
            rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;return button;
        }
        void DistinctAction(Button button,Color fill,Color text,Color rim,bool tick=false,bool forward=false,bool back=false)
        {
            var plate=button.GetComponentInChildren<PixelPlate>();plate.color=fill;plate.rimColor=rim;plate.customRim=true;
            plate.selectionTick=tick;plate.forwardArrow=forward;plate.backArrow=back;plate.symbolColor=text;plate.SetVerticesDirty();
            var label=button.GetComponentInChildren<Text>();label.color=button.interactable?text:new Color(.62f,.66f,.69f);
            label.fontStyle=FontStyle.Bold;label.rectTransform.offsetMin=new Vector2(back?44:26,0);
            label.rectTransform.offsetMax=new Vector2(tick||forward?-40:-16,0);
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1,.98f,.90f);
            colors.pressedColor=new Color(.72f,.78f,.83f);colors.disabledColor=new Color(.30f,.34f,.38f,.75f);button.colors=colors;
        }
        System.Action BackNavigation(out string label)
        {
            label=null;
            if(Game==null||Game.S.phase!=Phase.Morning)return null;
            if(choosingExploration){label="활동 선택으로";return ()=>{choosingExploration=false;Render();};}
            if(tab=="성좌 도감"&&codex.SelectedId!=null){label="도감으로";return ()=>{codex.Back();Render();};}
            if(tab=="보급창고"&&historyId!=null){label="보관함 목록으로";return ()=>{historyId=null;Render();};}
            if(tab=="예배방"&&chapelSection=="성유물 복원"&&restorationId!=null){label="복원 목록으로";return ()=>{restorationId=null;restorationStep=0;Render();};}
            if(tab=="쉘터")return null;
            label="쉘터로";return ()=>Navigate("쉘터");
        }
        void RenderBackNavigation(Transform root)
        {
            var action=BackNavigation(out string label);if(action==null)return;
            var button=PlacedButton(root,label,new Vector2(.205f,.035f),new Vector2(.405f,.102f),action);
            button.name="Back navigation";
            DistinctAction(button,new Color(.17f,.28f,.37f),new Color(.96f,.98f,1),new Color(.59f,.78f,.90f),back:true);
            button.GetComponentInChildren<Text>().fontSize=24;
        }
    }
}
