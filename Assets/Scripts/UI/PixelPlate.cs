using UnityEngine;
using UnityEngine.UI;
namespace Apocalypse
{
    // Texture-free stepped metalwork, shared by panels and button states.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PixelPlate : MaskableGraphic
    {
        public bool button, selected;
        public bool customRim, selectionTick, forwardArrow, backArrow, iconOnly;
        public int icon;
        public Color rimColor, symbolColor;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();Rect r=rectTransform.rect;if(r.width<4||r.height<4)return;
            if(iconOnly){DrawIcon(mesh,icon,r.center.x-12,r.center.y-12,symbolColor);return;}
            // A shared small mesh supplies the approved stepped iron frame without a texture atlas.
            Color edge=customRim?rimColor:selected?new Color(.65f,.78f,.88f):new Color(.39f,.44f,.53f);
            Cut(mesh,r,new Color(.018f,.023f,.035f,color.a),8);
            Cut(mesh,Inset(r,3),edge,5);
            Cut(mesh,Inset(r,6),color,3);
            Quad(mesh,r.xMin+9,r.yMax-6,r.width-18,3,Color.Lerp(edge,Color.white,.24f));
            Quad(mesh,r.xMin+9,r.yMin+3,r.width-18,3,new Color(.025f,.033f,.05f,color.a));
            if(!button){
                for(int i=0;i<2;i++){float x=i==0?r.xMin+5:r.xMax-12;Quad(mesh,x,r.yMax-10,7,3,edge);Quad(mesh,x,r.yMax-14,3,7,edge);}
                return;
            }
            if(selected)Quad(mesh,r.xMin+4,r.yMin+8,3,r.height-16,new Color(.76f,.87f,.95f));
            if(icon>0)DrawIcon(mesh,icon,r.xMin+16,r.center.y-12,symbolColor.a>0?symbolColor:new Color(.91f,.91f,.86f));
            if(backArrow){Quad(mesh,r.xMin+11,r.center.y-2,18,4,symbolColor);for(int i=0;i<3;i++)Quad(mesh,r.xMin+9+i*3,r.center.y-2-i*3,3,4+i*6,symbolColor);}
            if(selectionTick){float x=r.xMax-28,y=r.center.y;Quad(mesh,x,y,4,4,symbolColor);Quad(mesh,x+4,y-4,4,4,symbolColor);for(int i=0;i<3;i++)Quad(mesh,x+8+i*3,y-1+i*3,4,4,symbolColor);}
            if(forwardArrow)for(int i=0;i<3;i++)Quad(mesh,r.xMax-16-i*3,r.center.y-2-i*3,3,4+i*6,symbolColor);
        }
        public static int IconFor(string text)
        {
            if(text=="쉘터")return 6;if(text=="성좌 도감")return 1;if(text=="기록 보관소")return 8;
            if(text=="의무실"||text.StartsWith("체력 회복"))return 2;if(text=="예배방")return 1;
            if(text=="보급창고")return 4;if(text=="인재관리실")return 5;
            if(text.StartsWith("정신력 회복"))return 3;if(text.StartsWith("잠자기"))return 7;
            if(text=="공략")return 9;return 0;
        }
        static readonly string[] Icons={"",
            "00011000"+"00011000"+"00111100"+"11111111"+"11111111"+"00111100"+"00011000"+"00011000",
            "00111100"+"00111100"+"11111111"+"11111111"+"11111111"+"11111111"+"00111100"+"00111100",
            "00001110"+"00000110"+"00011100"+"00111000"+"01110000"+"11100000"+"11000000"+"10000000",
            "00111100"+"01100110"+"11111111"+"10011001"+"10011001"+"10011001"+"10011001"+"11111111",
            "01100110"+"11111111"+"01100110"+"00000000"+"01100110"+"11111111"+"11111111"+"11111111",
            "00011000"+"00111100"+"01111110"+"11111111"+"11011011"+"11011011"+"11000011"+"11000011",
            "10000000"+"10000000"+"10011110"+"11111111"+"11111111"+"11111111"+"10000001"+"10000001",
            "01111110"+"11000011"+"11011011"+"11000011"+"11011011"+"11011011"+"11000011"+"01111110",
            "10000001"+"11000011"+"01100110"+"00111100"+"00111100"+"01100110"+"11000011"+"10000001"};
        static void DrawIcon(VertexHelper mesh,int kind,float x,float y,Color tint)
        {
            if(kind<1||kind>=Icons.Length)return;string pixels=Icons[kind];
            for(int i=0;i<pixels.Length;i++)if(pixels[i]=='1')Quad(mesh,x+i%8*3,y+(7-i/8)*3,3,3,tint);
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
