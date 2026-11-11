using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace Apocalypse
{
    public sealed partial class GameApp
    {
        void RenderPersonnelRows(Transform body)
        {
            var people=new[]{Game.S.avatar}.Concat(Game.S.candidates).Where(c=>c!=null).ToList();
            personnelPage=Math.Max(0,Math.Min(personnelPage,Math.Max(0,(people.Count-1)/4)));
            for(int i=0;i<4;i++)
            {
                var c=people.Skip(personnelPage*4+i).FirstOrDefault();if(c==null)break;float y=.76f-i*.215f;
                var row=WorkPanel(body,"Personnel "+c.id,new Vector2(.005f,y),new Vector2(.995f,y+.20f));
                var icon=ScreenArea(row,"Personnel face",new Vector2(.01f,.04f),new Vector2(.14f,.96f));Art(Stack(icon,0),Portrait(c),105,true);
                Positioned(row,c.name+(c==Game.S.avatar?" · 화신체":""),new Vector2(.16f,.59f),new Vector2(.99f,.96f),25,gold);
                Positioned(row,Personality(c),new Vector2(.16f,.08f),new Vector2(.37f,.57f),21,muted);
                Positioned(row,"체력 "+Rules.Round(c.health)+"/"+c.MaxHealth+" · 공격력 "+c.Attack+"\n정신력 "+Rules.Round(c.sanity)+" · 허기 "+Rules.Round(c.hunger),new Vector2(.39f,.43f),new Vector2(.99f,.90f),21,ink);
                Positioned(row,c.species==Species.Human?"":Passive(c),new Vector2(.39f,.02f),new Vector2(.99f,.44f),20,muted);
            }
            PageBar(PageFooter(body),personnelPage,(people.Count+3)/4,p=>{personnelPage=p;Render();});
        }
    }
}

