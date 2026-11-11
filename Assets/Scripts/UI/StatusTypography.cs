using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Apocalypse
{
    // The existing caption remains the layout/state model; TextCore owns its glyph atlas.
    public sealed class StatusTypography:MonoBehaviour
    {
        public Text Model{get;private set;}public TextMeshProUGUI View{get;private set;}
        string shown;Color tint;int size;FontStyle style;TextAnchor anchor;HorizontalWrapMode wrap;
        public void Initialize(ReadableText model,TMP_FontAsset font)
        {
            Model=model;var go=new GameObject("Status glyphs",typeof(RectTransform));go.SetActive(false);go.transform.SetParent(transform,false);
            var rect=go.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            View=go.AddComponent<TextMeshProUGUI>();View.font=font;View.raycastTarget=false;View.enableAutoSizing=false;
            View.overflowMode=TextOverflowModes.Overflow;View.richText=false;Sync(true);go.SetActive(true);
        }
        void LateUpdate(){Sync(false);}
        public void Sync(bool force=false)
        {
            if(Model==null||View==null)return;
            if(!force&&shown==Model.text&&tint==Model.color&&size==Model.fontSize&&style==Model.fontStyle&&anchor==Model.alignment&&wrap==Model.horizontalOverflow)return;
            shown=Model.text;tint=Model.color;size=Model.fontSize;style=Model.fontStyle;anchor=Model.alignment;wrap=Model.horizontalOverflow;
            View.text=shown;View.color=tint;View.fontSize=size;View.fontStyle=style==FontStyle.Bold?FontStyles.Bold:FontStyles.Normal;
            View.alignment=anchor==TextAnchor.MiddleCenter?TextAlignmentOptions.Midline:TextAlignmentOptions.MidlineLeft;
            View.textWrappingMode=wrap==HorizontalWrapMode.Overflow?TextWrappingModes.NoWrap:TextWrappingModes.Normal;
        }
    }
}
