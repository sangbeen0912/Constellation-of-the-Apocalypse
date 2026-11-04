using System;
using System.Collections;
using System.IO;
using System.Linq;
using Apocalypse;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class NewGameWarningTests
{
    Button Find(GameObject root,string label){return root.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>().text==label);}
    GameObject Top(Button button)
    {
        Canvas.ForceUpdateCanvases();var rect=button.GetComponent<RectTransform>();
        var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center))};
        var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);return hits.First().gameObject;
    }
    void Click(Button button)
    {
        var top=Top(button);Assert.AreSame(button,top.GetComponentInParent<Button>(),"버튼이 가리지 않아야 합니다.");
        ExecuteEvents.ExecuteHierarchy(top,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
    }
    [UnityTest] public IEnumerator OneWarningCanCancelWithoutWritingAndConfirmationResetsEverything()
    {
        string temp=Path.Combine(Path.GetTempPath(),"ResetUI-"+Guid.NewGuid().ToString("N"));
        var root=new GameObject("Reset UI");var app=root.AddComponent<GameApp>();app.saveDirectoryOverride=temp;yield return null;
        app.BeginNew(511);if(app.Game.S.avatar==null)app.Game.Select(app.Game.S.candidates[0].id);var old=app.Game;
        old.S.permanent.unlocked.Add("sejong");old.S.permanent.completedStories.Add("story");old.S.permanent.stories.Add(new AcquiredStory{constellationId="sejong",body="old"});
        old.S.permanent.victories=3;old.S.permanent.named.Add(new NamedZombieRecord{id="old",name="old",health=99});
        old.S.permanent.deaths.Add(new DeathRecord{name="old"});old.S.relics.Add(new Relic{id="gungnir",name="궁니르"});
        old.S.recruitedRare.Add("lyra");old.S.totalKills=99;old.S.totalSeals=7;old.S.divinity=9999;old.S.food=9999;
        Assert.IsTrue(app.Saves.Write(old.S));app.Navigate("쉘터");yield return null;Click(Find(root,"타이틀"));yield return null;
        string before=File.ReadAllText(app.Saves.PathName);
        Click(Find(root,"새로 시작"));yield return null;
        Assert.AreEqual(1,root.GetComponentsInChildren<RectTransform>().Count(r=>r.name=="New game warning"));
        Assert.AreSame(old,app.Game);Assert.AreEqual(before,File.ReadAllText(app.Saves.PathName));
        Assert.IsFalse(Find(root,"이어하기").interactable);Assert.IsFalse(Find(root,"새로 시작").interactable);
        Assert.AreSame(Find(root,"취소").gameObject,EventSystem.current.currentSelectedGameObject);
        Assert.AreNotSame(Find(root,"이어하기"),Top(Find(root,"이어하기")).GetComponentInParent<Button>(),"경고창 뒤의 시작/이어하기는 눌리지 않아야 합니다.");
        Click(Find(root,"취소"));yield return null;Assert.AreSame(old,app.Game);Assert.AreEqual(before,File.ReadAllText(app.Saves.PathName));
        Click(Find(root,"새로 시작"));yield return null;Click(Find(root,"모두 초기화하고 시작"));yield return null;
        var s=app.Game.S;Assert.AreNotEqual(old.S.runId,s.runId);Assert.AreEqual(Phase.Morning,s.phase);Assert.AreEqual(1,s.day);Assert.IsEmpty(s.candidates);Assert.NotNull(s.avatar);Assert.AreEqual(Species.Human,s.avatar.species);
        Assert.AreEqual(0,s.permanent.unlocked.Count+s.permanent.completedStories.Count+s.permanent.stories.Count+s.permanent.deaths.Count+s.permanent.named.Count+s.permanent.victories);
        Assert.IsEmpty(s.relics);Assert.IsEmpty(s.recruitedRare);Assert.AreEqual(0,s.totalKills+s.totalSeals);
        var balance=GameData.LoadBalance();Assert.AreEqual(balance.initialDivinity,s.divinity);Assert.AreEqual(balance.initialFood,s.food);Assert.AreEqual(balance.initialMedicine,s.medicine);
        Assert.IsFalse(root.GetComponentsInChildren<RectTransform>().Any(r=>r.name=="New game warning"));
        Assert.IsTrue(app.LoadGame());Assert.AreEqual(s.runId,app.Game.S.runId);Assert.IsEmpty(app.Game.S.permanent.unlocked);
        var backup=JsonUtility.FromJson<GameState>(File.ReadAllText(app.Saves.PathName+".bak"));Assert.AreEqual(s.runId,backup.runId);Assert.IsEmpty(backup.permanent.unlocked);
        UnityEngine.Object.Destroy(root);yield return null;Directory.Delete(temp,true);
    }
}
