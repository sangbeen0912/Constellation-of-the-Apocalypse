using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Apocalypse
{
    public sealed partial class GameApp
    {
        sealed class BattleActor
        {
            public RectTransform rect,fill,bar;public RawImage image;public Text nameText,healthText;
            public Vector2 home;public int index;public bool dying;
        }
        RectTransform battleLayer,battleWorld,commandPanel,impactSlash;
        Texture2D enemyAtlas,battleInterior;
        readonly List<BattleActor> battleEnemies=new List<BattleActor>(8);
        readonly List<Button> battleSpeedButtons=new List<Button>(3);
        BattleState liveBattle;CombatMoment liveMoment;
        Text battleStatus,battleHeading,battleDamage;
        Image hitFlash;
        float battleClock,battleShake,battleSpeed=1,battleFinish;
        bool battleFast;int commandTarget=-1;
        public bool BattleVisible { get { return liveBattle!=null||Game!=null&&Game.S.combat!=null; } }
        public CombatReplay CurrentBattle { get { return liveBattle==null?null:new CombatReplay{avatarName=liveBattle.avatarName,eventTitle=liveBattle.title,scene=liveBattle.scene}; } }
        void RecordBattles(Action action){action();}
        void ClearBattles()
        {
            if(battleLayer!=null){battleLayer.gameObject.SetActive(false);foreach(var i in battleLayer.GetComponentsInChildren<RawImage>(true))i.texture=null;Destroy(battleLayer.gameObject);}
            battleLayer=battleWorld=commandPanel=null;liveBattle=null;liveMoment=null;battleEnemies.Clear();battleSpeedButtons.Clear();battleClock=battleFinish=battleShake=0;
            if(fieldBackdrop!=null)fieldBackdrop.anchoredPosition=Vector2.zero;
            if(enemyAtlas!=null){Resources.UnloadAsset(enemyAtlas);enemyAtlas=null;}
            fieldContentKey=null;
        }
        public void SkipBattlePresentation(){battleFast=true;if(liveBattle!=null&&liveBattle.ended){ClearBattles();Render();}}
        void RenderBattleScene()
        {
            if(Game.S.combat==null||fieldStage==null||battleLayer!=null)return;
            liveBattle=Game.S.combat;battleClock=battleFinish=0;battleFast=false;
            enemyAtlas=Resources.Load<Texture2D>("Art/Combat/encounter-enemies-v1");
            battleLayer=Box(fieldStage,"Battle scene",Vector2.zero,Vector2.one,Color.clear);
            battleWorld=Box(battleLayer,"Battle world",Vector2.zero,Vector2.one,Color.clear);
            battleDamage=Positioned(battleWorld,"",Vector2.zero,Vector2.zero,37,gold);
            battleDamage.rectTransform.sizeDelta=new Vector2(300,70);battleDamage.alignment=TextAnchor.MiddleCenter;battleDamage.raycastTarget=false;
            var edge=battleDamage.gameObject.AddComponent<Outline>();edge.effectColor=Color.black;edge.effectDistance=new Vector2(2,-2);
            impactSlash=Box(battleWorld,"Impact slash",Vector2.zero,Vector2.zero,gold);impactSlash.pivot=new Vector2(.5f,.5f);impactSlash.sizeDelta=new Vector2(9,100);impactSlash.localEulerAngles=new Vector3(0,0,-38);impactSlash.GetComponent<Image>().raycastTarget=false;impactSlash.gameObject.SetActive(false);
            hitFlash=Box(battleLayer,"Player damage flash",Vector2.zero,Vector2.one,Color.clear).GetComponent<Image>();hitFlash.raycastTarget=false;
            SyncLiveActors();RenderBattlePane();
        }
        BattleActor CreateLiveActor(int index)
        {
            var frame=Box(battleLayer,"Combat bar "+index,Vector2.zero,Vector2.one,new Color(.02f,.03f,.04f,.92f));
            var name=Positioned(frame,"",new Vector2(.03f,.5f),new Vector2(.97f,1),19,ink);
            var hp=Positioned(frame,"",new Vector2(.03f,.12f),new Vector2(.97f,.56f),18,gold);
            var fill=Box(frame,"Enemy health",new Vector2(0,0),new Vector2(1,.09f),new Color(.8f,.25f,.22f));
            var go=new GameObject("Battle actor "+index,typeof(RectTransform),typeof(RawImage));go.transform.SetParent(battleWorld,false);
            var rect=go.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=Vector2.zero;rect.pivot=new Vector2(.5f,0);
            var image=go.GetComponent<RawImage>();image.texture=enemyAtlas;image.raycastTarget=false;
            return new BattleActor{rect=rect,image=image,bar=frame,fill=fill,nameText=name,healthText=hp,index=index};
        }
        void SyncLiveActors()
        {
            if(liveBattle==null||battleWorld==null)return;
            var size=fieldStage.rect.size;
            int dead=liveBattle.lastMoment!=null&&liveBattle.lastMoment.kind==CombatMomentKind.AvatarAttack?liveBattle.lastMoment.target:-1;
            int count=0;foreach(var enemy in liveBattle.enemies)if(enemy.hp>0)count++;if(dead>=0&&liveBattle.enemies[dead].hp<=0)count++;
            int columns=Math.Max(1,Math.Min(4,count)),rows=count>4?2:1,slot=0;
            for(int i=0;i<liveBattle.enemies.Count&&slot<8;i++)
            {
                var e=liveBattle.enemies[i];if(e.hp<=0&&i!=dead)continue;
                BattleActor a;if(slot==battleEnemies.Count){a=CreateLiveActor(i);battleEnemies.Add(a);}else a=battleEnemies[slot];
                a.index=i;a.dying=e.hp<=0;a.image.color=Color.white;a.rect.localEulerAngles=Vector3.zero;
                int cell=e.kind==EnemyKind.Normal?0:e.kind==EnemyKind.Named?1:e.kind==EnemyKind.Special?2+(int)liveBattle.legion:9+(int)liveBattle.legion;
                a.image.uvRect=new Rect(cell%4*.25f,1-(cell/4+1)*.25f,.25f,.25f);
                float width=size.x/columns,height=size.y/rows,h=Mathf.Min(height*.80f,width*.94f);a.rect.sizeDelta=new Vector2(h,h);
                int col=slot%columns,row=slot/columns;
                a.home=new Vector2(width*(col+.5f),height*row+height*.03f);a.rect.anchoredPosition=a.home;
                a.bar.anchorMin=new Vector2(col/(float)columns,(row+.82f)/rows);a.bar.anchorMax=new Vector2((col+1f)/columns,(row+1f)/rows);
                a.nameText.text=e.name;a.healthText.text=e.hp+" / "+e.maxHp;a.fill.anchorMax=new Vector2(e.hp/(float)e.maxHp,.09f);
                a.rect.gameObject.SetActive(true);a.bar.gameObject.SetActive(true);slot++;
            }
            for(int i=slot;i<battleEnemies.Count;i++){battleEnemies[i].rect.gameObject.SetActive(false);battleEnemies[i].bar.gameObject.SetActive(false);}
            battleDamage.transform.SetAsLastSibling();impactSlash.SetAsLastSibling();hitFlash.transform.SetAsLastSibling();
        }
        void SetBattleSpeed(float value)
        {
            battleSpeed=value;
            for(int i=0;i<battleSpeedButtons.Count;i++){bool chosen=value==(1<<i);DistinctAction(battleSpeedButtons[i],chosen?new Color(.45f,.75f,.82f):new Color(.065f,.1f,.14f),chosen?new Color(.02f,.055f,.07f):ink,new Color(.4f,.65f,.75f),tick:chosen);}
        }
        void RenderBattlePane()
        {
            ClearChildren(fieldPane);battleSpeedButtons.Clear();fieldContentKey="battle:"+liveBattle.id;
            var stack=Stack(fieldPane,15);battleHeading=Label(stack,"전투 · "+(liveBattle.turn+1)+"턴",28,60,gold);
            battleStatus=Label(stack,"적의 움직임을 살피고 있습니다.",23,160,ink);
            var row=Row(stack,48);foreach(int n in new[]{1,2,4}){int speedValue=n;battleSpeedButtons.Add(MakeButton(row,"×"+n,()=>SetBattleSpeed(speedValue)));}
            MakeButton(stack,"연출 건너뛰기 · 명령에서 정지",SkipBattlePresentation,true,true).GetComponent<LayoutElement>().preferredHeight=80;
            Label(stack,"화신체 체력은 왼쪽 상태창에서 확인할 수 있습니다.",21,110,muted);SetBattleSpeed(battleSpeed);
            if(liveBattle.waiting)RenderCombatCommands();
        }
        void RenderCombatCommands()
        {
            if(commandPanel!=null||liveBattle==null||!liveBattle.waiting)return;
            ClearChildren(fieldPane);battleSpeedButtons.Clear();commandPanel=Stack(fieldPane,15);commandPanel.name="Emergency commands";
            Label(commandPanel,liveBattle.prompt,27,85,gold);Label(commandPanel,"명령당 신위 1 · 거절해도 소비\n보유 신위 "+Game.S.divinity,21,65,ink);
            commandTarget=-1;for(int i=0;i<liveBattle.enemies.Count;i++)if(liveBattle.enemies[i].hp>0){commandTarget=i;break;}
            var target=MakeButton(commandPanel,"",()=>{});target.GetComponent<LayoutElement>().preferredHeight=70;
            Action refresh=()=>{var e=liveBattle.enemies[commandTarget];target.GetComponentInChildren<Text>().text="대상: "+e.name+" · "+e.hp+"/"+e.maxHp+"\n누르면 다음 대상";RefreshCommandButtons();};
            target.onClick.RemoveAllListeners();target.onClick.AddListener(()=>{for(int n=1;n<=liveBattle.enemies.Count;n++){int i=(commandTarget+n)%liveBattle.enemies.Count;if(liveBattle.enemies[i].hp>0){commandTarget=i;break;}}refresh();});
            foreach(BattleCommand c in Enum.GetValues(typeof(BattleCommand)))
            {
                var command=c;var button=MakeButton(commandPanel,CommandName(c),()=>{if(!Game.IssueCombatCommand(command,commandTarget))return;CloseCommandPanel();Save();},Game.S.divinity>=1,true);button.GetComponent<LayoutElement>().preferredHeight=64;
            }
            var proceed=MakeButton(commandPanel,"명령 없이 계속",()=>{Game.ContinueCombat();CloseCommandPanel();Save();});proceed.GetComponent<LayoutElement>().preferredHeight=64;refresh();
            if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(proceed.gameObject);
        }
        static string CommandName(BattleCommand c){return c==BattleCommand.Attack?"공격 지정":c==BattleCommand.Defend?"방어":"퇴각";}
        void RefreshCommandButtons()
        {
            if(commandPanel==null)return;
            foreach(var button in commandPanel.GetComponentsInChildren<Button>())foreach(BattleCommand c in Enum.GetValues(typeof(BattleCommand)))
                if(button.name=="Button "+CommandName(c))button.GetComponentInChildren<Text>().text=CommandName(c)+" · 수락 "+Rules.Round(Game.CommandAcceptance(c,commandTarget)*100)+"%";
        }
        void CloseCommandPanel(){commandPanel=null;battleClock=0;RenderBattlePane();}
        bool UpdateBattlePresentation()
        {
            if(!BattleVisible)return false;
            if(battleLayer==null){if(fieldMain==null)Render();else RenderBattleScene();return true;}
            RefreshRunningHud();if(fieldTime!=null)fieldTime.text=Rules.Clock(Game.S.hours)+"   남은 "+Math.Max(0,Rules.DayHours-Game.S.hours).ToString("0.0")+"시간 · 전투";
            visibleLogs=Game.S.logs.Count;RefreshLogRows();
            float delta=Mathf.Min(.1f,Time.unscaledDeltaTime);battleClock+=delta*battleSpeed;
            battleShake=Mathf.Max(0,battleShake-delta);
            var shake=battleShake>0?new Vector2(Mathf.Sin(Time.unscaledTime*83)*10,Mathf.Cos(Time.unscaledTime*97)*6)*battleShake/.3f:Vector2.zero;
            battleWorld.anchoredPosition=shake;fieldBackdrop.anchoredPosition=shake;
            hitFlash.color=new Color(.8f,.015f,.01f,.35f*battleShake/.3f);
            foreach(var actor in battleEnemies)if(actor.rect.gameObject.activeSelf)
            {
                float p=Mathf.Clamp01(battleClock/.72f),motion=Mathf.Sin(p*Mathf.PI);actor.rect.anchoredPosition=actor.home;actor.rect.localScale=Vector3.one;
                if(actor.dying){actor.rect.localEulerAngles=new Vector3(0,0,-70*p);actor.image.color=new Color(1,.6f,.5f,1-p);}
                else if(liveMoment!=null&&liveMoment.kind==CombatMomentKind.EnemyAttack&&liveMoment.actor==actor.index)actor.rect.localScale=Vector3.one*(1+.16f*motion);
                else if(liveMoment!=null&&liveMoment.kind==CombatMomentKind.AvatarAttack&&liveMoment.target==actor.index){actor.rect.anchoredPosition+=new Vector2(Mathf.Sin(p*35)*8*(1-p),0);actor.image.color=Color.Lerp(new Color(1,.3f,.2f),Color.white,p);}
            }
            if(liveBattle.ended){if((battleFinish+=delta)>=1){ClearBattles();clock=0;Render();}return true;}
            if(liveBattle.waiting){if(Game.S.controlLocked){Game.ContinueCombat();CloseCommandPanel();}else if(battleClock>=.65f)RenderCombatCommands();return true;}
            if(commandPanel!=null)CloseCommandPanel();
            var color=battleDamage.color;color.a=1-Mathf.Clamp01(battleClock/.72f);battleDamage.color=color;
            impactSlash.gameObject.SetActive(liveMoment!=null&&liveMoment.kind==CombatMomentKind.AvatarAttack&&liveMoment.amount>0&&battleClock<.24f);
            if(!battleFast&&battleClock<.72f)return true;
            int budget=battleFast?24:1;
            while(budget-->0&&Game.S.combat!=null&&!liveBattle.waiting)
            {
                Game.StepCombat();liveMoment=liveBattle.lastMoment;SyncLiveActors();
                if(liveMoment!=null)
                {
                    if(battleStatus!=null)battleStatus.text=(liveMoment.message??(liveMoment.kind==CombatMomentKind.AvatarAttack?"화신체의 공격":"적의 행동"))+" · "+liveMoment.amount;
                    battleDamage.text=liveMoment.kind==CombatMomentKind.Blocked?"봉쇄":liveMoment.kind==CombatMomentKind.Summon?"소환 +"+liveMoment.amount:liveMoment.amount==0?"회피":(liveMoment.kind==CombatMomentKind.Recovery?"+":"-")+liveMoment.amount;
                    battleDamage.color=liveMoment.kind==CombatMomentKind.AvatarAttack?gold:new Color(1,.45f,.35f);
                    Vector2 point=new Vector2(fieldStage.rect.width*.5f,fieldStage.rect.height*.5f);
                    foreach(var victim in battleEnemies)if(liveMoment.kind==CombatMomentKind.AvatarAttack&&victim.index==liveMoment.target)point=victim.home+new Vector2(0,victim.rect.rect.height*.6f);
                    battleDamage.rectTransform.anchoredPosition=point;impactSlash.anchoredPosition=point;
                    if((liveMoment.kind==CombatMomentKind.EnemyAttack||liveMoment.kind==CombatMomentKind.Reflect)&&liveMoment.amount>0)battleShake=.3f;
                }
                if(battleHeading!=null)battleHeading.text=liveBattle.ended?liveBattle.outcome:"전투 · "+(liveBattle.turn+1)+"턴";
            }
            battleClock=0;return true;
        }
    }
}
