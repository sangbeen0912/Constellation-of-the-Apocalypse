using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.UI;
using Apocalypse;

public static class ProjectBuilder
{
    [MenuItem("Tools/Apocalypse/Generate Scene")]
    public static void Generate()
    {
        Directory.CreateDirectory("Assets/Scenes"); Directory.CreateDirectory("Assets/Prefabs");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera = new GameObject("Camera",typeof(Camera)); camera.tag = "MainCamera";
        camera.GetComponent<Camera>().backgroundColor = new Color(.035f,.055f,.085f); camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
        camera.GetComponent<Camera>().orthographic = true;
        var button = new GameObject("GameButton",typeof(RectTransform),typeof(Image),typeof(Button),typeof(LayoutElement));
        button.GetComponent<RectTransform>().sizeDelta = new Vector2(480,58);
        button.GetComponent<Image>().color = new Color(.10f,.16f,.20f);
        button.GetComponent<Button>().targetGraphic = button.GetComponent<Image>();
        var text = new GameObject("Label",typeof(RectTransform),typeof(Text)); text.transform.SetParent(button.transform,false);
        var rt = text.GetComponent<RectTransform>();rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=new Vector2(18,0);rt.offsetMax=new Vector2(-18,0);
        var label=text.GetComponent<Text>();label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.fontSize=23;label.alignment=TextAnchor.MiddleLeft;
        var bp=PrefabUtility.SaveAsPrefabAsset(button,"Assets/Prefabs/GameButton.prefab");UnityEngine.Object.DestroyImmediate(button);
        var panel=new GameObject("GamePanel",typeof(RectTransform),typeof(Image));
        var pp=PrefabUtility.SaveAsPrefabAsset(panel,"Assets/Prefabs/GamePanel.prefab");UnityEngine.Object.DestroyImmediate(panel);
        var app=new GameObject("ApocalypseConstellation",typeof(GameApp)).GetComponent<GameApp>();app.buttonPrefab=bp;app.panelPrefab=pp;
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/Apocalypse.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Apocalypse.unity",true)};
        PlayerSettings.companyName="Starfall Studio";PlayerSettings.productName="종말의 성좌";
        PlayerSettings.defaultScreenWidth=1920;PlayerSettings.defaultScreenHeight=1080;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
        PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
        var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        var input=settings.FindProperty("activeInputHandler");if(input!=null){input.intValue=2;settings.ApplyModifiedPropertiesWithoutUndo();}
        AssetDatabase.SaveAssets();Debug.Log("APOCALYPSE_SCENE_READY");
    }
    [MenuItem("Tools/Apocalypse/Build Windows")]
    public static void BuildWindows()
    {
        Generate();Directory.CreateDirectory("Builds/Windows");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{"Assets/Scenes/Apocalypse.unity"},locationPathName="Builds/Windows/ApocalypseConstellation.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development|BuildOptions.CompressWithLz4HC});
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Windows build failed: "+report.summary.result);
        Debug.Log("APOCALYPSE_WINDOWS_BUILD_SUCCEEDED");
    }
}
