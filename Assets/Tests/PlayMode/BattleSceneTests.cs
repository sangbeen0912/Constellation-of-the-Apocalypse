using System;
using System.Collections;
using System.Linq;
using Apocalypse;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
public class BattleSceneTests
{
    Button Find(GameObject root,string text){return root.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>().text==text);}
    void Prepare(GameApp app)
    {
        var g=app.Game;g.S.day=2;g.S.activeRift=0;g.S.phase=Phase.Running;g.S.goal=Goal.Rift;g.S.exploration.current=Area.Rift;
        g.S.avatar.personality=Temperament.Brave;g.S.avatar.baseHealth=1000;g.S.avatar.health=1000;g.S.avatar.baseAttack=15;g.S.avatar.sanity=100;g.S.food=1000;g.S.medicine=g.S.syringes=100;
    }
    void ResolveFight(GameObject root,GameApp app,string template)
    {
        Assert.IsTrue(app.Game.PresentGeneralEvent(template));app.Render();
        int index=Array.FindIndex(app.Game.S.pending.options,o=>o.normal>0||o.special>0);
        string text=app.Game.S.pending.choices[index];
        root.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>().text.StartsWith(text)).onClick.Invoke();
    }
    [UnityTest] public IEnumerator ActualLiveCombatAnimatesAndCommandWaitPausesCoreEvenWhenSkipping()
    {
        string temp=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"BattleScene-"+Guid.NewGuid().ToString("N"));
        var root=new GameObject("Battle scene test");var app=root.AddComponent<GameApp>();app.saveDirectoryOverride=temp;yield return null;
        app.BeginNew(412);Prepare(app);ResolveFight(root,app,"RF-005");yield return null;
        Assert.IsTrue(app.BattleVisible);Assert.AreEqual(1,app.Game.S.combat.enemies.Count(e=>e.kind==EnemyKind.Special));
        Assert.IsFalse(root.GetComponentsInChildren<RectTransform>().Any(r=>r.name=="Battle actor -1"),"아군 전신은 전투에 표시하지 않습니다.");
        var hero=root.GetComponentsInChildren<RectTransform>().First(r=>r.name.StartsWith("Battle actor "));
        var world=root.GetComponentsInChildren<RectTransform>().Single(r=>r.name=="Battle world");
        var bar=root.GetComponentsInChildren<RectTransform>().First(r=>r.name.StartsWith("Combat bar "));
        var flash=root.GetComponentsInChildren<Image>().Single(i=>i.name=="Player damage flash");
        Vector2 home=hero.anchoredPosition;Vector3 hud=bar.position;bool moved=false,shook=false,flashed=false;float until=Time.realtimeSinceStartup+5;
        while(Time.realtimeSinceStartup<until&&app.Game.S.combat!=null){yield return null;moved|=hero.localScale.x>1.02f||Vector2.Distance(home,hero.anchoredPosition)>1;shook|=world.anchoredPosition.sqrMagnitude>.1f;flashed|=flash.color.a>0&&flash.color.r>.5f;Assert.AreEqual(hud,bar.position);if(moved&&shook&&flashed)break;}
        Assert.IsTrue(moved);Assert.IsTrue(shook);Assert.IsTrue(flashed);
        app.SkipBattlePresentation();app.Game.S.combat.waiting=true;app.Game.S.combat.prompt="검증용 위기";yield return new WaitForSecondsRealtime(.8f);
        Assert.IsNotNull(root.GetComponentsInChildren<RectTransform>().Single(r=>r.name=="Emergency commands"));
        double hours=app.Game.S.hours;uint rng=app.Game.S.rng;int money=app.Game.S.divinity;
        app.SkipBattlePresentation();yield return new WaitForSecondsRealtime(.15f);
        Assert.AreEqual(hours,app.Game.S.hours);Assert.AreEqual(rng,app.Game.S.rng);Assert.AreEqual(money,app.Game.S.divinity);Assert.IsTrue(app.Game.S.combat.waiting);
        Assert.IsTrue(app.Saves.Write(app.Game.S));Assert.IsTrue(app.LoadGame());Assert.IsTrue(app.Game.S.combat.waiting);Assert.AreEqual(hours,app.Game.S.hours);
        UnityEngine.Object.Destroy(root);yield return null;System.IO.Directory.Delete(temp,true);
    }
    [UnityTest] public IEnumerator InteriorBackdropFinishesRealCombatAndCannotPayAgainAfterReload()
    {
        string temp=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"BattleInterior-"+Guid.NewGuid().ToString("N"));
        var root=new GameObject("Interior battle");var app=root.AddComponent<GameApp>();app.saveDirectoryOverride=temp;yield return null;
        app.BeginNew(413);Prepare(app);app.Game.S.avatar.baseAttack=1000;ResolveFight(root,app,"R-04");yield return null;
        Assert.IsTrue(root.GetComponentsInChildren<RawImage>().Any(i=>i.texture!=null&&i.texture.name=="battle-interior-v1"));
        Find(root,"연출 건너뛰기 · 명령에서 정지").onClick.Invoke();float until=Time.realtimeSinceStartup+8;
        while(app.BattleVisible&&Time.realtimeSinceStartup<until)yield return null;
        Assert.IsFalse(app.BattleVisible);app.enabled=false;
        Assert.IsTrue(app.Game.S.relics.Any(r=>r.id=="necronomicon"));int kills=app.Game.S.totalKills,money=app.Game.S.divinity;
        Assert.IsTrue(app.Saves.Write(app.Game.S));Assert.IsTrue(app.LoadGame());Assert.IsNull(app.Game.S.combat);
        Assert.AreEqual(kills,app.Game.S.totalKills);Assert.AreEqual(money,app.Game.S.divinity);Assert.AreEqual(1,app.Game.S.relics.Count(r=>r.id=="necronomicon"));
        Assert.IsFalse(Resources.FindObjectsOfTypeAll<Texture2D>().Any(t=>t.name=="battle-interior-v1"||t.name=="encounter-enemies-v1"));
        UnityEngine.Object.Destroy(root);yield return null;System.IO.Directory.Delete(temp,true);
    }
}
