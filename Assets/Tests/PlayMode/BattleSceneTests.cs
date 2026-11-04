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
    void ResolveFight(GameObject root,GameApp app,string template)
    {
        Assert.IsTrue(app.Game.PresentGeneralEvent(template));app.Render();
        int index=Array.FindIndex(app.Game.S.pending.options,o=>o.normal>0||o.special>0);
        string text=app.Game.S.pending.choices[index];
        root.GetComponentsInChildren<Button>().Single(b=>{string s=b.GetComponentInChildren<Text>().text;return s==text||s.StartsWith(text+"  · ");}).onClick.Invoke();
    }
    [UnityTest] public IEnumerator ActualCombatAnimatesAndShakesOnlyTheWorldAndSkipCannotPayTwice()
    {
        string temp=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"BattleScene-"+Guid.NewGuid().ToString("N"));
        var root=new GameObject("Battle scene test");var app=root.AddComponent<GameApp>();app.saveDirectoryOverride=temp;yield return null;
        app.BeginNew(412);app.Game.Select(app.Game.S.candidates[0].id);var g=app.Game;
        g.S.day=2;g.S.activeRift=0;g.S.phase=Phase.Running;g.S.goal=Goal.Rift;g.S.hours=0;g.S.food=1000;
        g.S.avatar.personality=Temperament.Brave;g.S.avatar.baseHealth=1000;g.S.avatar.health=1000;g.S.avatar.baseAttack=15;g.S.avatar.sanity=100;
        ResolveFight(root,app,"rift-special");Assert.IsTrue(app.BattleVisible);yield return null;
        var snapshot=app.CurrentBattle;Assert.AreEqual(EnemyKind.Special,snapshot.enemies[0].kind);
        var sprite=root.GetComponentsInChildren<RectTransform>().Single(r=>r.name=="Battle actor 0");Vector2 home=sprite.anchoredPosition;
        var world=root.GetComponentsInChildren<RectTransform>().Single(r=>r.name=="Battle world");
        var bar=root.GetComponentsInChildren<RectTransform>().Single(r=>r.name.StartsWith("Combat bar "+snapshot.avatarName));Vector3 hud=bar.position;
        double hours=g.S.hours;uint rng=g.S.rng;int money=g.S.divinity;bool moved=false,shook=false;
        for(int i=0;i<150;i++){yield return null;moved|=Vector2.Distance(home,sprite.anchoredPosition)>20;shook|=world.anchoredPosition.sqrMagnitude>.1f;Assert.AreEqual(hud,bar.position);if(moved&&shook)break;}
        Assert.IsTrue(moved,"공격할 때 캐릭터가 이동해야 합니다.");Assert.IsTrue(shook,"피격 시 전투 화면이 흔들려야 합니다.");
        Assert.AreEqual(hours,g.S.hours);Assert.AreEqual(rng,g.S.rng);Assert.AreEqual(money,g.S.divinity);
        Find(root,"전투 ×4").onClick.Invoke();Find(root,"전투 연출 건너뛰기").onClick.Invoke();
        Assert.IsFalse(app.BattleVisible);Assert.AreEqual(hours,g.S.hours);Assert.AreEqual(money,g.S.divinity);Assert.AreEqual(rng,g.S.rng);
        Assert.IsTrue(app.LoadGame());Assert.IsFalse(app.BattleVisible);Assert.AreEqual(money,app.Game.S.divinity);
        Assert.IsFalse(Resources.FindObjectsOfTypeAll<Texture2D>().Any(t=>t.name=="battle-actors-v1"));
        UnityEngine.Object.Destroy(root);yield return null;System.IO.Directory.Delete(temp,true);
    }
    [UnityTest] public IEnumerator InteriorEventShowsItsOwnBackdropAndClosesWithoutRecalculatingCombat()
    {
        string temp=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"BattleInterior-"+Guid.NewGuid().ToString("N"));
        var root=new GameObject("Interior battle");var app=root.AddComponent<GameApp>();app.saveDirectoryOverride=temp;yield return null;
        app.BeginNew(413);app.Game.Select(app.Game.S.candidates[0].id);var g=app.Game;g.S.day=2;g.S.activeRift=0;g.S.phase=Phase.Running;g.S.goal=Goal.Rift;
        g.S.avatar.personality=Temperament.Brave;g.S.avatar.baseAttack=1000;g.S.avatar.baseHealth=1000;g.S.avatar.health=1000;g.S.food=1000;g.S.medicine=1000;
        ResolveFight(root,app,"R-04");yield return null;Assert.AreEqual("interior",app.CurrentBattle.scene);
        Assert.IsTrue(root.GetComponentsInChildren<RawImage>().Any(i=>i.texture!=null&&i.texture.name=="battle-interior-v1"));
        int kills=g.S.totalKills;uint rng=g.S.rng;double time=g.S.hours;
        Find(root,"전투 ×4").onClick.Invoke();float until=Time.realtimeSinceStartup+5;
        while(app.BattleVisible&&Time.realtimeSinceStartup<until)yield return null;
        Assert.IsFalse(app.BattleVisible);Assert.AreEqual(kills,g.S.totalKills);Assert.AreEqual(rng,g.S.rng);Assert.AreEqual(time,g.S.hours);
        Assert.IsFalse(Resources.FindObjectsOfTypeAll<Texture2D>().Any(t=>t.name=="battle-interior-v1"||t.name=="battle-actors-v1"));
        UnityEngine.Object.Destroy(root);yield return null;System.IO.Directory.Delete(temp,true);
    }
}
