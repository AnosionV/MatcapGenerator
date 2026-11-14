using UnityEditor.AssetImporters;

namespace Anosion.MatcapGenerator
{
    [ScriptedImporter(1, MatcapDefinitionIO.Extension)]
    public sealed class MatcapImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var definition = MatcapDefinitionIO.Read(ctx.assetPath);
            var texture = MatcapTextureFactory.Create(definition, mipChain: true, makeNoLongerReadable: true);
            texture.name = System.IO.Path.GetFileNameWithoutExtension(ctx.assetPath);
            ctx.AddObjectToAsset("main", texture);
            ctx.SetMainObject(texture);
        }
    }
}
