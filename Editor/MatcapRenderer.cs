using System.Threading.Tasks;
using UnityEngine;

namespace Anosion.MatcapGenerator
{
    // MatcapDefinition からピクセル列を作る。Gradient と AnimationCurve はメインスレッドで LUT に焼き、
    // ピクセルごとの計算は LUT と純粋な数学関数だけで並列に行う。
    internal static class MatcapRenderer
    {
        private const int LutSize = 1024;

        // 1 ピクセルあたり 2x2 のサブサンプル。デカールの縁のジャギーを抑える。
        private static readonly float[] SubSampleOffsets = { 0.25f, 0.75f };

        private readonly struct DecalParams
        {
            public readonly Vector3 Direction;
            public readonly float InnerAngle;
            public readonly float OuterAngle;
            public readonly Color Color;
            public readonly float Strength;
            public readonly DecalBlendMode BlendMode;

            public DecalParams(MatcapDecal decal)
            {
                var p = decal.position;
                if (p.sqrMagnitude > 1f)
                {
                    p.Normalize();
                }

                Direction = new Vector3(p.x, p.y, Mathf.Sqrt(Mathf.Max(0f, 1f - p.sqrMagnitude)));
                OuterAngle = Mathf.Clamp(decal.size, 0f, 180f) * Mathf.Deg2Rad;
                InnerAngle = OuterAngle * (1f - Mathf.Clamp01(decal.softness));
                Color = decal.color;
                Strength = Mathf.Clamp01(decal.opacity) * decal.color.a;
                BlendMode = decal.blendMode;
            }
        }

        public static int GetSize(MatcapDefinition definition)
        {
            return (int)definition.resolution;
        }

        public static Color32[] Render(MatcapDefinition definition)
        {
            var size = GetSize(definition);

            var curveLut = new float[LutSize];
            var gradientLut = new Color[LutSize];
            for (var i = 0; i < LutSize; i++)
            {
                var t = i / (float)(LutSize - 1);
                curveLut[i] = Mathf.Clamp01(definition.remapCurve != null ? definition.remapCurve.Evaluate(t) : t);
                gradientLut[i] = definition.gradient != null ? definition.gradient.Evaluate(t) : new Color(t, t, t, 1f);
            }

            var decals = new DecalParams[definition.decals?.Count ?? 0];
            for (var i = 0; i < decals.Length; i++)
            {
                decals[i] = new DecalParams(definition.decals[i]);
            }

            var useAngle = definition.radiusMode == RadiusMode.Angle;
            var pixels = new Color32[size * size];
            var sampleWeight = 1f / (SubSampleOffsets.Length * SubSampleOffsets.Length);

            Parallel.For(0, size, y =>
            {
                for (var x = 0; x < size; x++)
                {
                    var sum = new Color(0f, 0f, 0f, 0f);
                    foreach (var oy in SubSampleOffsets)
                    {
                        foreach (var ox in SubSampleOffsets)
                        {
                            var u = (x + ox) / size * 2f - 1f;
                            var v = (y + oy) / size * 2f - 1f;
                            sum += Sample(u, v, useAngle, curveLut, gradientLut, decals);
                        }
                    }

                    // テクスチャの行 0 は画像の下端。lilToon の Matcap UV（法線の xy * 0.5 + 0.5）と同じ向き。
                    pixels[y * size + x] = (Color32)(sum * sampleWeight);
                }
            });

            return pixels;
        }

        private static Color Sample(float x, float y, bool useAngle, float[] curveLut, Color[] gradientLut, DecalParams[] decals)
        {
            // 円の外側は円周上の値で埋め、ミップやフィルタで縁に別の色が滲まないようにする。
            var r2 = x * x + y * y;
            if (r2 > 1f)
            {
                var len = Mathf.Sqrt(r2);
                x /= len;
                y /= len;
                r2 = 1f;
            }

            var r = Mathf.Sqrt(r2);
            var t = useAngle ? Mathf.Asin(r) / (Mathf.PI * 0.5f) : r;
            var color = SampleLut(gradientLut, SampleLut(curveLut, t));

            var z = Mathf.Sqrt(Mathf.Max(0f, 1f - r2));
            foreach (var decal in decals)
            {
                var dot = Mathf.Clamp(x * decal.Direction.x + y * decal.Direction.y + z * decal.Direction.z, -1f, 1f);
                var angle = Mathf.Acos(dot);
                var mask = decal.OuterAngle <= decal.InnerAngle
                    ? (angle <= decal.OuterAngle ? 1f : 0f)
                    : 1f - SmoothStep(decal.InnerAngle, decal.OuterAngle, angle);
                var a = mask * decal.Strength;
                if (a <= 0f)
                {
                    continue;
                }

                color = Blend(color, decal.Color, a, decal.BlendMode);
            }

            color.r = Mathf.Clamp01(color.r);
            color.g = Mathf.Clamp01(color.g);
            color.b = Mathf.Clamp01(color.b);
            color.a = Mathf.Clamp01(color.a);
            return color;
        }

        private static Color Blend(Color dst, Color src, float a, DecalBlendMode mode)
        {
            Color blended;
            switch (mode)
            {
                case DecalBlendMode.Add:
                    blended = new Color(dst.r + src.r, dst.g + src.g, dst.b + src.b);
                    break;
                case DecalBlendMode.Screen:
                    blended = new Color(
                        1f - (1f - dst.r) * (1f - src.r),
                        1f - (1f - dst.g) * (1f - src.g),
                        1f - (1f - dst.b) * (1f - src.b));
                    break;
                case DecalBlendMode.Multiply:
                    blended = new Color(dst.r * src.r, dst.g * src.g, dst.b * src.b);
                    break;
                default:
                    blended = src;
                    break;
            }

            return new Color(
                Mathf.LerpUnclamped(dst.r, blended.r, a),
                Mathf.LerpUnclamped(dst.g, blended.g, a),
                Mathf.LerpUnclamped(dst.b, blended.b, a),
                dst.a);
        }

        private static float SmoothStep(float edge0, float edge1, float x)
        {
            var t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        private static float SampleLut(float[] lut, float t)
        {
            var f = Mathf.Clamp01(t) * (lut.Length - 1);
            var i = Mathf.Min((int)f, lut.Length - 2);
            return Mathf.LerpUnclamped(lut[i], lut[i + 1], f - i);
        }

        private static Color SampleLut(Color[] lut, float t)
        {
            var f = Mathf.Clamp01(t) * (lut.Length - 1);
            var i = Mathf.Min((int)f, lut.Length - 2);
            return Color.LerpUnclamped(lut[i], lut[i + 1], f - i);
        }
    }
}
