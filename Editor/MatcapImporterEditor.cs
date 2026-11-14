using System.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditorInternal;
using UnityEngine;

namespace Anosion.MatcapGenerator
{
    // 値の変更は 2 段階で反映する。
    // 1. プレビュー: インポート済みテクスチャの GPU 側の中身を Graphics.CopyTexture で差し替える（0.1 秒間隔）。
    //    再インポートすると Inspector が作り直されてスライダーのドラッグが途切れるため、操作中はこちらだけを使う。
    // 2. 保存: 操作が止まってから .matcapgen に書き出して再インポートする。
    [CustomEditor(typeof(MatcapImporter))]
    internal sealed class MatcapImporterEditor : ScriptedImporterEditor
    {
        private const double PreviewInterval = 0.1;
        private const double PersistDelay = 0.5;

        // 開いている間に再インポートすると編集対象を見失う、Unity 内部のピッカー類。
        private static readonly string[] PickerWindowTypeNames = { "GradientPicker", "CurveEditorWindow", "ColorPicker" };

        private static readonly int[] ResolutionValues = { 128, 256, 512, 1024 };
        private static readonly GUIContent[] ResolutionOptions = ResolutionValues.Select(v => new GUIContent(v.ToString())).ToArray();

        private static readonly GUIContent[] RadiusModeOptions =
        {
            new GUIContent(MatcapText.RadiusModeOptionImage),
            new GUIContent(MatcapText.RadiusModeOptionAngle),
        };

        private static readonly GUIContent[] BlendOptions =
        {
            new GUIContent(MatcapText.DecalBlendOptionNormal),
            new GUIContent(MatcapText.DecalBlendOptionAdd),
            new GUIContent(MatcapText.DecalBlendOptionScreen),
            new GUIContent(MatcapText.DecalBlendOptionMultiply),
        };

        private const int DecalLineCount = 8;

        private MatcapDefinitionHolder _holder;
        private SerializedObject _holderObject;
        private ReorderableList _decalList;
        private string _assetPath;

        private bool _previewDirty;
        private bool _persistDirty;
        private double _lastPreviewTime;
        private double _lastChangeTime;

        protected override bool needsApplyRevert => false;

        public override bool showImportedObject => false;

        public override void OnEnable()
        {
            base.OnEnable();

            _assetPath = ((AssetImporter)target).assetPath;
            _holder = CreateInstance<MatcapDefinitionHolder>();
            _holder.hideFlags = HideFlags.DontSave;
            _holder.definition = MatcapDefinitionIO.Read(_assetPath);
            _holderObject = new SerializedObject(_holder);
            _decalList = CreateDecalList(_holderObject.FindProperty("definition.decals"));

            EditorApplication.update += Tick;
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        public override void OnDisable()
        {
            EditorApplication.update -= Tick;
            Undo.undoRedoPerformed -= OnUndoRedo;

            if (_persistDirty)
            {
                Persist();
            }

            if (_holder != null)
            {
                DestroyImmediate(_holder);
            }

            base.OnDisable();
        }

        public override void OnInspectorGUI()
        {
            _holderObject.Update();
            var definition = _holderObject.FindProperty("definition");
            var resolution = definition.FindPropertyRelative("resolution");
            var oldResolution = resolution.intValue;

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.IntPopup(resolution, ResolutionOptions, ResolutionValues, new GUIContent(MatcapText.ResolutionLabel));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(MatcapText.GradientSectionHeader, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(definition.FindPropertyRelative("gradient"), new GUIContent(MatcapText.GradientLabel));
            EditorGUILayout.CurveField(
                definition.FindPropertyRelative("remapCurve"),
                Color.green,
                new Rect(0f, 0f, 1f, 1f),
                new GUIContent(MatcapText.RemapCurveLabel));
            var radiusMode = definition.FindPropertyRelative("radiusMode");
            radiusMode.intValue = EditorGUILayout.Popup(
                new GUIContent(MatcapText.RadiusModeLabel, MatcapText.RadiusModeTooltip),
                radiusMode.intValue,
                RadiusModeOptions);

            EditorGUILayout.Space();
            _decalList.DoLayoutList();

            if (EditorGUI.EndChangeCheck())
            {
                _holderObject.ApplyModifiedProperties();
                if (resolution.intValue != oldResolution)
                {
                    // サイズが変わるとテクスチャを差し替えられないので、すぐ再インポートする。
                    Persist();
                }
                else
                {
                    MarkChanged();
                }
            }

            EditorGUILayout.Space();
            if (GUILayout.Button(MatcapText.ExportButton))
            {
                var definitionToExport = _holder.definition;
                var path = _assetPath;
                EditorApplication.delayCall += () => MatcapPngExporter.Export(path, definitionToExport);
            }

            ApplyRevertGUI();
        }

        private ReorderableList CreateDecalList(SerializedProperty decals)
        {
            var lineHeight = EditorGUIUtility.singleLineHeight;
            var spacing = EditorGUIUtility.standardVerticalSpacing;

            var list = new ReorderableList(_holderObject, decals, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, MatcapText.DecalSectionHeader),

                // 既定の「List is Empty」は Gemini を通していない英語の文言なので、空のときは何も描かない。
                drawNoneElementCallback = _ => { },
                elementHeightCallback = _ => DecalLineCount * (lineHeight + spacing) + spacing,
            };

            list.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var element = decals.GetArrayElementAtIndex(index);
                var position = element.FindPropertyRelative("position");
                var line = new Rect(rect.x, rect.y + spacing, rect.width, lineHeight);

                Rect Next()
                {
                    var current = line;
                    line.y += lineHeight + spacing;
                    return current;
                }

                EditorGUI.LabelField(Next(), string.Format(MatcapText.DecalElementLabel, index + 1), EditorStyles.boldLabel);
                EditorGUI.Slider(Next(), position.FindPropertyRelative("x"), -1f, 1f, new GUIContent(MatcapText.DecalPositionXLabel));
                EditorGUI.Slider(Next(), position.FindPropertyRelative("y"), -1f, 1f, new GUIContent(MatcapText.DecalPositionYLabel));
                EditorGUI.Slider(Next(), element.FindPropertyRelative("size"), 0f, 90f, new GUIContent(MatcapText.DecalSizeLabel, MatcapText.DecalSizeTooltip));
                EditorGUI.Slider(Next(), element.FindPropertyRelative("softness"), 0f, 1f, new GUIContent(MatcapText.DecalSoftnessLabel));
                EditorGUI.PropertyField(Next(), element.FindPropertyRelative("color"), new GUIContent(MatcapText.DecalColorLabel));
                EditorGUI.Slider(Next(), element.FindPropertyRelative("opacity"), 0f, 1f, new GUIContent(MatcapText.DecalOpacityLabel));
                var blendMode = element.FindPropertyRelative("blendMode");
                blendMode.intValue = EditorGUI.Popup(Next(), new GUIContent(MatcapText.DecalBlendLabel), blendMode.intValue, BlendOptions);
            };

            // 既定の追加は直前の要素の複製か全項目 0 になるため、MatcapDecal の初期値で追加する。
            list.onAddCallback = l =>
            {
                var index = decals.arraySize;
                decals.arraySize++;
                var element = decals.GetArrayElementAtIndex(index);
                var initial = new MatcapDecal();
                element.FindPropertyRelative("position").vector2Value = initial.position;
                element.FindPropertyRelative("size").floatValue = initial.size;
                element.FindPropertyRelative("softness").floatValue = initial.softness;
                element.FindPropertyRelative("color").colorValue = initial.color;
                element.FindPropertyRelative("opacity").floatValue = initial.opacity;
                element.FindPropertyRelative("blendMode").intValue = (int)initial.blendMode;
                l.index = index;
            };

            // 追加・削除・並べ替えは EndChangeCheck で拾えない場合があるので、ここでも反映する。
            list.onChangedCallback = _ =>
            {
                _holderObject.ApplyModifiedProperties();
                MarkChanged();
            };

            return list;
        }

        private void MarkChanged()
        {
            _previewDirty = true;
            _persistDirty = true;
            _lastChangeTime = EditorApplication.timeSinceStartup;
        }

        private void OnUndoRedo()
        {
            _holderObject.Update();
            MarkChanged();
            Repaint();
        }

        private void Tick()
        {
            var now = EditorApplication.timeSinceStartup;

            if (_previewDirty && now - _lastPreviewTime >= PreviewInterval)
            {
                _previewDirty = false;
                _lastPreviewTime = now;
                UpdatePreview();
            }

            if (_persistDirty
                && !_previewDirty
                && GUIUtility.hotControl == 0
                && now - _lastChangeTime >= PersistDelay
                && !IsPickerOpen())
            {
                Persist();
            }
        }

        private void UpdatePreview()
        {
            var imported = AssetDatabase.LoadAssetAtPath<Texture2D>(_assetPath);
            var size = MatcapRenderer.GetSize(_holder.definition);
            if (imported == null || imported.width != size || imported.height != size || imported.format != TextureFormat.RGBA32)
            {
                Persist();
                return;
            }

            var temporary = MatcapTextureFactory.Create(_holder.definition, mipChain: imported.mipmapCount > 1, makeNoLongerReadable: false);
            try
            {
                Graphics.CopyTexture(temporary, imported);
            }
            finally
            {
                DestroyImmediate(temporary);
            }

            SceneView.RepaintAll();
            Repaint();
        }

        private void Persist()
        {
            _persistDirty = false;
            _previewDirty = false;
            MatcapDefinitionIO.Write(_assetPath, _holder.definition);
            AssetDatabase.ImportAsset(_assetPath);
        }

        private static bool IsPickerOpen()
        {
            return Resources.FindObjectsOfTypeAll<EditorWindow>()
                .Any(w => PickerWindowTypeNames.Contains(w.GetType().Name));
        }
    }
}
