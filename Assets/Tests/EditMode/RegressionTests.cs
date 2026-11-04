using System;
using System.Linq;
using Apocalypse;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class RegressionTests
{
    GameEngine g;
    [SetUp] public void Setup(){g=GameEngine.New(555,GameData.LoadBalance(),GameData.LoadContent());g.Select(g.S.candidates[0].id);g.S.day=2;g.S.phase=Phase.Running;g.S.activeRift=0;g.S.goal=Goal.Rift;g.S.demons[0].progress=Rules.RiftProgressRequired;g.S.avatar.baseHealth=10000;g.S.avatar.health=10000;g.S.food=10000;g.S.avatar.baseAttack=20;g.S.avatar.hunger=100;g.S.avatar.sanity=100;}
    int FirstDamage(){string line=g.S.logs.First(l=>l.Contains("攻撃")||l.Contains("공격 "));return int.Parse(line.Split(new[]{"공격 "},StringSplitOptions.None)[1].Split(' ')[0]);}
    [TestCase("alexander",EnemyKind.Normal,1,100,0,22)]
    [TestCase("musashi",EnemyKind.Normal,1,100,0,23)]
    [TestCase("genghis",EnemyKind.Special,1,100,3,26)]
    [TestCase("heracles",EnemyKind.Normal,1,50,0,28)]
    [TestCase("heracles",EnemyKind.Normal,1,49,0,20)]
    [TestCase("artemis",EnemyKind.Named,1,100,0,27)]
    [TestCase("arthur",EnemyKind.Demon,1,100,0,27)]
    [TestCase("zeus",EnemyKind.Demon,1,100,0,50)]
    [TestCase("tesla",EnemyKind.Normal,1,100,0,26)]
    [TestCase("curie",EnemyKind.Normal,1,100,0,28)]
    public void DamageEffectsUseCorrectContexts(string ability,EnemyKind kind,int count,int hunger,int wins,int expected)
    {
        g.S.avatar.inherited.Add(ability);g.S.normalWins=wins;g.S.avatar.hunger=hunger;g.B.teslaChance=1;g.B.curieChance=1;
        g.B.normalHp=100;g.B.specialHp=100;g.B.demonHp=100;
        var named=kind==EnemyKind.Named?new NamedZombieRecord{id="n",name="past",health=100,attack=1}:null;
        g.Battle(kind,count,Sin.Pride,named);Assert.AreEqual(expected,FirstDamage());
    }
    [TestCase("napoleon",EnemyKind.Normal,1,100,9)]
    [TestCase("leonidas",EnemyKind.Special,1,30,6)]
    [TestCase("yi",EnemyKind.Normal,3,100,21)]
    public void DefenseEffects(string ability,EnemyKind kind,int enemies,int percentHealth,int expected)
    {
        g.S.avatar.inherited.Add(ability);g.S.avatar.health=g.S.avatar.MaxHealth*percentHealth/100.0;g.B.normalHp=100;g.B.specialHp=100;g.B.normalAttack=10;
        g.Battle(kind,enemies,Sin.Pride);string line=g.S.logs.First(l=>l.Contains("피격 "));int hit=int.Parse(line.Split(new[]{"피격 "},StringSplitOptions.None)[1].Split(' ')[0]);Assert.AreEqual(expected,hit);
    }
    [Test] public void SejongAndJoanReduceStressWithFinalRounding(){g.S.avatar.inherited.Add("sejong");g.Stress(20,new[]{Sin.Envy},true);Assert.AreEqual(83,g.S.avatar.sanity);g.S.avatar.inherited.Add("joan");g.S.avatar.sanity=30;g.Stress(20,new[]{Sin.Wrath});Assert.AreEqual(15,g.S.avatar.sanity);}
    [Test] public void HuaTreatmentBoostAndTime(){g.S.phase=Phase.Morning;g.S.avatar.health=1;g.S.avatar.inherited.Add("hua");g.Treat(false,false,true);Assert.AreEqual(1+Rules.Round(g.S.avatar.MaxHealth*.7*1.15),g.S.avatar.health);Assert.AreEqual(2,g.S.hours);}
    [Test] public void NamedAndSealedDemonCannotPayTwice(){var n=new NamedZombieRecord{id="unique",health=1,attack=1};g.Battle(EnemyKind.Named,1,Sin.Pride,n);int div=g.S.divinity;Assert.IsFalse(g.Battle(EnemyKind.Named,1,Sin.Pride,n));Assert.AreEqual(div,g.S.divinity);g.S.avatar.baseAttack=1000;g.Battle(EnemyKind.Demon,1,Sin.Pride);div=g.S.divinity;Assert.IsFalse(g.Battle(EnemyKind.Demon,1,Sin.Pride));Assert.AreEqual(div,g.S.divinity);}
    [Test] public void SavedPendingEventCannotBeAppliedTwice(){g.S.pending=new ChoiceEvent{id="one",constellationId="sejong",choices=new[]{"확인"}};g.S.phase=Phase.Event;var copy=new GameEngine(JsonUtility.FromJson<GameState>(JsonUtility.ToJson(g.S)),g.B,g.Content);Assert.IsTrue(copy.Resolve("one",0));Assert.IsFalse(copy.Resolve("one",0));Assert.AreEqual(1,copy.S.permanent.unlocked.Count);}
    [Test] public void CandidateCapacityRoutesOverflowToAdministration(){g.S.avatar.inherited.Add("cleopatra");g.B.scoutSuccess=1;int startingPopulation=g.S.candidates.Count+1;for(int i=0;i<20;i++)g.Recruit();Assert.AreEqual(9,g.S.candidates.Count);Assert.AreEqual(startingPopulation+20-Rules.MaxNpc,g.S.administrators);g.Validate();}
    [Test] public void ZhugeAndLeonardoChangeOnlyTheirActionResults()
    {
        g.S.demons[0].progress=0;bool scout=false,search=false;
        for(uint seed=1000;seed<2000&&(!scout||!search);seed++)
        {
            g.S.rng=seed*99991;string saved=JsonUtility.ToJson(g.S);
            var baseline=new GameEngine(JsonUtility.FromJson<GameState>(saved),g.B,g.Content);
            var modified=new GameEngine(JsonUtility.FromJson<GameState>(saved),g.B,g.Content);modified.S.avatar.inherited.AddRange(new[]{"zhuge","leonardo"});
            baseline.Tick();modified.Tick();
            if(baseline.S.logs.Any(l=>l.Contains("폐허 정찰"))){Assert.AreEqual(3,baseline.S.hours,.0001);Assert.AreEqual(2.7,modified.S.hours,.0001);scout=true;}
            if(baseline.S.logs.Any(l=>l.Contains("물자 수색 · 식량 +"))){int loot=baseline.S.food-g.S.food;Assert.AreEqual(Rules.Round(loot*1.15),modified.S.food-g.S.food);search=true;}
        }
        Assert.IsTrue(scout&&search);
    }
    [Test] public void CleopatraAddsFifteenPercentagePointsToRecruitment()
    {
        bool found=false;
        for(uint seed=10000;seed<200000;seed+=997)
        {
            g.S.rng=seed;double roll=g.Rng.Next();if(roll<.6||roll>=.75)continue;
            g.S.rng=seed;var other=new GameEngine(JsonUtility.FromJson<GameState>(JsonUtility.ToJson(g.S)),g.B,g.Content);other.S.avatar.inherited.Add("cleopatra");
            int before=g.S.candidates.Count;g.Recruit();other.Recruit();Assert.AreEqual(before,g.S.candidates.Count);Assert.AreEqual(before+1,other.S.candidates.Count);found=true;break;
        }
        Assert.IsTrue(found);
    }
    [Test] public void RealSceneAndReusablePrefabsHaveNoMissingReferences()
    {
        var scene=AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Apocalypse.unity");Assert.NotNull(scene);
        foreach(string path in new[]{"Assets/Prefabs/GameButton.prefab","Assets/Prefabs/GamePanel.prefab"}){var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);Assert.NotNull(prefab);Assert.IsTrue(prefab.GetComponents<Component>().All(c=>c!=null));}
        var button=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GameButton.prefab");Assert.NotNull(button.GetComponent<Button>().targetGraphic);Assert.NotNull(button.GetComponentInChildren<Text>());
    }
}

