using UnityEditor;
using UnityEngine;

namespace FrogCamp.Editor
{
    public sealed class FrogTextureImportSettings : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Frog/") ||
                !assetPath.EndsWith(".png")) return;

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
        }
    }
}
