using System;
using System.Collections;
using System.Linq;
using Apocalypse;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
public class PlannerScreenTests
{
    Button Button(GameObject root,string label){return root.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>().text==label);}
    [UnityTest] public IEnumerator ClinicAndUnaffordableEventChoicesAreVisibleAndDisabled()
    {
        string temp=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"PlannerUI-"+Guid.NewGuid().ToString("N"));
        var root=new GameObject("Planner clinic");var app=root.AddComponent<GameApp>();app.saveDirectoryOverride=temp;yield return null;
        app.BeginNew(123);if(app.Game.S.avatar==null)app.Game.Select(app.Game.S.candidates[0].id);var g=app.Game;
        g.S.avatar.baseHealth=120;g.S.avatar.health=20;g.S.avatar.sanity=20;g.S.syringes=1;g.S.medicine=1;g.S.hours=13.5;
        app.Navigate("의무실");yield return null;
        Assert.IsFalse(Button(root,"체력 회복 실행").interactable);Assert.IsFalse(Button(root,"잠자기 실행").interactable);
        Assert.AreEqual(3,root.GetComponentsInChildren<Button>().Count(b=>b.GetComponentInChildren<Text>().text.EndsWith(" 실행")));
        Assert.AreEqual(2,root.GetComponentsInChildren<Toggle>().Length);
        root.GetComponentsInChildren<Toggle>().Single(t=>t.name=="Item checkbox 응급 의료 키트 사용").isOn=true;yield return null;
        Assert.IsTrue(Button(root,"체력 회복 실행").interactable);
        Button(root,"체력 회복 실행").onClick.Invoke();yield return null;
        Assert.AreEqual(104,g.S.avatar.health);Assert.AreEqual(0,g.S.medicine);Assert.AreEqual(14.5,g.S.hours);
        g.S.avatar.sanity=100;g.S.controlLocked=false;g.S.day=2;g.S.phase=Phase.Running;g.S.goal=Goal.Rift;g.S.hours=0;g.S.food=4;g.S.eventSlots=0;
        Assert.IsFalse(g.PresentGeneralEvent("R-02"));g.S.activeRift=0;
        Assert.IsTrue(g.PresentGeneralEvent("R-02"));app.Render();yield return null;
        var choice=root.GetComponentsInChildren<Button>().Single(b=>b.GetComponentInChildren<Text>().text.StartsWith("생존자에게 필요한 물자를 제공한다"));
        Assert.IsFalse(choice.interactable);Assert.IsTrue(root.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("부족")));
        UnityEngine.Object.Destroy(root);yield return null;System.IO.Directory.Delete(temp,true);
    }
    [UnityTest] public IEnumerator RestorationImmediatePreviewOptionalHistoryAndConfirmationHaveSeparateTransactions()
    {
        string temp=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"PlannerRestore-"+Guid.NewGuid().ToString("N"));
        var root=new GameObject("Planner restoration");var app=root.AddComponent<GameApp>();app.saveDirectoryOverride=temp;yield return null;
        app.BeginNew(124);if(app.Game.S.avatar==null)app.Game.Select(app.Game.S.candidates[0].id);var g=app.Game;g.S.divinity=200;
        var relic=new Relic{id="gungnir",name="궁니르",healthBonus=2,attackBonus=1,days=16,normalKills=21,demonKills=2,paidDays=10,paidZombies=10,paidDemons=1,awaitingRestoration=true};
        relic.history.Add(new RelicChapter{userId="a",name="서도윤",days=6,normalKills=6,deceased=true});
        relic.history.Add(new RelicChapter{userId="b",name="라이라",days=10,normalKills=15,demonKills=2,deceased=true});
        g.S.relics.Add(relic);app.Navigate("예배방");yield return null;
        Button(root,"성유물 복원").onClick.Invoke();yield return null;
        SingleScreenTests.AssertInsideMain(root);
        Assert.IsFalse(root.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("성유물 생성")));
        Button(root,"복원 기록 보기 · 궁니르").onClick.Invoke();yield return null;
        Assert.IsTrue(root.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("복원 후 ·")));Assert.IsTrue(Button(root,"복원 확정 · 신위 100").interactable);Assert.AreEqual(200,g.S.divinity);Assert.AreEqual(2,relic.healthBonus);
        Button(root,"복원 목록으로").onClick.Invoke();yield return null;Button(root,"복원 기록 보기 · 궁니르").onClick.Invoke();yield return null;
        foreach(string label in new[]{"마지막 사용자의 기록","이전 전승 기록","이번 복원 결과"}){Button(root,label).onClick.Invoke();yield return null;Assert.AreEqual(200,g.S.divinity);SingleScreenTests.AssertInsideMain(root);}
        Button(root,"복원 확정 · 신위 100").onClick.Invoke();yield return null;
        Assert.AreEqual(100,g.S.divinity);Assert.AreEqual(5,relic.healthBonus);Assert.AreEqual(3,relic.attackBonus);Assert.IsFalse(relic.awaitingRestoration);Assert.AreEqual(2,relic.history.Count);
        Assert.IsTrue(app.LoadGame());Assert.AreEqual(1,app.Game.S.relics.Single().restorations);Assert.IsFalse(app.Game.RestoreRelic(relic.id));
        UnityEngine.Object.Destroy(root);yield return null;System.IO.Directory.Delete(temp,true);
    }
    [UnityTest] public IEnumerator AllRarePersonnelCardsShowMatchingFaceSpeciesPassiveAndStats()
    {
        string temp=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"PlannerRare-"+Guid.NewGuid().ToString("N"));
        var root=new GameObject("Planner rare");var app=root.AddComponent<GameApp>();app.saveDirectoryOverride=temp;yield return null;
        app.BeginNew(125);if(app.Game.S.avatar==null)app.Game.Select(app.Game.S.candidates[0].id);
        foreach(var npc in app.Game.Content.rareNpcs)app.Game.S.candidates.Add(app.Game.CreateRare(npc.id));
        app.Navigate("인재관리실");yield return null;
        foreach(var npc in app.Game.S.candidates.Where(c=>c.species!=Species.Human))
        {
            for(int page=0;page<3&&!root.GetComponentsInChildren<RectTransform>().Any(r=>r.name=="Personnel "+npc.id);page++)
            {var next=Button(root,"다음 페이지");if(!next.interactable){Button(root,"이전 페이지").onClick.Invoke();}else next.onClick.Invoke();yield return null;}
            var card=root.GetComponentsInChildren<RectTransform>().Single(r=>r.name=="Personnel "+npc.id);
            Assert.AreSame(ArtCatalog.Portrait(npc.name),card.GetComponentInChildren<RawImage>().texture);
            Assert.IsTrue(card.GetComponentsInChildren<Text>().Any(t=>t.text.Contains("공격력 "+npc.Attack)));
            Assert.IsTrue(card.GetComponentsInChildren<Text>().Any(t=>t.text==app.Game.Content.rareNpcs.Single(n=>n.species==npc.species).passive));
        }
        UnityEngine.Object.Destroy(root);yield return null;System.IO.Directory.Delete(temp,true);
    }
}

