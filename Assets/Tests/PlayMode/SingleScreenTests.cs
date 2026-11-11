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
        app.BeginNew(872);if(app.Game.S.avatar==null)app.Game.Select(app.Game.S.candidates[0].id);var s=app.Game.S;
        s.phase=Phase.Running;s.scheduledConstellation="sejong";s.constellationMeetingHour=23;s.hours=3;
        app.Render();yield return null;
        Assert.IsTrue(app.OmenVisible);Assert.IsNotNull(root.GetComponentsInChildren<RectTransform>().Single(r=>r.name=="Constellation watching scene"));
        Assert.AreEqual(1,root.GetComponentsInChildren<Button>().Count(b=>b.interactable));
        Assert.AreEqual("계속 진행",root.GetComponentsInChildren<Button>().Single(b=>b.interactable).GetComponentInChildren<Text>().text);
        yield return new WaitForSecondsRealtime(.3f);Assert.AreEqual(3,s.hours,"연출은 게임 시간을 소비하지 않습니다.");
        root.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>().text=="계속 진행").onClick.Invoke();
        Assert.AreEqual(s.day,s.omenShownDay);Assert.IsTrue(app.LoadGame());Assert.IsFalse(app.OmenVisible);
        app.Game.S.scheduledConstellation=null;app.Game.EndDay();Assert.AreEqual(Phase.Report,app.Game.S.phase);app.Render();yield return null;
        Assert.AreEqual("쉘터",app.ActiveTab);Assert.AreEqual(4,root.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("Facility tab ")));
        root.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>().text=="다음 날 아침").onClick.Invoke();yield return null;
        Assert.AreEqual(Phase.Morning,app.Game.S.phase);Assert.AreEqual("쉘터",app.ActiveTab);
        Assert.IsFalse(Resources.FindObjectsOfTypeAll<Texture2D>().Any(t=>t.name=="constellation-omen-v1"));
        UnityEngine.Object.Destroy(root);yield return null;System.IO.Directory.Delete(temp,true);
    }
    public static void AssertInsideMain(GameObject root)
    {
        Canvas.ForceUpdateCanvases();var main=root.GetComponentsInChildren<RectTransform>().Single(r=>r.name=="Main");
        bool field=main.GetComponentsInChildren<RectTransform>().Any(r=>r.name=="Location stage");
        foreach(var scroll in main.GetComponentsInChildren<ScrollRect>())
        {
            if(!field)CollectionAssert.Contains(new[]{"Codex entries viewport","Codex story viewport","Inheritance viewport"},scroll.name,"일반 행동은 스크롤 없이 표시하고 지정된 목록·긴 기록만 스크롤합니다.");
            Assert.IsNotNull(scroll.viewport.GetComponent<RectMask2D>());Assert.IsTrue(scroll.viewport.IsChildOf(main));
            Assert.IsFalse(scroll.horizontal);Assert.AreEqual(ScrollRect.MovementType.Clamped,scroll.movementType);
            AssertRectInside(scroll.viewport,main,scroll.name);
            foreach(var button in scroll.content.GetComponentsInChildren<Button>())
            {
                var rect=button.GetComponent<RectTransform>();
                Assert.LessOrEqual(rect.rect.height,scroll.viewport.rect.height+1,"목록 항목 하나는 화면에 전부 들어가야 합니다: "+button.name);
                AssertRectInside(rect,scroll.content,button.name+" content bounds");
                float saved=scroll.verticalNormalizedPosition;
                var bound=RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.content,rect);
                float range=scroll.content.rect.height-scroll.viewport.rect.height;
                scroll.verticalNormalizedPosition=range<=0?1:Mathf.Clamp01(1-(scroll.content.rect.yMax-bound.center.y-scroll.viewport.rect.height*.5f)/range);
                Canvas.ForceUpdateCanvases();AssertRectInside(rect,scroll.viewport,button.name+" must be fully reachable by scrolling");
                scroll.verticalNormalizedPosition=saved;
            }
        }
        foreach(var b in main.GetComponentsInChildren<Button>())
        {
            var scroll=b.GetComponentInParent<ScrollRect>();
            if(scroll==null||!b.transform.IsChildOf(scroll.content))AssertRectInside(b.GetComponent<RectTransform>(),main,b.name);
        }
        foreach(var t in main.GetComponentsInChildren<Text>().Where(t=>!string.IsNullOrEmpty(t.text)))
            Assert.LessOrEqual(t.preferredHeight,t.rectTransform.rect.height+2,"텍스트가 잘리면 안 됩니다: "+t.text);
    }
    static void AssertRectInside(RectTransform rect,RectTransform container,string message)
    {
        var corners=new Vector3[4];rect.GetWorldCorners(corners);
        foreach(var p in corners){var local=container.InverseTransformPoint(p);Assert.That(local.x,Is.InRange(container.rect.xMin-1,container.rect.xMax+1),message);Assert.That(local.y,Is.InRange(container.rect.yMin-1,container.rect.yMax+1),message);}
    }
    [UnityTest] public IEnumerator MainFacilitiesAndEveryOwnedCodexDetailKeepActionsInsideFixedScreen()
    {
        string temp=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"ScreenFit-"+Guid.NewGuid().ToString("N"));
        var root=new GameObject("Screen fit");var app=root.AddComponent<GameApp>();app.saveDirectoryOverride=temp;yield return null;
        app.BeginNew(1231);yield return null;AssertInsideMain(root);
        if(app.Game.S.avatar==null)app.Game.Select(app.Game.S.candidates[0].id);
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
            app.Game.S.exploration.current=template.area;app.Game.S.plot.completed.AddRange(template.prerequisites);app.Game.S.plot.completed.AddRange(template.anyPrerequisites);app.Game.S.food=1000;app.Game.S.medicine=app.Game.S.syringes=100;
            if(template.id.StartsWith("TRN-")){app.Game.S.exploration.current=Area.Subway;Assert.IsTrue(app.Game.OfferTransit());}
            else Assert.IsTrue(app.Game.PresentGeneralEvent(template.id),template.id);app.Game.S.pending.context="이전에 남긴 기록을 보존한 채 다시 조사합니다.";app.Render();yield return null;AssertInsideMain(root);
            app.Game.S.pending=null;
        }
        app.Game.S.phase=Phase.Morning;
        foreach(var npc in app.Game.Content.rareNpcs)app.Game.S.candidates.Add(app.Game.CreateRare(npc.id));
        app.Navigate("인재관리실");yield return null;AssertInsideMain(root);
        root.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>().text=="다음 페이지").onClick.Invoke();yield return null;AssertInsideMain(root);
        UnityEngine.Object.Destroy(root);yield return null;System.IO.Directory.Delete(temp,true);
    }
}
