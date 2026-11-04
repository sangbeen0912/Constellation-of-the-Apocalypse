using System.Linq;
using Apocalypse;
using NUnit.Framework;
using UnityEngine;
public class CombatPresentationTests
{
    GameEngine Veteran()
    {
        var g=GameEngine.New(790,GameData.LoadBalance(),GameData.LoadContent());if(g.S.avatar==null)g.Select(g.S.candidates[0].id);
        g.S.day=2;g.S.activeRift=0;g.S.goal=Goal.Rift;g.S.phase=Phase.Running;g.S.hours=0;
        g.S.avatar.baseHealth=1000;g.S.avatar.health=1000;g.S.avatar.baseAttack=18;g.S.food=1000;g.S.avatar.sanity=100;
        g.S.demons[0].progress=50;return g;
    }
    [TestCase(EnemyKind.Normal)] [TestCase(EnemyKind.Special)] [TestCase(EnemyKind.Named)] [TestCase(EnemyKind.Demon)]
    public void ObservingCombatNeverChangesRandomStateRewardsOrTimeAndCapturesActualHits(EnemyKind kind)
    {
        var g=Veteran();var clone=new GameEngine(JsonUtility.FromJson<GameState>(JsonUtility.ToJson(g.S)),GameData.LoadBalance(),GameData.LoadContent());
        CombatReplay replay=null;int calls=0;g.CombatRecorded+=r=>{replay=r;calls++;};
        var first=new NamedZombieRecord{id="named",name="서도윤",health=65,attack=7,legion=Sin.Pride};
        var second=new NamedZombieRecord{id="named",name="서도윤",health=65,attack=7,legion=Sin.Pride};
        bool result=g.Battle(kind,1,Sin.Pride,first),other=clone.Battle(kind,1,Sin.Pride,second);
        Assert.AreEqual(other,result);Assert.AreEqual(JsonUtility.ToJson(clone.S),JsonUtility.ToJson(g.S),"표현 관찰 때문에 규칙 결과가 달라지면 안 됩니다.");
        Assert.AreEqual(1,calls);Assert.AreEqual(kind,replay.enemies.Single().kind);Assert.AreEqual(1000,replay.health);
        Assert.AreEqual(g.S.avatar.health,replay.finalHealth);Assert.AreEqual("전투 승리",replay.outcome);
        Assert.IsTrue(replay.moments.Any(m=>m.kind==CombatMomentKind.AvatarAttack));
        int originalHp=replay.enemies[0].health;
        Assert.AreEqual(originalHp-replay.moments.Where(m=>m.kind==CombatMomentKind.AvatarAttack).Sum(m=>m.amount)<=0,true);
        Assert.AreEqual(0,replay.moments.Last(m=>m.kind==CombatMomentKind.AvatarAttack).health);
    }
    [Test] public void MixedFormationHasOneSpecialAndStableTargetsEvenAfterDeaths()
    {
        var g=Veteran();CombatReplay replay=null;g.CombatRecorded+=r=>replay=r;
        g.BattleFormation(3,9,Sin.Pride,false);
        Assert.AreEqual(4,replay.enemies.Count);Assert.AreEqual(1,replay.enemies.Count(e=>e.kind==EnemyKind.Special));
        CollectionAssert.AreEquivalent(new[]{0,1,2,3},replay.moments.Where(m=>m.kind==CombatMomentKind.AvatarAttack).Select(m=>m.target).Distinct());
    }
    [Test] public void DeathAndRecoveryUseSnapshotsAndInvalidCombatHasNoScene()
    {
        var g=Veteran();g.S.avatar=g.CreateRare("beyr");g.S.avatar.selectedDay=1;g.S.avatar.health=40;g.S.avatar.baseAttack=1;g.S.food=1000;
        CombatReplay replay=null;int calls=0;g.CombatRecorded+=r=>{replay=r;calls++;};
        g.Battle(EnemyKind.Special,1,Sin.Pride);
        Assert.IsTrue(replay.moments.Any(m=>m.kind==CombatMomentKind.Recovery));Assert.IsTrue(replay.died);Assert.IsNotEmpty(replay.avatarName);
        Assert.AreEqual(1,calls);Assert.IsFalse(g.Battle(EnemyKind.Normal,1,Sin.Pride));Assert.AreEqual(1,calls);
    }
    [Test] public void EventBackgroundsAreExplicitAndNeverLeakToTheNextBattle()
    {
        var g=Veteran();g.S.avatar.baseAttack=1000;g.S.divinity=1000;g.S.medicine=1000;g.S.syringes=1000;
        CombatReplay replay=null;g.CombatRecorded+=r=>replay=r;
        Assert.IsTrue(g.PresentGeneralEvent("R-04"));int fight=System.Array.FindIndex(g.S.pending.options,o=>o.normal>0||o.special>0);
        Assert.GreaterOrEqual(fight,0);g.Resolve(g.S.pending.id,fight);Assert.AreEqual("interior",replay.scene);Assert.AreEqual("읽혀서는 안 되는 기록",replay.eventTitle);
        g.S.phase=Phase.Running;g.S.hours=0;g.S.avatar.sanity=g.S.avatar.hunger=100;g.Battle(EnemyKind.Normal,1,Sin.Pride);
        Assert.AreEqual("rift",replay.scene);Assert.IsNull(replay.eventTitle);
        foreach(var e in g.Content.activityEvents)Assert.IsTrue(new[]{"ruins","interior","chapel","rift"}.Contains(e.scene)||e.scene.StartsWith("area:"),e.id);
    }
}
