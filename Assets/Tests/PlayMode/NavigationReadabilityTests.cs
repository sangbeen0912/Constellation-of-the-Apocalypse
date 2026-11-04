using System;
using System.Collections;
using System.Linq;
using Apocalypse;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class NavigationReadabilityTests
{
    static Button Find(GameObject root,string text){return root.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>().text==text);}
    static void VisibleClick(Button button)
    {
        Canvas.ForceUpdateCanvases();var rect=button.GetComponent<RectTransform>();
        var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
        var pointer=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left};
        var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
        Assert.IsTrue(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button,"뒤로 버튼이 다른 UI에 가리면 안 됩니다.");
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.pointerClickHandler);
    }
    static double Luminance(Color c)
    {
        Func<float,double> linear=v=>v<=.04045?v/12.92:Math.Pow((v+.055)/1.055,2.4);
        return .2126*linear(c.r)+.7152*linear(c.g)+.0722*linear(c.b);
    }
    static void Readable(Button b)
    {
        double a=Luminance(b.GetComponentInChildren<PixelPlate>().color),z=Luminance(b.GetComponentInChildren<Text>().color);
        Assert.GreaterOrEqual((Math.Max(a,z)+.05)/(Math.Min(a,z)+.05),4.5,"주요 조작 글자 대비: "+b.name);
    }
    [UnityTest] public IEnumerator FixedBackIsClickableAndSelectionAndProceedHaveDistinctReadableStates()
    {
        string temp=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"Navigation-"+Guid.NewGuid().ToString("N"));
        var root=new GameObject("Navigation readability");var app=root.AddComponent<GameApp>();app.saveDirectoryOverride=temp;yield return null;
        app.BeginNew(471);app.Game.Select(app.Game.S.candidates[0].id);
        Vector2? position=null;
        foreach(string tab in ArtCatalog.FacilityNames.Append("전투준비").Append("성좌 도감").Append("기록 보관소"))
        {
            app.Navigate(tab);yield return null;var back=Find(root,"쉘터로");var rect=back.GetComponent<RectTransform>();Canvas.ForceUpdateCanvases();
            Assert.GreaterOrEqual(rect.rect.width,300);Assert.GreaterOrEqual(rect.rect.height,64);
            var center=(Vector2)rect.TransformPoint(rect.rect.center);
            Assert.Less(RectTransformUtility.WorldToScreenPoint(null,center).y,Screen.height*.15f,"뒤로 버튼은 화면 아래쪽이어야 합니다.");
            if(position.HasValue)Assert.Less(Vector2.Distance(position.Value,center),1);position=center;
            Readable(back);VisibleClick(back);yield return null;Assert.AreEqual("쉘터",app.ActiveTab);
        }
        app.Game.S.permanent.unlocked.Add("sejong");app.Navigate("성좌 도감");app.Codex.Select("sejong",app.Game.S.permanent);app.Render();yield return null;
        VisibleClick(Find(root,"도감으로"));yield return null;Assert.IsNull(app.Codex.SelectedId);Assert.AreEqual("성좌 도감",app.ActiveTab);
        app.Game.S.relics.Add(new Relic{id="gungnir",name="궁니르",awaitingRestoration=true});
        app.Navigate("예배방");yield return null;VisibleClick(Find(root,"성유물 복원"));yield return null;
        VisibleClick(Find(root,"복원 기록 보기 · 궁니르"));yield return null;
        VisibleClick(Find(root,"복원 목록으로"));yield return null;Assert.IsNotNull(Find(root,"복원 기록 보기 · 궁니르"));
        VisibleClick(Find(root,"복원 기록 보기 · 궁니르"));yield return null;VisibleClick(Find(root,"후원"));yield return null;
        VisibleClick(Find(root,"쉘터로"));yield return null;Assert.AreEqual("쉘터",app.ActiveTab);
        app.Game.S.day=2;app.Game.S.activeRift=0;app.Navigate("전투준비");yield return null;
        foreach(string label in new[]{"탐사","공략"})
        {
            VisibleClick(Find(root,label));yield return null;var chosen=Find(root,label);var proceed=Find(root,"진행");
            Assert.IsTrue(chosen.GetComponentInChildren<PixelPlate>().selectionTick);Assert.IsTrue(proceed.GetComponentInChildren<PixelPlate>().forwardArrow);
            Assert.AreNotEqual(chosen.GetComponentInChildren<PixelPlate>().color,proceed.GetComponentInChildren<PixelPlate>().color);
            Readable(Find(root,"탐사"));Readable(Find(root,"공략"));Readable(proceed);
        }
        UnityEngine.Object.Destroy(root);yield return null;System.IO.Directory.Delete(temp,true);
    }
}
