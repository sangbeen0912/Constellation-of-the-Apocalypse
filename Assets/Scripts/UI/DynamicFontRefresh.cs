using System;
using UnityEngine;
namespace Apocalypse
{
    // Legacy Text may request new Korean glyphs while a reused field panel renders.
    // Repair UVs on the next frame only when the shared font atlas actually changes.
    public sealed class DynamicFontRefresh:MonoBehaviour
    {
        public Font trackedFont;public Action refresh;int pendingFrames;
        public bool IsSettled{get{return pendingFrames<=0;}}
        void OnEnable(){Font.textureRebuilt+=AtlasChanged;}
        void OnDisable(){Font.textureRebuilt-=AtlasChanged;}
        public void Schedule(){pendingFrames=3;}
        void AtlasChanged(Font changed){if(changed==trackedFont)Schedule();}
        void LateUpdate(){if(pendingFrames<=0)return;pendingFrames--;refresh?.Invoke();}
    }
}
