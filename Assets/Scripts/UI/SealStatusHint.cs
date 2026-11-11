using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Apocalypse
{
    public sealed class SealStatusHint:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler
    {
        public string message;public Font font;GameObject tip;
        public void OnPointerEnter(PointerEventData data)
        {
            if(tip!=null)return;
            tip=new GameObject("Seal duration",typeof(RectTransform),typeof(Image));tip.transform.SetParent(GetComponentInParent<Canvas>().transform,false);
            var r=tip.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=Vector2.zero;r.pivot=new Vector2(0,0);r.sizeDelta=new Vector2(315,55);
            var canvas=(RectTransform)tip.transform.parent;RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,data.position,null,out var point);r.localPosition=point+new Vector2(0,30);
            var image=tip.GetComponent<Image>();image.color=new Color(.025f,.04f,.05f,.98f);image.raycastTarget=false;
            var text=new GameObject("Seal information",typeof(RectTransform),typeof(ReadableText));text.transform.SetParent(tip.transform,false);var rect=text.GetComponent<RectTransform>();rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(10,5);rect.offsetMax=new Vector2(-10,-5);
            var label=text.GetComponent<Text>();label.font=font;label.fontSize=22;label.text=message;label.color=Color.white;label.alignment=TextAnchor.MiddleLeft;label.raycastTarget=false;
        }
        public void OnPointerExit(PointerEventData data){Close();}
        void OnDisable(){Close();}
        void Close(){if(tip!=null)Destroy(tip);tip=null;}
    }
}
