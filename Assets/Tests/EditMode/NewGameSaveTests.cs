using System;
using System.IO;
using Apocalypse;
using NUnit.Framework;
public class NewGameSaveTests
{
    string directory;SaveManager saves;
    [SetUp] public void Setup(){directory=Path.Combine(Path.GetTempPath(),"ResetSave-"+Guid.NewGuid().ToString("N"));saves=new SaveManager(directory);}
    [TearDown] public void Cleanup(){Directory.Delete(directory,true);}
    GameEngine New(uint seed){return GameEngine.New(seed,GameData.LoadBalance(),GameData.LoadContent());}
    [Test] public void CompactSavePreservesFormattedLegacyDataAndRecoveryBackup()
    {
        var g=New(916);g.S.day=12;g.S.divinity=327;g.S.permanent.unlocked.Add("sejong");
        string formatted=UnityEngine.JsonUtility.ToJson(g.S,true);File.WriteAllText(saves.PathName,formatted);
        var loaded=saves.Read();Assert.NotNull(loaded);string normalized=UnityEngine.JsonUtility.ToJson(loaded);
        Assert.IsTrue(saves.Write(loaded));Assert.Less(new FileInfo(saves.PathName).Length,System.Text.Encoding.UTF8.GetByteCount(formatted));
        Assert.AreEqual(normalized,UnityEngine.JsonUtility.ToJson(saves.Read()));
        loaded.divinity=501;Assert.IsTrue(saves.Write(loaded));Assert.AreEqual(501,saves.Read().divinity);
        File.WriteAllText(saves.PathName,"corrupt primary");
        var backup=saves.Read();Assert.AreEqual(327,backup.divinity);Assert.AreEqual(12,backup.day);Assert.Contains("sejong",backup.permanent.unlocked);
    }
    [Test] public void FullResetClearsPrimaryAndRecoveryCopySoOldUnlocksCannotReturn()
    {
        var old=New(1);old.S.permanent.unlocked.Add("sejong");old.S.permanent.victories=9;old.S.day=44;old.S.divinity=9999;
        Assert.IsTrue(saves.Write(old.S));var fresh=New(2);Assert.IsTrue(saves.ResetProgress(fresh.S));
        Assert.AreEqual(fresh.S.runId,saves.Read().runId);Assert.IsEmpty(saves.Read().permanent.unlocked);
        File.WriteAllText(saves.PathName,"corrupt primary");
        var recovered=saves.Read();Assert.AreEqual(fresh.S.runId,recovered.runId);Assert.AreEqual(1,recovered.day);
        Assert.AreEqual(0,recovered.permanent.victories);Assert.IsEmpty(recovered.permanent.unlocked);Assert.AreEqual(fresh.S.divinity,recovered.divinity);
    }
    [Test] public void FailedPrimaryReplacementDoesNotReplaceExistingProgress()
    {
        var old=New(3);old.S.permanent.unlocked.Add("zeus");Assert.IsTrue(saves.Write(old.S));
        using(var locked=new FileStream(saves.PathName,FileMode.Open,FileAccess.Read,FileShare.Read))
            Assert.IsFalse(saves.ResetProgress(New(4).S));
        Assert.AreEqual(old.S.runId,saves.Read().runId);CollectionAssert.AreEqual(new[]{"zeus"},saves.Read().permanent.unlocked);
    }
}
