using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Apocalypse
{
    public sealed partial class GameApp
    {
        static readonly Rect[] BattleRegions={
            new Rect(80,17,348,392),new Rect(499,37,329,372),new Rect(915,17,265,392),
            new Rect(46,427,398,403),new Rect(474,434,358,388),new Rect(868,431,337,388),
            new Rect(41,852,334,365),new Rect(443,827,349,396),new Rect(799,814,443,415)};

        sealed class BattleActor
        {
            public RectTransform rect,fill,bar;
            public RawImage image;
            public Text healthText,nameText;
            public Vector2 home;
            public int index=-1;
            public bool dying;
        }
        RectTransform battleLayer,battleWorld,commandPanel;
        Texture2D battleAtlas,legionAtlas,battleInterior;
        readonly List<BattleActor> battleEnemies=new List<BattleActor>(8);
        readonly List<Button> battleSpeedButtons=new List<Button>(3);
        BattleActor battleHero;
        BattleState liveBattle;
        Text battleStatus,battleHeading,battleDamage;
        RectTransform impactSlash;
        float battleClock,battleShake,battleSpeed=1,battleFinish;
        bool battleFast;
        int commandTarget=-1;
        CombatMoment liveMoment;
        public bool BattleVisible { get { return liveBattle!=null||Game!=null&&Game.S.combat!=null; } }
        public CombatReplay CurrentBattle { get { return liveBattle==null?null:new CombatReplay{avatarName=liveBattle.avatarName,eventTitle=liveBattle.title,scene=liveBattle.scene}; } }
        void RecordBattles(Action action){action();}
        void ClearBattles()
        {
            if(battleLayer!=null){battleLayer.gameObject.SetActive(false);foreach(var i in battleLayer.GetComponentsInChildren<RawImage>(true))i.texture=null;Destroy(battleLayer.gameObject);}
            battleLayer=null;commandPanel=null;liveBattle=null;battleEnemies.Clear();battleSpeedButtons.Clear();battleHero=null;liveMoment=null;battleClock=0;battleFinish=0;
            if(battleAtlas!=null){Resources.UnloadAsset(battleAtlas);battleAtlas=null;}
            if(legionAtlas!=null){Resources.UnloadAsset(legionAtlas);legionAtlas=null;}
            if(battleInterior!=null){Resources.UnloadAsset(battleInterior);battleInterior=null;}
        }
        public void SkipBattlePresentation()
        {
            battleFast=true;
            if(liveBattle!=null&&liveBattle.ended){ClearBattles();Render();}
        }
        void RenderBattleScene()
        {
            if(Game.S.combat==null)return;
            if(battleLayer!=null){battleLayer.SetAsLastSibling();return;}
            liveBattle=Game.S.combat;battleClock=0;battleFinish=0;battleFast=false;
            battleAtlas=Resources.Load<Texture2D>("Art/Combat/battle-actors-v1");
            battleLayer=Box(canvas.transform,"Battle scene",Vector2.zero,Vector2.one,bg);
            battleWorld=Box(battleLayer,"Battle world",Vector2.zero,Vector2.one,Color.clear);
            if(liveBattle.scene=="interior"){battleInterior=Resources.Load<Texture2D>("Art/Environments/battle-interior-v1");FixedArt(battleWorld,battleInterior,Vector2.zero,Vector2.one);}
            else DrawAreaArt(battleWorld,liveBattle.scene=="chapel"?Area.Sanctuary:Game.S.exploration.current,Vector2.zero,Vector2.one);
            Box(battleLayer,"Battle HUD shade",new Vector2(0,.75f),Vector2.one,new Color(.02f,.03f,.04f,.95f));
            battleHeading=Positioned(battleLayer,string.IsNullOrEmpty(liveBattle.title)?AreaRules.Name(Game.S.exploration.current)+" 전투":liveBattle.title,new Vector2(.025f,.94f),new Vector2(.85f,.995f),30,gold);
            battleHero=CreateLiveActor(-1,Game.S.avatar.species==Species.Human?(PortraitIdentity.Index(Game.S.avatar.name)>=6?1:0):(int)Game.S.avatar.species+1);
            SyncLiveActors();
            battleDamage=Positioned(battleWorld,"",Vector2.zero,Vector2.zero,40,gold);battleDamage.rectTransform.anchorMin=battleDamage.rectTransform.anchorMax=Vector2.zero;battleDamage.rectTransform.sizeDelta=new Vector2(360,90);battleDamage.alignment=TextAnchor.MiddleCenter;
            var outline=battleDamage.gameObject.AddComponent<Outline>();outline.effectColor=Color.black;outline.effectDistance=new Vector2(2,-2);battleDamage.raycastTarget=false;
            impactSlash=Box(battleWorld,"Impact slash",Vector2.zero,Vector2.zero,gold);impactSlash.pivot=new Vector2(.5f,.5f);impactSlash.sizeDelta=new Vector2(9,150);impactSlash.localEulerAngles=new Vector3(0,0,-38);impactSlash.GetComponent<Image>().raycastTarget=false;impactSlash.gameObject.SetActive(false);
            battleStatus=Positioned(battleLayer,"화신체 선공",new Vector2(.025f,.14f),new Vector2(.97f,.22f),27,ink);
            var footer=PrepCard(battleLayer,"Battle controls",new Vector2(.025f,.025f),new Vector2(.975f,.125f));
            var row=Row(footer,48);
            foreach(int speedValue in new[]{1,2,4}){int n=speedValue;var button=MakeButton(row,"전투 ×"+n,()=>SetBattleSpeed(n));battleSpeedButtons.Add(button);}
            MakeButton(row,"연출 건너뛰기 · 명령에서 정지",SkipBattlePresentation,true,true);
            SetBattleSpeed(battleSpeed);
            if(liveBattle.waiting)RenderCombatCommands();
        }
        Texture2D PortraitByName(string name){return Portrait(new Character{name=name});}
        BattleActor CreateLiveActor(int index,int sprite)
        {
            var frame=Box(battleLayer,"Combat bar "+index,Vector2.zero,Vector2.one,new Color(.025f,.04f,.047f,.97f));Plate(frame,panel);
            var name=Positioned(frame,"",new Vector2(.04f,.52f),new Vector2(.96f,.98f),23,ink);
            var health=Positioned(frame,"",new Vector2(.04f,.23f),new Vector2(.96f,.58f),21,ink);
            var track=Box(frame,"HP track",new Vector2(.04f,.06f),new Vector2(.96f,.18f),new Color(.10f,.13f,.15f));
            var fill=Box(track,"HP fill",Vector2.zero,Vector2.one,index<0?new Color(.35f,.73f,.78f):new Color(.8f,.3f,.26f));
            var go=new GameObject("Battle actor "+index,typeof(RectTransform),typeof(RawImage));go.transform.SetParent(battleWorld,false);
            var image=go.GetComponent<RawImage>();image.texture=battleAtlas;image.raycastTarget=false;
            var rect=go.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=Vector2.zero;rect.pivot=new Vector2(.5f,0);
            if(index<0||liveBattle.enemies[index].kind==EnemyKind.Named)
            {
                FixedArt(frame,PortraitByName(index<0?liveBattle.avatarName:liveBattle.enemies[index].named.name),new Vector2(.015f,.22f),new Vector2(.22f,.98f));name.rectTransform.anchorMin=new Vector2(.25f,.52f);health.rectTransform.anchorMin=new Vector2(.25f,.23f);
            }
            var actor=new BattleActor{rect=rect,image=image,bar=frame,fill=fill,healthText=health,nameText=name,index=index};
            SetLiveSprite(actor,sprite);return actor;
        }
        void SetLiveSprite(BattleActor actor,int sprite)
        {
            var r=BattleRegions[sprite];actor.image.uvRect=new Rect(r.x/1254,1-r.yMax/1254,r.width/1254,r.height/1254);
            float h=((RectTransform)canvas.transform).rect.height*.39f;
            actor.rect.sizeDelta=new Vector2(h*r.width/r.height,h);
        }
        void SyncLiveActors()
        {
            var b=liveBattle;if(b==null)return;
            var size=((RectTransform)canvas.transform).rect.size;
            var a=Game.S.avatar;
            battleHero.home=new Vector2(size.x*.20f,size.y*(b.waiting?.46f:.23f));
            float heroHeight=size.y*(b.waiting?.26f:.39f);battleHero.rect.sizeDelta=new Vector2(heroHeight*battleHero.rect.sizeDelta.x/battleHero.rect.sizeDelta.y,heroHeight);
            battleHero.bar.anchorMin=new Vector2(.025f,.78f);battleHero.bar.anchorMax=new Vector2(.31f,.935f);
            battleHero.nameText.text=b.avatarName;battleHero.healthText.text=a==null?"화신체 사망":"체력 "+Rules.Round(a.health)+" / "+a.MaxHealth+"   정신 "+Rules.Round(a.sanity);
            battleHero.fill.anchorMax=new Vector2(a==null?0:(float)(a.health/a.MaxHealth),1);
            int deadTarget=b.lastMoment!=null&&b.lastMoment.kind==CombatMomentKind.AvatarAttack?b.lastMoment.target:-1;
            int living=0;for(int i=0;i<b.enemies.Count;i++)if(b.enemies[i].hp>0||i==deadTarget)living++;
            int count=Math.Min(8,living),slot=0;
            for(int i=0;i<b.enemies.Count&&slot<count;i++)if(b.enemies[i].hp>0||i==deadTarget)
            {
                var e=b.enemies[i];BattleActor actor;
                if(slot>=battleEnemies.Count){actor=CreateLiveActor(i,e.kind==EnemyKind.Normal?6:e.kind==EnemyKind.Demon?8:7);battleEnemies.Add(actor);}
                else actor=battleEnemies[slot];
                actor.index=i;actor.dying=e.hp==0;actor.image.color=Color.white;actor.rect.localEulerAngles=Vector3.zero;SetLiveSprite(actor,e.kind==EnemyKind.Normal?6:e.kind==EnemyKind.Demon?8:7);
                if(e.kind==EnemyKind.Special||e.kind==EnemyKind.Demon)
                {
                    if(legionAtlas==null)legionAtlas=Resources.Load<Texture2D>("Art/Combat/legion-enemies-v2");
                    int cell=(e.kind==EnemyKind.Demon?0:8)+(int)b.legion;
                    actor.image.texture=legionAtlas;actor.image.uvRect=new Rect(cell%4*.25f,1-(cell/4+1)*.25f,.25f,.25f);
                    actor.rect.sizeDelta=new Vector2(size.y*.40f,size.y*.40f);
                }
                else actor.image.texture=battleAtlas;
                float h=size.y*(b.waiting?.26f:.39f);actor.rect.sizeDelta=new Vector2(h*actor.rect.sizeDelta.x/actor.rect.sizeDelta.y,h);
                actor.rect.gameObject.SetActive(true);actor.bar.gameObject.SetActive(true);
                float width=.64f/Math.Max(1,count);
                actor.home=new Vector2(size.x*(.47f+(slot+.5f)*.47f/Math.Max(1,count)),size.y*((b.waiting?.46f:.23f)+(slot%2)*.025f));
                actor.bar.anchorMin=new Vector2(.335f+slot*width,.78f);actor.bar.anchorMax=new Vector2(.335f+(slot+1)*width-.005f,.935f);
                actor.nameText.text=e.name;actor.healthText.text=e.hp+" / "+e.maxHp;actor.fill.anchorMax=new Vector2(e.hp/(float)e.maxHp,1);
                slot++;
            }
            for(int i=slot;i<battleEnemies.Count;i++){battleEnemies[i].rect.gameObject.SetActive(false);battleEnemies[i].bar.gameObject.SetActive(false);}
            battleHero.rect.anchoredPosition=battleHero.home;foreach(var e in battleEnemies)if(e.rect.gameObject.activeSelf)e.rect.anchoredPosition=e.home;
        }
        void SetBattleSpeed(float value)
        {
            battleSpeed=value;
            for(int i=0;i<battleSpeedButtons.Count;i++){bool chosen=value==(1<<i);DistinctAction(battleSpeedButtons[i],chosen?new Color(.45f,.75f,.82f):new Color(.065f,.1f,.14f),chosen?new Color(.02f,.055f,.07f):ink,new Color(.4f,.65f,.75f),tick:chosen);}
        }
        void RenderCombatCommands()
        {
            if(commandPanel!=null||liveBattle==null||!liveBattle.waiting)return;
            SyncLiveActors();
            commandPanel=Box(battleLayer,"Emergency commands",new Vector2(.055f,.15f),new Vector2(.945f,.43f),new Color(.025f,.035f,.045f,.98f));Plate(commandPanel,panel);
            var stack=Stack(commandPanel,20);Label(stack,liveBattle.prompt,29,46,gold);
            Label(stack,"명령당 신위 10 · 거부 시에도 소모   /   보유 "+Game.S.divinity,22,36,ink);
            var targetRow=Row(stack,42);Label(targetRow,"공격 대상",21,42,gold);
            commandTarget=-1;int displayed=0;
            for(int i=0;i<liveBattle.enemies.Count;i++)if(liveBattle.enemies[i].hp>0&&displayed++<6)
            {
                int at=i;if(commandTarget<0)commandTarget=i;
                MakeButton(targetRow,liveBattle.enemies[i].name+" · "+liveBattle.enemies[i].hp+"/"+liveBattle.enemies[i].maxHp,()=>{commandTarget=at;RefreshCommandButtons();});
            }
            var actions=Row(stack,58);
            foreach(BattleCommand c in Enum.GetValues(typeof(BattleCommand)))
            {
                var command=c;MakeButton(actions,CommandName(c),()=>{Game.IssueCombatCommand(command,commandTarget);CloseCommandPanel();Save();},Game.S.divinity>=10,true);
            }
            MakeButton(stack,"명령 없이 계속",()=>{Game.ContinueCombat();CloseCommandPanel();Save();});
            RefreshCommandButtons();
        }
        static string CommandName(BattleCommand c){return c==BattleCommand.Attack?"공격 지정":c==BattleCommand.Defend?"방어":"퇴각";}
        void RefreshCommandButtons()
        {
            if(commandPanel==null)return;
            foreach(var button in commandPanel.GetComponentsInChildren<Button>())
            {
                foreach(BattleCommand c in Enum.GetValues(typeof(BattleCommand)))if(button.name=="Button "+CommandName(c))
                    button.GetComponentInChildren<Text>().text=CommandName(c)+" · 수락 "+Rules.Round(Game.CommandAcceptance(c,commandTarget)*100)+"%";
            }
        }
        void CloseCommandPanel()
        {
            if(commandPanel!=null){commandPanel.gameObject.SetActive(false);Destroy(commandPanel.gameObject);commandPanel=null;}
            battleClock=0;
        }
        bool UpdateBattlePresentation()
        {
            if(!BattleVisible)return false;
            if(battleLayer==null){RenderBattleScene();return true;}
            if(liveBattle.ended)
            {
                battleHeading.text=liveBattle.outcome;battleStatus.text="결과 저장 완료";
                foreach(var actor in battleEnemies)if(actor.dying){float p=Mathf.Clamp01(battleFinish/.75f);actor.rect.localEulerAngles=new Vector3(0,0,-80*p);actor.image.color=new Color(1,.5f,.4f,1-p);}
                if(Game.S.avatar==null){battleHero.rect.localEulerAngles=new Vector3(0,0,80*Mathf.Clamp01(battleFinish/.75f));}
                if((battleFinish+=Time.unscaledDeltaTime)>=1){ClearBattles();clock=0;Render();}
                return true;
            }
            float delta=Mathf.Min(.1f,Time.unscaledDeltaTime);battleClock+=delta*battleSpeed;
            if(battleShake>0){battleShake=Mathf.Max(0,battleShake-delta);float amp=12*battleShake/.22f;battleWorld.anchoredPosition=new Vector2(Mathf.Sin(Time.unscaledTime*83)*amp,Mathf.Cos(Time.unscaledTime*97)*amp*.65f);}
            else battleWorld.anchoredPosition=Vector2.zero;
            float motion=Mathf.Sin(Mathf.Clamp01(battleClock/.65f)*Mathf.PI);
            battleHero.rect.anchoredPosition=battleHero.home;
            foreach(var actor in battleEnemies)if(actor.rect.gameObject.activeSelf)
            {
                actor.rect.anchoredPosition=actor.home;
                if(actor.dying){float p=Mathf.Clamp01(battleClock/.72f);actor.rect.localEulerAngles=new Vector3(0,0,-80*p);actor.image.color=new Color(1,.5f,.4f,1-p);}
                if(liveMoment!=null&&liveMoment.kind==CombatMomentKind.EnemyAttack&&liveMoment.actor==actor.index)actor.rect.anchoredPosition+=new Vector2(-120*motion,0);
                if(liveMoment!=null&&liveMoment.kind==CombatMomentKind.AvatarAttack&&liveMoment.target==actor.index)battleHero.rect.anchoredPosition+=new Vector2(Mathf.Max(0,actor.home.x-battleHero.home.x-180)*motion,0);
            }
            if(liveBattle.waiting)
            {
                if(Game.S.controlLocked){Game.ContinueCombat();CloseCommandPanel();}
                else if(battleClock>=.65f)RenderCombatCommands();
                return true;
            }
            if(commandPanel!=null)CloseCommandPanel();
            if(battleDamage!=null){battleDamage.rectTransform.anchoredPosition+=new Vector2(0,delta*45);var c=battleDamage.color;c.a=1-Mathf.Clamp01(battleClock/.72f);battleDamage.color=c;impactSlash.gameObject.SetActive(liveMoment!=null&&liveMoment.amount>0&&battleClock<.24f);}
            if(!battleFast&&battleClock<.72f)return true;
            int budget=battleFast?24:1;
            while(budget-->0&&Game.S.combat!=null&&!liveBattle.waiting)
            {
                Game.StepCombat();liveMoment=liveBattle.lastMoment;
                SyncLiveActors();
                if(liveMoment!=null)
                {
                    string actor=liveMoment.actor>=0&&liveMoment.actor<liveBattle.enemies.Count?liveBattle.enemies[liveMoment.actor].name:liveBattle.avatarName;
                    battleStatus.text=(liveMoment.message??actor)+" · "+(liveMoment.kind==CombatMomentKind.AvatarAttack?"공격 ":liveMoment.kind==CombatMomentKind.Recovery?"회복 ":"피해 ")+liveMoment.amount;
                    battleDamage.text=liveMoment.kind==CombatMomentKind.Blocked?"봉쇄":liveMoment.kind==CombatMomentKind.Summon?"소환 +"+liveMoment.amount:liveMoment.amount==0?"회피":(liveMoment.kind==CombatMomentKind.Recovery?"+":"-")+liveMoment.amount;battleDamage.color=liveMoment.kind==CombatMomentKind.Recovery?new Color(.4f,.95f,.6f):liveMoment.kind==CombatMomentKind.AvatarAttack?gold:new Color(1,.45f,.35f);
                    Vector2 point=battleHero.home;foreach(var victim in battleEnemies)if(liveMoment.kind==CombatMomentKind.AvatarAttack&&victim.index==liveMoment.target)point=victim.home;
                    battleDamage.rectTransform.anchoredPosition=point+new Vector2(0,250);impactSlash.anchoredPosition=point+new Vector2(0,170);
                    if(liveMoment.kind==CombatMomentKind.EnemyAttack&&liveMoment.amount>0)battleShake=.22f;
                }
                battleHeading.text=(string.IsNullOrEmpty(liveBattle.title)?"전투":liveBattle.title)+"  /  "+(liveBattle.turn+1)+"턴";
            }
            battleClock=0;return true;
        }
    }
}
