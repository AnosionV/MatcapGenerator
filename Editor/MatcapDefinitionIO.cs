using System.IO;
using UnityEditor;

namespace Anosion.MatcapGenerator
{
    internal static class MatcapDefinitionIO
    {
        public const string Extension = "matcapgen";

        public static MatcapDefinition Read(string assetPath)
        {
            var definition = new MatcapDefinition();
            var json = File.Exists(assetPath) ? File.ReadAllText(assetPath) : string.Empty;
            if (!string.IsNullOrWhiteSpace(json))
            {
                EditorJsonUtility.FromJsonOverwrite(json, definition);
            }

            definition.decals ??= new System.Collections.Generic.List<MatcapDecal>();
            return definition;
        }

        public static void Write(string assetPath, MatcapDefinition definition)
        {
            File.WriteAllText(assetPath, ToJson(definition));
        }

        public static string ToJson(MatcapDefinition definition)
        {
            return EditorJsonUtility.ToJson(definition, true);
        }
    }
}
