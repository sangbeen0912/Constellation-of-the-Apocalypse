using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
public static class NovemberOptimization
{
    [Serializable] public class Entry{public string path,format;public int width,height;public long before,after;}
    [Serializable] public class Report{public long beforeTextureBytes,afterTextureBytes;public Entry[] textures;}
    [MenuItem("Tools/Apocalypse/Apply November Optimization")]
    public static void Apply()
    {
        var paths=Directory.GetFiles("Assets/Resources/Art","*.png",SearchOption.AllDirectories).OrderBy(x=>x).ToArray();
        var report=new Report{textures=new Entry[paths.Length]};
        for(int i=0;i<paths.Length;i++)
        {
            string p=paths[i].Replace('\\','/');long before=0;var importer=(TextureImporter)AssetImporter.GetAtPath(p);string userData=importer.userData;
            try
            {
                File.WriteAllText("TestResults/texture-baseline.flag","same dimensions; compression disabled only during this measurement");
                importer.userData="measurement-uncompressed";importer.SaveAndReimport();
                var uncompressed=AssetDatabase.LoadAssetAtPath<Texture2D>(p);before=Profiler.GetRuntimeMemorySizeLong(uncompressed);
            }
            finally
            {
                if(File.Exists("TestResults/texture-baseline.flag"))File.Delete("TestResults/texture-baseline.flag");
                importer.userData=userData;importer.SaveAndReimport();
            }
            var t=AssetDatabase.LoadAssetAtPath<Texture2D>(p);
            var row=new Entry{path=p,before=before,after=Profiler.GetRuntimeMemorySizeLong(t),width=t.width,height=t.height,format=t.format.ToString()};
            report.textures[i]=row;report.beforeTextureBytes+=before;report.afterTextureBytes+=row.after;
        }
        Directory.CreateDirectory("TestResults");File.WriteAllText("TestResults/november-textures.json",JsonUtility.ToJson(report,true));
        Debug.Log("NOVEMBER_TEXTURE_OPTIMIZATION "+report.beforeTextureBytes+" -> "+report.afterTextureBytes);
    }
}
