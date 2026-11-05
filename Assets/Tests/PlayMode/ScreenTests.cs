using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Apocalypse;

public class ScreenTests
{
    [UnityTest] public IEnumerator ShelterIsFourFacilitiesAndPreparationOwnsGoalsAndDeparture()
    {
        string temp=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"ApocalypsePrep-"+System.Guid.NewGuid().ToString("N"));
        var root=new GameObject("Preparation test");var app=root.AddComponent<GameApp>();app.saveDirectoryOverride=temp;yield return null;
        app.BeginNew(321);if(app.Game.S.avatar==null)app.Game.Select(app.Game.S.candidates[0].id);app.Game.S.avatar.personality=Temperament.Brave;app.Navigate("쉘터");yield return null;
        var labels=root.GetComponentsInChildren<Button>().Select(b=>b.GetComponentInChildren<Text>().text).ToArray();
        CollectionAssert.AreEquivalent(ArtCatalog.FacilityNames.Select(n=>"시설 이용 · "+n),labels.Where(n=>n.StartsWith("시설 이용 · ")));
        Assert.IsFalse(labels.Contains("진행")||labels.Contains("탐사")||labels.Contains("공략"));
        app.Game.S.day=2;app.Game.S.activeRift=0;app.Game.S.demons[0].progress=23;app.Game.S.demons[1].remaining=8;app.Game.S.demons[2].remaining=12;
        app.Game.S.totalSeals=99;app.Navigate("전투준비");yield return null;Canvas.ForceUpdateCanvases();
        var texts=root.GetComponentsInChildren<Text>().Select(t=>t.text).ToArray();
        CollectionAssert.IsSubsetOf(new[]{"군단 봉쇄 돌파  23 / 50","악마 봉인  2 / 7","체력  200%","공격력  140%","현재 봉인 2명 기준"},texts);
        Assert.IsFalse(texts.Any(t=>t.Contains("일반 +1점")||t.Contains("50점 이후")));
        Assert.AreEqual(7,root.GetComponentsInChildren<SealGlyph>().Length);
        Assert.IsTrue(root.GetComponentsInChildren<RawImage>().Any(i=>i.texture==ArtCatalog.Environment(4)));
        Assert.IsFalse(root.GetComponentsInChildren<Button>().Any(b=>b.GetComponentInChildren<Text>().text=="침수된 지하철역"),"목적지는 탐사를 누른 뒤 표시합니다.");
        var main=root.GetComponentsInChildren<RectTransform>().Single(r=>r.name=="Main");
        foreach(string label in new[]{"탐사","공략","진행"})
        {
            var button=root.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>().text==label);
            var corners=new Vector3[4];button.GetComponent<RectTransform>().GetWorldCorners(corners);
            foreach(var corner in corners)
            {
                var local=main.InverseTransformPoint(corner);
                Assert.That(local.x,Is.InRange(main.rect.xMin-1,main.rect.xMax+1));Assert.That(local.y,Is.InRange(main.rect.yMin-1,main.rect.yMax+1));
            }
        }
        root.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>().text=="공략").onClick.Invoke();yield return null;
        Assert.IsTrue(root.GetComponentsInChildren<Text>().Any(t=>t.text=="오늘의 활동: 공략"));
        root.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>().text=="진행").onClick.Invoke();yield return null;
        Assert.AreEqual(Goal.Rift,app.Game.S.goal);Assert.IsTrue(app.Game.S.phase==Phase.Running||app.Game.S.phase==Phase.Event);
        UnityEngine.Object.Destroy(root);yield return null;System.IO.Directory.Delete(temp,true);
    }
    [UnityTest] public IEnumerator AllConstellationsUseTheSameWholeIllustrationInEventAndOwnedCodex()
    {
        string temp=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"ApocalypseArt-"+System.Guid.NewGuid().ToString("N"));
        var root=new GameObject("Illustration test");var app=root.AddComponent<GameApp>();app.saveDirectoryOverride=temp;yield return null;
        app.BeginNew(95);if(app.Game.S.avatar==null)app.Game.Select(app.Game.S.candidates[0].id);
        foreach(string id in ArtCatalog.ConstellationIds)
        {
            var definition=app.Game.Content.Find(id);
            app.Game.S.phase=Phase.Event;
            app.Game.S.pending=new ChoiceEvent{id="art-"+id,constellationId=id,title=definition.title,body=definition.story,choices=new[]{"이야기 확인 · 영구 해금"}};
            app.Render();yield return null;Canvas.ForceUpdateCanvases();
            var eventArt=root.GetComponentsInChildren<RawImage>().Single(x=>x.texture==ArtCatalog.Constellation(id));
            AssertWholeImage(eventArt);
            Assert.AreEqual(1,ConstellationArtLease.ResidentCount);
            root.GetComponentsInChildren<Button>().Single(x=>x.GetComponentInChildren<Text>().text=="이야기 확인 · 영구 해금").onClick.Invoke();
            app.Game.S.phase=Phase.Morning;app.Navigate("성좌 도감");yield return null;
            Assert.IsTrue(app.Game.S.permanent.unlocked.Contains(id));
            while(!root.GetComponentsInChildren<Button>().Any(x=>x.GetComponentInChildren<Text>().text==CodexIndex.DisplayName(definition)))
            {root.GetComponentsInChildren<Button>().Single(x=>x.GetComponentInChildren<Text>().text=="다음 페이지").onClick.Invoke();yield return null;}
            root.GetComponentsInChildren<Button>().Single(x=>x.GetComponentInChildren<Text>().text==CodexIndex.DisplayName(definition)).onClick.Invoke();
            yield return null;Canvas.ForceUpdateCanvases();
            var codexArt=root.GetComponentsInChildren<RawImage>().Single(x=>x.texture==ArtCatalog.Constellation(id));
            AssertWholeImage(codexArt);
            Assert.IsTrue(root.GetComponentsInChildren<Text>().Any(x=>x.text==definition.ability));
            Assert.IsTrue(root.GetComponentsInChildren<Text>().Any(x=>x.text==definition.description));
            Assert.IsTrue(root.GetComponentsInChildren<Text>().Any(x=>x.text==definition.story));
            Assert.IsTrue(root.GetComponentsInChildren<Text>().Any(x=>x.text=="소속  "+definition.affiliation));
            app.Codex.Back();
        }
        app.Navigate("쉘터");yield return null;
        Assert.AreEqual(0,ConstellationArtLease.ResidentCount,"성좌 화면을 닫으면 큰 그림 메모리를 반환합니다.");
        UnityEngine.Object.Destroy(root);yield return null;System.IO.Directory.Delete(temp,true);
    }
    static void AssertWholeImage(RawImage image)
    {
        Assert.AreEqual(AspectRatioFitter.AspectMode.FitInParent,image.GetComponent<AspectRatioFitter>().aspectMode);
        var frame=(RectTransform)image.transform.parent;
        Assert.LessOrEqual(image.rectTransform.rect.width,frame.rect.width+1);
        Assert.LessOrEqual(image.rectTransform.rect.height,frame.rect.height+1);
        Assert.AreEqual(image.texture.width/(float)image.texture.height,image.rectTransform.rect.width/image.rectTransform.rect.height,.01f);
        Assert.AreEqual(new Rect(0,0,1,1),image.uvRect,"전체 그림을 표시해야 합니다.");
    }
    [UnityTest] public IEnumerator CodexUsesOwnedPagedRowsGroupsAndFullArtDetailWithoutScrolling()
    {
        string temp=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"ApocalypseCodex-"+System.Guid.NewGuid().ToString("N"));
        var root=new GameObject("Codex test");var app=root.AddComponent<GameApp>();app.saveDirectoryOverride=temp;yield return null;
        app.BeginNew(77);if(app.Game.S.avatar==null)app.Game.Select(app.Game.S.candidates[0].id);app.Game.S.permanent.unlocked.Add("sejong");app.Navigate("성좌 도감");yield return null;
        var texts=root.GetComponentsInChildren<Text>().Select(x=>x.text).ToArray();
        Assert.IsFalse(texts.Any(x=>x.Contains("매일 독립")||x.Contains("40%")||x.Contains("제우스")));
        string label=CodexIndex.DisplayName(app.Game.Content.Find("sejong"));
        root.GetComponentsInChildren<Button>().First(x=>x.GetComponentInChildren<Text>().text==label).onClick.Invoke();yield return null;
        Assert.IsTrue(root.GetComponentsInChildren<Text>().Any(x=>x.text.Contains("소속")));
        var art=root.GetComponentsInChildren<RawImage>().Single(x=>x.texture==ArtCatalog.Constellation("sejong"));AssertWholeImage(art);
        Assert.Greater(((RectTransform)art.transform.parent).rect.height,200);
        Assert.IsFalse(root.GetComponentsInChildren<RectTransform>().Any(x=>x.name=="Constellation art blank"));
        root.GetComponentsInChildren<Button>().First(x=>x.GetComponentInChildren<Text>().text=="도감으로").onClick.Invoke();yield return null;
        var toggle=root.GetComponentsInChildren<Button>().First(x=>x.GetComponentInChildren<Text>().text.StartsWith("▾"));toggle.onClick.Invoke();yield return null;
        Assert.IsFalse(root.GetComponentsInChildren<Button>().Any(x=>x.GetComponentInChildren<Text>().text==label));
        app.Game.S.permanent.unlocked=app.Game.Content.constellations.Select(x=>x.id).ToList();app.Codex.Toggle(0);app.Render();yield return null;Canvas.ForceUpdateCanvases();
        Assert.AreEqual(0,root.GetComponentsInChildren<ScrollRect>().Length);
        var firstPage=root.GetComponentsInChildren<Button>().Where(b=>b.GetComponentInChildren<Text>().text.Contains(" / ")).Select(b=>b.GetComponentInChildren<Text>().text).ToArray();
        Assert.AreEqual(6,firstPage.Length);
        var next=root.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>().text=="다음 페이지");Assert.IsTrue(next.interactable);next.onClick.Invoke();yield return null;
        var secondPage=root.GetComponentsInChildren<Button>().Where(b=>b.GetComponentInChildren<Text>().text.Contains(" / ")).Select(b=>b.GetComponentInChildren<Text>().text).ToArray();
        Assert.IsFalse(firstPage.Intersect(secondPage).Any());app.Render();yield return null;
        CollectionAssert.AreEquivalent(secondPage,root.GetComponentsInChildren<Button>().Where(b=>b.GetComponentInChildren<Text>().text.Contains(" / ")).Select(b=>b.GetComponentInChildren<Text>().text));
        UnityEngine.Object.Destroy(root);yield return null;System.IO.Directory.Delete(temp,true);
    }
    [UnityTest] public IEnumerator PersonnelHasStableIconsStatsAndFacilitiesHaveBackgrounds()
    {
        string temp=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"ApocalypsePersonnel-"+System.Guid.NewGuid().ToString("N"));
        var root=new GameObject("Personnel test");var app=root.AddComponent<GameApp>();app.saveDirectoryOverride=temp;yield return null;app.BeginNew(67);if(app.Game.S.avatar==null)app.Game.Select(app.Game.S.candidates[0].id);
        foreach(string tab in ArtCatalog.FacilityNames)
        {
            app.Navigate(tab);yield return null;Assert.IsTrue(root.GetComponentsInChildren<RawImage>().Any(x=>x.texture==ArtCatalog.Environment(System.Array.IndexOf(ArtCatalog.FacilityNames,tab))));
            foreach(var image in root.GetComponentsInChildren<RawImage>().Where(x=>x.texture.width>480))
                Assert.NotNull(image.GetComponent<AspectRatioFitter>(),"시설 아트 비율을 유지해야 합니다.");
        }
        foreach(var c in app.Game.S.candidates.Append(app.Game.S.avatar))
        {
            var card=root.GetComponentsInChildren<RectTransform>().First(x=>x.name=="Personnel "+c.id);
            Assert.IsTrue(card.GetComponentsInChildren<Text>().Any(x=>x.text.Contains(c.name)));
            Assert.IsTrue(card.GetComponentsInChildren<Text>().Any(x=>x.text.Contains("공격력 "+c.Attack)));
            Assert.AreSame(ArtCatalog.Portrait(c.name),card.GetComponentInChildren<RawImage>().texture);
            var portrait=card.GetComponentInChildren<RawImage>();
            Assert.Greater(portrait.texture.width,128);
            Assert.AreEqual(AspectRatioFitter.AspectMode.FitInParent,portrait.GetComponent<AspectRatioFitter>().aspectMode);
        }
        UnityEngine.Object.Destroy(root);yield return null;System.IO.Directory.Delete(temp,true);
    }
    [UnityTest] public IEnumerator CanvasButtonsAndSelectionAreConnected()
    {
        string temp=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"ApocalypsePlay-"+System.Guid.NewGuid().ToString("N"));
        var root=new GameObject("PlayMode app");var app=root.AddComponent<GameApp>();app.saveDirectoryOverride=temp;yield return null;
        Assert.AreEqual(new Vector2(1920,1080),root.GetComponentInChildren<CanvasScaler>().referenceResolution);
        Assert.IsTrue(root.GetComponentsInChildren<Button>().Any(x=>x.GetComponentInChildren<Text>().text=="새로 시작"));
        // A distinct test save location is used by persistence tests; avoid writing a user's slot here.
        app.BeginNew(321);yield return null;
        Assert.IsEmpty(app.Game.S.candidates);Assert.IsFalse(root.GetComponentsInChildren<Button>().Any(x=>x.GetComponentInChildren<Text>().text=="계약한다"));
        Assert.AreEqual(Species.Human,app.Game.S.avatar.species);yield return null;
        Assert.NotNull(app.Game.S.avatar);Assert.AreEqual(Phase.Morning,app.Game.S.phase);
        Canvas.ForceUpdateCanvases();
        var footer=root.GetComponentsInChildren<RectTransform>().First(x=>x.name=="Footer");
        Assert.Greater(footer.GetChild(0).GetComponent<RectTransform>().rect.width,footer.rect.width*.8f,"속도 버튼 영역은 하단 너비를 사용해야 합니다.");
        foreach(var meterText in root.GetComponentsInChildren<Text>().Where(x=>x.text.StartsWith("체력  ")||x.text.StartsWith("정신력  ")||x.text.StartsWith("허기  ")))
            Assert.GreaterOrEqual(meterText.rectTransform.rect.width+1,meterText.preferredWidth,"상태 수치 전체가 읽혀야 합니다: "+meterText.text);
        foreach(int sanity in new[]{0,9,30,96,100})
        {
            app.Game.S.avatar.sanity=sanity;app.Render();yield return null;Canvas.ForceUpdateCanvases();
            var meter=root.GetComponentsInChildren<Text>().First(x=>x.text.StartsWith("정신력  "));
            Assert.AreEqual(HorizontalWrapMode.Overflow,meter.horizontalOverflow);
            Assert.GreaterOrEqual(meter.rectTransform.rect.width+1,meter.preferredWidth);
        }
        app.Game.S.avatar.sanity=100;
        app.Navigate("전투준비");yield return null;
        var start=root.GetComponentsInChildren<Button>().First(x=>x.GetComponentInChildren<Text>().text=="진행");start.onClick.Invoke();yield return null;
        Assert.AreEqual(Phase.Morning,app.Game.S.phase,"탐사는 목적지 UI에서 출발을 확정합니다.");
        root.GetComponentsInChildren<Button>().Single(x=>x.GetComponentInChildren<Text>().text=="탐사 출발").onClick.Invoke();yield return null;
        Assert.IsTrue(app.Game.S.phase==Phase.Running||app.Game.S.phase==Phase.Event);
        Assert.IsTrue(app.LoadGame());Assert.NotNull(app.Game.S.avatar);
        UnityEngine.Object.Destroy(root);yield return null;System.IO.Directory.Delete(temp,true);
    }
}
