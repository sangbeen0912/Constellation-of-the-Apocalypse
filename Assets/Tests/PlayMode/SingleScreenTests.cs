using System;
using System.Collections;
using System.Linq;
using Apocalypse;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class SingleScreenTests
{
    [UnityTest] public IEnumerator WatchingScenePausesTimeSavesOnceAndReportReturnsToShelter()
    {
        string temp=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"Watching-"+Guid.NewGuid().ToString("N"));
        var root=new GameObject("Watching test");var app=root.AddComponent<GameApp>();app.saveDirectoryOverride=temp;yield return null;
        app.BeginNew(872);app.Game.Select(app.Game.S.candidates[0].id);var s=app.Game.S;
        s.phase=Phase.Running;s.scheduledConstellation="sejong";s.constellationMeetingHour=23;s.hours=3;
        app.Render();yield return null;
        Assert.IsTrue(app.OmenVisible);Assert.IsNotNull(root.GetComponentsInChildren<RectTransform>().Single(r=>r.name=="Constellation watching scene"));
        yield return new WaitForSecondsRealtime(.3f);Assert.AreEqual(3,s.hours,"연출은 게임 시간을 소비하지 않습니다.");
        root.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>().text=="계속 진행").onClick.Invoke();
        Assert.AreEqual(s.day,s.omenShownDay);Assert.IsTrue(app.LoadGame());Assert.IsFalse(app.OmenVisible);
        app.Game.S.scheduledConstellation=null;app.Game.EndDay();Assert.AreEqual(Phase.Report,app.Game.S.phase);app.Render();yield return null;
        Assert.AreEqual("쉘터",app.ActiveTab);Assert.AreEqual(4,root.GetComponentsInChildren<Button>().Count(b=>b.GetComponentInChildren<Text>().text.StartsWith("시설 이용")));
        root.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>().text=="다음 날 아침").onClick.Invoke();yield return null;
        Assert.AreEqual(Phase.Morning,app.Game.S.phase);Assert.AreEqual("쉘터",app.ActiveTab);
        Assert.IsFalse(Resources.FindObjectsOfTypeAll<Texture2D>().Any(t=>t.name=="constellation-omen-v1"));
        UnityEngine.Object.Destroy(root);yield return null;System.IO.Directory.Delete(temp,true);
    }
    public static void AssertInsideMain(GameObject root)
    {
        Canvas.ForceUpdateCanvases();var main=root.GetComponentsInChildren<RectTransform>().Single(r=>r.name=="Main");
        Assert.AreEqual(0,main.GetComponentsInChildren<ScrollRect>().Length);
        foreach(var b in main.GetComponentsInChildren<Button>())
        {
            var corners=new Vector3[4];b.GetComponent<RectTransform>().GetWorldCorners(corners);
            foreach(var p in corners){var local=main.InverseTransformPoint(p);Assert.That(local.x,Is.InRange(main.rect.xMin-1,main.rect.xMax+1),b.name);Assert.That(local.y,Is.InRange(main.rect.yMin-1,main.rect.yMax+1),b.name);}
        }
        foreach(var t in main.GetComponentsInChildren<Text>().Where(t=>!string.IsNullOrEmpty(t.text)))
            Assert.LessOrEqual(t.preferredHeight,t.rectTransform.rect.height+2,"텍스트가 잘리면 안 됩니다: "+t.text);
    }
    [UnityTest] public IEnumerator MainFacilitiesAndEveryOwnedCodexDetailFitWithoutVerticalScrolling()
    {
        string temp=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"ScreenFit-"+Guid.NewGuid().ToString("N"));
        var root=new GameObject("Screen fit");var app=root.AddComponent<GameApp>();app.saveDirectoryOverride=temp;yield return null;
        app.BeginNew(1231);yield return null;AssertInsideMain(root);
        app.Game.Select(app.Game.S.candidates[0].id);
        foreach(string tab in ArtCatalog.FacilityNames.Append("쉘터").Append("전투준비"))
        {app.Navigate(tab);yield return null;AssertInsideMain(root);}
        app.Game.S.permanent.unlocked=app.Game.Content.constellations.Select(c=>c.id).ToList();
        foreach(var c in app.Game.Content.constellations)
        {
            app.Codex.Select(c.id,app.Game.S.permanent);app.Navigate("성좌 도감");yield return null;AssertInsideMain(root);
            app.Game.S.phase=Phase.Event;app.Game.S.pending=new ChoiceEvent{id="fit-"+c.id,constellationId=c.id,title=c.title,body=c.story,choices=new[]{"이야기 확인 · 영구 해금"}};
            app.Render();yield return null;AssertInsideMain(root);app.Game.S.pending=null;app.Game.S.phase=Phase.Morning;
        }
        app.Game.S.day=2;app.Game.S.activeRift=0;
        foreach(var template in app.Game.Content.activityEvents)
        {
            app.Game.S.phase=Phase.Running;app.Game.S.goal=template.location;app.Game.S.hours=0;app.Game.S.eventSlots=0;app.Game.S.divineEventDay=0;
            Assert.IsTrue(app.Game.PresentGeneralEvent(template.id),template.id);app.Render();yield return null;AssertInsideMain(root);
            app.Game.S.pending=null;
        }
        app.Game.S.phase=Phase.Morning;
        foreach(var npc in app.Game.Content.rareNpcs)app.Game.S.candidates.Add(app.Game.CreateRare(npc.id));
        app.Navigate("인재관리실");yield return null;AssertInsideMain(root);
        root.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>().text=="다음 페이지").onClick.Invoke();yield return null;AssertInsideMain(root);
        UnityEngine.Object.Destroy(root);yield return null;System.IO.Directory.Delete(temp,true);
    }
}
