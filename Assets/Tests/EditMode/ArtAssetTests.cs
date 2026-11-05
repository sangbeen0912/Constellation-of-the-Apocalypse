using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Apocalypse;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class ArtAssetTests
{
    [Test] public void EncounterUsesOneCompressedEnemyAtlasWithoutUnusedAlliedBattleArt()
    {
        const string path="Assets/Resources/Art/Combat/encounter-enemies-v1.png";
        var texture=Resources.Load<Texture2D>("Art/Combat/encounter-enemies-v1");Assert.NotNull(texture);
        Assert.AreEqual(texture.width,texture.height);Assert.GreaterOrEqual(texture.width,1024);Assert.IsFalse(texture.isReadable);Assert.AreEqual(1,texture.mipmapCount);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);Assert.AreEqual(FilterMode.Point,importer.filterMode);
        Assert.AreEqual(TextureImporterFormat.BC7,importer.GetPlatformTextureSettings("Standalone").format);
        Assert.IsNull(Resources.Load<Texture2D>("Art/Combat/battle-actors-v1"));Assert.IsNull(Resources.Load<Texture2D>("Art/Combat/legion-enemies-v2"));
        Directory.CreateDirectory("TestResults");File.WriteAllText("TestResults/encounter-art.txt","width="+texture.width+"; height="+texture.height+"; runtimeBytes="+UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture)+"; pngBytes="+new FileInfo(path).Length);
        Resources.UnloadAsset(texture);
    }
    [Test] public void TwelveNewPortraitsAreDistinctAndAllGeneratedNamesHaveArt()
    {
        var hashes = new HashSet<string>();
        for (int i=0;i<12;i++)
        {
            string key="Art/Portraits/survivor-"+i.ToString("00")+"-v3";
            var texture=Resources.Load<Texture2D>(key);Assert.NotNull(texture,key);
            Assert.That(texture.width,Is.InRange(129,256));Assert.AreEqual(texture.width,texture.height);
            using(var sha=SHA256.Create()) hashes.Add(System.Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes("Assets/Resources/"+key+".png"))));
            Assert.IsTrue(((TextureImporter)AssetImporter.GetAtPath("Assets/Resources/"+key+".png")).DoesSourceTextureHaveAlpha());
        }
        Assert.AreEqual(12,hashes.Count);
        foreach(string surname in new[]{"서","윤","강","한","이","정","백","신"})
            foreach(string given in new[]{"도윤","서연","지후","하린","시온","유진","민서","태오"})
                Assert.NotNull(ArtCatalog.Portrait(surname+given),surname+given);
        Assert.AreEqual("Art/Portraits/survivor-01-v3",ArtCatalog.PortraitKey("서도윤"),"정면 서도윤의 외형 연결을 유지해야 합니다.");
    }

    [Test] public void NewIllustrationsRetainResolutionAndPixelImportSettings()
    {
        var textures=new List<Texture2D>();
        for(int i=0;i<12;i++)textures.Add(Resources.Load<Texture2D>("Art/Portraits/survivor-"+i.ToString("00")+"-v3"));
        for(int i=0;i<6;i++)textures.Add(ArtCatalog.Environment(i));
        foreach(string id in ArtCatalog.ConstellationIds)textures.Add(ArtCatalog.Constellation(id));
        foreach(var texture in textures)
        {
            Assert.NotNull(texture);
            var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
            Assert.AreEqual(FilterMode.Point,texture.filterMode);Assert.IsFalse(importer.mipmapEnabled);
            Assert.IsFalse(texture.isReadable,"표시 전용 아트의 CPU 복사본을 보관하지 않습니다.");
            Assert.AreEqual(TextureImporterCompression.Uncompressed,importer.textureCompression);
            Assert.AreEqual(TextureImporterNPOTScale.None,importer.npotScale);
            importer.GetSourceTextureWidthAndHeight(out int width,out int height);
            if(AssetDatabase.GetAssetPath(texture).Contains("/Portraits/"))
            { Assert.Greater(width,256);Assert.Greater(height,256);Assert.LessOrEqual(texture.width,256);Assert.AreEqual(256,importer.maxTextureSize); }
            else if(AssetDatabase.GetAssetPath(texture).Contains("/Environments/"))
            {
                int cap=texture.name=="clinic-v2"||texture.name=="warehouse-v2"||texture.name=="personnel-v2"?512:1024;
                Assert.AreEqual(cap,importer.maxTextureSize);Assert.AreEqual(cap,texture.width);
                Assert.AreEqual(height/(float)width,texture.height/(float)texture.width,.003,"배경 비율을 보존합니다.");
            }
            else { Assert.AreEqual(width,texture.width);Assert.AreEqual(height,texture.height); }
        }
    }

    [Test] public void AllTwentyConstellationsHaveDistinctLandscapeArtAndUnknownIdsAreRejected()
    {
        var content=GameData.LoadContent();Assert.AreEqual(20,content.constellations.Count);
        CollectionAssert.AreEquivalent(content.constellations.ConvertAll(c=>c.id),ArtCatalog.ConstellationIds);
        var hashes=new HashSet<string>();
        foreach(var constellation in content.constellations)
        {
            var texture=ArtCatalog.Constellation(constellation.id);Assert.NotNull(texture,constellation.id);
            Assert.Greater(texture.width,texture.height,"성좌는 가로형 장면 일러스트입니다.");
            using(var sha=SHA256.Create())hashes.Add(System.Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(AssetDatabase.GetAssetPath(texture)))));
        }
        Assert.AreEqual(20,hashes.Count,"다른 성좌에 동일한 대체 그림을 쓰지 않습니다.");
        Assert.IsNull(ArtCatalog.Constellation("unknown"));Assert.IsNull(ArtCatalog.Constellation(null));
        Assert.IsNull(ArtCatalog.Constellation("../Portraits/survivor-01-v3"));
    }

    [Test] public void IllustrationLeaseUnloadsPreviousAndRetainsSharedOwner()
    {
        using(var first=new ConstellationArtLease())
        using(var second=new ConstellationArtLease())
        {
            var original=first.Use("sejong");Assert.NotNull(original);
            int originalId=original.GetInstanceID();
            Assert.AreSame(original,first.Use("sejong"));Assert.AreEqual(1,ConstellationArtLease.ResidentCount);
            Assert.AreSame(original,second.Use("sejong"));
            var replacement=first.Use("zeus");Assert.NotNull(replacement);
            int replacementId=replacement.GetInstanceID();
            Assert.IsTrue(original!=null,"다른 화면에서 사용 중인 아트는 반환하지 않습니다.");
            Assert.AreEqual(2,ConstellationArtLease.ResidentCount);
            second.Dispose();Assert.IsFalse(IsTextureResident(originalId),"마지막 사용자가 닫으면 실제 Unity 텍스처를 반환합니다.");
            Assert.AreEqual(1,ConstellationArtLease.ResidentCount);
            Assert.IsNull(first.Use("unknown"));Assert.IsFalse(IsTextureResident(replacementId));
            Assert.AreEqual(0,ConstellationArtLease.ResidentCount);
        }
    }
    static bool IsTextureResident(int instanceId)
    {
        // In the editor a persistent asset's managed wrapper can compare non-null
        // even after its native data is unloaded. Inspect the loaded native set.
        foreach(var texture in Resources.FindObjectsOfTypeAll<Texture2D>())
            if(texture.GetInstanceID()==instanceId)return true;
        return false;
    }
}
