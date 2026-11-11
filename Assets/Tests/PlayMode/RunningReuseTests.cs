using System;
using System.Collections;
using System.Linq;
using Apocalypse;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
public class RunningReuseTests
{
    [UnityTest] public IEnumerator CachedHudRefreshesChangedResourcesHealthAndClockAcrossCombat()
    {
        string temp=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"HudCache-"+Guid.NewGuid().ToString("N"));
        var root=new GameObject("HUD cache");var app=root.AddComponent<GameApp>();app.saveDirectoryOverride=temp;yield return null;app.BeginNew(923);app.enabled=false;
        var g=app.Game;g.S.day=2;g.S.phase=Phase.Running;g.S.goal=Goal.Explore;g.S.activeRift=0;g.S.exploration.current=Area.Residence;
        g.S.scheduledConstellation=null;g.S.avatar.personality=Temperament.Brave;g.S.avatar.baseHealth=100;g.S.avatar.health=80;g.S.avatar.baseAttack=10000;g.S.avatar.sanity=g.S.avatar.hunger=100;
        app.Navigate("하루 기록");yield return null;
        var stage=root.GetComponentsInChildren<RectTransform>().Single(r=>r.name=="Location stage");int id=stage.GetInstanceID();
        g.S.avatar.healthInvestment=10;g.S.divinity=1234;g.S.food=4321;g.S.medicine=21;g.S.hours=2.5;g.S.controlLocked=true;
        app.Render();yield return null;
        var text=root.GetComponentsInChildren<Text>();Assert.IsTrue(text.Any(t=>t.text=="체력  80 / 110"));Assert.IsTrue(text.Any(t=>t.text=="신위  1234"));
        Assert.IsTrue(text.Any(t=>t.text=="식량  4321"));Assert.IsTrue(text.Any(t=>t.text=="의료품  21"));Assert.IsTrue(text.Any(t=>t.text=="개입권 잠김"));
        Assert.IsTrue(text.Any(t=>t.text.Contains("11:30")));Assert.AreEqual(id,stage.GetInstanceID());
        g.S.controlLocked=false;g.Battle(EnemyKind.Normal,1,Sin.Pride);g.S.combat.waiting=true;app.Render();
        typeof(GameApp).GetMethod("UpdateBattlePresentation",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(app,null);
        Assert.IsTrue(root.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("남은")&&t.text.EndsWith("· 전투")));
        g.ContinueCombat();app.SkipBattlePresentation();app.enabled=true;float limit=Time.realtimeSinceStartup+6;
        while(app.BattleVisible&&Time.realtimeSinceStartup<limit)yield return null;
        app.enabled=false;Assert.IsFalse(app.BattleVisible);Assert.AreEqual(id,stage.GetInstanceID());
        Assert.IsFalse(root.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("남은")&&t.text.EndsWith("· 전투")));
        UnityEngine.Object.Destroy(root);yield return null;System.IO.Directory.Delete(temp,true);
    }
    [UnityTest] public IEnumerator LogCacheUpdatesAtCapacityAndWhenAnExistingEntryChanges()
    {
        string temp=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"LogCache-"+Guid.NewGuid().ToString("N"));
        var root=new GameObject("Log cache");var app=root.AddComponent<GameApp>();app.saveDirectoryOverride=temp;yield return null;app.BeginNew(924);app.enabled=false;
        var g=app.Game;g.S.day=2;g.S.phase=Phase.Running;g.S.goal=Goal.Explore;g.S.exploration.current=Area.Residence;g.S.scheduledConstellation=null;
        for(int i=0;i<350;i++)g.Log("기록 "+i);
        typeof(GameApp).GetField("visibleLogs",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(app,g.S.logs.Count);
        app.Navigate("하루 기록");yield return null;
        int stage=root.GetComponentsInChildren<RectTransform>().Single(r=>r.name=="Location stage").GetInstanceID();
        g.Log("전투 시작 · 망자 · 균열 진행도");Assert.AreEqual(350,g.S.logs.Count);app.Render();yield return null;
        Assert.IsTrue(root.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("전투 시작 · 일반 좀비 · 군단 봉쇄 돌파")));
        g.S.logs[g.S.logs.Count-20]="동일 개수의 수정 기록";app.Render();yield return null;
        Assert.IsTrue(root.GetComponentsInChildren<Text>().Any(t=>t.text=="동일 개수의 수정 기록"));
        Assert.AreEqual(60,root.GetComponentsInChildren<Text>().Count(t=>t.name=="Observation log row"));
        Assert.AreEqual(stage,root.GetComponentsInChildren<RectTransform>().Single(r=>r.name=="Location stage").GetInstanceID());
        g.S.phase=Phase.Morning;app.Navigate("하루 기록");yield return null;
        Assert.IsTrue(root.GetComponentsInChildren<Text>().Any(t=>t.text=="동일 개수의 수정 기록"));
        UnityEngine.Object.Destroy(root);yield return null;System.IO.Directory.Delete(temp,true);
    }
    [UnityTest] public IEnumerator EventsAndCombatKeepTheSameLocationAndLogPanelsAndLockOtherButtons()
    {
        string temp=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"UnifiedField-"+Guid.NewGuid().ToString("N"));
        var root=new GameObject("Unified field QA");var app=root.AddComponent<GameApp>();app.saveDirectoryOverride=temp;yield return null;app.BeginNew(542);app.enabled=false;
        var g=app.Game;g.S.day=2;g.S.phase=Phase.Running;g.S.goal=Goal.Explore;g.S.activeRift=0;g.S.exploration.current=Area.Residence;
        g.S.avatar.personality=Temperament.Brave;g.S.avatar.baseHealth=2000;g.S.avatar.health=2000;g.S.avatar.baseAttack=8;g.S.avatar.hunger=g.S.avatar.sanity=100;g.S.scheduledConstellation=null;
        app.Navigate("하루 기록");yield return null;
        int stage=root.GetComponentsInChildren<RectTransform>().Single(r=>r.name=="Location stage").GetInstanceID();
        int logs=root.GetComponentsInChildren<RectTransform>().Single(r=>r.name=="Day observation").GetInstanceID();
        Assert.IsTrue(g.PresentGeneralEvent("HOM-D01"));app.Render();yield return null;
        var pane=root.GetComponentsInChildren<RectTransform>().Single(r=>r.name=="Event and command pane");
        foreach(var b in root.GetComponentsInChildren<Button>().Where(b=>!b.transform.IsChildOf(pane)))Assert.IsFalse(b.interactable,b.name);
        Assert.AreEqual(stage,root.GetComponentsInChildren<RectTransform>().Single(r=>r.name=="Location stage").GetInstanceID());
        g.Resolve(g.S.pending.id,2);app.Render();Assert.IsTrue(g.PresentGeneralEvent("HOM-D03"));app.Render();yield return null;
        root.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>().text.StartsWith(g.S.pending.choices[0])).onClick.Invoke();yield return null;
        Assert.IsTrue(app.BattleVisible);Assert.AreEqual(stage,root.GetComponentsInChildren<RectTransform>().Single(r=>r.name=="Location stage").GetInstanceID());
        Assert.AreEqual(logs,root.GetComponentsInChildren<RectTransform>().Single(r=>r.name=="Day observation").GetInstanceID());
        Assert.IsFalse(root.GetComponentsInChildren<RectTransform>().Any(r=>r.name=="Battle actor -1"));
        Assert.IsTrue(root.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("남은")&&t.text.Contains("시간")));
        UnityEngine.Object.Destroy(root);yield return null;System.IO.Directory.Delete(temp,true);
    }
    [UnityTest] public IEnumerator AutomaticActionsReuseTheMainScreenAndRefreshVisibleHunger()
    {
        string temp=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"RunningReuse-"+Guid.NewGuid().ToString("N"));
        var root=new GameObject("Running reuse");var app=root.AddComponent<GameApp>();app.saveDirectoryOverride=temp;yield return null;app.BeginNew(412);
        var g=app.Game;g.S.day=2;g.S.phase=Phase.Running;g.S.goal=Goal.Explore;g.S.activeRift=-1;g.S.exploration.current=Area.Residence;
        g.S.scheduledConstellation=null;g.S.avatar.hunger=g.S.avatar.sanity=100;g.S.food=1000;g.S.nextImportantHour=100;g.S.eventTarget=4;g.S.avatar.personality=Temperament.Brave;
        app.Navigate("하루 기록");yield return null;var main=root.GetComponentsInChildren<RectTransform>().Single(r=>r.name=="Main");int id=main.GetInstanceID(),before=g.S.totalActions;
        float limit=Time.realtimeSinceStartup+8;while(g.S.totalActions<before+3&&Time.realtimeSinceStartup<limit)yield return null;
        app.enabled=false;Assert.GreaterOrEqual(g.S.totalActions,before+3);Assert.AreEqual(id,root.GetComponentsInChildren<RectTransform>().Single(r=>r.name=="Main").GetInstanceID());
        Assert.IsTrue(root.GetComponentsInChildren<Text>().Any(t=>t.text=="허기  "+Rules.Round(g.S.avatar.hunger)+" / 100"));
        UnityEngine.Object.Destroy(root);yield return null;System.IO.Directory.Delete(temp,true);
    }
}
