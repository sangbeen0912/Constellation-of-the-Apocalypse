using UnityEngine;
using UnityEngine.UI;
namespace Apocalypse
{
    // Texture-free stepped metalwork, shared by panels and button states.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PixelPlate : MaskableGraphic
    {
        public bool button, selected;
        public bool customRim, selectionTick, forwardArrow, backArrow;
        public Color rimColor, symbolColor;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();Rect r=rectTransform.rect;
            if(r.width<12||r.height<12)return;
            Cut(mesh,r,new Color(.012f,.016f,.015f,color.a),6);
            r=Inset(r,2);
            Cut(mesh,r,customRim?rimColor:selected?new Color(.63f,.51f,.29f,color.a):new Color(.31f,.34f,.30f,color.a),5);
            r=Inset(r,2);Cut(mesh,r,color,4);
            Quad(mesh,r.xMin+5,r.yMax-3,r.width-10,2,new Color(.45f,.47f,.39f,.35f));
            Quad(mesh,r.xMin+5,r.yMin+1,r.width-10,2,new Color(.008f,.012f,.011f,.7f));
            if(button)
            {
                if(backArrow)
                {
                    for(int i=0;i<4;i++)Quad(mesh,r.xMin+8+i*3,r.center.y-2-i*3,3,4+i*6,symbolColor);
                    Quad(mesh,r.xMin+17,r.center.y-2,13,4,symbolColor);
                }
                else
                {
                    Quad(mesh,r.xMin+5,r.yMin+7,3,r.height-14,customRim?rimColor:selected?new Color(.90f,.76f,.45f):new Color(.38f,.44f,.40f));
                    if(selected&&!selectionTick&&!forwardArrow){Quad(mesh,r.xMin+12,r.center.y-2,4,4,new Color(.95f,.82f,.51f));}
                }
                if(selectionTick)
                {
                    float x=r.xMax-28,y=r.center.y-2;
                    Quad(mesh,x,y,4,4,symbolColor);Quad(mesh,x+4,y-4,4,4,symbolColor);
                    for(int i=0;i<4;i++)Quad(mesh,x+8+i*3,y-1+i*3,4,4,symbolColor);
                }
                if(forwardArrow)for(int i=0;i<4;i++)Quad(mesh,r.xMax-17-i*3,r.center.y-2-i*3,3,4+i*6,symbolColor);
            }
            else
            {
                for(int i=0;i<4;i++){float x=i%2==0?r.xMin+6:r.xMax-9,y=i/2==0?r.yMin+6:r.yMax-9;
                    Quad(mesh,x,y,3,3,new Color(.52f,.46f,.32f,.8f));}
            }
        }
        static Rect Inset(Rect r,float d){return new Rect(r.x+d,r.y+d,r.width-2*d,r.height-2*d);}
        static void Cut(VertexHelper mesh,Rect r,Color c,float cut)
        {
            int n=mesh.currentVertCount;mesh.AddVert(r.center,c,Vector2.zero);
            mesh.AddVert(new Vector2(r.xMin+cut,r.yMin),c,Vector2.zero);
            mesh.AddVert(new Vector2(r.xMax-cut,r.yMin),c,Vector2.zero);
            mesh.AddVert(new Vector2(r.xMax,r.yMin+cut),c,Vector2.zero);
            mesh.AddVert(new Vector2(r.xMax,r.yMax-cut),c,Vector2.zero);
            mesh.AddVert(new Vector2(r.xMax-cut,r.yMax),c,Vector2.zero);
            mesh.AddVert(new Vector2(r.xMin+cut,r.yMax),c,Vector2.zero);
            mesh.AddVert(new Vector2(r.xMin,r.yMax-cut),c,Vector2.zero);
            mesh.AddVert(new Vector2(r.xMin,r.yMin+cut),c,Vector2.zero);
            for(int i=0;i<8;i++)mesh.AddTriangle(n,n+1+i,n+1+(i+1)%8);
        }
        static void Quad(VertexHelper mesh,float x,float y,float w,float h,Color c)
        {
            int n=mesh.currentVertCount;mesh.AddVert(new Vector3(x,y),c,Vector2.zero);mesh.AddVert(new Vector3(x,y+h),c,Vector2.zero);
            mesh.AddVert(new Vector3(x+w,y+h),c,Vector2.zero);mesh.AddVert(new Vector3(x+w,y),c,Vector2.zero);
            mesh.AddTriangle(n,n+1,n+2);mesh.AddTriangle(n,n+2,n+3);
        }
    }
}
