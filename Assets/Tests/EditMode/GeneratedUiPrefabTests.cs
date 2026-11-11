using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
public class GeneratedUiPrefabTests
{
    [Test] public void GeneratedButtonCaptionHasAConcreteSerializedScript()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GameButton.prefab");Assert.IsNotNull(prefab);
        var label=prefab.GetComponentInChildren<Text>();Assert.IsNotNull(label,"배포 프리팹에도 버튼 글자 컴포넌트를 보존해야 합니다.");
        var data=new SerializedObject(label);Assert.IsNotNull(data.FindProperty("m_Script").objectReferenceValue);
        foreach(var transform in prefab.GetComponentsInChildren<Transform>(true))
            Assert.AreEqual(0,GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject));
    }
}
