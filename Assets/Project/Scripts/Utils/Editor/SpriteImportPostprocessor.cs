#if UNITY_EDITOR
using UnityEditor;

namespace Project.Scripts.Utils.Editor
{
    public class SpriteImportPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (false == assetImporter.importSettingsMissing)
                return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 256f;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
#endif