using System.Linq;
using Apocalypse;
using NUnit.Framework;
public class DemonMechanicsTests
{
    GameEngine Start(Sin sin,int attack=10)
    {
        var g=GameEngine.New(159,GameData.LoadBalance(),GameData.LoadContent());g.S.day=2;g.S.activeRift=(int)sin;g.S.demons[(int)sin].progress=50;g.S.goal=Goal.Rift;g.S.phase=Phase.Running;g.S.exploration.current=Area.Rift;
        g.S.avatar.baseHealth=10000;g.S.avatar.health=10000;g.S.avatar.baseAttack=attack;g.S.avatar.sanity=g.S.avatar.hunger=100;g.S.avatar.personality=Temperament.Defiant;g.S.food=100000;
        g.InteractiveCombat=true;g.Battle(EnemyKind.Demon,1,sin);return g;
    }
    void Step(GameEngine g){if(g.S.combat.waiting)g.ContinueCombat();g.StepCombat();}
    void NextAttack(GameEngine g){int before=g.S.combat.avatarActions;for(int i=0;i<50&&g.S.combat.avatarActions==before;i++)Step(g);}
    [Test] public void PrideReducesFiveHitsThenIncreasesExactlyTwo()
    {
        var g=Start(Sin.Pride);int[] hits=new int[8];
        for(int i=0;i<8;i++){NextAttack(g);hits[i]=g.S.combat.lastMoment.amount;}
        CollectionAssert.AreEqual(new[]{6,6,6,6,6,11,11,10},hits);
    }
    [Test] public void GreedThirdAttackHealsHalfActualDamageAndLustKeepsHpDamage()
    {
        var g=Start(Sin.Greed,1);var e=g.S.combat.enemies[0];e.hp=100;e.attacks=2;NextAttack(g);Step(g);
        Assert.AreEqual(104,e.hp);Assert.AreEqual(9990,g.S.avatar.health);
        g=Start(Sin.Lust,1);g.S.avatar.sanity=100;g.S.combat.enemies[0].attacks=2;NextAttack(g);Step(g);
        Assert.AreEqual(9991,g.S.avatar.health);Assert.AreEqual(85,g.S.avatar.sanity);
    }
    [Test] public void EnvyReflectsHalfFourTurnsActualDamageOnce()
    {
        var g=Start(Sin.Envy,20);var e=g.S.combat.enemies[0];e.hp=155;
        while(g.S.combat.turn<4)Step(g);
        Assert.AreEqual(80,e.envyDamage);Assert.AreEqual(9920,g.S.avatar.health);Assert.IsTrue(e.envyUsed);Assert.AreEqual(-1,e.envyStart);
    }
    [Test] public void GluttonyForcesCommandThenAbsorbsSurvivorsWithoutKillRewards()
    {
        var g=Start(Sin.Gluttony,120);var b=g.S.combat;NextAttack(g);g.StepCombat();
        Assert.IsTrue(b.waiting);StringAssert.Contains("소환",b.prompt);Assert.AreEqual(3,b.enemies.Count);Assert.AreEqual(240,b.enemies[0].hp);
        g.ContinueCombat();g.S.avatar.baseAttack=1;int before=g.S.divinity;
        while(b.turn<2)Step(g);NextAttack(g);g.StepCombat();
        Assert.AreEqual(274,b.enemies[0].hp);Assert.IsTrue(b.enemies.Skip(1).All(z=>z.hp==0));Assert.AreEqual(0,g.S.totalKills);Assert.AreEqual(before,g.S.divinity);
    }
    [Test] public void WrathThresholdKeepsAttackAndIncomingModifiersUntilEnd()
    {
        var g=Start(Sin.Wrath,10);var e=g.S.combat.enemies[0];e.hp=136;NextAttack(g);Assert.IsTrue(e.enraged);g.StepCombat();Assert.AreEqual(9984,g.S.avatar.health);
        NextAttack(g);Assert.AreEqual(12,g.S.combat.lastMoment.amount);
    }
    [Test] public void SlothBlocksFourthOpportunityButEnemiesStillAct()
    {
        var g=Start(Sin.Sloth,1);for(int i=0;i<4;i++)NextAttack(g);
        Assert.AreEqual(CombatMomentKind.Blocked,g.S.combat.lastMoment.kind);Assert.AreEqual(327,g.S.combat.enemies[0].hp);
        g.StepCombat();Assert.AreEqual(9968,g.S.avatar.health);
    }
    [Test] public void KillingGluttonyEndsSummonsWithoutBonusZombieRewards()
    {
        var g=Start(Sin.Gluttony,120);NextAttack(g);g.StepCombat();g.ContinueCombat();g.S.avatar.baseAttack=1000;int money=g.S.divinity;
        while(g.S.combat!=null)Step(g);
        Assert.AreEqual(1,g.S.totalKills);Assert.AreEqual(money+400,g.S.divinity);Assert.AreEqual(100,g.S.demons[(int)Sin.Gluttony].remaining);
    }
    [Test] public void AttackDesignationSurvivesSlothBlockedOpportunity()
    {
        var g=Start(Sin.Sloth,1);var b=g.S.combat;
        while(b.avatarActions<3)NextAttack(g);
        while(b.enemyIndex>=0)Step(g);
        b.lockedTarget=0;g.StepCombat();Assert.AreEqual(CombatMomentKind.Blocked,b.lastMoment.kind);Assert.AreEqual(0,b.lockedTarget);
        NextAttack(g);Assert.AreEqual(CombatMomentKind.AvatarAttack,b.lastMoment.kind);Assert.AreEqual(-1,b.lockedTarget);
    }
    [Test] public void HalfUndeadRecoversBeforeCrisisCommandAndReplaysHealingSeparately()
    {
        var g=Start(Sin.Pride,1);var b=g.S.combat;
        g.S.avatar=g.CreateRare("beyr");g.S.avatar.selectedDay=1;g.S.avatar.health=50;b.enemies[0].attack=10;
        g.StepCombat();g.StepCombat();
        Assert.AreEqual(54,g.S.avatar.health);Assert.IsTrue(b.recovered);Assert.AreEqual(CombatMomentKind.EnemyAttack,b.lastMoment.kind);
        Assert.IsFalse(b.waiting);Assert.NotNull(b.queuedMoment);g.StepCombat();Assert.AreEqual(CombatMomentKind.Recovery,b.lastMoment.kind);Assert.IsNull(b.queuedMoment);
    }
}
