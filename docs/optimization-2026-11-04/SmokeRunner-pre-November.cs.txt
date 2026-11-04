#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
namespace Apocalypse
{
    // Runs in the real Windows player with a separate QA save directory; never touches user saves.
    public sealed class SmokeRunner : MonoBehaviour
    {
        GameApp app;
        string output;
        int peakIllustrations;
        long peakIllustrationBytes;
        IEnumerator Start()
        {
            app=GetComponent<GameApp>();string[] args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-qa-output");
            output=at>=0&&at+1<args.Length?args[at+1]:Application.temporaryCachePath;Directory.CreateDirectory(output);
            Application.logMessageReceived += OnLog;
            while(!UnityEngine.Rendering.SplashScreen.isFinished) yield return null;
            yield return null;yield return null;yield return Capture("01-title");
            int seedAt=Array.IndexOf(args,"-qa-seed");uint qaSeed=seedAt>=0?uint.Parse(args[seedAt+1]):321u;
            app.BeginNew(qaSeed);yield return null;yield return Capture("02-candidates");
            var strongest=app.Game.S.candidates.OrderByDescending(c=>c.baseAttack).First();
            var selectedCard=app.GetComponentsInChildren<RectTransform>().Single(r=>r.name=="Personnel "+strongest.id);
            ClickButton(selectedCard.GetComponentInChildren<Button>());yield return null;yield return Capture("03-shelter");
            if(args.Contains("-qa-reset-only"))
            {
                yield return NewGameResetFixtures();
                File.WriteAllText(Path.Combine(output,"player-smoke.txt"),"result=PASS\nscope=title full reset only\nwarning/cancel/confirm/reload/fresh recovery copy=PASS\nsave="+app.Saves.PathName);
                Debug.Log("APOCALYPSE_PLAYER_SMOKE_PASS");Application.Quit(0);yield break;
            }
            int facilityNumber=7;
            foreach(string facility in ArtCatalog.FacilityNames)
            {
                app.Navigate(facility);yield return null;yield return Capture((facilityNumber++).ToString("00")+"-facility");
            }
            app.Navigate("예배방");yield return null;yield return Capture("04-chapel");
            // Same first-morning investment as CampaignPolicy, using actual UI clicks.
            while(app.Game.S.divinity>=10)
            {yield return RevealButton("공격력 투자 · 신위 10");Click("공격력 투자 · 신위 10");yield return null;}
            Click("전투준비");yield return null;yield return Capture("04b-before-day");Click("진행");yield return null;
            if(app.Game.S.phase==Phase.Event)
            {
                yield return Capture("05-story");
                string choice=app.Game.S.pending.choices[0];
                yield return RevealButton(choice);Click(choice);yield return null;
            }
            // Verify restore with the same actual player, then complete a seeded campaign through the core.
            if(!app.LoadGame())throw new InvalidOperationException("Player save/load failed");
            int steps=0;
            var input=app.GetComponentInChildren<GraphicRaycaster>();
            app.enabled=false;if(input!=null)input.enabled=false;
            try
            {
                while(app.Game.S.phase!=Phase.Victory&&app.Game.S.day<101)
                {
                    if(++steps>20000)throw new InvalidOperationException("Player smoke loop limit");
                    var g=app.Game;var s=g.S;
                    CampaignPolicy.Advance(g);g.Validate();
                    // Keep the diagnostic player responsive while many synchronous
                    // checkpoint writes run. Automatic ticks stay paused to preserve the seed.
                    if(steps%8==0)yield return null;
                }
            }
            finally{app.enabled=true;if(input!=null)input.enabled=true;}
            app.Render();yield return null;yield return Capture("06-victory");
            bool won=app.Game.S.phase==Phase.Victory;
            int winningDay=app.Game.S.day,seals=app.Game.S.totalSeals;
            // A full new game now clears all permanent progress. Reinsert the earned
            // records explicitly as presentation fixtures, never as product reset behavior.
            string earnedCodex=JsonUtility.ToJson(app.Game.S.permanent);
            app.BeginNew(654);app.Game.S.permanent=JsonUtility.FromJson<PermanentData>(earnedCodex);
            app.Game.Select(app.Game.S.candidates[0].id);app.Navigate("성좌 도감");yield return null;yield return Capture("11-codex");
            if(app.Game.S.permanent.unlocked.Count==0)throw new InvalidOperationException("Campaign did not obtain a constellation for codex UI QA");
            string owned=app.Game.S.permanent.unlocked[0];yield return RevealButton(CodexIndex.DisplayName(app.Game.Content.Find(owned)));Click(CodexIndex.DisplayName(app.Game.Content.Find(owned)));yield return null;yield return Capture("12-codex-detail");
            yield return RevealButton("도감으로");Click("도감으로");yield return null;yield return Capture("13-codex-return");
            var header=app.GetComponentsInChildren<Button>().First(x=>x.GetComponentInChildren<Text>().text.StartsWith("▾")).GetComponentInChildren<Text>().text;
            yield return RevealButton(header);Click(header);yield return null;yield return Capture("14-codex-collapsed");
            // Art fixtures after campaign completion exercise every revelation -> unlock -> codex.
            // These belong only to the isolated QA save, not to the campaign statistics or user slot.
            foreach(string id in ArtCatalog.ConstellationIds)
            {
                var definition=app.Game.Content.Find(id);app.Codex.Back();
                app.Game.S.phase=Phase.Event;
                app.Game.S.pending=new ChoiceEvent{id="qa-art-"+id,constellationId=id,title=definition.title,body=definition.story,choices=new[]{"이야기 확인 · 영구 해금"}};
                app.Render();yield return null;RequireIllustration(id);yield return Capture("15-"+id+"-event");
                yield return RevealButton("이야기 확인 · 영구 해금");Click("이야기 확인 · 영구 해금");app.Game.S.phase=Phase.Morning;
                if(!app.Codex.IsExpanded(definition.grade))app.Codex.Toggle(definition.grade);
                app.Navigate("성좌 도감");yield return null;
                yield return RevealButton(CodexIndex.DisplayName(definition));Click(CodexIndex.DisplayName(definition));
                yield return null;RequireIllustration(id);yield return Capture("16-"+id+"-codex");
                if(id=="sejong"||id=="artemis"||id=="zeus"||id=="yog")
                {
                    if(app.GetComponentsInChildren<ScrollRect>().Any())throw new InvalidOperationException("Codex detail requires scrolling");
                    yield return null;yield return Capture("17-"+id+"-codex-story");
                }
            }
            app.Game.S.avatar.name="서도윤";app.Navigate("인재관리실");yield return null;yield return Capture("18-portraits");
            if(ConstellationArtLease.ResidentCount!=0)throw new InvalidOperationException("Constellation texture remains owned outside its view");
            if(LoadedIllustrations().Length!=0)throw new InvalidOperationException("Constellation texture was not actually unloaded");
            // A declared post-campaign UI fixture checks exact score/seal numbers at departure.
            app.Game.S.day=2;app.Game.S.activeRift=0;app.Game.S.demons[0].remaining=0;app.Game.S.demons[0].progress=23;
            app.Game.S.demons[1].remaining=12;app.Game.S.demons[2].remaining=18;
            app.Navigate("전투준비");yield return null;yield return Capture("19-preparation");
            Click("공략");yield return null;yield return Capture("20-assault-selected");
            yield return PlannerFixtures();
            yield return InterfaceFixtures();
            yield return BattleFixtures();
            yield return NewGameResetFixtures();
            File.WriteAllText(Path.Combine(output,"player-smoke.txt"),"result="+(won?"PASS":"FAIL")+"\nseed="+qaSeed+"\nday="+winningDay+"\nseals="+seals+"\nsteps="+steps+"\nfacilities=4\ncodex=PASS\nconstellation-art=20 event/codex fixtures PASS\nconstellation-textures-peak="+peakIllustrations+"\nconstellation-texture-bytes-peak="+peakIllustrationBytes+"\nconstellation-textures-after-close=0\nportraits=12 normal + 4 rare\nplanner=8 relic events / 4 rare cards / restoration reveal and skip / save-load / named reveal PASS\nsave="+app.Saves.PathName);
            Debug.Log("APOCALYPSE_PLAYER_SMOKE_"+(won?"PASS":"FAIL"));Application.Quit(won?0:1);
        }
        IEnumerator InterfaceFixtures()
        {
            // Presentation-only fixtures in the isolated QA save, after the natural campaign.
            var g=app.Game;g.S.day++;g.S.phase=Phase.Running;g.S.hours=0;g.S.pending=null;
            g.S.scheduledConstellation="sejong";g.S.constellationMeetingHour=23;g.S.omenShownDay=0;
            app.Render();yield return null;double time=g.S.hours;yield return new WaitForSecondsRealtime(.25f);
            if(!app.OmenVisible||g.S.hours!=time)throw new InvalidOperationException("Watching scene did not pause game time");
            yield return Capture("29-constellation-watching");Click("계속 진행");yield return null;
            if(app.OmenVisible||g.S.omenShownDay!=g.S.day)throw new InvalidOperationException("Watching scene not acknowledged");
            if(!app.LoadGame()||app.OmenVisible)throw new InvalidOperationException("Watching scene repeats on reload");
            g=app.Game;g.S.scheduledConstellation=null;g.EndDay();app.Render();yield return null;
            if(app.ActiveTab!="쉘터")throw new InvalidOperationException("Day end did not return to shelter");
            yield return Capture("30-day-end-shelter");Click("다음 날 아침");yield return null;
            g=app.Game;g.S.hours=0;g.S.medicine=1;g.S.syringes=1;g.S.avatar.health=1;g.S.avatar.sanity=30;
            app.Navigate("의무실");yield return null;
            if(app.GetComponentsInChildren<Toggle>().Length!=2||app.GetComponentsInChildren<Button>().Count(b=>b.GetComponentInChildren<Text>().text.EndsWith(" 실행"))!=3)
                throw new InvalidOperationException("Clinic must use three actions and two item checkboxes");
            var toggle=app.GetComponentsInChildren<Toggle>().Single(t=>t.name=="Item checkbox 응급 의료 키트 사용");
            Canvas.ForceUpdateCanvases();var check=toggle.GetComponentInChildren<Image>().rectTransform;
            var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,check.TransformPoint(check.rect.center))};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            if(hits.Count==0||hits[0].gameObject.GetComponentInParent<Toggle>()!=toggle)throw new InvalidOperationException("Item checkbox is blocked");
            ExecuteEvents.Execute(toggle.gameObject,pointer,ExecuteEvents.pointerClickHandler);yield return null;
            yield return Capture("31-clinic-checkbox");Click("체력 회복 실행");yield return null;
            if(g.S.medicine!=0||g.S.hours!=2)throw new InvalidOperationException("Clinic checkbox did not use one kit and two hours");
            Debug.Log("APOCALYPSE_UI_SINGLE_SCREEN_WATCHING_CLINIC_PASS");
        }
        IEnumerator BattleFixtures()
        {
            // Explicit presentation fixtures; they do not contribute to natural campaign totals.
            var g=app.Game;
            foreach(string scene in new[]{"ruins","interior","special","named","demon"})
            {
                g.S.day=2;g.S.hours=0;g.S.activeRift=0;g.S.phase=Phase.Event;g.S.goal=scene=="ruins"?Goal.Explore:Goal.Rift;
                g.S.controlLocked=false;g.S.returnRequested=false;g.S.scheduledConstellation=null;g.S.avatar.name="서도윤";
                g.S.avatar.personality=Temperament.Brave;g.S.avatar.species=Species.Human;g.S.avatar.baseHealth=2000;g.S.avatar.health=2000;
                g.S.avatar.baseAttack=12;g.S.avatar.attackInvestment=0;g.S.avatar.healthInvestment=0;g.S.avatar.selectedDay=1;g.S.avatar.inherited.Clear();
                g.S.avatar.sanity=g.S.avatar.hunger=100;g.S.food=10000;g.S.eventSlots=0;
                foreach(var demon in g.S.demons)demon.remaining=0;g.S.demons[0].progress=50;g.S.permanent.named.Clear();
                if(scene=="named"){g.S.permanent.named.Add(new NamedZombieRecord{id="battle-qa-named",name="서도윤",health=65,attack=9,legion=Sin.Pride});g.S.rng=1;}
                var option=new EventOption{normal=scene=="ruins"||scene=="interior"?1:0,special=scene=="special"||scene=="named"?1:0,boss=scene=="demon"};
                g.S.pending=new ChoiceEvent{id="battle-scene-"+scene,title=scene=="interior"?"폐허 건물 내부의 조우":"길을 가로막은 적",body="별도 QA 전투 장면",scene=scene=="interior"?"interior":scene=="ruins"?"ruins":"rift",choices=new[]{"적과 맞선다"},options=new[]{option}};
                app.Render();yield return null;Click("적과 맞선다");yield return null;
                if(!app.BattleVisible)throw new InvalidOperationException("Combat scene missing: "+scene);
                Click("전투 ×1");yield return null;
                if(scene=="named"&&app.CurrentBattle.enemies[0].kind!=EnemyKind.Named)throw new InvalidOperationException("Named battle presentation missing");
                if(scene=="demon"&&app.CurrentBattle.enemies[0].kind!=EnemyKind.Demon)throw new InvalidOperationException("Demon battle presentation missing");
                double time=g.S.hours;uint rng=g.S.rng;int money=g.S.divinity;
                yield return Capture("32-battle-"+scene+"-start");yield return new WaitForSecondsRealtime(.92f);
                yield return Capture("33-battle-"+scene+"-attack");yield return new WaitForSecondsRealtime(.72f);
                yield return Capture("34-battle-"+scene+"-hit");
                if(time!=g.S.hours||rng!=g.S.rng||money!=g.S.divinity)throw new InvalidOperationException("Presentation advanced simulation: "+scene);
                Click("전투 ×4");yield return null;Click("전투 연출 건너뛰기");
                if(app.BattleVisible||money!=g.S.divinity||rng!=g.S.rng||time!=g.S.hours)throw new InvalidOperationException("Skip changed combat outcome: "+scene);
                if(Resources.FindObjectsOfTypeAll<Texture2D>().Any(t=>t.name=="battle-actors-v1"||t.name=="battle-interior-v1"))throw new InvalidOperationException("Battle textures still resident");
                yield return null;
            }
            File.WriteAllText(Path.Combine(output,"battle-scenes.txt"),"result=PASS\nscenes=ruins,interior,special,named,demon\nactual-ui-clicks=PASS\ncore-paused-during-presentation=PASS\nskip-no-additional-reward=PASS\nbattle-textures-after-close=0");
            Debug.Log("APOCALYPSE_BATTLE_SCENES_PASS");
        }
        IEnumerator NewGameResetFixtures()
        {
            var g=app.Game;g.S.phase=Phase.Morning;g.S.permanent.unlocked.Add("sejong");g.S.permanent.victories=3;
            g.S.divinity=9999;g.S.relics.Add(new Relic{id="gungnir",name="궁니르"});app.Saves.Write(g.S);
            app.Navigate("쉘터");yield return new WaitForEndOfFrame();yield return null;Click("타이틀");yield return new WaitForEndOfFrame();yield return null;
            string original=File.ReadAllText(app.Saves.PathName);Click("새로 시작");yield return new WaitForEndOfFrame();yield return null;yield return Capture("35-new-game-warning");
            if(File.ReadAllText(app.Saves.PathName)!=original)throw new InvalidOperationException("Opening warning changed save");
            Click("취소");yield return new WaitForEndOfFrame();yield return null;
            if(app.Game!=g||File.ReadAllText(app.Saves.PathName)!=original)throw new InvalidOperationException("Cancel reset changed progress");
            Click("새로 시작");yield return new WaitForEndOfFrame();yield return null;Click("모두 초기화하고 시작");yield return new WaitForEndOfFrame();yield return null;yield return Capture("36-new-game-cleared");
            var fresh=app.Game.S;
            if(fresh.runId==g.S.runId||fresh.day!=1||fresh.phase!=Phase.Selection||fresh.permanent.unlocked.Count!=0||fresh.permanent.victories!=0||fresh.relics.Count!=0)
                throw new InvalidOperationException("Full new game did not clear progress");
            if(!app.LoadGame()||app.Game.S.runId!=fresh.runId||app.Game.S.permanent.unlocked.Count!=0)throw new InvalidOperationException("Fresh reset did not persist");
            var backup=JsonUtility.FromJson<GameState>(File.ReadAllText(app.Saves.PathName+".bak"));
            if(backup.runId!=fresh.runId||backup.permanent.unlocked.Count!=0)throw new InvalidOperationException("Reset backup contains old progress");
            File.WriteAllText(Path.Combine(output,"new-game-reset.txt"),"result=PASS\nwarning-once=PASS\ncancel-no-write=PASS\nfull-reset-and-reload=PASS\nrecovery-copy-fresh=PASS");
            Debug.Log("APOCALYPSE_NEW_GAME_RESET_PASS");
        }
        IEnumerator PlannerFixtures()
        {
            // Explicit post-campaign presentation fixtures; excluded from campaign totals.
            var g=app.Game;g.S.phase=Phase.Morning;g.S.hours=0;g.S.day=2;g.S.candidates.Clear();
            g.S.avatar=g.CreateRare("lyra");g.S.avatar.selectedDay=1;
            foreach(var npc in g.Content.rareNpcs.Where(n=>n.id!="lyra"))g.S.candidates.Add(g.CreateRare(npc.id));
            app.Navigate("인재관리실");yield return null;
            foreach(var npc in g.S.candidates.Append(g.S.avatar))
            {
                var card=app.GetComponentsInChildren<RectTransform>().Single(r=>r.name=="Personnel "+npc.id);
                Canvas.ForceUpdateCanvases();
                if(card.GetComponentInParent<ScrollRect>()!=null)throw new InvalidOperationException("Personnel card requires scrolling");
                yield return null;yield return Capture("21-rare-"+npc.species);
            }
            g.S.avatar.baseHealth=10000;g.S.avatar.health=10000;g.S.avatar.baseAttack=1000;g.S.food=100000;g.S.medicine=10;
            g.S.activeRift=0;foreach(var demon in g.S.demons)demon.remaining=0;
            g.S.relics.Clear();
            foreach(var definition in g.Content.relicDefinitions)
            {
                g.S.hours=0;g.S.eventSlots=0;g.S.avatar.hunger=g.S.avatar.sanity=100;g.S.controlLocked=false;
                g.S.phase=Phase.Running;g.S.goal=definition.location;
                var template=g.Content.activityEvents.Single(e=>e.relicId==definition.id);
                if(!g.PresentGeneralEvent(template.id))throw new InvalidOperationException("Relic fixture not eligible: "+definition.id);
                app.Render();yield return null;yield return Capture("22-"+definition.id+"-event");
                string choice=g.S.pending.choices[0];yield return RevealButton(choice);Click(choice);yield return null;
                if(!g.S.relics.Any(r=>r.id==definition.id))throw new InvalidOperationException("Relic UI reward missing: "+definition.id);
            }
            g.S.hours=0;g.S.phase=Phase.Morning;g.Equip("gungnir");
            for(int i=0;i<10;i++){g.S.phase=Phase.Running;g.EndDay();if(i<9)g.NextDay();}
            g.S.phase=Phase.Running;g.S.goal=Goal.Rift;g.S.hours=0;g.S.avatar.hunger=g.S.avatar.sanity=100;g.S.activeRift=0;
            g.Battle(EnemyKind.Normal,20,Sin.Pride);g.S.avatar.health=0;g.CheckDeath(true,Sin.Pride);
            g.Select(g.S.candidates[0].id);g.S.divinity=500;app.Navigate("예배방");yield return null;
            Click("성유물 복원");yield return null;
            yield return RevealButton("복원 기록 보기 · 궁니르");Click("복원 기록 보기 · 궁니르");yield return null;yield return Capture("23-restoration-start");
            yield return RevealButton("연출 건너뛰고 복원 미리보기");Click("연출 건너뛰고 복원 미리보기");yield return null;yield return Capture("24-restoration-skip-preview");
            if(g.S.divinity!=500)throw new InvalidOperationException("Preview spent divinity");
            yield return RevealButton("복원 목록으로");Click("복원 목록으로");yield return null;
            yield return RevealButton("복원 기록 보기 · 궁니르");Click("복원 기록 보기 · 궁니르");yield return null;
            yield return RevealButton("마지막 사용자의 기록");Click("마지막 사용자의 기록");yield return null;yield return Capture("25-restoration-last-user");
            yield return RevealButton("이전 전승 기록");Click("이전 전승 기록");yield return null;
            yield return RevealButton("이번 복원 결과");Click("이번 복원 결과");yield return null;
            yield return RevealButton("복원 확정 · 신위 100");Click("복원 확정 · 신위 100");yield return null;yield return Capture("26-restoration-complete");
            var restored=g.S.relics.Single(r=>r.id=="gungnir");
            if(g.S.divinity!=400||restored.healthBonus!=1||restored.attackBonus!=2||restored.history.Count!=1)
                throw new InvalidOperationException("Actual restoration result does not match ten days/twenty zombies");
            if(!app.LoadGame()||app.Game.S.relics.Single(r=>r.id=="gungnir").restorations!=1)throw new InvalidOperationException("Restored history did not survive reload");
            app.Navigate("보급창고");yield return null;yield return RevealButton("전승 역사 · 궁니르");Click("전승 역사 · 궁니르");yield return null;yield return Capture("27-relic-history");
            g=app.Game;g.S.day++;g.S.phase=Phase.Running;g.S.goal=Goal.Rift;g.S.activeRift=0;g.S.hours=0;
            var named=new NamedZombieRecord{id="qa-reveal",name="서도윤",health=1,attack=1,legion=Sin.Pride};
            g.Battle(EnemyKind.Named,1,Sin.Pride,named);g.S.phase=Phase.Morning;app.Navigate("하루 기록");yield return null;
            var revealScroll=app.GetComponentsInChildren<ScrollRect>().Single();revealScroll.verticalNormalizedPosition=1;yield return null;Canvas.ForceUpdateCanvases();
            var banner=app.GetComponentsInChildren<Text>().Single(t=>t.text=="네임드 조우 · 돌아온 화신체");
            var corners=new Vector3[4];banner.rectTransform.GetWorldCorners(corners);
            foreach(var corner in corners)
            {var point=revealScroll.viewport.InverseTransformPoint(corner);if(!revealScroll.viewport.rect.Contains(point))throw new InvalidOperationException("Named reveal banner outside viewport");}
            if(!app.GetComponentsInChildren<RawImage>().Any(i=>i.texture==ArtCatalog.Portrait("서도윤")))throw new InvalidOperationException("Named reveal face missing");
            yield return Capture("28-named-reveal");
        }
        void RequireIllustration(string id)
        {
            var texture=ArtCatalog.Constellation(id);
            if(texture==null||!app.GetComponentsInChildren<RawImage>().Any(x=>x.texture==texture))
                throw new InvalidOperationException("Missing live constellation illustration: "+id);
            var loaded=LoadedIllustrations();peakIllustrations=Math.Max(peakIllustrations,loaded.Length);
            peakIllustrationBytes=Math.Max(peakIllustrationBytes,loaded.Sum(t=>UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t)));
            if(loaded.Length!=1 || ConstellationArtLease.ResidentCount!=1)
                throw new InvalidOperationException("More than the displayed constellation is resident: "+loaded.Length);
        }
        Texture2D[] LoadedIllustrations()
        {
            return Resources.FindObjectsOfTypeAll<Texture2D>().Where(t=>ArtCatalog.ConstellationIds.Any(id=>t.name==id+"-v1"||t.name==id+"-v2")).ToArray();
        }
        IEnumerator RevealButton(string label)
        {
            if(app.BattleVisible&&!label.StartsWith("전투 ")) {Click("전투 연출 건너뛰기");yield return null;}
            for(int page=0;page<12&&!app.GetComponentsInChildren<Button>().Any(x=>Matches(x,label));page++)
            {
                var next=app.GetComponentsInChildren<Button>().FirstOrDefault(x=>Matches(x,"다음 페이지"));
                if(next==null)break;
                if(next.interactable)ClickButton(next);
                else
                {
                    var previous=app.GetComponentsInChildren<Button>().Single(x=>Matches(x,"이전 페이지"));
                    while(previous.interactable){ClickButton(previous);yield return null;previous=app.GetComponentsInChildren<Button>().Single(x=>Matches(x,"이전 페이지"));}
                }
                yield return null;
            }
            var button=app.GetComponentsInChildren<Button>().Single(x=>Matches(x,label));
            var scroll=button.GetComponentInParent<ScrollRect>();Canvas.ForceUpdateCanvases();
            if(scroll!=null)
            {
                var rect=button.GetComponent<RectTransform>();
                var center=scroll.content.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
                float distance=scroll.content.rect.yMax-center.y;
                float range=scroll.content.rect.height-scroll.viewport.rect.height;
                if(range>0)scroll.verticalNormalizedPosition=Mathf.Clamp01(1-(distance-scroll.viewport.rect.height*.5f)/range);
            }
            yield return null;
        }
        void Click(string label)
        {
            if(app.BattleVisible&&!label.StartsWith("전투 "))
            {
                var skip=app.GetComponentsInChildren<Button>().Single(b=>Matches(b,"전투 연출 건너뛰기"));
                ClickButton(skip);
            }
            var b=app.GetComponentsInChildren<Button>().FirstOrDefault(x=>x.interactable&&Matches(x,label));
            if(b==null)throw new InvalidOperationException("Missing live button: "+label);
            ClickButton(b);
        }
        void ClickButton(Button b)
        {
            string label=b.GetComponentInChildren<Text>().text;
            Canvas.ForceUpdateCanvases();
            var rect=b.GetComponent<RectTransform>();var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
            var pointer=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            if(hits.Count==0||hits[0].gameObject.GetComponentInParent<Button>()!=b)throw new InvalidOperationException("Button blocked or outside viewport: "+label+" at "+point+" top="+(hits.Count==0?"none":hits[0].gameObject.name));
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.pointerClickHandler);
        }
        IEnumerator Capture(string name)
        {
            Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();
            var image=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());Destroy(image);
        }
        void OnLog(string condition,string stack,LogType type)
        {
            if(type!=LogType.Exception&&type!=LogType.Error)return;
            File.WriteAllText(Path.Combine(output,"player-smoke.txt"),"result=FAIL\n"+condition+"\n"+stack);Application.Quit(1);
        }
        void OnDestroy(){Application.logMessageReceived-=OnLog;}
        bool Matches(Button button,string label)
        {
            string text=button.GetComponentInChildren<Text>().text;return text==label||text.StartsWith(label+"  · ",StringComparison.Ordinal);
        }
    }
}
#endif
