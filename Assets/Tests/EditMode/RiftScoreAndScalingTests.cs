using System.Linq;
using Apocalypse;
using NUnit.Framework;
using UnityEngine;

public class RiftScoreAndScalingTests
{
    GameEngine g;
    [SetUp] public void Setup()
    {
        g=GameEngine.New(832,GameData.LoadBalance(),GameData.LoadContent());if(g.S.avatar==null)g.Select(g.S.candidates[0].id);
        g.S.day=2;g.S.activeRift=0;g.S.phase=Phase.Running;g.S.goal=Goal.Explore;
        g.S.avatar.baseHealth=100000;g.S.avatar.health=100000;g.S.avatar.baseAttack=1000;
        g.S.avatar.hunger=g.S.avatar.sanity=100;g.S.food=100000;
    }
    [Test] public void SpecialIsThreePointsTotalAndOneEnemyWithSeparateMoney()
    {
        int money=g.S.divinity;Assert.IsTrue(g.Battle(EnemyKind.Special,5,Sin.Pride));
        Assert.AreEqual(3,g.S.demons[0].progress);Assert.AreEqual(1,g.S.totalKills);Assert.AreEqual(money+15,g.S.divinity);
    }
    [Test] public void NamedIsThreePointsOnlyInRiftAndCannotPayTwice()
    {
        var named=new NamedZombieRecord{id="old",name="이전 화신",health=41,attack=7,legion=Sin.Pride};
        int money=g.S.divinity;Assert.IsFalse(g.Battle(EnemyKind.Named,1,Sin.Pride,named));
        Assert.AreEqual(0,g.S.demons[0].progress);Assert.AreEqual(money,g.S.divinity);
        g.S.goal=Goal.Rift;Assert.IsTrue(g.Battle(EnemyKind.Named,1,Sin.Pride,named));
        Assert.AreEqual(3,g.S.demons[0].progress);Assert.AreEqual(money+30,g.S.divinity);
        Assert.IsFalse(g.Battle(EnemyKind.Named,1,Sin.Pride,named));Assert.AreEqual(3,g.S.demons[0].progress);
        Assert.AreEqual(41,named.health);Assert.AreEqual(7,named.attack);
    }
    [Test] public void WrongLegionAndNoOpenRiftCannotAddScore()
    {
        Assert.IsTrue(g.Battle(EnemyKind.Normal,2,Sin.Wrath));Assert.AreEqual(0,g.S.demons[0].progress);
        g.S.activeRift=-1;Assert.IsTrue(g.Battle(EnemyKind.Special,1,Sin.Pride));Assert.AreEqual(0,g.S.demons[0].progress);
    }
    [Test] public void FiftyPointsUnlockLaterRiftEventWithoutImmediateBoss()
    {
        g.S.demons[0].progress=49;g.Battle(EnemyKind.Normal,1,Sin.Pride);
        Assert.AreEqual(50,g.S.demons[0].progress);Assert.AreEqual(0,g.S.totalSeals);Assert.IsNull(g.S.pending);Assert.AreEqual(Phase.Running,g.S.phase);
        Assert.IsTrue(g.CanReachDemon(Sin.Pride));Assert.IsFalse(g.Battle(EnemyKind.Demon,1,Sin.Pride));
        g.S.hours=6;g.S.nextImportantHour=6;g.S.eventTarget=1;g.Tick();
        Assert.AreNotEqual("균열의 마지막 문",g.S.pending.title,"탐사에서는 악마 이벤트가 발생하지 않아야 합니다.");
        g.S.pending=null;g.S.phase=Phase.Running;g.S.goal=Goal.Rift;g.S.hours=6;g.S.nextImportantHour=6;g.Tick();
        Assert.AreEqual("균열의 마지막 문",g.S.pending.title);Assert.AreEqual(Phase.Event,g.S.phase);Assert.AreEqual(0,g.S.totalSeals);
    }
    [Test] public void NamedNaturalEncounterIsRestrictedToRiftActivity()
    {
        var named=new NamedZombieRecord{id="spawn",name="회귀 검사 망자",health=1,attack=1,legion=Sin.Pride};g.S.permanent.named.Add(named);
        g.B.exploreCombatWeight=g.B.riftCombatWeight=100000;g.S.avatar.personality=Temperament.Brave;
        for(int i=0;i<100;i++){g.S.hours=0;g.S.nextImportantHour=100;g.S.avatar.sanity=g.S.avatar.hunger=100;g.Tick();}
        Assert.IsFalse(named.defeated);Assert.IsFalse(g.S.logs.Any(l=>l.Contains("회귀 검사 망자")));
        g.S.goal=Goal.Rift;
        for(int i=0;i<100&&!named.defeated;i++)
        {
            g.S.hours=0;g.S.eventSlots=0;g.S.phase=Phase.Running;g.S.avatar.sanity=g.S.avatar.hunger=100;
            Assert.IsTrue(g.PresentGeneralEvent("RF-005"));
            Assert.IsFalse(g.S.pending.body.Contains(named.name));g.Resolve(g.S.pending.id,0);
        }
        Assert.IsTrue(named.defeated);
    }
    [Test] public void EveryEnemyUsesCurrentSealAdditiveScalingAndFinalRounding()
    {
        var named=new NamedZombieRecord{health=57,attack=13};
        for(int seals=0;seals<=6;seals++)
        {
            foreach(var demon in g.S.demons)demon.remaining=0;
            for(int i=1;i<=seals;i++)g.S.demons[i].remaining=10;
            int[] health={g.B.normalHp,g.B.november.specialHp[0],named.health,g.B.november.demonHp[0]};
            int[] attack={g.B.normalAttack,g.B.november.specialAttack[0],named.attack,g.B.november.demonAttack[0]};
            for(int kind=0;kind<4;kind++)
            {
                var actual=g.GetEnemyStats((EnemyKind)kind,named);
                Assert.AreEqual(Rules.Round(health[kind]*(1+.5*seals)),actual.health);
                Assert.AreEqual(Rules.Round(attack[kind]*(1+.2*seals)),actual.attack);
                Assert.AreEqual(actual.health,g.GetEnemyStats((EnemyKind)kind,named).health,"중복 배율 적용 금지");
            }
        }
        Assert.AreEqual(57,named.health);Assert.AreEqual(13,named.attack);
    }
    [Test] public void ExpiredSealLowersStatsAndLifetimeSealsAndAvatarDoNotScaleEnemies()
    {
        g.S.totalSeals=99;g.S.avatar.attackInvestment=1000;
        Assert.AreEqual(g.B.normalHp,g.GetEnemyStats(EnemyKind.Normal).health);
        g.S.demons[1].remaining=1;g.S.demons[2].remaining=8;
        Assert.AreEqual(Rules.Round(g.B.normalHp*2),g.GetEnemyStats(EnemyKind.Normal).health);
        g.EndDay();g.NextDay();
        Assert.AreEqual(1,g.ActiveSealCount);Assert.AreEqual(Rules.Round(g.B.normalHp*1.5),g.GetEnemyStats(EnemyKind.Normal).health);
        Assert.AreEqual(Rules.Round(g.B.normalAttack*1.2),g.GetEnemyStats(EnemyKind.Normal).attack);
    }
    [Test] public void DeathAndSealResetCurrentProgress()
    {
        g.S.demons[0].progress=23;g.S.avatar.health=0;Assert.IsTrue(g.CheckDeath(true,Sin.Pride));
        Assert.AreEqual(0,g.S.demons[0].progress);Assert.AreEqual(-1,g.S.activeRift);
        if(g.S.avatar==null)g.Select(g.S.candidates[0].id);g.S.day++;g.S.activeRift=0;g.S.demons[0].progress=35;
        Assert.IsTrue(g.Seal(Sin.Pride,"reset"));Assert.AreEqual(0,g.S.demons[0].progress);
    }
    [Test] public void LegacySaveMigratesWithoutInventingPointsOrLosingResourcesAndStars()
    {
        g.S.rulesRevision=0;g.S.permanent.unlocked.Add("sejong");g.S.demons[0].progress=999;
        g.S.pending=new ChoiceEvent{id="old-event",title="균열의 마지막 문",choices=new[]{"도전"}};g.S.phase=Phase.Event;
        string old=JsonUtility.ToJson(g.S).Replace("\"progress\":999","\"routeWins\":6,\"attempts\":3");
        var migrated=new GameEngine(JsonUtility.FromJson<GameState>(old),g.B,g.Content);
        Assert.AreEqual(0,migrated.S.demons[0].progress);Assert.AreEqual(g.S.divinity,migrated.S.divinity);
        Assert.IsTrue(migrated.S.permanent.unlocked.Contains("sejong"));Assert.AreEqual("old-event",migrated.S.pending.id);
        Assert.AreEqual("군단의 집결지",migrated.S.pending.title);Assert.AreEqual(Rules.RulesRevision,migrated.S.rulesRevision);
    }
}
