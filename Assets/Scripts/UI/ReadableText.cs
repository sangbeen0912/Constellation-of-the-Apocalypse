using UnityEngine;
using UnityEngine.UI;
namespace Apocalypse
{
    // UGUI Text leaves its atlas callback guard set when an empty mesh returns early.
    // A later Korean atlas resize must still invalidate this label's cached UVs.
    public class ReadableText:Text
    {
        StatusTypography presenter;
        public void UseTextCore(TMPro.TMP_FontAsset font)
        {
            if(presenter!=null||font==null)return;
            presenter=gameObject.AddComponent<StatusTypography>();presenter.Initialize(this,font);SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            if(presenter!=null){mesh.Clear();m_DisableFontTextureRebuiltCallback=false;return;}
            try{base.OnPopulateMesh(mesh);}
            finally{m_DisableFontTextureRebuiltCallback=false;}
        }
        protected override void UpdateGeometry()
        {
            base.UpdateGeometry();
            if(IsActive())canvasRenderer.SetTexture(mainTexture);
        }
        public void RefreshGlyphs()
        {
            m_DisableFontTextureRebuiltCallback=false;
            cachedTextGenerator.Invalidate();cachedTextGeneratorForLayout.Invalidate();FontTextureChanged();
        }
    }
}
