using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Apocalypse;

public class GameRulesTests
{
    GameEngine g;
    [SetUp] public void Setup() { g=GameEngine.New(1729,GameData.LoadBalance(),GameData.LoadContent()); }
    void Select() { Assert.IsTrue(g.Select(g.S.candidates[0].id)); }
    void Veteran() { Select();g.S.day=2;g.S.activeRift=(int)Sin.Pride;g.S.phase=Phase.Running;g.S.goal=Goal.Rift;g.S.demons[0].progress=Rules.RiftProgressRequired; }
    [Test] public void ThreeCandidatesAndSelection() { Assert.AreEqual(3,g.S.candidates.Count);Select();Assert.AreEqual(2,g.S.candidates.Count);Assert.AreEqual(Phase.Morning,g.S.phase);Assert.IsFalse(g.Select(g.S.candidates[0].id)); }
    [Test] public void ContentContainsTwentyUniqueCompleteStories()
    {
        var c=g.Content.constellations;Assert.AreEqual(20,c.Count);Assert.AreEqual(20,c.Select(x=>x.id).Distinct().Count());Assert.AreEqual(20,c.Select(x=>x.eventId).Distinct().Count());
        CollectionAssert.AreEqual(new[]{12,5,2,1},Enumerable.Range(0,4).Select(n=>c.Count(x=>x.grade==n)));
        foreach(var x in c){Assert.Greater(x.story.Length,100);Assert.IsNotEmpty(x.description);}
    }
    [Test] public void HungerUsesActivityTimeWithoutDailyDoubleCharge() { Select();g.S.food=0;g.S.avatar.hunger=100;g.S.phase=Phase.Running;g.SpendTime(Activity.Search,3);Assert.AreEqual(88,g.S.avatar.hunger);g.EndDay();double before=g.S.avatar.hunger;g.NextDay();Assert.AreEqual(before,g.S.avatar.hunger); }
    [Test] public void FractionalActivityRetainsPrecision() { Select();g.S.food=0;g.S.avatar.hunger=100;for(int i=0;i<10;i++)g.SpendTime(Activity.Combat,.01);Assert.AreEqual(99.3,g.S.avatar.hunger,.00001); }
    [TestCase(49,100,100,49)] [TestCase(49,12,61,0)] [TestCase(50,100,50,100)] [TestCase(29,0,29,0)]
    public void AutomaticEating(int hunger,int food,int expected,int remaining) { Select();g.S.avatar.hunger=hunger;g.S.food=food;g.AutoEat();Assert.AreEqual(expected,g.S.avatar.hunger);Assert.AreEqual(remaining,g.S.food); }
    [Test] public void FractionalFoodNeverOverfills() { Select();g.S.avatar.hunger=49.6;g.S.food=100;g.AutoEat();Assert.AreEqual(99.6,g.S.avatar.hunger,.0001);Assert.AreEqual(50,g.S.food); }
    [Test] public void ControlLockPersistsAfterRecoveryUntilNextMorning()
    { Select();g.S.avatar.hunger=29;g.S.food=100;g.AutoEat();Assert.IsTrue(g.S.controlLocked);Assert.Greater(g.S.avatar.hunger,30);g.S.phase=Phase.Running;g.EndDay();g.NextDay();Assert.IsFalse(g.S.controlLocked); }
    [Test] public void LockedNpcCanChooseNormalEarlyReturn()
    { Select();g.S.avatar.sanity=29;g.StartDay(Goal.Explore);if(g.S.pending!=null)g.Resolve(g.S.pending.id,0);for(int i=0;i<10&&g.S.phase==Phase.Running;i++)g.Tick();Assert.AreEqual(Phase.Report,g.S.phase);Assert.NotNull(g.S.avatar);Assert.IsTrue(g.S.controlLocked); }
    [Test] public void LowSanityEventAutomaticallyResolves()
    { Veteran();g.S.controlLocked=true;g.S.avatar.sanity=29;g.S.hours=6;g.S.nextImportantHour=6;g.S.goal=Goal.Rift;g.Tick();Assert.AreNotEqual(Phase.Event,g.S.phase);Assert.IsNull(g.S.pending); }
    [TestCase(true)] [TestCase(false)] public void ZeroHungerOrSanityKillsImmediately(bool hunger)
    { Select();if(hunger)g.S.avatar.hunger=0;else{g.S.avatar.sanity=0;g.S.sanityCauses.Add(Sin.Envy);}Assert.IsTrue(g.CheckDeath(false,null));Assert.AreEqual(Phase.Death,g.S.phase);Assert.AreEqual(2,g.S.day);Assert.AreEqual(hunger?Sin.Gluttony:Sin.Envy,g.S.permanent.named[0].legion); }
    [Test] public void CombatDeathOverridesStarvationAndClearsRift()
    { Veteran();g.S.avatar.hunger=0;g.CheckDeath(true,Sin.Pride);Assert.AreEqual(Sin.Pride,g.S.permanent.named[0].legion);Assert.AreEqual(-1,g.S.activeRift);Assert.IsTrue(g.S.permanent.deaths[0].combat);Assert.IsNull(g.S.avatar);Assert.Greater(g.S.permanent.named[0].health,0); }
    [Test] public void TiedCauseTagsOnlyChooseEligibleLegions()
    { var seen=new System.Collections.Generic.HashSet<Sin>();for(uint seed=1;seed<100;seed++){var x=GameEngine.New(seed,g.B,g.Content);x.Select(x.S.candidates[0].id);x.S.sanityCauses.AddRange(new[]{Sin.Greed,Sin.Envy});x.S.avatar.sanity=0;x.CheckDeath(false,null);var sin=x.S.permanent.named[0].legion;Assert.Contains(sin,new[]{Sin.Greed,Sin.Envy});seen.Add(sin);}Assert.AreEqual(2,seen.Count); }
    [Test] public void NoRiftDeathGovernmentAndReplacementSafeDay()
    { Select();g.S.candidates.Clear();g.S.avatar.health=0;g.CheckDeath(false,null);Assert.AreEqual(1,g.S.candidates.Count);Assert.IsFalse(g.S.candidates[0].mutant);SelectReplacement();Assert.IsFalse(g.Battle(EnemyKind.Normal,1,Sin.Wrath));Assert.AreEqual(-1,g.S.activeRift);g.S.phase=Phase.Running;g.EndDay();g.NextDay();Assert.GreaterOrEqual(g.S.activeRift,0); }
    void SelectReplacement(){Assert.IsTrue(g.Select(g.S.candidates[0].id));}
    [Test] public void DeathKeepsDivinityAndReturnsRelics()
    { Select();g.S.divinity=500;g.S.relics.Add(new Relic{id="gungnir",name="궁니르"});string id=g.S.relics[0].id;g.Equip(id);int money=g.S.divinity;g.S.avatar.health=0;g.CheckDeath(false,null);Assert.AreEqual(money,g.S.divinity);Assert.AreEqual(id,g.S.relics[0].id);SelectReplacement();Assert.IsEmpty(g.S.avatar.relics); }
    [Test] public void NewAvatarFirstDayHasNoCombatThenRiftNextDay()
    { Select();Assert.IsFalse(g.Battle(EnemyKind.Normal,1,Sin.Pride));g.S.phase=Phase.Running;g.EndDay();g.NextDay();Assert.GreaterOrEqual(g.S.activeRift,0);int rift=g.S.activeRift;g.OpenRift();Assert.AreEqual(rift,g.S.activeRift); }
    [Test] public void DailyConstellationRateAndGradeDistribution()
    {
        const int trials=30000;int hits=0;int[] grades=new int[4];
        for(int i=1;i<=trials;i++){g.S.day=i;if(g.RollConstellation()){hits++;grades[g.Content.Find(g.S.scheduledConstellation).grade]++;}Assert.IsFalse(g.RollConstellation());g.S.pending=null;g.S.phase=Phase.Running;}
        Assert.AreEqual(.3,hits/(double)trials,.015);
        for(int j=0;j<4;j++)Assert.AreEqual(Rules.GradeWeights[j]/100.0,grades[j]/(double)hits,.02);
    }
    [Test] public void DepletedGradeRedistributesAndAllUnlockedStops()
    {
        g.S.permanent.unlocked.AddRange(g.Content.constellations.Where(c=>c.grade==0).Select(c=>c.id));CollectionAssert.AreEqual(new[]{0,35,10,5},g.AvailableGradeWeights());
        int[] grades=new int[4];int hits=0;for(int i=1;i<=20000;i++){g.S.day=i;if(g.RollConstellation()){grades[g.Content.Find(g.S.scheduledConstellation).grade]++;hits++;}g.S.pending=null;}
        Assert.AreEqual(0,grades[0]);Assert.AreEqual(.7,grades[1]/(double)hits,.025);Assert.AreEqual(.2,grades[2]/(double)hits,.025);Assert.AreEqual(.1,grades[3]/(double)hits,.025);
        g.S.permanent.unlocked=g.Content.constellations.Select(c=>c.id).ToList();for(int i=20001;i<20100;i++){g.S.day=i;Assert.IsFalse(g.RollConstellation());}
    }
    [Test] public void AllStoriesAreObtainableUnderControlLockWithoutDuplicates()
    {
        Select();g.S.controlLocked=true;int day=0;
        while(g.S.permanent.unlocked.Count<20&&day<2000)
        {
            g.S.day=++day;g.S.phase=Phase.Running;
            if(g.RollConstellation())
            {
                int before=g.S.permanent.unlocked.Count;Assert.IsTrue(g.EncounterConstellation());
                Assert.AreEqual(before+1,g.S.permanent.unlocked.Count);Assert.IsNull(g.S.pending);
                Assert.IsFalse(g.EncounterConstellation());
            }
        }
        Assert.AreEqual(20,g.S.permanent.unlocked.Count);Assert.AreEqual(20,g.S.permanent.completedStories.Count);
    }
    [TestCase(10,10,11)] [TestCase(15,10,17)] [TestCase(20,10,22)] public void StatInvestmentRoundHalfUp(int basic,int invest,int expected)
    { Select();g.S.avatar.baseAttack=basic;g.S.divinity=100;g.Invest(Stat.Attack,invest);Assert.AreEqual(expected,g.S.avatar.Attack);g.S.avatar.baseAttack*=2;Assert.AreEqual(Rules.Round(basic*2*(1+invest/100.0)),g.S.avatar.Attack); }
    [Test] public void IncreasingMaxHealthDoesNotHealAndInheritanceChargesOnce()
    { Select();double hp=g.S.avatar.health;g.S.divinity=1000;g.Invest(Stat.Health,50);Assert.AreEqual(hp,g.S.avatar.health);foreach(var c in g.Content.constellations.Where(c=>c.grade!=3).Take(2)){g.S.permanent.unlocked.Add(c.id);int before=g.S.divinity;Assert.IsTrue(g.Inherit(c.id));Assert.AreEqual(before-Rules.InheritCosts[c.grade],g.S.divinity);Assert.IsFalse(g.Inherit(c.id));} }
    [Test] public void EightEventRelicsStartAtZeroAndCannotBeDuplicated()
    {
        Select();g.S.day=2;g.S.activeRift=0;g.S.avatar.personality=Temperament.Brave;g.S.avatar.baseHealth=100000;g.S.avatar.health=100000;g.S.avatar.baseAttack=10000;g.S.food=100000;g.S.medicine=10;
        foreach(var d in g.Content.relicDefinitions)
        {
            g.S.hours=0;g.S.eventSlots=0;g.S.goal=d.location;g.S.phase=Phase.Running;
            g.S.controlLocked=false;g.S.avatar.hunger=g.S.avatar.sanity=100;
            Assert.IsFalse(g.AcquireRelic(d.id),"성유물은 직접 생성할 수 없습니다.");
            var e=g.Content.activityEvents.Single(x=>x.relicId==d.id);Assert.IsTrue(g.PresentGeneralEvent(e.id));
            Assert.IsTrue(g.Resolve(g.S.pending.id,0));var relic=g.S.relics.Single(r=>r.id==d.id);
            Assert.AreEqual(0,relic.healthBonus);Assert.AreEqual(0,relic.attackBonus);
            Assert.IsFalse(g.EligibleEvents(d.location).Any(x=>x.relicId==d.id));Assert.IsFalse(g.PresentGeneralEvent(e.id));
        }
        Assert.AreEqual(8,g.S.relics.Count);
    }
    [Test] public void KillAndRiftRewardsSeparateAndIdempotent()
    { Veteran();g.S.avatar.baseAttack=1000;g.S.avatar.hunger=100;int before=g.S.divinity;Assert.IsTrue(g.Battle(EnemyKind.Demon,1,Sin.Pride));Assert.AreEqual(before+400,g.S.divinity);Assert.IsFalse(g.Seal(Sin.Pride,"duplicate"));Assert.IsFalse(g.Reward(g.S.rewardLedger[0],100));Assert.AreEqual(before+400,g.S.divinity); }
    [TestCase(EnemyKind.Normal,1)] [TestCase(EnemyKind.Special,15)] public void EnemyRewardIsPerEnemy(EnemyKind kind,int reward)
    { Veteran();g.S.avatar.baseAttack=1000;int before=g.S.divinity;g.Battle(kind,3,Sin.Pride);Assert.AreEqual(before+reward*(kind==EnemyKind.Special?1:3),g.S.divinity); }
    [Test] public void NamedRecordIsIndependentAndHadesDoublesReward()
    { Veteran();var n=new NamedZombieRecord{id="test",name="old",health=30,attack=1,legion=Sin.Pride};g.S.permanent.named.Add(n);g.S.avatar.inherited.Add("hades");g.S.avatar.baseAttack=1000;int before=g.S.divinity;g.Battle(EnemyKind.Named,1,Sin.Pride,n);Assert.IsTrue(n.defeated);Assert.AreEqual(before+60,g.S.divinity); }
    [TestCase(false,100)] [TestCase(true,120)] public void SealDurationSameDayAndNextDay(bool yog,int days)
    { Veteran();if(yog)g.S.avatar.inherited.Add("yog");Assert.IsTrue(g.Seal(Sin.Pride,"seal"));Assert.AreEqual(days,g.S.demons[0].remaining);g.EndDay();Assert.AreEqual(days,g.S.demons[0].remaining);g.NextDay();Assert.AreEqual(days-1,g.S.demons[0].remaining);Assert.GreaterOrEqual(g.S.activeRift,1); }
    [Test] public void YogDoesNotAlterExistingSealAndSeventhSealWinsBeforeDecrement()
    { Veteran();g.S.demons[0].remaining=1;g.S.avatar.inherited.Add("yog");Assert.AreEqual(1,g.S.demons[0].remaining);for(int i=1;i<6;i++)g.S.demons[i].remaining=1;g.S.activeRift=6;g.Seal(Sin.Sloth,"last");Assert.AreEqual(Phase.Victory,g.S.phase);Assert.AreEqual(1,g.S.demons[0].remaining);Assert.AreEqual(1,g.S.permanent.victories);g.EndDay();Assert.AreEqual(Phase.Victory,g.S.phase); }
    [Test] public void SaveLoadReplaysIdenticalRandomResultsAndPendingEvent()
    {
        Veteran();g.S.goal=Goal.Explore;string json=JsonUtility.ToJson(g.S);var other=new GameEngine(JsonUtility.FromJson<GameState>(json),g.B,g.Content);
        for(int i=0;i<20;i++){if(g.S.phase==Phase.Event){g.Resolve(g.S.pending.id,0);other.Resolve(other.S.pending.id,0);}else{g.Tick();other.Tick();}}
        Assert.AreEqual(JsonUtility.ToJson(g.S),JsonUtility.ToJson(other.S));
    }
    [Test] public void SaveUsesBackupAndRejectsCorruptPrimary()
    {
        string dir=Path.Combine(Path.GetTempPath(),"ApocalypseTest-"+Guid.NewGuid().ToString("N"));
        try{var saves=new SaveManager(dir);Assert.IsTrue(saves.Write(g.S),saves.LastError);g.S.divinity++;Assert.IsTrue(saves.Write(g.S));File.WriteAllText(saves.PathName,"broken-json");var loaded=saves.Read();Assert.NotNull(loaded);Assert.AreEqual(g.S.divinity-1,loaded.divinity);Assert.IsNotEmpty(saves.LastError);}
        finally{Directory.Delete(dir,true);}
    }
    [Test] public void EndDayIsIdempotentAndNoFreeSanityRecovery()
    { Select();g.S.avatar.sanity=42;g.S.phase=Phase.Running;g.EndDay();string report=g.S.report;g.EndDay();Assert.AreEqual(report,g.S.report);g.NextDay();Assert.AreEqual(42,g.S.avatar.sanity);Assert.IsFalse(g.NextDay()); }
    [Test] public void HighIntensityHungerBudgetIsMeasured()
    { double cost=8*g.B.hungerRates[(int)Activity.Sleep]+4*g.B.hungerRates[(int)Activity.Travel]+6*g.B.hungerRates[(int)Activity.Search]+4*g.B.hungerRates[(int)Activity.Scout]+2*g.B.hungerRates[(int)Activity.Combat];Assert.AreEqual(78,cost);double continuous=4*3+8*4+8*5+4*7;Assert.AreEqual(112,continuous); }
}

