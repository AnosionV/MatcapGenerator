using UnityEditor;

namespace Anosion.MatcapGenerator
{
    internal static class MatcapCreateMenu
    {
        [MenuItem("Assets/Create/Anosion/" + MatcapText.MenuCreate)]
        private static void Create()
        {
            ProjectWindowUtil.CreateAssetWithContent(
                MatcapText.DefaultFileName + "." + MatcapDefinitionIO.Extension,
                MatcapDefinitionIO.ToJson(new MatcapDefinition()));
        }
    }
}
