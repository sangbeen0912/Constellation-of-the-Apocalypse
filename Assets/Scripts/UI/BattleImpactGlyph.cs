using UnityEngine;
using UnityEngine.UI;
namespace Apocalypse
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class BattleImpactGlyph : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            for(int i=0;i<8;i++)
            {
                float x=-52+i*14,y=-45+i*13;int n=helper.currentVertCount;
                helper.AddVert(new Vector3(x,y),new Color(1,.88f,.53f),Vector2.zero);
                helper.AddVert(new Vector3(x,y+17),Color.white,Vector2.zero);
                helper.AddVert(new Vector3(x+10,y+17),Color.white,Vector2.zero);
                helper.AddVert(new Vector3(x+10,y),new Color(1,.88f,.53f),Vector2.zero);
                helper.AddTriangle(n,n+1,n+2);helper.AddTriangle(n,n+2,n+3);
            }
        }
    }
}
