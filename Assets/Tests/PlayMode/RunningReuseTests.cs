using System;
using System.Collections;
using System.Linq;
using Apocalypse;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
public class RunningReuseTests
{
    [UnityTest] public IEnumerator AutomaticActionsReuseTheMainScreenAndRefreshVisibleHunger()
    {
        string temp=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"RunningReuse-"+Guid.NewGuid().ToString("N"));
        var root=new GameObject("Running reuse");var app=root.AddComponent<GameApp>();app.saveDirectoryOverride=temp;yield return null;app.BeginNew(412);
        var g=app.Game;g.S.day=2;g.S.phase=Phase.Running;g.S.goal=Goal.Explore;g.S.activeRift=-1;g.S.exploration.current=Area.Residence;
        g.S.scheduledConstellation=null;g.S.avatar.hunger=g.S.avatar.sanity=100;g.S.food=1000;g.S.nextImportantHour=100;g.S.eventTarget=4;g.S.avatar.personality=Temperament.Brave;
        app.Navigate("하루 기록");yield return null;var main=root.GetComponentsInChildren<RectTransform>().Single(r=>r.name=="Main");int id=main.GetInstanceID(),before=g.S.totalActions;
        float limit=Time.realtimeSinceStartup+8;while(g.S.totalActions<before+3&&Time.realtimeSinceStartup<limit)yield return null;
        app.enabled=false;Assert.GreaterOrEqual(g.S.totalActions,before+3);Assert.AreEqual(id,root.GetComponentsInChildren<RectTransform>().Single(r=>r.name=="Main").GetInstanceID());
        Assert.IsTrue(root.GetComponentsInChildren<Text>().Any(t=>t.text=="허기  "+Rules.Round(g.S.avatar.hunger)+" / 100"));
        UnityEngine.Object.Destroy(root);yield return null;System.IO.Directory.Delete(temp,true);
    }
}
