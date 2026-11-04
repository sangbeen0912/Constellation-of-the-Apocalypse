using System;
using System.Collections;
using System.Linq;
using Apocalypse;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class LogReuseTests
{
    [UnityTest] public IEnumerator RevealingLogsReusesTheScreenAndSixtyRowsAndStillScrolls()
    {
        string temp=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"LogReuse-"+Guid.NewGuid().ToString("N"));
        var root=new GameObject("Log reuse");var app=root.AddComponent<GameApp>();app.saveDirectoryOverride=temp;yield return null;
        app.BeginNew(293);if(app.Game.S.avatar==null)app.Game.Select(app.Game.S.candidates[0].id);
        for(int i=0;i<120;i++)app.Game.S.logs.Add("최적화 관측 기록 "+i);
        app.Navigate("하루 기록");
        root.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>().text=="즉시 표시").onClick.Invoke();yield return null;
        int main=root.GetComponentsInChildren<RectTransform>().Single(r=>r.name=="Main").GetInstanceID();
        var rows=root.GetComponentsInChildren<Text>().Where(t=>t.name=="Observation log row").ToArray();
        Assert.AreEqual(60,rows.Length);var ids=rows.Select(t=>t.GetInstanceID()).ToArray();
        for(int i=0;i<20;i++){app.Game.S.logs.Add("추가 관측 "+i);yield return null;}
        Assert.AreEqual(main,root.GetComponentsInChildren<RectTransform>().Single(r=>r.name=="Main").GetInstanceID(),"기록 공개 때문에 전체 화면을 다시 만들면 안 됩니다.");
        CollectionAssert.AreEquivalent(ids,root.GetComponentsInChildren<Text>().Where(t=>t.name=="Observation log row").Select(t=>t.GetInstanceID()));
        Assert.IsTrue(rows.Any(t=>t.text=="추가 관측 19"));
        Canvas.ForceUpdateCanvases();var scroll=root.GetComponentsInChildren<ScrollRect>().Single();
        Assert.Greater(scroll.content.rect.height,scroll.viewport.rect.height);
        ExecuteEvents.Execute(scroll.gameObject,new PointerEventData(EventSystem.current){scrollDelta=new Vector2(0,-4)},ExecuteEvents.scrollHandler);yield return null;
        Assert.Less(scroll.verticalNormalizedPosition,.99f);
        app.Navigate("의무실");yield return null;Assert.AreEqual(0,root.GetComponentsInChildren<Text>().Count(t=>t.name=="Observation log row"));
        app.Game.S.logs.Add("닫은 후 기록");yield return null;Assert.AreEqual("의무실",app.ActiveTab);
        UnityEngine.Object.Destroy(root);yield return null;System.IO.Directory.Delete(temp,true);
    }
}
