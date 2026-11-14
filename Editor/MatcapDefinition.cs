using System;
using System.Collections.Generic;
using UnityEngine;

namespace Anosion.MatcapGenerator
{
    internal enum MatcapResolution
    {
        R128 = 128,
        R256 = 256,
        R512 = 512,
        R1024 = 1024,
    }

    internal enum RadiusMode
    {
        ImageDistance = 0,
        Angle = 1,
    }

    internal enum DecalBlendMode
    {
        Normal = 0,
        Add = 1,
        Screen = 2,
        Multiply = 3,
    }

    [Serializable]
    internal sealed class MatcapDecal
    {
        // 画像の中心が (0, 0)、端が ±1。単位円の外は円周上に寄せて扱う。
        public Vector2 position = new Vector2(-0.35f, 0.35f);

        // 球面上での角半径（度）。
        public float size = 12f;

        // 0 で縁がくっきり、1 で中心から縁まで全体がぼける。
        public float softness = 0.5f;

        public Color color = Color.white;
        public float opacity = 1f;
        public DecalBlendMode blendMode = DecalBlendMode.Add;
    }

    // .matcapgen ファイルの中身。EditorJsonUtility で JSON として読み書きする。
    [Serializable]
    internal sealed class MatcapDefinition
    {
        public MatcapResolution resolution = MatcapResolution.R512;
        public Gradient gradient = CreateDefaultGradient();
        public AnimationCurve remapCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        public RadiusMode radiusMode = RadiusMode.Angle;
        public List<MatcapDecal> decals = new List<MatcapDecal>();

        private static Gradient CreateDefaultGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(0.5f, 0.5f, 0.5f), 1f),
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f),
                });
            return gradient;
        }
    }
}
