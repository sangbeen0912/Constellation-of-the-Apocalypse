using System.Collections;
using Apocalypse;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
public class ReadableTextTests
{
    sealed class ProbeText:ReadableText
    {
        public bool Guard{get{return m_DisableFontTextureRebuiltCallback;}}
        public void Generate(){using(var mesh=new VertexHelper())base.OnPopulateMesh(mesh);}
    }
    [UnityTest] public IEnumerator EmptyMeshCannotDisableLaterKoreanAtlasRefresh()
    {
        var go=new GameObject("Empty then Korean",typeof(RectTransform));var label=go.AddComponent<ProbeText>();
        var font=Font.CreateDynamicFontFromOSFont("Malgun Gothic",24);label.font=font;label.text="";label.Generate();
        Assert.IsFalse(label.Guard,"빈 메시에 대한 조기 반환 뒤 글꼴 갱신을 계속 허용해야 합니다.");
        label.rectTransform.sizeDelta=new Vector2(300,60);label.text="체력 100 / 100";label.Generate();Assert.IsFalse(label.Guard);
        font.RequestCharactersInTexture("정신력 공격력 개입 가능",30);label.RefreshGlyphs();label.Generate();
        Assert.Greater(label.cachedTextGenerator.vertexCount,0);Assert.IsFalse(label.Guard);
        Object.Destroy(go);Object.Destroy(font);yield return null;
    }
}
