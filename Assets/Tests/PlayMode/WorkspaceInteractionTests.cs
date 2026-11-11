using System;
using System.Collections;
using System.Linq;
using Apocalypse;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
public class WorkspaceInteractionTests
{
    static Button Find(GameObject root,string label){return root.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>().text==label);}
    static void Click(GameObject go)
    {
        Canvas.ForceUpdateCanvases();var rect=go.GetComponent<RectTransform>();
        var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center)),button=PointerEventData.InputButton.Left};
        var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
        Assert.IsTrue(hits.Count>0&&hits[0].gameObject.transform.IsChildOf(go.transform),"조작 대상이 가려지면 안 됩니다: "+go.name);
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.pointerClickHandler);
    }
    [UnityTest] public IEnumerator StatusTypographyRendersAndTracksHealthAndResourceChanges()
    {
        string temp=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"Typography-"+Guid.NewGuid().ToString("N"));
        var root=new GameObject("Typography");var app=root.AddComponent<GameApp>();app.saveDirectoryOverride=temp;yield return null;
        app.BeginNew(1110);app.enabled=false;yield return null;
        var captions=root.GetComponentsInChildren<StatusTypography>();Assert.GreaterOrEqual(captions.Length,13);
        app.Game.S.avatar.health=55;app.Game.S.divinity=12345;app.SendMessage("RefreshRunningHud");yield return null;Canvas.ForceUpdateCanvases();
        Assert.IsTrue(captions.Any(c=>c.Model.text.StartsWith("체력  55")));Assert.IsTrue(captions.Any(c=>c.Model.text=="신위  12345"));
        foreach(var caption in captions)
        {
            Assert.AreEqual(caption.Model.text,caption.View.text);Assert.AreEqual(caption.Model.color,caption.View.color);
            caption.View.ForceMeshUpdate();Assert.Greater(caption.View.textInfo.characterCount,0,"실제 표시용 글자 메시가 있어야 합니다.");
            Assert.IsTrue(caption.View.textInfo.characterInfo.Any(c=>c.isVisible));
            Assert.LessOrEqual(caption.View.preferredHeight,caption.Model.rectTransform.rect.height+2,"표시용 글자가 영역에 들어가야 합니다: "+caption.Model.text);
        }
        UnityEngine.Object.Destroy(root);yield return null;System.IO.Directory.Delete(temp,true);
    }
    [UnityTest] public IEnumerator HomeTreatmentNeedsOnlyItemAndExecuteAndDepartureNeedsThreeClicks()
    {
        string temp=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"Workspace-"+Guid.NewGuid().ToString("N"));
        var root=new GameObject("Workspace interaction");var app=root.AddComponent<GameApp>();app.saveDirectoryOverride=temp;yield return null;
        app.BeginNew(1101);app.enabled=false;var g=app.Game;g.S.avatar.health=1;g.S.medicine=1;app.Render();yield return null;
        var toggle=root.GetComponentsInChildren<Toggle>().Single(t=>t.name=="Item checkbox 응급 의료 키트 사용");Click(toggle.gameObject);yield return null;
        Click(Find(root,"체력 회복 실행").gameObject);yield return null;
        Assert.AreEqual(0,g.S.medicine);Assert.AreEqual(1,g.S.hours);Assert.Greater(g.S.avatar.health,1);Assert.AreEqual("쉘터",app.ActiveTab);
        Assert.IsFalse(root.GetComponentsInChildren<Button>().Any(b=>b.GetComponentInChildren<Text>().text=="침수된 지하철역"));
        Click(Find(root,"탐사").gameObject);yield return null;Click(Find(root,"침수된 지하철역").gameObject);yield return null;
        Assert.AreEqual(Phase.Morning,g.S.phase);Assert.AreEqual(1,g.S.hours);
        Click(Find(root,"탐사 출발").gameObject);yield return null;Assert.AreEqual(Area.Subway,g.S.exploration.current);Assert.IsTrue(g.S.phase==Phase.Running||g.S.phase==Phase.Event);
        UnityEngine.Object.Destroy(root);yield return null;System.IO.Directory.Delete(temp,true);
    }
    [UnityTest] public IEnumerator BatchQuantityIsPreviewOnlyUntilSingleExecute()
    {
        string temp=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"BatchUI-"+Guid.NewGuid().ToString("N"));
        var root=new GameObject("Batch UI");var app=root.AddComponent<GameApp>();app.saveDirectoryOverride=temp;yield return null;
        app.BeginNew(1102);app.Game.S.divinity=1000;app.Navigate("예배방");yield return null;
        Click(Find(root,"×10").gameObject);yield return null;Assert.AreEqual(1000,app.Game.S.divinity);Assert.AreEqual(0,app.Game.UpgradeCount(Stat.Health));
        Assert.IsNotNull(Find(root,"체력 10회 강화 · 신위 100"));SingleScreenTests.AssertInsideMain(root);
        Click(Find(root,"체력 10회 강화 · 신위 100").gameObject);yield return null;
        Assert.AreEqual(10,app.Game.UpgradeCount(Stat.Health));Assert.AreEqual(900,app.Game.S.divinity);Assert.IsNotNull(Find(root,"체력 10회 강화 · 신위 200"));
        app.Game.S.demons[0].remaining=5;app.Game.S.activeRift=1;app.Game.S.demons[1].progress=27;
        app.SendMessage("RefreshRunningHud");yield return null;
        Assert.IsTrue(root.GetComponentsInChildren<Text>().Any(t=>t.text=="악마 봉인 1 / 7"));
        Assert.IsTrue(root.GetComponentsInChildren<Text>().Any(t=>t.text=="체력 150% · 공격력 120%"));
        Assert.IsTrue(root.GetComponentsInChildren<Text>().Any(t=>t.text=="봉쇄 돌파 27 / 50"));
        var hint=root.GetComponentsInChildren<SealStatusHint>().Single(h=>h.name=="Seal Pride");
        hint.OnPointerEnter(new PointerEventData(EventSystem.current){position=new Vector2(100,200)});yield return null;
        Assert.IsTrue(root.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("봉인 5일 남음")));hint.OnPointerExit(null);
        UnityEngine.Object.Destroy(root);yield return null;System.IO.Directory.Delete(temp,true);
    }
    [UnityTest] public IEnumerator ClickingEnemySelectsWithoutSpendingAndAttackCommandSpendsOnce()
    {
        string temp=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"DirectTarget-"+Guid.NewGuid().ToString("N"));
        var root=new GameObject("Direct target");var app=root.AddComponent<GameApp>();app.saveDirectoryOverride=temp;yield return null;
        app.BeginNew(1103);app.enabled=false;var g=app.Game;g.S.day=2;g.S.activeRift=0;g.S.phase=Phase.Running;g.S.goal=Goal.Rift;g.S.exploration.current=Area.Rift;
        g.S.avatar.personality=Temperament.Brave;g.S.avatar.sanity=g.S.avatar.hunger=100;g.S.divinity=100;
        Assert.IsTrue(g.PresentGeneralEvent("RF-005"));g.Resolve(g.S.pending.id,0);Assert.Greater(g.S.combat.enemies.Count,1);
        g.S.combat.waiting=true;g.S.combat.prompt="위기 명령";app.Render();yield return null;
        string before=JsonUtility.ToJson(g.S);
        Click(root.GetComponentsInChildren<RectTransform>().Single(r=>r.name=="Combat bar 1").gameObject);yield return null;
        Assert.AreEqual(before,JsonUtility.ToJson(g.S),"선택만으로 신위·시간·난수·적 체력이 변하지 않습니다.");
        Assert.IsTrue(root.GetComponentsInChildren<Text>().Any(t=>t.text=="▶ "+g.S.combat.enemies[1].name));
        var attack=root.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>().text.StartsWith("공격 지정 ·"));Click(attack.gameObject);yield return null;
        Assert.AreEqual(99,g.S.divinity);Assert.IsFalse(g.S.combat.waiting);
        UnityEngine.Object.Destroy(root);yield return null;System.IO.Directory.Delete(temp,true);
    }
}
