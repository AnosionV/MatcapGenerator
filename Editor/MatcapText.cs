namespace Anosion.MatcapGenerator
{
    // UI 文言は Gemini Hydrogen が UI 要素の目的情報だけから作ったものをそのまま使う。手で言い換えない。
    internal static class MatcapText
    {
        public const string MenuCreate = "Matcap テクスチャ";
        public const string DefaultFileName = "新しい Matcap";
        public const string ResolutionLabel = "解像度";
        public const string GradientSectionHeader = "円形グラデーション";
        public const string GradientLabel = "色（中心→外周）";
        public const string RemapCurveLabel = "色の広がりカーブ";
        public const string RadiusModeLabel = "距離の測り方";
        public const string RadiusModeTooltip = "「視線からの角度」はモデル上で色の帯が均等に見えます。「画像上の距離」は画像の中心からの直線距離を使うため、輪郭付近の色が細くなります。";
        public const string RadiusModeOptionImage = "画像上の距離";
        public const string RadiusModeOptionAngle = "視線からの角度";
        public const string DecalSectionHeader = "デカール";
        public const string DecalElementLabel = "デカール {0}";
        public const string DecalPositionXLabel = "位置（横）";
        public const string DecalPositionYLabel = "位置（縦）";
        public const string DecalSizeLabel = "大きさ（角度）";
        public const string DecalSizeTooltip = "画像のピクセル数ではなく、球面上の角度で大きさを指定します。外周寄りに置くと画像上では潰れて見えます。";
        public const string DecalSoftnessLabel = "ぼかし";
        public const string DecalColorLabel = "色";
        public const string DecalOpacityLabel = "不透明度";
        public const string DecalBlendLabel = "合成モード";
        public const string DecalBlendOptionNormal = "通常";
        public const string DecalBlendOptionAdd = "加算";
        public const string DecalBlendOptionScreen = "スクリーン";
        public const string DecalBlendOptionMultiply = "乗算";
        public const string ExportButton = "PNG として保存";
        public const string OverwriteDialogTitle = "上書きの確認";
        public const string OverwriteDialogMessage = "{0} はすでに存在します。上書きしますか？";
        public const string OverwriteDialogOk = "上書きする";
        public const string OverwriteDialogCancel = "キャンセル";
    }
}
