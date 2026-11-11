using UnityEngine;
using UnityEngine.UI;
namespace Apocalypse
{
    public sealed partial class GameApp
    {
        void ResetNewGameView()
        {
            speed=1;battleSpeed=1;clock=0;healthItem=mentalItem=false;
            choosingExploration=transitWalking=false;walkSeconds=0;shelterFacility="의무실";upgradeQuantity=1;
            restorationId=historyId=null;restorationStep=historyChapter=0;chapelSection="후원";
            codexPage=personnelPage=relicPage=inheritancePage=restorePage=0;
            codex.Back();for(int grade=0;grade<4;grade++)if(!codex.IsExpanded(grade))codex.Toggle(grade);
            scrollPositions.Clear();workspaceScrollPositions.Clear();workspaceScrolls.Clear();activeScroll=null;scrollKey=null;
        }
        void RenderNewGameWarning(Transform root)
        {
            var shield=Box(root,"New game warning",Vector2.zero,Vector2.one,new Color(0,0,0,.86f));
            var dialog=PrepCard(shield,"Reset confirmation",new Vector2(.24f,.25f),new Vector2(.76f,.77f));
            var layout=dialog.GetComponent<VerticalLayoutGroup>();layout.padding=new RectOffset(32,32,28,28);layout.spacing=16;
            Label(dialog,"모든 진행을 초기화할까요?",36,66,new Color(1,.69f,.52f));
            Label(dialog,"현재 진행과 함께 아래 내용이 모두 초기화됩니다.\n\n획득한 성좌 · 성유물 · 생존자 · 보유 자원\n승리 · 사망 · 도감 · 전승 기록\n\n초기화 후에는 처음부터 다시 시작합니다.",25,245,ink);
            var actions=Row(dialog,64);
            var cancel=MakeButton(actions,"취소",()=>{confirmNew=false;Render();});
            var reset=MakeButton(actions,"모두 초기화하고 시작",()=>{
                if(!confirmNew)return;
                confirmNew=false;BeginNew();
            });
            DistinctAction(cancel,new Color(.12f,.19f,.23f),ink,new Color(.45f,.63f,.72f));
            DistinctAction(reset,new Color(.50f,.10f,.10f),Color.white,new Color(.95f,.49f,.38f),forward:true);
            if(UnityEngine.EventSystems.EventSystem.current!=null)UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(cancel.gameObject);
        }
    }
}
