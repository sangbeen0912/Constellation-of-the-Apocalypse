using System;
using System.Linq;
using Apocalypse;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

public class RequestedChangesTests
{
    GameEngine g;
    [SetUp] public void Setup()
    {
        g=GameEngine.New(721,GameData.LoadBalance(),GameData.LoadContent());g.Select(g.S.candidates[0].id);
        g.S.day=2;g.S.activeRift=0;g.S.avatar.personality=Temperament.Brave;g.S.avatar.baseAttack=1000;g.S.avatar.baseHealth=10000;g.S.avatar.health=10000;g.S.food=10000;
    }
    [Test] public void OnlyTwoGoalsAndLegacyGoalsMigrate()
    {
        Assert.AreEqual(2,Enum.GetValues(typeof(Goal)).Length);
        foreach(int old in new[]{0,1,2}){g.S.goal=(Goal)old;var loaded=new GameEngine(JsonUtility.FromJson<GameState>(JsonUtility.ToJson(g.S)),g.B,g.Content);Assert.AreEqual(Goal.Explore,loaded.S.goal);}
    }
    [Test] public void RiftRequiresFiftyKillPointsInsteadOfVisitCounts()
    {
        var d=g.S.demons[0];
        g.S.constellationRolledDay=g.S.day;Assert.IsTrue(g.StartDay(Goal.Rift));
        for(int battle=0;battle<16;battle++)Assert.IsTrue(g.Battle(EnemyKind.Normal,3,Sin.Pride));
        Assert.AreEqual(48,d.progress);Assert.IsFalse(g.CanReachDemon(Sin.Pride));Assert.IsFalse(g.Battle(EnemyKind.Demon,1,Sin.Pride));
        Assert.IsTrue(g.Battle(EnemyKind.Normal,1,Sin.Pride));Assert.AreEqual(49,d.progress);Assert.IsFalse(g.CanReachDemon(Sin.Pride));
        Assert.IsTrue(g.Battle(EnemyKind.Special,1,Sin.Pride));Assert.AreEqual(52,d.progress);Assert.IsTrue(g.CanReachDemon(Sin.Pride));
        Assert.AreEqual(Phase.Running,g.S.phase,"50점에 도달해도 즉시 악마 이벤트나 전투를 시작하지 않습니다.");
        int money=g.S.divinity;Assert.IsTrue(g.Battle(EnemyKind.Demon,1,Sin.Pride));Assert.AreEqual(money+400,g.S.divinity);
        Assert.AreEqual(0,d.progress);
    }
    [Test] public void RepeatedStartsWithoutKillsCannotEarnProgress()
    {g.S.constellationRolledDay=g.S.day;g.StartDay(Goal.Rift);g.S.phase=Phase.Morning;g.StartDay(Goal.Rift);Assert.AreEqual(0,g.S.demons[0].progress);}
    [Test] public void SpecialZombieIsExactlyOneEvenAtPublicEntryPoint()
    {g.S.phase=Phase.Running;int money=g.S.divinity;g.Battle(EnemyKind.Special,5,Sin.Pride);Assert.AreEqual(money+15,g.S.divinity);Assert.IsTrue(g.S.logs.Any(x=>x.Contains("특수 좀비 × 1")));Assert.AreEqual(1,g.S.totalKills);}
    [Test] public void ExploreCombatAwardsProgressAndDivinitySeparately()
    {g.S.phase=Phase.Running;g.S.goal=Goal.Explore;int money=g.S.divinity;g.Battle(EnemyKind.Normal,3,Sin.Pride);Assert.AreEqual(3,g.S.demons[0].progress);Assert.AreEqual(money+3,g.S.divinity);}
    [Test] public void KillProgressSurvivesSaving()
    {g.S.demons[0].progress=19;var copy=new GameEngine(JsonUtility.FromJson<GameState>(JsonUtility.ToJson(g.S)),g.B,g.Content);Assert.AreEqual(19,copy.S.demons[0].progress);Assert.IsFalse(copy.CanReachDemon(Sin.Pride));}
    [Test] public void NoBossEventOnFirstAssaultDay()
    {g.S.constellationRolledDay=g.S.day;g.StartDay(Goal.Rift);g.S.hours=6;g.S.nextImportantHour=6;g.Tick();Assert.AreNotEqual("균열의 마지막 문",g.S.pending.title);Assert.IsTrue(g.EligibleEvents(Goal.Rift).Any(e=>e.id==g.S.pending.templateId));}
    [Test] public void AssaultPolicyHasMoreCombatThanExplore()
    {
        int[] kills=new int[2];
        for(int mode=0;mode<2;mode++)for(uint seed=1;seed<=40;seed++)
        {
            var x=GameEngine.New(seed,g.B,g.Content);x.Select(x.S.candidates[0].id);x.S.avatar.personality=Temperament.Brave;x.S.avatar.baseHealth=100000;x.S.avatar.health=100000;x.S.avatar.baseAttack=1000;x.S.food=100000;x.S.day=2;x.S.activeRift=0;x.S.constellationRolledDay=2;x.StartDay(mode==0?Goal.Explore:Goal.Rift);
            for(int step=0;step<200&&x.S.phase!=Phase.Report;step++){if(x.S.phase==Phase.Event)x.Resolve(x.S.pending.id,0);else x.Tick();}
            kills[mode]+=x.S.totalKills;
        }
        Assert.Greater(kills[1],kills[0]*1.5);
    }
    [Test] public void CodexShowsOnlyOwnedAndGroupsToggleIndependently()
    {
        var index=new CodexIndex();Assert.IsEmpty(index.Entries(g.Content,g.S.permanent,0));g.S.permanent.unlocked.Add("sejong");g.S.permanent.unlocked.Add("zeus");
        CollectionAssert.AreEqual(new[]{"sejong"},index.Entries(g.Content,g.S.permanent,0).Select(x=>x.id));Assert.IsFalse(index.Select("yog",g.S.permanent));Assert.IsTrue(index.Select("zeus",g.S.permanent));
        index.Toggle(0);Assert.IsFalse(index.IsExpanded(0));Assert.IsTrue(index.IsExpanded(2));index.Back();Assert.IsNull(index.SelectedId);
    }
    [Test] public void AcquisitionStoryIsSnapshotAndNotNewCatalogText()
    {
        g.S.phase=Phase.Event;g.S.pending=new ChoiceEvent{id="snapshot",constellationId="sejong",title="그날의 이야기",body="그날 실제로 읽은 문장",choices=new[]{"확인"}};
        g.Resolve("snapshot",0);g.Content.Find("sejong").story="다음 버전에서 수정된 문장";
        Assert.AreEqual("그날 실제로 읽은 문장",g.S.permanent.stories.Single().body);Assert.AreEqual("그날의 이야기",g.S.permanent.stories.Single().title);
    }
    [Test] public void AllConstellationsHaveIdentityAffiliationAndReadableLabel()
    {foreach(var c in g.Content.constellations){Assert.IsNotEmpty(c.constellationName);Assert.IsNotEmpty(c.affiliation);StringAssert.Contains(c.name,CodexIndex.DisplayName(c));StringAssert.Contains(Rules.Grades[c.grade],CodexIndex.DisplayName(c));}Assert.AreEqual("올림포스",g.Content.Find("zeus").affiliation);}
    [Test] public void TwelveDistinctIconsArePixelExactAndNameMappingStable()
    {
        Assert.AreEqual(12,PortraitIdentity.Count);Assert.AreEqual(1,PortraitIdentity.Index("서도윤"));
        foreach(string surname in new[]{"서","윤","강","한","이","정","백","신"})
            foreach(string given in new[]{"도윤","서연","지후","하린","시온","유진","민서","태오"})
            {
                int index=PortraitIdentity.Index(surname+given);Assert.That(index,Is.InRange(0,11));
                Assert.AreEqual(index,PortraitIdentity.Index(surname+given));
                if(new[]{"서연","하린","유진","민서"}.Contains(given))Assert.GreaterOrEqual(index,6);
                else Assert.Less(index,6);
            }
    }
    [Test] public void FourFacilitiesAreDistinctAndImportedWithoutFiltering()
    {
        var hashes=new System.Collections.Generic.HashSet<string>();
        for(int i=0;i<4;i++){var t=ArtCatalog.Environment(i);Assert.NotNull(t);Assert.Greater(t.width,480);Assert.AreEqual(FilterMode.Point,t.filterMode);var path=AssetDatabase.GetAssetPath(t);var importer=(TextureImporter)AssetImporter.GetAtPath(path);Assert.IsFalse(importer.mipmapEnabled);Assert.AreEqual(TextureImporterCompression.Uncompressed,importer.textureCompression);using(var sha=System.Security.Cryptography.SHA256.Create())hashes.Add(Convert.ToBase64String(sha.ComputeHash(System.IO.File.ReadAllBytes(path))));}
        Assert.AreEqual(4,hashes.Count);
    }
}
