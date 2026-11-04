using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Apocalypse;
public static class UIOptimizationBenchmark
{
    [Serializable] sealed class Result { public int samples, vertices, collections;public long managedHeapDelta, allocationProbeDelta;public double milliseconds; }
    public static void Run()
    {
        var go=new GameObject("Plate benchmark",typeof(RectTransform),typeof(CanvasRenderer),typeof(PixelPlate));
        go.GetComponent<RectTransform>().sizeDelta=new Vector2(320,60);
        var plate=go.GetComponent<PixelPlate>();plate.button=true;
        var populate=(Action<VertexHelper>)Delegate.CreateDelegate(typeof(Action<VertexHelper>),plate,typeof(PixelPlate).GetMethod("OnPopulateMesh",BindingFlags.NonPublic|BindingFlags.Instance,null,new[]{typeof(VertexHelper)},null));
        var result=new Result{samples=10000};
        using(var helper=new VertexHelper())
        {
            for(int i=0;i<100;i++)populate(helper);
            var probeBefore=UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();var probe=new byte[1048576];
            result.allocationProbeDelta=UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong()-probeBefore;GC.KeepAlive(probe);
            GC.Collect();var timer=new Stopwatch();long before=UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();int collections=GC.CollectionCount(0);timer.Start();
            for(int i=0;i<result.samples;i++)populate(helper);
            timer.Stop();result.managedHeapDelta=UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong()-before;result.collections=GC.CollectionCount(0)-collections;result.milliseconds=timer.Elapsed.TotalMilliseconds;result.vertices=helper.currentVertCount;
        }
        UnityEngine.Object.DestroyImmediate(go);
        var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-ui-benchmark-output");
        string path=at>=0?args[at+1]:"TestResults/ui-mesh.json";Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path,JsonUtility.ToJson(result,true));UnityEngine.Debug.Log("UI_MESH_BENCHMARK "+JsonUtility.ToJson(result));
    }
}
