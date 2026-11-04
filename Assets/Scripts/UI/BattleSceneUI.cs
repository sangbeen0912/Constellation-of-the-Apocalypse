using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Apocalypse
{
    public sealed partial class GameApp
    {
        // Source alpha bounds plus eight pixels of clear padding. A generated atlas
        // is not perfectly divided into thirds; explicit regions avoid neighbour bleed.
        static readonly Rect[] BattleRegions={
            new Rect(80,17,348,392),new Rect(499,37,329,372),new Rect(915,17,265,392),
            new Rect(46,427,398,403),new Rect(474,434,358,388),new Rect(868,431,337,388),
            new Rect(41,852,334,365),new Rect(443,827,349,396),new Rect(799,814,443,415)};
        sealed class BattleActor
        {
            public RectTransform rect,fill;
            public RawImage image;
            public Text healthText;
            public Vector2 home;
            public double health,max;
            public float fall;
        }
        readonly Queue<CombatReplay> combatQueue=new Queue<CombatReplay>();
        readonly List<BattleActor> battleEnemies=new List<BattleActor>();
        readonly List<Button> battleSpeedButtons=new List<Button>();
        RectTransform battleLayer,hitEffect,battleWorld;
        Texture2D battleAtlas,battleInterior;
        CombatReplay currentCombat;
        BattleActor battleHero;
        Text battleStatus,battleDamage,battleHeading;
        int battleStep;
        float battleClock,battleIntro,battleResult,battleSpeed=1;
        bool battleImpact,battleEnded;
        float battleShake;
        public bool BattleVisible { get { return currentCombat!=null||combatQueue.Count>0; } }
        public CombatReplay CurrentBattle { get { return currentCombat; } }

        void RecordBattles(Action action)
        {
            var engine=Game;
            engine.CombatRecorded+=EnqueueCombat;
            try{action();}finally{engine.CombatRecorded-=EnqueueCombat;}
        }
        void EnqueueCombat(CombatReplay replay){combatQueue.Enqueue(replay);}
        void ClearBattles()
        {
            combatQueue.Clear();currentCombat=null;
            if(battleLayer!=null)
            {
                battleLayer.gameObject.SetActive(false);
                foreach(var image in battleLayer.GetComponentsInChildren<RawImage>(true))if(image.texture==battleAtlas||image.texture==battleInterior)image.texture=null;
                Destroy(battleLayer.gameObject);battleLayer=null;
            }
            if(battleAtlas!=null){Resources.UnloadAsset(battleAtlas);battleAtlas=null;}
            if(battleInterior!=null){Resources.UnloadAsset(battleInterior);battleInterior=null;}
            battleEnemies.Clear();battleSpeedButtons.Clear();battleHero=null;
        }
        public void SkipBattlePresentation(){ClearBattles();clock=0;Render();}
        void RenderBattleScene()
        {
            if(battleLayer!=null){battleLayer.SetAsLastSibling();return;}
            if(combatQueue.Count==0)return;
            currentCombat=combatQueue.Dequeue();battleStep=0;battleClock=0;battleIntro=.5f;battleResult=0;battleImpact=false;battleEnded=false;
            battleAtlas=Resources.Load<Texture2D>("Art/Combat/battle-actors-v1");
            battleLayer=Box(canvas.transform,"Battle scene",Vector2.zero,Vector2.one,bg);
            battleWorld=Box(battleLayer,"Battle world",Vector2.zero,Vector2.one,Color.clear);battleShake=0;
            Texture2D backdrop=city;
            if(currentCombat.scene=="interior"){if(battleInterior==null)battleInterior=Resources.Load<Texture2D>("Art/Environments/battle-interior-v1");backdrop=battleInterior;}
            else if(currentCombat.scene=="chapel")backdrop=Facility(1);
            else if(currentCombat.scene=="rift")backdrop=gate;
            FixedArt(battleWorld,backdrop,new Vector2(-.02f,-.02f),new Vector2(1.02f,1.02f));
            Box(battleLayer,"Battle sky shade",new Vector2(0,.75f),Vector2.one,new Color(.015f,.025f,.035f,.87f));
            Box(battleWorld,"Battle floor",Vector2.zero,new Vector2(1,.23f),new Color(.018f,.025f,.024f,.75f));
            battleHeading=Positioned(battleLayer,string.IsNullOrEmpty(currentCombat.eventTitle)?currentCombat.location==Goal.Rift?"균열 전투":"필드 전투":currentCombat.eventTitle,new Vector2(.035f,.955f),new Vector2(.70f,.995f),25,gold);
            var heroBar=BattleBar(currentCombat.avatarName,currentCombat.health,currentCombat.maxHealth,new Vector2(.035f,.78f),new Vector2(.32f,.945f),new Color(.40f,.72f,.75f));
            Art(Stack(ScreenArea(heroBar.Item1,"Combat portrait",new Vector2(.025f,.08f),new Vector2(.22f,.93f)),0),PortraitByName(currentCombat.avatarName),126,true);
            // Make room for the identity portrait without changing its established name mapping.
            heroBar.Item2.rectTransform.anchorMin=new Vector2(.25f,.36f);
            var heroName=heroBar.Item1.Find("Combat name").GetComponent<Text>();heroName.rectTransform.anchorMin=new Vector2(.25f,.58f);
            int heroSprite=currentCombat.species==Species.Human?(PortraitIdentity.Index(currentCombat.avatarName)>=6?1:0):(int)currentCombat.species+1;
            battleHero=BattleSprite(0,heroSprite,new Vector2(.23f,.205f),.40f,currentCombat.health,currentCombat.maxHealth,heroBar.Item2,heroBar.Item3);
            for(int i=0;i<currentCombat.enemies.Count;i++)
            {
                var enemy=currentCombat.enemies[i];float width=.61f/currentCombat.enemies.Count;
                var bar=BattleBar(enemy.name,enemy.health,enemy.health,new Vector2(.36f+i*width,.78f),new Vector2(.36f+(i+1)*width-.008f,.945f),new Color(.75f,.34f,.29f));
                if(enemy.kind==EnemyKind.Named)
                {
                    string name=enemy.name.StartsWith("네임드 좀비 · ")?enemy.name.Substring("네임드 좀비 · ".Length):enemy.name;
                    Art(Stack(ScreenArea(bar.Item1,"Named identity",new Vector2(.02f,.22f),new Vector2(.25f,.94f)),0),PortraitByName(name),110,true);
                    bar.Item2.rectTransform.anchorMin=new Vector2(.28f,.31f);bar.Item1.Find("Combat name").GetComponent<Text>().rectTransform.anchorMin=new Vector2(.28f,.58f);
                }
                int sprite=enemy.kind==EnemyKind.Demon?8:enemy.kind==EnemyKind.Special?7:6;
                float x=currentCombat.enemies.Count==1?.72f:.57f+.34f*i/(currentCombat.enemies.Count-1);
                var actor=BattleSprite(i+1,sprite,new Vector2(x,.205f+i%2*.018f),enemy.kind==EnemyKind.Demon?.45f:.34f-i*.012f,enemy.health,enemy.health,bar.Item2,bar.Item3);
                if(enemy.kind==EnemyKind.Named)actor.image.color=new Color(.74f,.77f,.92f);
                battleEnemies.Add(actor);
            }
            battleStatus=Positioned(battleLayer,"적과 대치합니다.",new Vector2(.035f,.095f),new Vector2(.965f,.17f),27,ink);
            battleDamage=Positioned(battleLayer,"",Vector2.zero,Vector2.zero,48,gold);battleDamage.alignment=TextAnchor.MiddleCenter;
            battleDamage.fontStyle=FontStyle.Bold;var stroke=battleDamage.gameObject.AddComponent<Outline>();stroke.effectColor=Color.black;stroke.effectDistance=new Vector2(2,-2);
            battleDamage.rectTransform.anchorMin=battleDamage.rectTransform.anchorMax=Vector2.zero;
            battleDamage.rectTransform.sizeDelta=new Vector2(240,90);battleDamage.raycastTarget=false;
            var effectGo=new GameObject("Pixel impact",typeof(RectTransform),typeof(CanvasRenderer),typeof(BattleImpactGlyph));effectGo.transform.SetParent(battleWorld,false);
            hitEffect=effectGo.GetComponent<RectTransform>();hitEffect.anchorMin=hitEffect.anchorMax=Vector2.zero;hitEffect.sizeDelta=new Vector2(160,160);
            effectGo.GetComponent<BattleImpactGlyph>().raycastTarget=false;effectGo.SetActive(false);
            var footer=Row(ScreenArea(battleLayer,"Battle controls",new Vector2(.035f,.025f),new Vector2(.965f,.085f)),54);
            foreach(int multiplier in new[]{1,2,4})
            {
                int value=multiplier;var button=MakeButton(footer,"전투 ×"+multiplier,()=>SetBattleSpeed(value));
                battleSpeedButtons.Add(button);
            }
            MakeButton(footer,"전투 연출 건너뛰기",SkipBattlePresentation,true,true);
            SetBattleSpeed(battleSpeed);
        }
        Texture2D PortraitByName(string name)
        {if(!portraits.ContainsKey(name))portraits[name]=ArtCatalog.Portrait(name);return portraits[name];}
        Tuple<RectTransform,Text,RectTransform> BattleBar(string name,double health,double max,Vector2 min,Vector2 maxAnchor,Color color)
        {
            var panel=Box(battleLayer,"Combat bar "+name,min,maxAnchor,new Color(.025f,.04f,.047f,.97f));Plate(panel,new Color(.04f,.06f,.065f));
            var label=Positioned(panel,name,new Vector2(.05f,.58f),new Vector2(.95f,.96f),22,ink);label.name="Combat name";
            var hp=Positioned(panel,"체력 "+Rules.Round(health)+" / "+Rules.Round(max),new Vector2(.05f,.31f),new Vector2(.95f,.60f),24,ink);
            var track=Box(panel,"Combat health track",new Vector2(.05f,.11f),new Vector2(.95f,.24f),new Color(.11f,.14f,.15f));
            var fill=Box(track,"Combat health fill",Vector2.zero,new Vector2((float)(health/max),1),color);
            return Tuple.Create(panel,hp,fill);
        }
        BattleActor BattleSprite(int index,int sprite,Vector2 place,float height,double health,double max,Text label,RectTransform fill)
        {
            var region=BattleRegions[sprite];
            float left=region.xMin/1254f,right=region.xMax/1254f,top=region.yMin/1254f,bottom=region.yMax/1254f;
            var size=((RectTransform)canvas.transform).rect.size;
            var shadow=Box(battleWorld,"Actor shadow "+index,new Vector2(place.x-.055f,place.y-.005f),new Vector2(place.x+.055f,place.y+.006f),new Color(0,0,0,.65f));shadow.GetComponent<Image>().raycastTarget=false;
            var go=new GameObject("Battle actor "+index,typeof(RectTransform),typeof(RawImage));go.transform.SetParent(battleWorld,false);
            var rect=go.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=Vector2.zero;rect.pivot=new Vector2(.5f,0);
            rect.sizeDelta=new Vector2(size.y*height*(right-left)/(bottom-top),size.y*height);
            var image=go.GetComponent<RawImage>();image.texture=battleAtlas;image.uvRect=new Rect(left,1-bottom,right-left,bottom-top);image.raycastTarget=false;
            var actor=new BattleActor{rect=rect,image=image,home=new Vector2(size.x*place.x,size.y*place.y),health=health,max=max,healthText=label,fill=fill};
            rect.anchoredPosition=actor.home;return actor;
        }
        void SetBattleSpeed(float value)
        {
            battleSpeed=value;
            for(int i=0;i<battleSpeedButtons.Count;i++)
            {
                bool chosen=value==(1<<i);
                DistinctAction(battleSpeedButtons[i],chosen?new Color(.45f,.75f,.82f):new Color(.065f,.1f,.14f),chosen?new Color(.02f,.055f,.07f):ink,new Color(.40f,.65f,.75f),tick:chosen);
            }
        }
        void SetActorHealth(BattleActor actor,double value)
        {
            actor.health=Math.Max(0,value);actor.healthText.text="체력 "+Rules.Round(actor.health)+" / "+Rules.Round(actor.max);
            actor.fill.anchorMax=new Vector2((float)(actor.health/actor.max),1);
        }
        void FloatDamage(BattleActor actor,string value,Color color)
        {
            battleDamage.text=value;battleDamage.color=color;battleDamage.rectTransform.anchoredPosition=actor.home+new Vector2(0,actor.rect.sizeDelta.y*.82f);
        }
        void ApplyBattleMoment(CombatMoment moment)
        {
            if(moment.kind==CombatMomentKind.AvatarAttack)
            {
                var target=battleEnemies[moment.target];SetActorHealth(target,moment.health);FloatDamage(target,"-"+moment.amount,gold);
                battleStatus.text=currentCombat.avatarName+"의 공격 · 피해 "+moment.amount;
                hitEffect.anchoredPosition=target.home+new Vector2(0,target.rect.sizeDelta.y*.48f);hitEffect.gameObject.SetActive(true);
            }
            else
            {
                SetActorHealth(battleHero,moment.health);
                bool heal=moment.kind==CombatMomentKind.Recovery;
                if(!heal&&moment.amount>0)battleShake=.22f;
                FloatDamage(battleHero,heal?"+"+moment.amount:moment.amount==0?"회피":"-"+moment.amount,heal?new Color(.42f,.91f,.55f):new Color(1,.50f,.36f));
                battleStatus.text=heal?"불완전한 생명 · 체력 회복":moment.amount==0?"공격을 피했습니다.":"적의 반격 · 피해 "+moment.amount;
                hitEffect.anchoredPosition=battleHero.home+new Vector2(0,battleHero.rect.sizeDelta.y*.5f);hitEffect.gameObject.SetActive(!heal&&moment.amount>0);
            }
        }
        void ResetBattleActors()
        {
            ResetActor(battleHero);
            foreach(var actor in battleEnemies)ResetActor(actor);
        }
        void ResetActor(BattleActor actor)
        {
            actor.rect.anchoredPosition=actor.home;actor.rect.localEulerAngles=Vector3.zero;
            actor.image.color=Color.white;
            if(actor.health<=0){actor.fall=Mathf.Min(1,actor.fall+Time.unscaledDeltaTime*battleSpeed*3);actor.rect.localEulerAngles=new Vector3(0,0,-85*actor.fall);actor.image.color=new Color(.40f,.42f,.43f,1-.8f*actor.fall);}
        }
        bool UpdateBattlePresentation()
        {
            if(!BattleVisible)return false;
            if(battleLayer==null){Render();return true;}
            if(battleShake>0){battleShake=Mathf.Max(0,battleShake-Time.unscaledDeltaTime);float amp=12*battleShake/.22f;battleWorld.anchoredPosition=new Vector2(Mathf.Round(Mathf.Sin(Time.unscaledTime*83)*amp),Mathf.Round(Mathf.Cos(Time.unscaledTime*97)*amp*.65f));}
            else battleWorld.anchoredPosition=Vector2.zero;
            float delta=Mathf.Min(.1f,Time.unscaledDeltaTime)*battleSpeed;
            if(battleIntro>0){battleIntro-=delta;return true;}
            if(battleEnded)
            {
                battleResult+=delta;
                if(battleResult>=1.2f)
                {
                    currentCombat=null;battleLayer.gameObject.SetActive(false);
                    foreach(var image in battleLayer.GetComponentsInChildren<RawImage>(true))if(image.texture==battleAtlas||image.texture==battleInterior)image.texture=null;
                    Destroy(battleLayer.gameObject);battleLayer=null;battleEnemies.Clear();battleSpeedButtons.Clear();
                    if(combatQueue.Count==0&&battleAtlas!=null){Resources.UnloadAsset(battleAtlas);battleAtlas=null;}
                    if(combatQueue.Count==0&&battleInterior!=null){Resources.UnloadAsset(battleInterior);battleInterior=null;}
                    clock=0;Render();
                }
                return true;
            }
            if(battleStep>=currentCombat.moments.Count)
            {
                SetActorHealth(battleHero,currentCombat.finalHealth);battleEnded=true;battleResult=0;
                battleHeading.text=currentCombat.outcome;battleStatus.text=currentCombat.reason;battleDamage.text="";hitEffect.gameObject.SetActive(false);
                ResetBattleActors();if(currentCombat.died){battleHero.rect.localEulerAngles=new Vector3(0,0,85);battleHero.image.color=new Color(.55f,.55f,.55f,.55f);}
                return true;
            }
            var moment=currentCombat.moments[battleStep];battleClock+=delta;float duration=moment.kind==CombatMomentKind.Recovery?.55f:.72f;
            float progress=Mathf.Clamp01(battleClock/duration);
            ResetBattleActors();
            float lunge=Mathf.Sin(Mathf.Clamp01(progress/.8f)*Mathf.PI);
            if(moment.kind==CombatMomentKind.AvatarAttack)
            {
                var target=battleEnemies[moment.target];
                battleHero.rect.anchoredPosition=battleHero.home+new Vector2(Mathf.Max(0,target.home.x-battleHero.home.x-210)*lunge,0);
                battleHero.rect.localEulerAngles=new Vector3(0,0,-9*lunge);
                if(battleImpact&&target.health>0){target.rect.anchoredPosition+=new Vector2(Mathf.Sin(progress*45)*13,0);target.image.color=new Color(1,.50f,.40f);}
            }
            else if(moment.kind==CombatMomentKind.GroupAttack)
            {
                foreach(var enemy in battleEnemies)if(enemy.health>0)enemy.rect.anchoredPosition+=new Vector2(-110*lunge,0);
                if(battleImpact)
                {
                    battleHero.rect.anchoredPosition+=new Vector2(moment.amount==0?-60*lunge:Mathf.Sin(progress*45)*14,0);
                    if(moment.amount>0)battleHero.image.color=new Color(1,.50f,.40f);
                }
            }
            else battleHero.image.color=new Color(.60f,1,.72f);
            if(progress>=.43f&&!battleImpact){battleImpact=true;ApplyBattleMoment(moment);}
            if(battleImpact)
            {
                battleDamage.rectTransform.anchoredPosition+=new Vector2(0,delta*45);
                var c=battleDamage.color;c.a=1-Mathf.Clamp01((progress-.55f)/.45f);battleDamage.color=c;
                hitEffect.gameObject.SetActive(progress<.72f&&moment.kind!=CombatMomentKind.Recovery&&moment.amount>0);
            }
            if(progress>=1){battleStep++;battleClock=0;battleImpact=false;battleDamage.text="";hitEffect.gameObject.SetActive(false);}
            return true;
        }
    }
}
