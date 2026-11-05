#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Apocalypse
{
    // Real Windows rendering and pointer clicks; GameApp creates an isolated QA save slot.
    public sealed class SmokeRunner : MonoBehaviour
    {
        GameApp app;string output;int screenshot;
        void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        void OnLog(string message,string trace,LogType type)
        {
            if(type!=LogType.Exception&&type!=LogType.Error)return;
            File.WriteAllText(Path.Combine(output,"player-smoke.txt"),"result=FAIL\n"+message+"\n"+trace);Application.Quit(1);
        }
        IEnumerator Frame(){yield return new WaitForEndOfFrame();yield return null;}
        IEnumerator Capture(string label)
        {
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,(++screenshot).ToString("00")+"-"+label+".png"));yield return null;yield return null;
        }
        Button Find(string prefix)
        {
            return app.GetComponentsInChildren<Button>().First(b=>b.interactable&&b.GetComponentInChildren<Text>().text.StartsWith(prefix));
        }
        void Click(string prefix)
        {
            var button=Find(prefix);Canvas.ForceUpdateCanvases();var rect=button.GetComponent<RectTransform>();
            var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center)),button=PointerEventData.InputButton.Left};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            Check(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button,"Pointer blocked: "+prefix);
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.pointerClickHandler);
        }
        void Fixture(Area area)
        {
            var g=app.Game;var s=g.S;s.day=2;s.phase=Phase.Running;s.hours=0;s.goal=area==Area.Rift?Goal.Rift:Goal.Explore;s.exploration.current=area;s.activeRift=0;s.eventSlots=0;
            s.avatar.personality=Temperament.Brave;s.avatar.baseHealth=2000;s.avatar.health=2000;s.avatar.baseAttack=10000;s.avatar.hunger=s.avatar.sanity=100;
            s.controlLocked=false;s.scheduledConstellation=null;s.food=s.divinity=100000;s.medicine=s.syringes=1000;s.returnRequested=false;
            s.divineEventDay=0;
        }
        IEnumerator Start()
        {
            app=GetComponent<GameApp>();var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-qa-output");
            output=at>=0?args[at+1]:Application.temporaryCachePath;Directory.CreateDirectory(output);Application.logMessageReceived+=OnLog;
            while(!UnityEngine.Rendering.SplashScreen.isFinished)yield return null;
            app.enabled=false;yield return Frame();yield return Capture("title");
            Click("새로 시작");yield return Frame();yield return Capture("reset-warning");Click("취소");yield return Frame();
            Click("새로 시작");yield return Frame();Click("모두 초기화하고 시작");yield return Frame();
            Check(app.Game.S.phase==Phase.Morning&&app.Game.S.avatar!=null&&app.Game.S.candidates.Count==0,"Automatic one human start");
            yield return Capture("shelter");
            app.Game.S.phase=Phase.Running;app.Game.S.scheduledConstellation="sejong";app.Game.S.omenShownDay=0;app.Render();yield return Frame();
            Check(app.GetComponentsInChildren<Button>().Count(b=>b.interactable)==1,"Only mandatory omen continue is enabled");
            yield return Capture("omen-locked");Click("계속 진행");yield return Frame();
            app.Game.S.scheduledConstellation=null;app.Game.S.phase=Phase.Morning;app.Game.S.divinity=1000;
            Check(app.Game.Invest(Stat.Attack,100)&&app.Game.UpgradeCost(Stat.Attack)==20&&app.Game.UpgradeCost(Stat.Health)==10,"Independent upgrade tiers");
            app.Navigate("예배방");yield return Frame();yield return Capture("tiered-upgrades");app.Navigate("쉘터");yield return Frame();
            Click("전투준비");yield return Frame();
            Check(!app.GetComponentsInChildren<Button>().Any(b=>b.GetComponentInChildren<Text>().text=="침수된 지하철역"),"Destinations hidden before exploration click");
            Click("탐사");yield return Frame();Click("침수된 지하철역");yield return Frame();yield return Capture("subway-preparation");
            Click("탐사 출발");yield return Frame();app.Game.S.scheduledConstellation=null;app.Render();yield return Frame();
            int stageId=app.GetComponentsInChildren<RectTransform>().Single(r=>r.name=="Location stage").GetInstanceID();
            Check(app.Game.OfferTransit(),"Offer transit");app.Render();yield return Frame();yield return Capture("walking-route");
            app.Game.B.november.rerollEventChance=0;
            double hours=app.Game.S.hours;int destination=app.Game.S.exploration.destination;Click("표시된 길을 거절");yield return Frame();
            Check(app.TransitWalkVisible,"Walking shown after reroll");app.enabled=true;yield return new WaitForSecondsRealtime(.7f);app.enabled=false;
            yield return Capture("walking-animation");Click("넘기기");yield return Frame();
            Check(app.Game.S.hours==hours+1&&app.Game.S.exploration.destination!=destination&&app.Game.S.eventSlots==0,"Transit reroll exactly one hour, no story slot");
            Check(app.GetComponentsInChildren<RectTransform>().Single(r=>r.name=="Location stage").GetInstanceID()==stageId,"Travel and events reuse location stage");
            yield return Capture("route-reroll");
            Click("쉘터로 돌아가기");yield return Frame();Check(app.Game.S.phase==Phase.Report,"Free return from transit");yield return Capture("returned-shelter");
            app.Game.B.november.rerollEventChance=.25;
            Fixture(Area.Subway);app.Game.B.november.rerollEventChance=1;app.Game.S.eventTarget=4;
            Check(app.Game.OfferTransit(),"Story transit offer");app.Render();yield return Frame();Click("표시된 길을 거절");yield return Frame();Click("넘기기");yield return Frame();
            Check(!app.Game.IsTransit(app.Game.S.pending)&&app.Game.S.eventSlots==1&&app.Game.S.exploration.resumeTransit,"Reroll story counts as one actual story");
            yield return Capture("reroll-subway-story");Check(app.Saves.Write(app.Game.S)&&app.LoadGame(),"Reroll story save reload");yield return Frame();
            Click(app.Game.S.pending.choices[2]);yield return Frame();hours=app.Game.S.hours;app.Game.Tick();app.Render();yield return Frame();
            Check(app.Game.IsTransit(app.Game.S.pending)&&app.Game.S.hours==hours&&app.Game.S.eventSlots==1,"Story continues to route without duplicate travel charge");
            app.Game.B.november.rerollEventChance=.25;
            Fixture(Area.Museum);app.Game.S.pending=null;app.Navigate("하루 기록");yield return Frame();yield return Capture("fixed-field-return");
            hours=app.Game.S.hours;Click("지하철역으로 돌아가기");yield return Frame();Check(app.Game.S.hours==hours+.5&&app.Game.S.exploration.current==Area.Subway,"Fixed subway button");
            foreach(string id in new[]{"HOM-D01","HOM-D02","HOM-D03"})
            {
                Fixture(Area.Residence);app.Game.S.pending=null;app.Game.InteractiveCombat=false;app.Game.B.divineSuccessChance=1;
                Check(app.Game.PresentGeneralEvent(id),id);app.Render();yield return Frame();yield return Capture("residence-divine-"+id);
                int before=app.Game.S.divinity;Click(app.Game.S.pending.choices[0]);yield return Frame();Check(app.Game.S.divinity>=before+3&&app.Game.S.divineEventDay==app.Game.S.day,"Residence divinity reward and daily cap");
            }
            app.Game.B.divineSuccessChance=.6;app.Game.InteractiveCombat=true;
            string[] areaEvents={"SUB-N01","MUS-01","MRT-03","ARM-02","SAN-01","FUN-01","HER-01"};
            foreach(string id in areaEvents)
            {
                var e=app.Game.Content.activityEvents.Single(t=>t.id==id);Fixture(e.area);Check(app.Game.PresentGeneralEvent(id),id);app.Render();yield return Frame();yield return Capture("area-"+e.area);
                app.Game.S.pending=null;
            }
            foreach(var e in app.Game.Content.activityEvents.Where(t=>!string.IsNullOrEmpty(t.relicId)))
            {
                Fixture(e.area);app.Game.InteractiveCombat=false;app.Game.S.plot.completed.AddRange(e.prerequisites);app.Game.S.plot.completed.AddRange(e.anyPrerequisites);
                Check(app.Game.PresentGeneralEvent(e.id),e.id);app.Render();yield return Frame();yield return Capture("relic-"+e.relicId);
                Click(app.Game.S.pending.choices[0]);yield return Frame();Check(app.Game.S.relics.Any(r=>r.id==e.relicId),"Relic reward "+e.relicId);
            }
            Check(app.Game.S.relics.Count==10,"Ten relics");
            foreach(var c in app.Game.Content.constellations)
            {
                Fixture(Area.Residence);app.Game.S.phase=Phase.Event;app.Game.S.pending=new ChoiceEvent{id="qa-"+c.id,title=c.title,body=c.story,constellationId=c.id,choices=new[]{"이야기 확인 · 영구 해금"}};
                app.Render();yield return Frame();Check(ConstellationArtLease.ResidentCount==1,"One illustration at a time");
                if(c.id=="napoleon"||c.id=="yog")yield return Capture("constellation-"+c.id);
                Click("이야기 확인");yield return Frame();Check(app.Game.S.permanent.unlocked.Contains(c.id),"Constellation unlocked");
            }
            app.Game.S.phase=Phase.Morning;foreach(var n in app.Game.Content.rareNpcs)app.Game.S.candidates.Add(app.Game.CreateRare(n.id));
            app.Navigate("인재관리실");yield return Frame();yield return Capture("personnel");
            Fixture(Area.Rift);app.Game.InteractiveCombat=true;app.Game.S.avatar.baseAttack=8;
            Check(app.Game.PresentGeneralEvent("RF-005"),"Mixed formation");app.Render();yield return Frame();Click(app.Game.S.pending.choices[0]);yield return Frame();yield return Capture("live-battle");
            Click("연출 건너뛰기");app.enabled=true;float deadline=Time.realtimeSinceStartup+10;
            while(app.Game.S.combat!=null&&!app.Game.S.combat.waiting&&Time.realtimeSinceStartup<deadline)yield return null;
            Check(app.Game.S.combat!=null&&app.Game.S.combat.waiting,"Emergency command reached");yield return new WaitForSecondsRealtime(.8f);yield return Capture("emergency-commands");
            hours=app.Game.S.hours;uint rng=app.Game.S.rng;int money=app.Game.S.divinity;
            app.SkipBattlePresentation();yield return Frame();Check(app.Game.S.hours==hours&&app.Game.S.rng==rng&&app.Game.S.divinity==money,"Skip pauses at command");
            Check(app.Saves.Write(app.Game.S)&&app.LoadGame(),"Live battle save reload");yield return new WaitForSecondsRealtime(.8f);
            Check(app.Game.S.combat.waiting,"Command restored");
            app.Game.S.avatar.baseAttack=10000;app.SkipBattlePresentation();Click("명령 없이 계속");deadline=Time.realtimeSinceStartup+10;
            while(app.BattleVisible&&Time.realtimeSinceStartup<deadline)
            {
                if(app.GetComponentsInChildren<Button>().Any(b=>b.GetComponentInChildren<Text>().text=="명령 없이 계속"))Click("명령 없이 계속");
                yield return null;
            }
            Check(!app.BattleVisible,"Battle finished");app.enabled=false;
            Fixture(Area.Rift);app.Game.S.activeRift=4;app.Game.S.demons[4].progress=50;app.Game.S.avatar.baseAttack=120;app.Game.Battle(EnemyKind.Demon,1,Sin.Gluttony);app.Render();app.enabled=true;
            deadline=Time.realtimeSinceStartup+8;while(!app.Game.S.combat.waiting&&Time.realtimeSinceStartup<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.8f);Check(app.Game.S.combat.enemies.Count==3,"Glutton summons two");yield return Capture("gluttony-summon");
            app.enabled=false;app.BeginNew(1);app.enabled=false;app.Game.InteractiveCombat=false;
            int seedAt=Array.IndexOf(args,"-qa-seed");uint seed=seedAt>=0?uint.Parse(args[seedAt+1]):1;app.BeginNew(seed);app.Game.InteractiveCombat=false;
            int steps=0;while(app.Game.S.phase!=Phase.Victory&&app.Game.S.day<=100)
            {
                CampaignPolicy.Advance(app.Game);app.Game.Validate();if(++steps>20000)throw new InvalidOperationException("Campaign budget");if(steps%8==0)yield return null;
            }
            Check(app.Game.S.phase==Phase.Victory,"Natural campaign victory");app.Game.InteractiveCombat=true;app.Render();yield return Frame();yield return Capture("campaign-victory");
            Check(app.Saves.Write(app.Game.S)&&app.LoadGame(),"Final save reload");yield return Frame();
            string report="result=PASS\nversion=Unified encounter rules5\napproved-events=174; supplemental-rift=4; residence-divine=3\nrelics=10; constellations=20\nactual-pointer-clicks=PASS\ntransit/reroll/free-return=PASS\nlive-animation/command/skip/reload/gluttony=PASS\ncampaign-seed="+seed+"; days="+app.Game.S.day+"; steps="+steps+"\nsave="+app.Saves.PathName;
            File.WriteAllText(Path.Combine(output,"player-smoke.txt"),report);Debug.Log("APOCALYPSE_PLAYER_SMOKE_PASS");Application.Quit(0);
        }
        void OnDestroy(){Application.logMessageReceived-=OnLog;}
    }
}
#endif
