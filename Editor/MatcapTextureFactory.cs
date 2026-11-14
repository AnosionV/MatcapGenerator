using UnityEngine;

namespace Anosion.MatcapGenerator
{
    internal static class MatcapTextureFactory
    {
        // インポート結果とライブプレビュー用の一時テクスチャは、Graphics.CopyTexture で
        // 丸ごと写せるように形式・サイズ・ミップ数をそろえる。
        public static Texture2D Create(MatcapDefinition definition, bool mipChain, bool makeNoLongerReadable)
        {
            var size = MatcapRenderer.GetSize(definition);
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, mipChain, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            texture.SetPixels32(MatcapRenderer.Render(definition));
            texture.Apply(mipChain, makeNoLongerReadable);
            return texture;
        }
    }
}
