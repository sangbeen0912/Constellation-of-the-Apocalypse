using UnityEditor;
using UnityEngine;

public sealed class PixelAssetImporter : AssetPostprocessor
{
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
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.alphaIsTransparency = true;
        importer.maxTextureSize = 4096;
        // UI portraits never exceed 210 reference pixels. Import from the preserved
        // source to a 256px runtime texture; full source PNG remains available for art edits.
        if(assetPath.StartsWith("Assets/Resources/Art/Portraits/"))importer.maxTextureSize=256;
        if(assetPath.StartsWith("Assets/Resources/Art/Relics/"))importer.maxTextureSize=128;
        if(assetPath.StartsWith("Assets/Resources/Art/Combat/"))importer.maxTextureSize=2048;
    }
}
