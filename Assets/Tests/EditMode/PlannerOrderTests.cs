using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Apocalypse;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

public class PlannerOrderTests
{
    GameEngine g;
    [SetUp] public void Setup()
    {
        g=GameEngine.New(12009,GameData.LoadBalance(),GameData.LoadContent());g.Select(g.S.candidates[0].id);
        g.S.day=2;g.S.phase=Phase.Running;g.S.goal=Goal.Explore;g.S.activeRift=0;
        g.S.avatar.personality=Temperament.Brave;g.S.avatar.baseHealth=10000;g.S.avatar.health=10000;
        g.S.avatar.baseAttack=1000;g.S.avatar.hunger=g.S.avatar.sanity=100;g.S.food=100000;g.S.medicine=20;
    }
    void Present(string id)
    {
        var t=g.Content.activityEvents.First(e=>e.id==id);g.S.goal=t.location;g.S.phase=Phase.Running;g.S.eventSlots=0;g.S.hours=0;
        g.S.controlLocked=false;g.S.avatar.hunger=g.S.avatar.sanity=100;
        Assert.IsTrue(g.PresentGeneralEvent(id));
    }
    Relic Wear(string id="gungnir")
    {
        var relic=new Relic{id=id,name=g.Content.relicDefinitions.First(d=>d.id==id).name};g.S.relics.Add(relic);g.S.phase=Phase.Morning;
        Assert.IsTrue(g.Equip(id));g.S.phase=Phase.Running;return relic;
    }
    [TestCase(Goal.Explore)] [TestCase(Goal.Rift)]
    public void GeneralPoolIsUniformAndLocationSpecific(Goal location)
    {
        var eligible=g.EligibleEvents(location);var counts=eligible.ToDictionary(e=>e.id,e=>0);
        for(int i=0;i<30000;i++)counts[g.DrawGeneralEvent(location).id]++;
        foreach(var e in eligible){Assert.AreEqual(location,e.location);Assert.AreEqual(1.0/eligible.Count,counts[e.id]/30000.0,.012,e.id);}
        g.S.relics.Add(new Relic{id=eligible.First(e=>!string.IsNullOrEmpty(e.relicId)).relicId});
        g.S.divineEventDay=g.S.day;
        Assert.IsFalse(g.EligibleEvents(location).Any(e=>e.divinityEvent||g.S.relics.Any(r=>r.id==e.relicId)));
    }
    [Test] public void DayCountDistributionRemainsUnconfirmedWithoutInventedUniformOdds()
    {
        uint before=g.S.rng;g.B.dailyEventCountWeights=new int[0];Assert.AreEqual(1,g.DailyEventCount());Assert.AreEqual(before,g.S.rng);
        for(int i=0;i<3;i++){g.B.dailyEventCountWeights=new int[3];g.B.dailyEventCountWeights[i]=1;Assert.AreEqual(i+1,g.DailyEventCount());}
    }
    [Test] public void ThreeGeneralChoicesExcludeConstellationAndOrdinaryCombat()
    {
        g.B.dailyEventCountWeights=new[]{0,0,1};g.B.returnHour=24;g.B.returnHealthRatio=0;g.B.returnRiskPerSeal=0;
        g.S.phase=Phase.Morning;g.S.dayStarted=false;g.StartDay(Goal.Explore);
        int max=0;for(int i=0;i<300&&g.S.phase!=Phase.Report;i++)
        {if(g.S.phase==Phase.Event)g.Resolve(g.S.pending.id,g.AutomaticChoice(g.S.pending));else g.Tick();max=Math.Max(max,g.S.eventSlots);}
        Assert.AreEqual(Phase.Report,g.S.phase);Assert.AreEqual(3,max);Assert.AreEqual(3,g.S.eventSlots);
        g.S.phase=Phase.Running;Assert.IsFalse(g.PresentGeneralEvent("field-cache"));
    }
    [Test] public void LivingEarlyReturnMeetsScheduledStarBeforeReturning()
    {
        g.S.eventSlots=1;g.S.scheduledConstellation="sejong";Assert.IsTrue(g.RequestReturn("체력 위험"));
        Assert.AreEqual(Phase.Event,g.S.phase);Assert.AreEqual("sejong",g.S.pending.constellationId);
        Assert.IsTrue(g.Resolve(g.S.pending.id,0));Assert.IsTrue(g.S.permanent.unlocked.Contains("sejong"));
        Assert.AreEqual(Phase.Report,g.S.phase);Assert.NotNull(g.S.avatar);Assert.AreEqual(0,g.S.permanent.deaths.Count);
    }
    [Test] public void DeathCancelsScheduledAndPendingRewardsButKeepsPriorMoney()
    {
        g.S.scheduledConstellation="zeus";int money=g.S.divinity;g.S.avatar.health=0;g.CheckDeath(true,Sin.Pride);
        Assert.IsNull(g.S.scheduledConstellation);Assert.IsNull(g.S.pending);Assert.IsFalse(g.S.permanent.unlocked.Contains("zeus"));
        Assert.AreEqual(money,g.S.divinity);Assert.AreEqual(-1,g.S.activeRift);
    }
    [TestCase(30,false)] [TestCase(29,true)]
    public void ThirtyBoundaryAndLockLastForTheRestOfDay(int value,bool locked)
    {
        g.S.avatar.sanity=value;g.Stress(0,null);Assert.AreEqual(locked,g.S.controlLocked);
        g.S.avatar.sanity=100;g.Stress(0,null);Assert.AreEqual(locked,g.S.controlLocked);
    }
    [Test] public void EventTimeAndResourcesAreRecheckedBeforeCommit()
    {
        Present("F-04");g.S.food=5;g.S.hours=20;var e=g.S.pending;
        Assert.IsTrue(g.CanChoose(e,0,out _));g.S.food=4;
        Assert.IsFalse(g.Resolve(e.id,0));Assert.AreEqual(4,g.S.food);Assert.AreSame(e,g.S.pending);
        g.S.food=5;g.S.hours=20.01;Assert.IsFalse(g.CanChoose(e,0,out string reason));StringAssert.Contains("부족",reason);
        Assert.IsFalse(g.Resolve(e.id,0));Assert.IsEmpty(g.S.relics);
        g.S.hours=20;Assert.IsTrue(g.Resolve(e.id,0));Assert.AreEqual(0,g.S.food);Assert.AreEqual(24,g.S.hours);
        Assert.AreEqual("bicorne",g.S.relics.Single().id);Assert.IsFalse(g.Resolve(e.id,0));
    }
    [Test] public void AutomaticChoiceExcludesUnaffordableOptions()
    {
        Present("R-01");g.S.hours=18;g.S.avatar.personality=Temperament.Cautious;g.S.controlLocked=true;
        Assert.AreEqual(2,g.AutomaticChoice(g.S.pending));Assert.IsTrue(g.Resolve(g.S.pending.id,1));
        Assert.IsEmpty(g.S.relics);Assert.AreEqual(18,g.S.hours);
    }
    [Test] public void EventDeathDoesNotGrantUnfinishedRelic()
    {
        Present("R-04");g.S.avatar.sanity=30;g.S.scheduledConstellation="sejong";int before=g.S.divinity;
        Assert.IsTrue(g.Resolve(g.S.pending.id,0));Assert.AreEqual(Phase.Death,g.S.phase);Assert.IsEmpty(g.S.relics);
        Assert.AreEqual(before,g.S.divinity);Assert.IsNull(g.S.scheduledConstellation);Assert.IsNull(g.S.pending);
    }
    [Test] public void PartialBattleKillMoneySurvivesButRelicDoesNot()
    {
        Present("R-01");g.S.avatar.baseHealth=100;g.S.avatar.health=1;g.S.avatar.baseAttack=60;
        int money=g.S.divinity;Assert.IsTrue(g.Resolve(g.S.pending.id,0));
        Assert.AreEqual(Phase.Death,g.S.phase);Assert.AreEqual(1,g.S.totalKills);Assert.AreEqual(money+15,g.S.divinity);
        Assert.IsEmpty(g.S.relics);Assert.AreEqual(0,g.S.demons[0].progress);
    }
    [Test] public void DivineGeneralEventIsOncePerDayAndPaysThreeToTenOnlyOnSuccess()
    {
        g.B.divineSuccessChance=1;Present("field-prayer");int before=g.S.divinity;g.Resolve(g.S.pending.id,0);
        Assert.That(g.S.divinity-before,Is.InRange(3,10));Assert.IsFalse(g.EligibleEvents(Goal.Explore).Any(e=>e.divinityEvent));
        g.S.day++;g.B.divineSuccessChance=0;Present("field-prayer");before=g.S.divinity;g.Resolve(g.S.pending.id,0);Assert.AreEqual(before,g.S.divinity);
    }
    [Test] public void NamedReplacementHasThirtyPercentAndOnlyAfterSpecialChoice()
    {
        var named=new NamedZombieRecord{id="memory",name="옛 화신체",health=100,attack=10,legion=Sin.Pride};
        g.S.permanent.named.Add(named);g.S.goal=Goal.Explore;
        for(int i=0;i<100;i++)Assert.IsNull(g.RollSpecialReplacement(Sin.Pride));
        g.S.goal=Goal.Rift;int hits=0;for(int i=0;i<20000;i++)if(g.RollSpecialReplacement(Sin.Pride)!=null)hits++;
        Assert.AreEqual(.3,hits/20000.0,.015);
        Present("rift-special");Assert.IsFalse(g.S.pending.title.Contains(named.name)||g.S.pending.body.Contains(named.name));
        named.defeated=true;Assert.IsNull(g.RollSpecialReplacement(Sin.Pride));
        g.S.phase=Phase.Running;g.S.activeRift=-1;g.S.nextRiftDay=g.S.day;for(int i=1;i<7;i++)g.S.demons[i].remaining=5;
        g.OpenRift();Assert.AreEqual(0,g.S.activeRift);Assert.IsFalse(named.defeated);
    }
    [TestCase("kael",120,28,Species.Machine)] [TestCase("nerissa",105,26,Species.Fishfolk)]
    [TestCase("beyr",140,20,Species.HalfUndead)] [TestCase("lyra",95,32,Species.Beastkin)]
    public void RareNpcIsFieldOnlyWithFixedStatsAndStableArt(string id,int hp,int attack,Species species)
    {
        g.B.scoutSuccess=1;Assert.IsFalse(g.RecruitRare(id));g.S.goal=Goal.Rift;Assert.IsFalse(g.PresentGeneralEvent("recruit-"+id));
        Present("recruit-"+id);g.Resolve(g.S.pending.id,0);
        var npc=g.S.candidates.Single(c=>c.species==species);Assert.AreEqual(hp,npc.baseHealth);Assert.AreEqual(attack,npc.baseAttack);
        Assert.NotNull(ArtCatalog.Portrait(npc.name));Assert.IsFalse(g.EligibleEvents(Goal.Explore).Any(e=>e.rareNpcId==id));
        for(int i=0;i<100;i++)Assert.AreEqual(Species.Human,g.CreateCharacter(false).species);
        g.S.avatar.health=0;g.CheckDeath(false,null);Assert.IsTrue(g.Select(npc.id));Assert.AreEqual(species,g.S.avatar.species);
    }
    [Test] public void MachineReducesDamageAndBeastkinOnlyBoostsFirstAttack()
    {
        g.S.avatar=g.CreateRare("kael");g.B.maxCombatRounds=1;g.B.normalHp=10000;g.B.normalAttack=10;g.Battle(EnemyKind.Normal,1,Sin.Pride);
        Assert.AreEqual(111,g.S.avatar.health);
        g.S.logs.Clear();g.S.avatar=g.CreateRare("lyra");g.S.hours=0;g.B.maxCombatRounds=2;g.Battle(EnemyKind.Normal,1,Sin.Pride);
        var damage=g.S.logs.Where(l=>l.Contains("공격 ")).Select(l=>int.Parse(l.Split(new[]{"공격 "},StringSplitOptions.None)[1].Split(' ')[0])).ToArray();
        CollectionAssert.AreEqual(new[]{38,32},damage);
    }
    [Test] public void HalfUndeadHealsAtFirstThresholdOncePerCombat()
    {
        g.S.avatar=g.CreateRare("beyr");g.S.avatar.health=50;g.B.maxCombatRounds=3;g.B.normalHp=10000;g.B.normalAttack=10;
        g.Battle(EnemyKind.Normal,1,Sin.Pride);Assert.AreEqual(34,g.S.avatar.health);Assert.AreEqual(1,g.S.logs.Count(l=>l.Contains("불완전한 생명")));
        g.S.logs.Clear();g.S.hours=0;g.S.avatar.health=50;g.Battle(EnemyKind.Normal,1,Sin.Pride);Assert.AreEqual(1,g.S.logs.Count(l=>l.Contains("불완전한 생명")));
    }
    [Test] public void FishfolkAvoidsTwelvePercentOfIncomingAttacks()
    {
        g.S.avatar=g.CreateRare("nerissa");g.B.maxCombatRounds=1;g.B.normalHp=10000;g.B.normalAttack=1;int avoid=0;
        for(int i=0;i<10000;i++){g.S.hours=0;g.S.avatar.health=105;g.S.avatar.hunger=g.S.avatar.sanity=100;g.Battle(EnemyKind.Normal,1,Sin.Pride);if(g.S.avatar.health==105)avoid++;}
        Assert.AreEqual(.12,avoid/10000.0,.015);
    }
    [Test] public void FoodQuantityDistributionIsConditionalOnDiscovery()
    {
        int[] counts=new int[3];for(int i=0;i<30000;i++)counts[g.RollFoundFoodAmount()-1]++;
        double[] expected={.6,.3,.1};for(int i=0;i<3;i++)Assert.AreEqual(expected[i],counts[i]/30000.0,.012);
        int food=g.S.food;g.B.foodDiscoveryChance=0;g.SearchFood();Assert.AreEqual(food,g.S.food);
        g.B.foodDiscoveryChance=1;g.SearchFood();Assert.That(g.S.food-food,Is.InRange(1,3));
    }
    [Test] public void SuppliesAreUniformAndCostExactlyOne()
    {
        g.S.phase=Phase.Morning;g.S.divinity=40000;int food=g.S.food,kits=g.S.medicine,shots=g.S.syringes;
        for(int i=0;i<30000;i++)Assert.IsTrue(g.Supply());
        Assert.AreEqual(10000,g.S.divinity);
        int[] counts={(g.S.food-food)/5,g.S.medicine-kits,g.S.syringes-shots};
        foreach(int count in counts)Assert.AreEqual(1.0/3,count/30000.0,.015);
    }
    [TestCase(false,false,false,6)] [TestCase(false,false,true,2)]
    [TestCase(true,false,false,6)] [TestCase(true,false,true,2)] [TestCase(true,true,false,10)]
    public void ClinicUsesMaximumSeventyPercentAndCorrectTime(bool mental,bool sleep,bool item,int hours)
    {
        g.S.phase=Phase.Morning;g.S.avatar.baseHealth=120;g.S.avatar.health=20;g.S.avatar.sanity=20;g.S.syringes=1;
        int kits=g.S.medicine;var p=g.PreviewTreatment(mental,sleep,item);Assert.IsTrue(p.available);Assert.AreEqual(hours,p.hours);
        Assert.IsTrue(g.Treat(mental,sleep,item));Assert.AreEqual(hours,g.S.hours);
        Assert.AreEqual(!mental||sleep?104:20,g.S.avatar.health);Assert.AreEqual(mental||sleep?90:20,g.S.avatar.sanity);
        Assert.AreEqual(item&&!mental?kits-1:kits,g.S.medicine);Assert.AreEqual(item&&mental?0:1,g.S.syringes);
    }
    [Test] public void SleepCannotUseItemsAndInsufficientTimeCannotCharge()
    {
        g.S.phase=Phase.Morning;g.S.syringes=1;int kits=g.S.medicine;
        Assert.IsFalse(g.Treat(true,true,true));Assert.AreEqual(1,g.S.syringes);
        g.S.hours=18;Assert.IsTrue(g.PreviewTreatment(false).available);g.S.hours=18.1;
        Assert.IsFalse(g.Treat(false));Assert.AreEqual(kits,g.S.medicine);g.S.hours=22.1;
        Assert.IsFalse(g.Treat(false,false,true));Assert.AreEqual(kits,g.S.medicine);
    }
    [Test] public void EightRelicEventsAreUniqueLocationBoundAndUseZeroInitialStats()
    {
        Assert.AreEqual(8,g.Content.relicDefinitions.Count);
        foreach(var definition in g.Content.relicDefinitions)
        {
            var e=g.Content.activityEvents.Single(x=>x.relicId==definition.id);Assert.AreEqual(definition.location,e.location);
            Present(e.id);Assert.IsTrue(g.Resolve(g.S.pending.id,0));
            var item=g.S.relics.Single(r=>r.id==definition.id);Assert.AreEqual(0,item.healthBonus);Assert.AreEqual(0,item.attackBonus);
            Assert.NotNull(ArtCatalog.Relic(item.id));Assert.IsFalse(g.EligibleEvents(e.location).Any(x=>x.relicId==item.id));
        }
        Assert.AreEqual(8,g.S.relics.Count);
    }
    [Test] public void EquippedRelicRecordsEachZombieOnceAndDemonSeparatelyWithoutLiveGrowth()
    {
        var relic=Wear();g.S.goal=Goal.Rift;
        g.Battle(EnemyKind.Normal,3,Sin.Pride);g.Battle(EnemyKind.Special,1,Sin.Pride);
        var named=new NamedZombieRecord{id="dead",name="이전 화신",health=1,attack=1,legion=Sin.Pride};g.Battle(EnemyKind.Named,1,Sin.Pride,named);
        Assert.AreEqual(5,relic.Zombies);Assert.AreEqual(9,g.S.demons[0].progress);
        g.S.demons[0].progress=50;g.Battle(EnemyKind.Demon,1,Sin.Pride);
        Assert.AreEqual(1,relic.demonKills);Assert.AreEqual(5,relic.Zombies);Assert.AreEqual(0,relic.healthBonus);Assert.AreEqual(0,relic.attackBonus);
        Assert.AreEqual(3,relic.history[0].normalKills);Assert.AreEqual(1,relic.history[0].specialKills);Assert.AreEqual(1,relic.history[0].namedKills);
    }
    [Test] public void RelicLimitsOneAndUnequippedKillsDoNotAccumulate()
    {
        var a=Wear();g.S.phase=Phase.Morning;var b=new Relic{id="grail",name="성배"};g.S.relics.Add(b);g.Equip(b.id);
        CollectionAssert.AreEqual(new[]{"grail"},g.S.avatar.relics);g.S.phase=Phase.Running;g.Battle(EnemyKind.Normal,1,Sin.Pride);
        Assert.AreEqual(0,a.Zombies);Assert.AreEqual(1,b.Zombies);
    }
    [Test] public void RestorationCarriesRemaindersAndNeverPaysSameAchievementsTwice()
    {
        var relic=Wear();string first=g.S.avatar.id;relic.days=7;relic.normalKills=6;relic.history[0].days=7;relic.history[0].normalKills=6;relic.history[0].lastCountedDay=g.S.day;
        g.S.avatar.health=0;g.CheckDeath(false,null);Assert.IsTrue(relic.awaitingRestoration);Assert.IsTrue(relic.history[0].deceased);
        g.Select(g.S.candidates[0].id);g.S.divinity=1000;string before=JsonUtility.ToJson(relic);
        var preview=g.PreviewRestoration(relic.id);Assert.AreEqual(before,JsonUtility.ToJson(relic));Assert.AreEqual(0,preview.health);Assert.AreEqual(7,preview.carryDays);
        Assert.IsTrue(g.RestoreRelic(relic.id));Assert.AreEqual(900,g.S.divinity);Assert.AreEqual(0,relic.healthBonus);
        g.Equip(relic.id);Assert.AreNotEqual(first,g.S.avatar.id);
        relic.days+=3;relic.specialKills+=4;relic.demonKills+=1;relic.history[1].days=3;relic.history[1].specialKills=4;relic.history[1].demonKills=1;relic.history[1].lastCountedDay=g.S.day;
        g.S.avatar.health=0;g.CheckDeath(false,null);g.Select(g.S.candidates[0].id);g.S.divinity=1000;
        preview=g.PreviewRestoration(relic.id);Assert.AreEqual(4,preview.health);Assert.AreEqual(2,preview.attack);
        Assert.IsTrue(g.RestoreRelic(relic.id));Assert.AreEqual(4,relic.healthBonus);Assert.AreEqual(2,relic.attackBonus);
        var copy=new GameEngine(JsonUtility.FromJson<GameState>(JsonUtility.ToJson(g.S)),g.B,g.Content);
        Assert.IsFalse(copy.RestoreRelic(relic.id));Assert.AreEqual(900,copy.S.divinity);
        Assert.AreEqual(2,copy.S.relics[0].history.Count);Assert.AreEqual(10,copy.S.relics[0].paidDays);Assert.AreEqual(10,copy.S.relics[0].paidZombies);Assert.AreEqual(1,copy.S.relics[0].paidDemons);
    }
    [Test] public void RestorationRejectsAliveAndInsufficientFundsAndDayCreditIsIdempotent()
    {
        var relic=Wear();g.S.phase=Phase.Morning;g.S.divinity=1000;Assert.IsFalse(g.RestoreRelic(relic.id));
        g.S.phase=Phase.Running;g.EndDay();g.EndDay();Assert.AreEqual(1,relic.days);
        g.S.avatar.health=0;g.CheckDeath(false,null);Assert.AreEqual(1,relic.days);
        g.Select(g.S.candidates[0].id);g.S.divinity=99;Assert.IsFalse(g.RestoreRelic(relic.id));Assert.AreEqual(99,g.S.divinity);Assert.IsTrue(relic.awaitingRestoration);
    }
    [Test] public void OldSaveArchivesRemovedRelicsAndPreservesResourcesAndFiftyPointProgress()
    {
        string directory=Path.Combine(Path.GetTempPath(),"PlannerMigration-"+Guid.NewGuid().ToString("N"));
        try
        {
            g.S.rulesRevision=2;g.S.demons[0].progress=35;g.S.relics.Add(new Relic{id="old-random",name="옛 성유물",attackBonus=9});
            var saves=new SaveManager(directory);Assert.IsTrue(saves.Write(g.S));string original=File.ReadAllText(saves.PathName);
            var state=saves.Read();Assert.AreEqual(original,File.ReadAllText(saves.PathName+".pre-20261030.bak"));
            var loaded=new GameEngine(state,g.B,g.Content);Assert.AreEqual(35,loaded.S.demons[0].progress);
            Assert.AreEqual(g.S.food,loaded.S.food);Assert.AreEqual(g.S.divinity,loaded.S.divinity);
            Assert.IsEmpty(loaded.S.relics);Assert.AreEqual(9,loaded.S.legacyRelics.Single().previousAttackBonus);
            Assert.AreEqual(original,File.ReadAllText(saves.PathName));Assert.AreEqual(Rules.RulesRevision,loaded.S.rulesRevision);
        }
        finally{Directory.Delete(directory,true);}
    }
    [Test] public void TwelveNewAssetsKeepAlphaPixelSettingsAndBoundedRuntimeSizes()
    {
        foreach(var npc in g.Content.rareNpcs)
        {
            var texture=ArtCatalog.Portrait(npc.name);Assert.NotNull(texture);Assert.LessOrEqual(texture.width,256);
            var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));Assert.IsTrue(importer.DoesSourceTextureHaveAlpha());
            Assert.AreEqual(FilterMode.Point,texture.filterMode);Assert.IsFalse(texture.isReadable);Assert.IsFalse(importer.mipmapEnabled);
        }
        foreach(var relic in g.Content.relicDefinitions){var texture=ArtCatalog.Relic(relic.id);Assert.NotNull(texture);Assert.LessOrEqual(texture.width,128);Assert.AreEqual(FilterMode.Point,texture.filterMode);Assert.IsFalse(texture.isReadable);}
    }
    [Test] public void EventCostsAreLiteralAndSharedContentDoesNotMutateAnotherRun()
    {
        Present("R-01");Assert.AreEqual(7,g.S.pending.options[1].hours);
        var b=GameData.LoadBalance();b.eventCosts.Single(c=>c.id=="R-01").hours[1]=1;
        var other=GameEngine.New(444,b,g.Content);other.Select(other.S.candidates[0].id);other.S.day=2;other.S.activeRift=0;other.S.phase=Phase.Running;other.S.goal=Goal.Rift;
        other.PresentGeneralEvent("R-01");Assert.AreEqual(1,other.S.pending.options[1].hours);Assert.AreEqual(7,g.S.pending.options[1].hours);
        Assert.AreEqual(0,g.Content.activityEvents.Single(e=>e.id=="R-01").options[1].hours);
        Assert.IsTrue(g.Resolve(g.S.pending.id,1));Assert.AreEqual(7,g.S.hours);Assert.AreEqual(85,g.S.avatar.hunger,"명시한 허기15 외에 숨은 수색 비용을 더하지 않습니다.");
    }
    [Test] public void FourthSupplyAutomaticallyGetsOneQuarterChanceAndIsStored()
    {
        g.Content.supplies.Add(new SupplyDefinition{id="future",name="새 보급품",amount=2});g.S.phase=Phase.Morning;g.S.divinity=40000;
        int food=g.S.food,kits=g.S.medicine,shots=g.S.syringes;
        for(int i=0;i<20000;i++)g.Supply();
        int[] counts={(g.S.food-food)/5,g.S.medicine-kits,g.S.syringes-shots,g.S.additionalSupplies.Single(s=>s.id=="future").amount/2};
        foreach(int count in counts)Assert.AreEqual(.25,count/20000.0,.015);
    }
    [Test] public void ClinicRoundsOnlyFinalValuesAndClampsAtMaximum()
    {
        g.S.phase=Phase.Morning;g.S.avatar.baseHealth=115;g.S.avatar.health=20.5;g.S.avatar.sanity=20.5;
        var p=g.PreviewTreatment(false);Assert.AreEqual(101,p.healthAfter);Assert.IsTrue(g.Treat(false));Assert.AreEqual(101,g.S.avatar.health);
        g.S.hours=0;Assert.IsTrue(g.Treat(true));Assert.AreEqual(91,g.S.avatar.sanity);
        g.S.hours=0;g.S.avatar.health=114;Assert.IsTrue(g.Treat(false));Assert.AreEqual(115,g.S.avatar.health);
    }
    [Test] public void RiftEventsRequireAnOpenRiftAndACombatEligibleDay()
    {
        g.S.day=g.S.avatar.selectedDay;Assert.IsEmpty(g.EligibleEvents(Goal.Rift));
        g.S.phase=Phase.Morning;Assert.IsTrue(g.StartDay(Goal.Rift));Assert.AreEqual(Goal.Explore,g.S.goal);
        g.S.day++;g.S.activeRift=-1;Assert.IsEmpty(g.EligibleEvents(Goal.Rift));
        g.S.activeRift=0;Assert.IsNotEmpty(g.EligibleEvents(Goal.Rift));
    }
    [Test] public void ReadyBossDoesNotSkipTheDaysMinimumGeneralChoice()
    {
        g.S.demons[0].progress=50;g.S.phase=Phase.Morning;g.StartDay(Goal.Rift);g.S.scheduledConstellation=null;
        g.Tick();Assert.AreEqual(1,g.S.eventSlots);Assert.AreNotEqual("균열의 마지막 문",g.S.pending.title);
        g.Resolve(g.S.pending.id,g.S.pending.choices.Length-1);g.Tick();Assert.AreEqual("균열의 마지막 문",g.S.pending.title);Assert.AreEqual(1,g.S.eventSlots);
    }
}

