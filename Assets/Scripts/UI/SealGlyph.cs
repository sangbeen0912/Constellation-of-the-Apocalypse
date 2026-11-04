using UnityEngine;
using UnityEngine.UI;
namespace Apocalypse
{
    // A small native UI mark; no texture allocation or unused icon atlas.
    public sealed class SealGlyph : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var rect=GetPixelAdjustedRect();float cell=Mathf.Min(rect.width,rect.height)/16;
            var origin=rect.center-new Vector2(cell*8,cell*8);
            for(int y=0;y<16;y++)for(int x=0;x<16;x++)
            {
                float dx=Mathf.Abs(x-7.5f),dy=Mathf.Abs(y-7.5f),edge=Mathf.Max(dx,dy)+Mathf.Min(dx,dy)*.4f;
                bool ring=edge>=6.5f&&edge<7.7f;
                bool star=dx+dy<3.5f||dx<.6f&&dy<5.5f||dy<.6f&&dx<5.5f;
                if(!ring&&!star)continue;
                int first=mesh.currentVertCount;Vector2 corner=origin+new Vector2(x*cell,y*cell);
                mesh.AddVert(corner,color,Vector2.zero);mesh.AddVert(corner+new Vector2(cell,0),color,Vector2.zero);
                mesh.AddVert(corner+new Vector2(cell,cell),color,Vector2.zero);mesh.AddVert(corner+new Vector2(0,cell),color,Vector2.zero);
                mesh.AddTriangle(first,first+1,first+2);mesh.AddTriangle(first,first+2,first+3);
            }
        }
    }
}
