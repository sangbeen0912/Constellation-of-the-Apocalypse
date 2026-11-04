using System;
using System.Collections.Generic;
using System.Linq;
using Apocalypse;
using NUnit.Framework;
using UnityEngine;
public class NovemberRulesTests
{
    static GameEngine Veteran()
    {
        var g=GameEngine.New(444,GameData.LoadBalance(),GameData.LoadContent());
        g.S.day=2;g.S.phase=Phase.Running;g.S.activeRift=0;g.S.goal=Goal.Rift;g.S.exploration.current=Area.Rift;
        g.S.avatar.personality=Temperament.Brave;g.S.avatar.baseHealth=10000;g.S.avatar.health=10000;g.S.avatar.sanity=g.S.avatar.hunger=100;
        g.S.food=100000;g.S.divinity=100000;g.S.demons[0].progress=50;return g;
    }
    [Test] public void RetreatAndDeferredRecruitmentTextNeverCreatesCombatOrRewards()
    {
        var content=GameData.LoadContent();
        foreach(var e in content.activityEvents.Where(e=>!e.supplemental&&!e.id.StartsWith("TRN-")))
        {
            var o=e.options[2];
            Assert.AreEqual(0,o.normal+o.special,e.id);Assert.IsFalse(o.recruit,e.id);
            Assert.IsTrue(string.IsNullOrEmpty(o.rareNpcId),e.id);Assert.IsTrue(string.IsNullOrEmpty(o.relicId),e.id);
            Assert.IsFalse(o.mentalRisk,e.id);
        }
        Assert.AreEqual(0,content.activityEvents.Single(e=>e.id=="HOM-01").options[1].normal);
        Assert.IsFalse(content.activityEvents.Single(e=>e.id=="MUS-07").options[2].mentalRisk);
    }
    [Test] public void GuaranteedBattleAndOptionalEncounterRemainDistinct()
    {
        var c=GameData.LoadContent();
        foreach(string id in new[]{"SUB-N11","MRT-07","ARM-04","SAN-08","SV-009","SV-004"})
        {var o=c.activityEvents.Single(e=>e.id==id).options[0];Assert.Greater(o.normal,0,id);Assert.IsFalse(o.optionalEncounter,id);}
        var g=Veteran();g.S.goal=Goal.Explore;g.S.exploration.current=Area.Museum;
        Assert.IsTrue(g.PresentGeneralEvent("MUS-09"));Assert.AreEqual(.35,g.S.pending.options[1].encounterChance);
        var mixed=c.activityEvents.Single(e=>e.id=="ARM-14").options[1];Assert.AreEqual(1,mixed.normal);Assert.AreEqual(1,mixed.special);
        Assert.IsTrue(c.activityEvents.Single(e=>e.id=="SAN-08").options[1].recruit);
    }
    [TestCase(1)] [TestCase(10000)]
    public void AlternateSanityRiskNeverChargesBothBattleAndSanity(int seed)
    {
        var g=Veteran();g.InteractiveCombat=true;g.S.goal=Goal.Explore;g.S.exploration.current=Area.Sanctuary;
        Assert.IsTrue(g.PresentGeneralEvent("SAN-04"));Assert.AreEqual(0,g.S.pending.options[1].sanity);
        g.S.rng=(uint)seed;bool fight=g.Rng.Next()<.5;g.S.rng=(uint)seed;
        double sanity=g.S.avatar.sanity;Assert.IsTrue(g.Resolve(g.S.pending.id,1));
        if(fight){Assert.AreEqual(Phase.Combat,g.S.phase);Assert.AreEqual(sanity-g.B.normalStress,g.S.avatar.sanity);}
        else{Assert.IsNull(g.S.combat);Assert.AreEqual(sanity-8,g.S.avatar.sanity);}
    }
    [Test] public void RevisitsPreservePartialRecordsAndCompletedBranchContext()
    {
        var g=Veteran();g.S.goal=Goal.Explore;g.S.exploration.current=Area.Museum;
        Assert.IsTrue(g.PresentGeneralEvent("MUS-14"));Assert.IsTrue(g.Resolve(g.S.pending.id,2));
        Assert.Contains("restorer_partial_note",g.S.plot.partial);
        g.S.eventSlots=0;Assert.IsTrue(g.PresentGeneralEvent("MUS-14"));StringAssert.Contains("필사",g.S.pending.context);
        var home=g.Content.activityEvents.Single(e=>e.id=="HOM-03");
        g.S.plot.completed.Add("branch:HOM-02:0");StringAssert.Contains("회색 문",g.PlotContext(home));
        g.S.plot.completed.Remove("branch:HOM-02:0");g.S.plot.completed.Add("branch:HOM-02:1");StringAssert.Contains("관리실",g.PlotContext(home));
    }
    static void AcceptNext(GameEngine g)
    {
        for(uint seed=1;seed<100000;seed++){g.S.rng=seed;double roll=g.Rng.Next();if(roll<.1){g.S.rng=seed;return;}}
        Assert.Fail("seed");
    }
    [Test] public void CatalogHas174ApprovedAndFourPreservedRoutesWithNoDeletedOrDuplicateIds()
    {
        var c=GameData.LoadContent();Assert.AreEqual(178,c.activityEvents.Count);Assert.AreEqual(174,c.activityEvents.Count(e=>!e.supplemental));
        Assert.AreEqual(178,c.activityEvents.Select(e=>e.id).Distinct().Count());Assert.AreEqual(10,c.relicDefinitions.Count);
        foreach(string id in new[]{"SP-008","SV-007","SV-010","SU-001","SU-002","SU-006","SU-007","SU-010"})Assert.IsFalse(c.activityEvents.Any(e=>e.id==id));
        Assert.AreEqual(4,c.activityEvents.Count(e=>e.supplemental&&e.area==Area.Rift&&!string.IsNullOrEmpty(e.relicId)));
        var special=c.activityEvents.Single(e=>e.id=="RF-005");Assert.AreEqual(1,special.options.Max(o=>o.special));Assert.AreEqual(2,special.options[0].normal);
    }
    public static IEnumerable<string> EventIds(){return GameData.LoadContent().activityEvents.Where(e=>!e.id.StartsWith("TRN-")).Select(e=>e.id).ToArray();}
    [TestCaseSource(nameof(EventIds))] public void EveryRegisteredChoiceResolvesWithoutUnfinishedOrDuplicateTransactions(string id)
    {
        for(int choice=0;choice<3;choice++)
        {
            var g=Veteran();g.S.avatar.baseAttack=100000;g.S.medicine=g.S.syringes=100;
            var e=g.Content.activityEvents.Single(t=>t.id==id);g.S.goal=e.location;g.S.exploration.current=e.area;
            g.S.plot.completed.AddRange(e.prerequisites);g.S.plot.completed.AddRange(e.anyPrerequisites);
            Assert.IsNotEmpty(e.body,id);Assert.AreEqual(3,e.options.Length,id);
            Assert.IsTrue(g.PresentGeneralEvent(id),id);string key=g.S.pending.id;
            Assert.IsTrue(g.CanChoose(g.S.pending,choice,out string reason),reason);
            Assert.IsTrue(g.Resolve(key,choice),id);Assert.IsFalse(g.Resolve(key,choice));g.Validate();
            Assert.AreNotEqual(Phase.Event,g.S.phase,id);Assert.IsNull(g.S.combat);
        }
    }
    [Test] public void TransitIsUniformExcludesRecentTwoAndNeverConsumesStorySlots()
    {
        var g=Veteran();g.S.goal=Goal.Explore;g.S.exploration.current=Area.Subway;int[] counts=new int[6];
        for(int i=0;i<6000;i++){g.S.phase=Phase.Running;Assert.IsTrue(g.OfferTransit());counts[g.S.exploration.destination-3]++;}
        foreach(int n in counts)Assert.AreEqual(1.0/6,n/6000.0,.025);
        for(int i=0;i<8;i++)
        {
            var blocked=g.S.exploration.rejected.ToArray();int previous=g.S.exploration.destination;g.S.hours=0;
            Assert.IsTrue(g.Resolve(g.S.pending.id,1));Assert.AreEqual(2,g.S.hours);
            Assert.AreNotEqual(previous,g.S.exploration.destination);Assert.IsFalse(g.S.exploration.rejected.Take(g.S.exploration.rejected.Count-1).Contains(g.S.exploration.destination));
            Assert.AreEqual(Math.Min(2,i+1),g.S.exploration.rejected.Count);Assert.AreEqual(0,g.S.eventSlots);
        }
    }
    [Test] public void TransitCannotTrapPlayerWhenBothPaidChoicesAreUnaffordable()
    {
        var g=Veteran();g.S.goal=Goal.Explore;g.S.exploration.current=Area.Subway;g.OfferTransit();g.S.hours=23.5;
        Assert.IsFalse(g.CanChoose(g.S.pending,0,out _));Assert.IsFalse(g.CanChoose(g.S.pending,1,out _));
        Assert.IsTrue(g.ReturnToShelter());Assert.AreEqual(23.5,g.S.hours);Assert.AreEqual(Phase.Report,g.S.phase);Assert.AreEqual(Area.Shelter,g.S.exploration.current);
    }
    [Test] public void DailyStoryDistributionIsThreeOrFourAndNewGameHasOneHuman()
    {
        var g=Veteran();int threes=0;for(int i=0;i<10000;i++){int n=g.DailyEventCount();Assert.That(n,Is.InRange(3,4));if(n==3)threes++;}
        Assert.AreEqual(.5,threes/10000.0,.025);
        var fresh=GameEngine.New(321,g.B,g.Content);Assert.AreEqual(Phase.Morning,fresh.S.phase);Assert.NotNull(fresh.S.avatar);Assert.IsEmpty(fresh.S.candidates);Assert.AreEqual(Species.Human,fresh.S.avatar.species);
    }
    [TestCase(0,300,9,70,7)] [TestCase(1,300,10,55,9)] [TestCase(2,260,9,50,8)] [TestCase(3,310,10,55,9)]
    [TestCase(4,360,8,75,7)] [TestCase(5,270,12,45,11)] [TestCase(6,330,8,80,6)]
    public void LegionStatsUseCurrentSealsOnce(int sin,int hp,int attack,int specialHp,int specialAttack)
    {
        var g=Veteran();g.S.demons[(sin+1)%7].remaining=10;g.S.demons[(sin+2)%7].remaining=10;
        var boss=g.GetEnemyStats(EnemyKind.Demon,null,(Sin)sin);Assert.AreEqual(hp*2,boss.health);Assert.AreEqual(Rules.Round(attack*1.4),boss.attack);
        var special=g.GetEnemyStats(EnemyKind.Special,null,(Sin)sin);Assert.AreEqual(specialHp*2,special.health);Assert.AreEqual(Rules.Round(specialAttack*1.4),special.attack);
    }
    [TestCase(Temperament.Brave,85,70,35)] [TestCase(Temperament.Cautious,70,90,85)]
    [TestCase(Temperament.Cold,85,90,80)] [TestCase(Temperament.Defiant,50,45,35)]
    [TestCase(Temperament.Aggressive,95,35,15)] [TestCase(Temperament.Fearful,45,90,95)]
    public void CommandRatesAndHpBoundaryBonuses(Temperament personality,int attack,int defend,int retreat)
    {
        var g=Veteran();g.InteractiveCombat=true;g.Battle(EnemyKind.Normal,1,Sin.Pride);g.S.avatar.personality=personality;
        Assert.AreEqual(attack/100.0,g.CommandAcceptance(BattleCommand.Attack,0));Assert.AreEqual(defend/100.0,g.CommandAcceptance(BattleCommand.Defend));
        Assert.AreEqual(retreat/100.0,g.CommandAcceptance(BattleCommand.Retreat));
        g.S.avatar.health=g.S.avatar.MaxHealth*.1;g.S.avatar.hunger=35;
        Assert.AreEqual(Math.Min(95,retreat+45)/100.0,g.CommandAcceptance(BattleCommand.Retreat));
    }
    [Test] public void DefenseCoversIndividualEnemiesButExpiresBeforeNextAvatarAction()
    {
        var g=Veteran();g.InteractiveCombat=true;g.S.avatar.baseAttack=1;g.Battle(EnemyKind.Normal,2,Sin.Pride);g.StepCombat();
        g.S.combat.waiting=true;AcceptNext(g);Assert.IsTrue(g.IssueCombatCommand(BattleCommand.Defend));
        double hp=g.S.avatar.health;g.StepCombat();Assert.AreEqual(hp-2,g.S.avatar.health);g.StepCombat();Assert.AreEqual(hp-4,g.S.avatar.health);
        g.StepCombat();g.StepCombat();Assert.IsFalse(g.S.combat.defending);g.StepCombat();Assert.AreEqual(hp-9,g.S.avatar.health);
    }
    [Test] public void RejectedCommandCostsTenAndCannotBeAppliedTwice()
    {
        var g=Veteran();g.InteractiveCombat=true;g.Battle(EnemyKind.Normal,1,Sin.Pride);g.S.avatar.personality=Temperament.Fearful;g.S.combat.waiting=true;
        for(uint seed=1;seed<1000000;seed++){g.S.rng=seed;double roll=g.Rng.Next();if(roll>=.45){g.S.rng=seed;break;}}
        int before=g.S.divinity;Assert.IsTrue(g.IssueCombatCommand(BattleCommand.Attack,0));Assert.AreEqual(before-10,g.S.divinity);Assert.AreEqual(-1,g.S.combat.lockedTarget);
        Assert.IsFalse(g.IssueCombatCommand(BattleCommand.Attack,0));Assert.AreEqual(before-10,g.S.divinity);
    }
    [Test] public void YataMirrorRequiresVictoryAndRetreatCannotGiveIt()
    {
        var g=Veteran();g.InteractiveCombat=true;g.S.goal=Goal.Explore;g.S.exploration.current=Area.ThemePark;g.S.plot.completed.Add("last_parade_clue");g.S.avatar.baseAttack=1;
        Assert.IsTrue(g.PresentGeneralEvent("FUN-08"));g.Resolve(g.S.pending.id,1);Assert.NotNull(g.S.combat);Assert.IsFalse(g.ReturnToShelter());
        g.S.combat.waiting=true;AcceptNext(g);g.IssueCombatCommand(BattleCommand.Retreat);
        Assert.IsNull(g.S.combat);Assert.IsEmpty(g.S.relics);Assert.IsFalse(g.S.plot.completed.Contains("FUN-08"));
        g.S.phase=Phase.Running;g.S.hours=0;g.S.eventSlots=0;g.InteractiveCombat=false;g.S.avatar.baseAttack=1000;
        Assert.IsTrue(g.PresentGeneralEvent("FUN-08"));g.Resolve(g.S.pending.id,1);Assert.AreEqual("yata-mirror",g.S.relics.Single().id);
    }
    [Test] public void SaveReloadDuringCombatPreservesTurnTargetsAndRng()
    {
        var g=Veteran();g.InteractiveCombat=true;g.S.avatar.baseAttack=5;g.BattleFormation(2,1,Sin.Pride,false);g.StepCombat();
        var loaded=new GameEngine(JsonUtility.FromJson<GameState>(JsonUtility.ToJson(g.S)),g.B,g.Content);loaded.InteractiveCombat=true;
        for(int i=0;i<100&&g.S.combat!=null;i++){if(g.S.combat.waiting){g.ContinueCombat();loaded.ContinueCombat();}else{g.StepCombat();loaded.StepCombat();}Assert.AreEqual(JsonUtility.ToJson(g.S),JsonUtility.ToJson(loaded.S));}
    }
    [Test] public void DeathResetsOnlyAvatarPlotsAndKeepsGlobalOwnership()
    {
        var g=Veteran();g.S.plot.completed.Add("restorer_note");g.S.plot.partial.Add("restorer_partial_note");g.S.uniqueRecruits.Add("GUARD_DEFENSE_TEAM");g.S.permanent.unlocked.Add("sejong");g.S.relics.Add(new Relic{id="yata-mirror",name="야타의 거울"});
        g.S.avatar.health=0;g.CheckDeath(true,Sin.Pride);Assert.IsEmpty(g.S.plot.completed);Assert.IsEmpty(g.S.plot.partial);Assert.AreEqual(1,g.S.uniqueRecruits.Count);Assert.AreEqual(1,g.S.relics.Count);Assert.AreEqual(1,g.S.permanent.unlocked.Count);
    }
    [Test] public void SavedNamedEnemyRebindsToThePermanentRecord()
    {
        var g=Veteran();g.InteractiveCombat=true;var named=new NamedZombieRecord{id="saved-named",name="서도윤",health=80,attack=5,legion=Sin.Pride};g.S.permanent.named.Add(named);
        g.Battle(EnemyKind.Named,1,Sin.Pride,named);
        var loaded=new GameEngine(JsonUtility.FromJson<GameState>(JsonUtility.ToJson(g.S)),g.B,g.Content);loaded.InteractiveCombat=true;loaded.S.avatar.baseAttack=1000;loaded.StepCombat();
        Assert.IsNull(loaded.S.combat);Assert.IsTrue(loaded.S.permanent.named.Single().defeated);Assert.IsNull(loaded.RollSpecialReplacement(Sin.Pride));
    }
    [Test] public void PersonalityChoicesSeparateCautionExpectedGainAggressionAndStubbornness()
    {
        var g=Veteran();var e=new ChoiceEvent{id="personality",choices=new[]{"조사","전투","관찰"},options=new[]{new EventOption{hours=2,completes=true},new EventOption{normal=1,relicId="gungnir",completes=true},new EventOption()}};
        g.S.avatar.baseAttack=1000;
        foreach(var p in new[]{Temperament.Cautious,Temperament.Fearful}){g.S.avatar.personality=p;Assert.AreEqual(2,g.AutomaticChoice(e));}
        foreach(var p in new[]{Temperament.Cold,Temperament.Aggressive,Temperament.Brave}){g.S.avatar.personality=p;Assert.AreEqual(1,g.AutomaticChoice(e));}
        g.S.avatar.personality=Temperament.Defiant;Assert.AreEqual(0,g.AutomaticChoice(e));
        g.S.avatar.personality=Temperament.Cold;g.S.avatar.baseAttack=1;g.S.avatar.baseHealth=10;g.S.avatar.health=10;Assert.AreEqual(0,g.AutomaticChoice(e));
    }
}
