using UnityEditor;
using UnityEngine;

public sealed class PixelAssetImporter : AssetPostprocessor
{
    public override uint GetVersion(){return 5;}
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/Art/Portraits/") &&
            !assetPath.StartsWith("Assets/Resources/Art/Constellations/") &&
            !assetPath.StartsWith("Assets/Resources/Art/Relics/") &&
            !assetPath.StartsWith("Assets/Resources/Art/Environments/") &&
            !assetPath.StartsWith("Assets/Resources/Art/Combat/")) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Default;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.isReadable = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        bool blockCompress=!System.IO.File.Exists("TestResults/texture-baseline.flag");
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.alphaIsTransparency = true;
        importer.maxTextureSize = 4096;
        // UI portraits never exceed 210 reference pixels. Import from the preserved
        // source to a 256px runtime texture; full source PNG remains available for art edits.
        if(assetPath.StartsWith("Assets/Resources/Art/Portraits/"))importer.maxTextureSize=256;
        if(assetPath.StartsWith("Assets/Resources/Art/Relics/"))importer.maxTextureSize=128;
        if(assetPath.StartsWith("Assets/Resources/Art/Combat/"))importer.maxTextureSize=1024;
        // Facility cards are small. Large event/battle backgrounds retain 1024px;
        // the six-region atlas keeps 2048px so each tile remains legible.
        if(assetPath.Contains("/Environments/"))importer.maxTextureSize=1024;
        if(assetPath.EndsWith("clinic-v2.png")||assetPath.EndsWith("warehouse-v2.png")||assetPath.EndsWith("personnel-v2.png"))importer.maxTextureSize=512;
        if(assetPath.EndsWith("november-areas-v1.png"))importer.maxTextureSize=2048;
        var platform=importer.GetPlatformTextureSettings("Standalone");platform.overridden=blockCompress;platform.maxTextureSize=importer.maxTextureSize;
        platform.format=TextureImporterFormat.BC7;platform.compressionQuality=100;importer.SetPlatformTextureSettings(platform);
    }
}
