using System.Linq;
using Apocalypse;
using NUnit.Framework;
using UnityEngine;
public class PreparationBatchTests
{
    GameEngine New(){var g=GameEngine.New(992,GameData.LoadBalance(),GameData.LoadContent());g.S.divinity=1000;return g;}
    [TestCase(Stat.Health)][TestCase(Stat.Attack)]
    public void BatchQuoteCrossesTierAtTheSamePriceAsIndividualUpgrades(Stat stat)
    {
        var batch=New();var single=New();
        Assert.IsTrue(batch.UpgradeBatch(stat,9));Assert.IsTrue(single.UpgradeBatch(stat,9));
        Assert.AreEqual(190,batch.UpgradeBatchCost(stat,10));
        int before=batch.S.divinity;Assert.IsTrue(batch.UpgradeBatch(stat,10));
        for(int i=0;i<10;i++)Assert.IsTrue(single.Invest(stat,single.UpgradeCost(stat)));
        Assert.AreEqual(before-190,batch.S.divinity);Assert.AreEqual(19,batch.UpgradeCount(stat));
        Assert.AreEqual(single.S.divinity,batch.S.divinity);Assert.AreEqual(single.S.avatar.MaxHealth,batch.S.avatar.MaxHealth);Assert.AreEqual(single.S.avatar.Attack,batch.S.avatar.Attack);
        Assert.AreEqual(0,batch.UpgradeCount(stat==Stat.Health?Stat.Attack:Stat.Health));
    }
    [TestCase(Stat.Health)][TestCase(Stat.Attack)]
    public void UnaffordableBatchDoesNotPartiallyUpgrade(Stat stat)
    {
        var g=New();g.S.divinity=99;string before=JsonUtility.ToJson(g.S);int writes=0;g.Checkpoint+=()=>writes++;
        Assert.IsFalse(g.UpgradeBatch(stat,10));Assert.AreEqual(before,JsonUtility.ToJson(g.S));Assert.AreEqual(0,writes);
        g.S.phase=Phase.Running;g.S.divinity=1000;before=JsonUtility.ToJson(g.S);
        Assert.IsFalse(g.UpgradeBatch(stat,10));Assert.AreEqual(before,JsonUtility.ToJson(g.S));
    }
    [TestCase(0)][TestCase(11)][TestCase(-1)]
    public void InvalidQuantitiesDoNotSpendOrRoll(int count)
    {
        var g=New();string before=JsonUtility.ToJson(g.S);
        Assert.AreEqual(-1,g.UpgradeBatchCost(Stat.Health,count));Assert.IsFalse(g.UpgradeBatch(Stat.Health,count));Assert.IsFalse(g.SupplyBatch(count));
        Assert.AreEqual(before,JsonUtility.ToJson(g.S));
    }
    [Test] public void FiveSuppliesPreserveFiveIndependentRollsAndAutoEating()
    {
        var batch=New();var single=New();batch.S.avatar.hunger=single.S.avatar.hunger=40;
        Assert.IsTrue(batch.SupplyBatch(5));for(int i=0;i<5;i++)Assert.IsTrue(single.Supply());
        Assert.AreEqual(single.S.rng,batch.S.rng);Assert.AreEqual(single.S.divinity,batch.S.divinity);
        Assert.AreEqual(single.S.food,batch.S.food);Assert.AreEqual(single.S.medicine,batch.S.medicine);Assert.AreEqual(single.S.syringes,batch.S.syringes);
        Assert.AreEqual(single.S.avatar.hunger,batch.S.avatar.hunger);Assert.AreEqual(single.S.controlLocked,batch.S.controlLocked);
        CollectionAssert.AreEqual(single.S.logs,batch.S.logs);CollectionAssert.AreEqual(single.S.additionalSupplies.Select(x=>x.id+":"+x.amount),batch.S.additionalSupplies.Select(x=>x.id+":"+x.amount));
        Assert.AreEqual(0,batch.S.hours);
    }
    [Test] public void InsufficientSupplyBatchDoesNotConsumeAnyRoll()
    {
        var g=New();g.S.divinity=4;string before=JsonUtility.ToJson(g.S);Assert.IsFalse(g.SupplyBatch(5));Assert.AreEqual(before,JsonUtility.ToJson(g.S));
    }
}
