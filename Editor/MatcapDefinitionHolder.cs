using UnityEngine;

namespace Anosion.MatcapGenerator
{
    // Inspector で SerializedObject 経由で編集するための入れ物。アセットとしては保存しない。
    internal sealed class MatcapDefinitionHolder : ScriptableObject
    {
        public MatcapDefinition definition = new MatcapDefinition();
    }
}
