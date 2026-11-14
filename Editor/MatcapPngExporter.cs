using System.IO;
using UnityEditor;
using UnityEngine;

namespace Anosion.MatcapGenerator
{
    internal static class MatcapPngExporter
    {
        public static void Export(string matcapAssetPath, MatcapDefinition definition)
        {
            var pngPath = Path.ChangeExtension(matcapAssetPath, ".png");
            if (File.Exists(pngPath)
                && !EditorUtility.DisplayDialog(
                    MatcapText.OverwriteDialogTitle,
                    string.Format(MatcapText.OverwriteDialogMessage, Path.GetFileName(pngPath)),
                    MatcapText.OverwriteDialogOk,
                    MatcapText.OverwriteDialogCancel))
            {
                return;
            }

            var texture = MatcapTextureFactory.Create(definition, mipChain: false, makeNoLongerReadable: false);
            try
            {
                File.WriteAllBytes(pngPath, texture.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }

            AssetDatabase.ImportAsset(pngPath);
            if (AssetImporter.GetAtPath(pngPath) is TextureImporter importer)
            {
                importer.sRGBTexture = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.mipmapEnabled = true;
                importer.maxTextureSize = Mathf.Max(importer.maxTextureSize, MatcapRenderer.GetSize(definition));
                importer.SaveAndReimport();
            }

            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath));
        }
    }
}
