using System;
using System.IO;
using UnityEngine;
namespace Apocalypse
{
    public sealed class SaveManager
    {
        readonly string path;
        public string LastError { get; private set; }
        public string PathName { get { return path; } }
        public SaveManager(string directory) { Directory.CreateDirectory(directory); path = Path.Combine(directory, "constellation-v1.json"); }
        public bool Exists { get { return File.Exists(path) || File.Exists(path + ".bak"); } }
        // Called only after the player confirms a full new game (or in an isolated test).
        // The backup must also contain the fresh run, rather than resurrecting old unlocks.
        public bool ResetProgress(GameState fresh)
        {
            if(!Write(fresh))return false;
            try
            {
                string backup=path+".bak",temp=path+".bak.tmp";
                File.Copy(path,temp,true);ReadFile(temp);
                if(File.Exists(backup))File.Replace(temp,backup,null);else File.Move(temp,backup);
                LastError="";return true;
            }
            catch(Exception e)
            {
                // The fresh primary has already committed. Never restore the old in-memory
                // run over it merely because refreshing its recovery copy failed.
                LastError="새 게임은 저장됐지만 백업 갱신에 실패했습니다: "+e.Message;return true;
            }
        }
        public bool Write(GameState state)
        {
            try
            {
                string temp = path + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(state, true)); ReadFile(temp);
                if (File.Exists(path))
                {
                    try { ReadFile(path); File.Copy(path, path + ".bak", true); }
                    catch (Exception) { /* Keep the valid backup if the primary was corrupt. */ }
                    File.Replace(temp, path, null);
                }
                else File.Move(temp, path);
                LastError = ""; return true;
            }
            catch (Exception e) { LastError = e.Message; return false; }
        }
        GameState ReadFile(string file)
        {
            var s = JsonUtility.FromJson<GameState>(File.ReadAllText(file));
            if (s == null || s.version != Rules.SaveVersion || string.IsNullOrEmpty(s.runId) || s.rng == 0 || s.demons == null || s.demons.Count != 7 || s.permanent == null)
                throw new InvalidDataException("지원하지 않거나 손상된 저장 데이터입니다.");
            if(s.rulesRevision<Rules.RulesRevision && file!=path+".tmp")
            {
                string archive=file+(s.rulesRevision<3?".pre-20261030.bak":".pre-20261104.bak");
                if(!File.Exists(archive))File.Copy(file,archive,false);
            }
            return s;
        }
        public GameState Read()
        {
            try { var s = ReadFile(path); LastError = ""; return s; }
            catch (Exception primary)
            {
                try { var s = ReadFile(path + ".bak"); LastError = "주 저장 오류로 백업을 불러왔습니다: " + primary.Message; return s; }
                catch (Exception) { LastError = primary.Message; return null; }
            }
        }
    }
    public static class GameData
    {
        public static Balance LoadBalance() { return JsonUtility.FromJson<Balance>(Resources.Load<TextAsset>("balance").text); }
        public static Content LoadContent()
        {
            var c=JsonUtility.FromJson<Content>(Resources.Load<TextAsset>("content").text);
            var activity=JsonUtility.FromJson<Content>(Resources.Load<TextAsset>("activity-content").text);
            c.activityEvents=activity.activityEvents;c.relicDefinitions=activity.relicDefinitions;c.rareNpcs=activity.rareNpcs;c.supplies=activity.supplies;
            return c;
        }
    }
}
