using System;
using System.Collections.Generic;
using Minebot.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Minebot.Editor
{
    internal static class DualGridAuthoringInspector
    {
        private const float CardWidth = 104f;
        private const float EditableCardHeight = 126f;
        private const float PreviewCardHeight = 88f;
        private const float CardSpacing = 8f;
        private const float SectionSpacing = 10f;
        private const int CanonicalTileCount = 6;
        private const float CompositePreviewTileSize = 32f;
        private const float CompositePreviewPadding = 8f;
        private const float CompositePreviewCellSpacing = 0f;
        private const float CompositePreviewGroupHeaderHeight = 18f;
        private const float CompositePreviewGroupSpacing = 8f;

        private static readonly PatternDefinition[] ExplicitPatterns = BuildExplicitPatterns();
        private static readonly PatternDefinition[] CanonicalPatterns = BuildCanonicalPatterns();
        private static readonly SizePreviewDefinition[][] ResolvedPreviewRows = BuildResolvedPreviewRows();

        public static void DrawArtSetConfigurationFields(SerializedObject serializedObject)
        {
            if (serializedObject == null)
            {
                return;
            }

            SerializedProperty layoutSettings = serializedObject.FindProperty("layoutSettings");
            SerializedProperty families = serializedObject.FindProperty("families");
            SerializedProperty legacyTopology = serializedObject.FindProperty("legacyTopology");
            SerializedProperty fogNear = serializedObject.FindProperty("fogNearDualGridTiles");
            SerializedProperty fogDeep = serializedObject.FindProperty("fogDeepDualGridTiles");

            DrawCoreConfiguration(layoutSettings, families, legacyTopology);
            EditorGUILayout.Space(SectionSpacing);
            DrawIndexedTileSet(
                "雾层 16 格",
                "近雾和深雾也按双网格方向索引配置，下面直接按 16 个状态编辑。",
                new[]
                {
                    new IndexedTileSetDefinition("近雾", fogNear, typeof(TileBase)),
                    new IndexedTileSetDefinition("深雾", fogDeep, typeof(TileBase))
                });
        }

        public static void DrawProfileConfigurationFields(SerializedObject serializedObject)
        {
            if (serializedObject == null)
            {
                return;
            }

            SerializedProperty layoutSettings = serializedObject.FindProperty("layoutSettings");
            SerializedProperty families = serializedObject.FindProperty("families");
            SerializedProperty legacyTopology = serializedObject.FindProperty("legacyTopology");

            DrawCoreConfiguration(layoutSettings, families, legacyTopology);
        }

        public static void DrawValidationMessages(IEnumerable<string> issues)
        {
            bool hasIssue = false;
            if (issues != null)
            {
                foreach (string issue in issues)
                {
                    hasIssue = true;
                    EditorGUILayout.HelpBox(issue, MessageType.Warning);
                }
            }

            if (!hasIssue)
            {
                EditorGUILayout.HelpBox("双网格配置有效。", MessageType.Info);
            }
        }

        private static void DrawCoreConfiguration(
            SerializedProperty layoutSettings,
            SerializedProperty families,
            SerializedProperty legacyTopology)
        {
            EditorGUILayout.LabelField("布局", EditorStyles.boldLabel);
            if (layoutSettings != null)
            {
                EditorGUILayout.PropertyField(layoutSettings);
            }

            EditorGUILayout.Space(SectionSpacing);
            DrawFamilies(families);
            EditorGUILayout.Space(SectionSpacing);
            DrawLegacyTopology(legacyTopology);
        }

        private static void DrawFamilies(SerializedProperty families)
        {
            EditorGUILayout.LabelField("地形族配置", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "参考 RuleTile 的编辑方式：先按方向或基元配置输入，再在下方查看最终 16 格预览。最终预览才是运行时会取用的结果。",
                MessageType.None);

            if (families == null)
            {
                EditorGUILayout.HelpBox("未找到地形族序列化字段。", MessageType.Warning);
                return;
            }

            if (families.arraySize == 0)
            {
                EditorGUILayout.HelpBox("当前还没有地形族条目。初始化后即可按方向编辑每个材质层。", MessageType.Warning);
                if (GUILayout.Button("初始化地形族"))
                {
                    InitializeFamilyArray(families);
                }

                return;
            }

            if (families.arraySize != DualGridTerrain.MaterialFamilies.Length)
            {
                EditorGUILayout.HelpBox($"地形族数量应为 {DualGridTerrain.MaterialFamilies.Length}。", MessageType.Warning);
                if (GUILayout.Button("修复地形族数量"))
                {
                    InitializeFamilyArray(families);
                }
            }

            int familyCount = Mathf.Min(families.arraySize, DualGridTerrain.MaterialFamilies.Length);
            for (int i = 0; i < familyCount; i++)
            {
                SerializedProperty family = families.GetArrayElementAtIndex(i);
                DrawFamily(family, DualGridTerrain.MaterialFamilies[i], i);
                EditorGUILayout.Space(6f);
            }
        }

        private static void DrawFamily(SerializedProperty family, TerrainRenderLayerId expectedLayerId, int familyIndex)
        {
            if (family == null)
            {
                return;
            }

            SerializedProperty layerId = family.FindPropertyRelative("layerId");
            SerializedProperty enabled = family.FindPropertyRelative("enabled");
            SerializedProperty authoringMode = family.FindPropertyRelative("authoringMode");
            SerializedProperty atlas16Source = family.FindPropertyRelative("atlas16Source");
            SerializedProperty explicit16Tiles = family.FindPropertyRelative("explicit16Tiles");
            SerializedProperty canonical6Tiles = family.FindPropertyRelative("canonical6Tiles");
            SerializedProperty perIndexOverrides16 = family.FindPropertyRelative("perIndexOverrides16");
            SerializedProperty allowAutoRotateCanonical = family.FindPropertyRelative("allowAutoRotateCanonical");
            SerializedProperty resolved16Tiles = family.FindPropertyRelative("resolved16Tiles");

            if (layerId != null)
            {
                layerId.enumValueIndex = (int)expectedLayerId;
            }

            EnsureArraySize(explicit16Tiles, DualGridTerrain.TileCount);
            EnsureArraySize(canonical6Tiles, CanonicalTileCount);
            EnsureArraySize(perIndexOverrides16, DualGridTerrain.TileCount);

            string title = $"{ResolveLayerLabel(expectedLayerId)}";
            if (enabled != null && !enabled.boolValue)
            {
                title += "（已禁用）";
            }

            family.isExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(family.isExpanded, title);
            if (family.isExpanded)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (enabled != null)
                        {
                            EditorGUILayout.PropertyField(enabled, new GUIContent("启用"), GUILayout.MaxWidth(120f));
                        }

                        if (authoringMode != null)
                        {
                            EditorGUILayout.PropertyField(authoringMode, new GUIContent("输入模式"));
                        }
                    }

                    if (authoringMode != null)
                    {
                        DrawModeHelp((DualGridAuthoringMode)authoringMode.enumValueIndex);
                    }

                    DrawAuthoringInputs(
                        familyIndex,
                        authoringMode,
                        atlas16Source,
                        explicit16Tiles,
                        canonical6Tiles,
                        allowAutoRotateCanonical);
                    DrawAdvancedOutput(perIndexOverrides16);
                    DrawResolvedPreview(explicit16Tiles, canonical6Tiles, perIndexOverrides16, resolved16Tiles, authoringMode, allowAutoRotateCanonical);
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private static void DrawModeHelp(DualGridAuthoringMode authoringMode)
        {
            switch (authoringMode)
            {
                case DualGridAuthoringMode.Canonical6:
                    EditorGUILayout.HelpBox(
                        "只配置 6 种基础形态。启用自动旋转后，会把单角、邻边、对角和三角扩展到 16 个方向状态。",
                        MessageType.None);
                    break;
                case DualGridAuthoringMode.Atlas16:
                    EditorGUILayout.HelpBox(
                        "Atlas16 当前主要保存图集描述信息；运行时最终仍优先取“手工最终 16 格”或其它已生成的最终结果。",
                        MessageType.Warning);
                    break;
                default:
                    EditorGUILayout.HelpBox(
                        "显式 16 格模式会把 16 个方向状态逐个映射到 Tile，最适合精确控制每个角落组合。",
                        MessageType.None);
                    break;
            }
        }

        private static void DrawAuthoringInputs(
            int familyIndex,
            SerializedProperty authoringMode,
            SerializedProperty atlas16Source,
            SerializedProperty explicit16Tiles,
            SerializedProperty canonical6Tiles,
            SerializedProperty allowAutoRotateCanonical)
        {
            DualGridAuthoringMode mode = authoringMode != null
                ? (DualGridAuthoringMode)authoringMode.enumValueIndex
                : DualGridAuthoringMode.Explicit16;

            EditorGUILayout.Space(4f);
            switch (mode)
            {
                case DualGridAuthoringMode.Canonical6:
                    EditorGUILayout.LabelField("输入基元", EditorStyles.miniBoldLabel);
                    if (allowAutoRotateCanonical != null)
                    {
                        EditorGUILayout.PropertyField(allowAutoRotateCanonical, new GUIContent("允许自动旋转"));
                    }

                    DrawTileGrid(canonical6Tiles, CanonicalPatterns, 3, editable: true, typeof(Tile));
                    break;
                case DualGridAuthoringMode.Atlas16:
                    EditorGUILayout.LabelField("图集输入", EditorStyles.miniBoldLabel);
                    if (atlas16Source != null)
                    {
                        EditorGUILayout.PropertyField(atlas16Source);
                    }

                    break;
                default:
                    EditorGUILayout.LabelField("按方向输入", EditorStyles.miniBoldLabel);
                    DrawTileGrid(explicit16Tiles, ExplicitPatterns, 4, editable: true, typeof(Tile));
                    break;
            }

            EditorGUILayout.LabelField(
                $"最终预览会按常见尺寸样例展示（族索引 {familyIndex + 1}）。",
                EditorStyles.miniLabel);
        }

        private static void DrawAdvancedOutput(SerializedProperty perIndexOverrides16)
        {
            if (perIndexOverrides16 == null)
            {
                return;
            }

            perIndexOverrides16.isExpanded = EditorGUILayout.Foldout(
                perIndexOverrides16.isExpanded,
                "高级：按索引覆盖",
                true);
            if (!perIndexOverrides16.isExpanded)
            {
                return;
            }

            EditorGUILayout.HelpBox(
                "按索引覆盖会叠加到最终结果上。最终 16 格只作为下方预览展示，不再单独手工维护一份隐藏缓存。",
                MessageType.None);
            EditorGUILayout.LabelField("按索引覆盖", EditorStyles.miniBoldLabel);
            DrawTileGrid(perIndexOverrides16, ExplicitPatterns, 4, editable: true, typeof(Tile));
        }

        private static void DrawResolvedPreview(
            SerializedProperty explicit16Tiles,
            SerializedProperty canonical6Tiles,
            SerializedProperty perIndexOverrides16,
            SerializedProperty resolved16Tiles,
            SerializedProperty authoringMode,
            SerializedProperty allowAutoRotateCanonical)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("最终 16 格预览", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField("按 1×1 / 1×2 / 1×3 / 2×2 / 2×3 / 3×3 的合成结果总览。", EditorStyles.miniLabel);
            EditorGUILayout.HelpBox("Dual Grid 本身支持任意 N×M 连通区域；这里固定展示常见尺寸样例，并给出 dual 输出尺寸公式。", MessageType.None);

            Tile[] previewTiles = ResolvePreviewTiles(
                explicit16Tiles,
                canonical6Tiles,
                perIndexOverrides16,
                resolved16Tiles,
                authoringMode != null ? (DualGridAuthoringMode)authoringMode.enumValueIndex : DualGridAuthoringMode.Explicit16,
                allowAutoRotateCanonical != null && allowAutoRotateCanonical.boolValue);

            DrawResolvedCompositePreview(previewTiles);
            DrawSupportedSizeMappings();

            int missingCount = CountMissingTiles(previewTiles);
            if (missingCount > 0)
            {
                EditorGUILayout.HelpBox($"最终 16 格还缺少 {missingCount} 个方向状态。", MessageType.Warning);
            }
        }

        private static void DrawLegacyTopology(SerializedProperty legacyTopology)
        {
            EditorGUILayout.LabelField("旧拓扑资源", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("轮廓 Tile 仍按双网格索引工作，这里也直接按方向状态编辑。", MessageType.None);

            if (legacyTopology == null)
            {
                EditorGUILayout.HelpBox("未找到旧拓扑资源字段。", MessageType.Warning);
                return;
            }

            SerializedProperty wallContourTiles = legacyTopology.FindPropertyRelative("wallContourTiles");
            SerializedProperty dangerContourTiles = legacyTopology.FindPropertyRelative("dangerContourTiles");

            DrawIndexedTileSet(
                "轮廓 16 格",
                null,
                new[]
                {
                    new IndexedTileSetDefinition("墙体轮廓", wallContourTiles, typeof(Tile)),
                    new IndexedTileSetDefinition("危险轮廓", dangerContourTiles, typeof(Tile))
                });
        }

        private static void DrawIndexedTileSet(string title, string description, IReadOnlyList<IndexedTileSetDefinition> tileSets)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            if (!string.IsNullOrWhiteSpace(description))
            {
                EditorGUILayout.HelpBox(description, MessageType.None);
            }

            if (tileSets == null)
            {
                return;
            }

            for (int i = 0; i < tileSets.Count; i++)
            {
                IndexedTileSetDefinition definition = tileSets[i];
                if (definition.Property == null)
                {
                    continue;
                }

                EnsureArraySize(definition.Property, DualGridTerrain.TileCount);
                EditorGUILayout.LabelField(definition.Label, EditorStyles.miniBoldLabel);
                DrawTileGrid(definition.Property, ExplicitPatterns, 4, editable: true, definition.ObjectType);
                EditorGUILayout.Space(6f);
            }
        }

        private static void DrawTileGrid(
            SerializedProperty arrayProperty,
            IReadOnlyList<PatternDefinition> patterns,
            int columns,
            bool editable,
            Type objectReferenceType)
        {
            if (arrayProperty == null || patterns == null)
            {
                return;
            }

            int rows = Mathf.CeilToInt(patterns.Count / (float)columns);
            float cardHeight = editable ? EditableCardHeight : PreviewCardHeight;
            for (int row = 0; row < rows; row++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int column = 0; column < columns; column++)
                    {
                        int patternIndex = (row * columns) + column;
                        if (patternIndex >= patterns.Count)
                        {
                            break;
                        }

                        Rect rect = GUILayoutUtility.GetRect(CardWidth, cardHeight, GUILayout.Width(CardWidth), GUILayout.Height(cardHeight));
                        PatternDefinition pattern = patterns[patternIndex];
                        DrawEditableCard(rect, arrayProperty.GetArrayElementAtIndex(pattern.TileIndex), pattern, objectReferenceType);
                        if (column < columns - 1)
                        {
                            GUILayout.Space(CardSpacing);
                        }
                    }
                }

                GUILayout.Space(4f);
            }
        }

        private static void DrawPreviewGrid(Tile[] tiles, IReadOnlyList<PatternDefinition> patterns, int columns)
        {
            if (tiles == null || patterns == null)
            {
                return;
            }

            int rows = Mathf.CeilToInt(patterns.Count / (float)columns);
            for (int row = 0; row < rows; row++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int column = 0; column < columns; column++)
                    {
                        int patternIndex = (row * columns) + column;
                        if (patternIndex >= patterns.Count)
                        {
                            break;
                        }

                        PatternDefinition pattern = patterns[patternIndex];
                        Rect rect = GUILayoutUtility.GetRect(CardWidth, PreviewCardHeight, GUILayout.Width(CardWidth), GUILayout.Height(PreviewCardHeight));
                        DrawPreviewCard(
                            rect,
                            pattern.TileIndex >= 0 && pattern.TileIndex < tiles.Length ? tiles[pattern.TileIndex] : null,
                            pattern);
                        if (column < columns - 1)
                        {
                            GUILayout.Space(CardSpacing);
                        }
                    }
                }

                GUILayout.Space(4f);
            }
        }

        private static void DrawResolvedCompositePreview(Tile[] tiles)
        {
            if (tiles == null)
            {
                return;
            }

            for (int rowIndex = 0; rowIndex < ResolvedPreviewRows.Length; rowIndex++)
            {
                SizePreviewDefinition[] row = ResolvedPreviewRows[rowIndex];
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int sampleIndex = 0; sampleIndex < row.Length; sampleIndex++)
                    {
                        SizePreviewDefinition sample = row[sampleIndex];
                        float sampleWidth = GetSizePreviewWidth(sample);
                        float sampleHeight = GetSizePreviewHeight(sample);
                        Rect sampleRect = GUILayoutUtility.GetRect(sampleWidth, sampleHeight, GUILayout.Width(sampleWidth), GUILayout.Height(sampleHeight));
                        DrawSizePreviewPanel(sampleRect, sample, tiles);
                        if (sampleIndex < row.Length - 1)
                        {
                            GUILayout.Space(CompositePreviewGroupSpacing);
                        }
                    }
                }

                if (rowIndex < ResolvedPreviewRows.Length - 1)
                {
                    GUILayout.Space(CompositePreviewGroupSpacing);
                }
            }
        }

        private static float GetSizePreviewWidth(SizePreviewDefinition sample)
        {
            return CompositePreviewPadding * 2f
                + (sample.DisplayWidth * CompositePreviewTileSize)
                + (Mathf.Max(0, sample.DisplayWidth - 1) * CompositePreviewCellSpacing);
        }

        private static float GetSizePreviewHeight(SizePreviewDefinition sample)
        {
            return CompositePreviewPadding * 2f
                + CompositePreviewGroupHeaderHeight
                + (sample.DisplayHeight * CompositePreviewTileSize)
                + (Mathf.Max(0, sample.DisplayHeight - 1) * CompositePreviewCellSpacing);
        }

        private static void DrawSizePreviewPanel(Rect rect, SizePreviewDefinition sample, Tile[] tiles)
        {
            DrawCardFrame(rect, true);

            Rect canvasRect = new Rect(
                rect.x + CompositePreviewPadding,
                rect.y + CompositePreviewPadding,
                rect.width - CompositePreviewPadding * 2f,
                rect.height - CompositePreviewPadding * 2f);
            EditorGUI.DrawRect(canvasRect, new Color(0.12f, 0.12f, 0.14f));

            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.82f, 0.9f, 0.96f) }
            };
            GUIStyle metaStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = new Color(0.65f, 0.72f, 0.78f) }
            };

            Rect headerRect = new Rect(canvasRect.x, canvasRect.y, canvasRect.width, CompositePreviewGroupHeaderHeight);
            EditorGUI.LabelField(headerRect, sample.Label, labelStyle);
            EditorGUI.LabelField(headerRect, $"{sample.DisplayWidth}×{sample.DisplayHeight} dual", metaStyle);

            Rect gridRect = new Rect(
                canvasRect.x,
                headerRect.yMax + 4f,
                canvasRect.width,
                canvasRect.height - CompositePreviewGroupHeaderHeight - 4f);

            for (int row = 0; row < sample.DisplayHeight; row++)
            {
                int displayY = sample.SourceHeight - row;
                for (int column = 0; column < sample.DisplayWidth; column++)
                {
                    Rect tileRect = new Rect(
                        gridRect.x + column * (CompositePreviewTileSize + CompositePreviewCellSpacing),
                        gridRect.y + row * (CompositePreviewTileSize + CompositePreviewCellSpacing),
                        CompositePreviewTileSize,
                        CompositePreviewTileSize);
                    Tile tile = ResolveRectanglePreviewTile(tiles, sample.SourceWidth, sample.SourceHeight, column, displayY);
                    if (tile == null)
                    {
                        continue;
                    }

                    DrawCompositeTilePreview(tileRect, tile);
                }
            }
        }

        private static void DrawEditableCard(Rect rect, SerializedProperty tileProperty, PatternDefinition pattern, Type objectReferenceType)
        {
            DrawCardFrame(rect, tileProperty != null && tileProperty.objectReferenceValue != null);

            Rect headerRect = new Rect(rect.x + 6f, rect.y + 4f, rect.width - 12f, 16f);
            Rect maskRect = new Rect(rect.x + 8f, rect.y + 24f, 28f, 28f);
            Rect previewRect = new Rect(rect.x + 42f, rect.y + 22f, rect.width - 50f, 32f);
            Rect objectRect = new Rect(rect.x + 6f, rect.y + rect.height - 22f, rect.width - 12f, 18f);
            Rect descriptionRect = new Rect(rect.x + 6f, rect.y + 60f, rect.width - 12f, 30f);

            EditorGUI.LabelField(headerRect, pattern.Label, EditorStyles.miniBoldLabel);
            DrawPatternMask(maskRect, pattern);
            DrawTilePreview(previewRect, tileProperty != null ? tileProperty.objectReferenceValue : null);
            DrawPatternDescription(descriptionRect, pattern.Description);

            if (tileProperty != null)
            {
                tileProperty.objectReferenceValue = EditorGUI.ObjectField(
                    objectRect,
                    GUIContent.none,
                    tileProperty.objectReferenceValue,
                    objectReferenceType ?? typeof(TileBase),
                    false);
            }
        }

        private static void DrawPreviewCard(Rect rect, Tile tile, PatternDefinition pattern)
        {
            DrawCardFrame(rect, tile != null);

            Rect headerRect = new Rect(rect.x + 6f, rect.y + 4f, rect.width - 12f, 16f);
            Rect maskRect = new Rect(rect.x + 8f, rect.y + 24f, 28f, 28f);
            Rect previewRect = new Rect(rect.x + 42f, rect.y + 18f, rect.width - 50f, 44f);

            EditorGUI.LabelField(headerRect, pattern.Label, EditorStyles.miniBoldLabel);
            DrawPatternMask(maskRect, pattern);
            DrawTilePreview(previewRect, tile);
        }

        private static void DrawCompositeTilePreview(Rect rect, UnityEngine.Object tileObject)
        {
            if (tileObject is Tile tile && tile.sprite != null)
            {
                DrawSpritePreview(rect, tile.sprite);
                return;
            }

            Texture previewTexture = tileObject != null
                ? AssetPreview.GetAssetPreview(tileObject) ?? AssetPreview.GetMiniThumbnail(tileObject)
                : null;
            if (previewTexture != null)
            {
                GUI.DrawTexture(rect, previewTexture, ScaleMode.ScaleToFit, true);
            }
        }

        private static void DrawCardFrame(Rect rect, bool hasTile)
        {
            Color fill = hasTile ? new Color(0.15f, 0.19f, 0.24f) : new Color(0.13f, 0.14f, 0.16f);
            Color border = hasTile ? new Color(0.27f, 0.57f, 0.76f) : new Color(0.24f, 0.25f, 0.28f);

            EditorGUI.DrawRect(rect, fill);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), border);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), border);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), border);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), border);
        }

        private static void DrawPatternMask(Rect rect, PatternDefinition pattern)
        {
            float spacing = 2f;
            float cellSize = (rect.width - spacing) * 0.5f;
            DrawMaskCell(new Rect(rect.x, rect.y, cellSize, cellSize), pattern.TopLeft);
            DrawMaskCell(new Rect(rect.x + cellSize + spacing, rect.y, cellSize, cellSize), pattern.TopRight);
            DrawMaskCell(new Rect(rect.x, rect.y + cellSize + spacing, cellSize, cellSize), pattern.BottomLeft);
            DrawMaskCell(new Rect(rect.x + cellSize + spacing, rect.y + cellSize + spacing, cellSize, cellSize), pattern.BottomRight);
        }

        private static void DrawMaskCell(Rect rect, bool filled)
        {
            Color fill = filled ? new Color(0.34f, 0.78f, 0.7f) : new Color(0.21f, 0.23f, 0.27f);
            Color border = filled ? new Color(0.6f, 0.92f, 0.88f) : new Color(0.29f, 0.31f, 0.35f);
            EditorGUI.DrawRect(rect, fill);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), border);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), border);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), border);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), border);
        }

        private static void DrawTilePreview(Rect rect, UnityEngine.Object tileObject)
        {
            DrawCardFrame(rect, tileObject != null);
            Rect innerRect = new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f);

            if (tileObject is Tile tile && tile.sprite != null)
            {
                DrawSpritePreview(innerRect, tile.sprite);
                return;
            }

            Texture previewTexture = tileObject != null
                ? AssetPreview.GetAssetPreview(tileObject) ?? AssetPreview.GetMiniThumbnail(tileObject)
                : null;
            if (previewTexture != null)
            {
                GUI.DrawTexture(innerRect, previewTexture, ScaleMode.ScaleToFit, true);
                return;
            }

            GUIStyle emptyStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUI.LabelField(innerRect, tileObject == null ? "Empty" : "Preview", emptyStyle);
        }

        private static void DrawPreviewPlaceholder(Rect rect)
        {
            EditorGUI.DrawRect(rect, new Color(0.16f, 0.16f, 0.18f));
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), new Color(0.22f, 0.24f, 0.27f));
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), new Color(0.22f, 0.24f, 0.27f));
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), new Color(0.22f, 0.24f, 0.27f));
            EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), new Color(0.22f, 0.24f, 0.27f));
        }

        private static void DrawSpritePreview(Rect rect, Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
            {
                return;
            }

            Rect texCoords = new Rect(
                sprite.textureRect.x / sprite.texture.width,
                sprite.textureRect.y / sprite.texture.height,
                sprite.textureRect.width / sprite.texture.width,
                sprite.textureRect.height / sprite.texture.height);
            GUI.DrawTextureWithTexCoords(rect, sprite.texture, texCoords, true);
        }

        private static void DrawPatternDescription(Rect rect, string description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return;
            }

            GUIStyle style = new GUIStyle(EditorStyles.miniLabel)
            {
                wordWrap = true,
                alignment = TextAnchor.UpperLeft
            };
            EditorGUI.LabelField(rect, description, style);
        }

        private static Tile[] ResolvePreviewTiles(
            SerializedProperty explicit16Tiles,
            SerializedProperty canonical6Tiles,
            SerializedProperty perIndexOverrides16,
            SerializedProperty resolved16Tiles,
            DualGridAuthoringMode authoringMode,
            bool allowAutoRotateCanonical)
        {
            var resolved = new Tile[DualGridTerrain.TileCount];

            switch (authoringMode)
            {
                case DualGridAuthoringMode.Canonical6:
                    ApplyCanonicalTiles(ReadTiles(canonical6Tiles, CanonicalTileCount), resolved, allowAutoRotateCanonical);
                    break;
                case DualGridAuthoringMode.Explicit16:
                    CopyIfSized(ReadTiles(explicit16Tiles, DualGridTerrain.TileCount), resolved);
                    break;
            }

            if (!HasAnyTile(resolved))
            {
                CopyIfSized(ReadTiles(resolved16Tiles, DualGridTerrain.TileCount), resolved);
            }

            ApplyOverrides(ReadTiles(perIndexOverrides16, DualGridTerrain.TileCount), resolved);
            return resolved;
        }

        private static Tile ResolveRectanglePreviewTile(Tile[] resolvedTiles, int sourceWidth, int sourceHeight, int displayX, int displayY)
        {
            if (resolvedTiles == null || resolvedTiles.Length == 0)
            {
                return null;
            }

            int tileIndex = DualGridTerrain.ComputeIndex(
                IsFilledRectangleCell(displayX - 1, displayY, sourceWidth, sourceHeight),
                IsFilledRectangleCell(displayX, displayY, sourceWidth, sourceHeight),
                IsFilledRectangleCell(displayX - 1, displayY - 1, sourceWidth, sourceHeight),
                IsFilledRectangleCell(displayX, displayY - 1, sourceWidth, sourceHeight));

            return tileIndex >= 0 && tileIndex < resolvedTiles.Length ? resolvedTiles[tileIndex] : null;
        }

        private static void DrawSupportedSizeMappings()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("尺寸映射", EditorStyles.miniBoldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("源区域 -> Dual 输出", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField("1×1 -> 2×2");
                EditorGUILayout.LabelField("1×2 -> 2×3");
                EditorGUILayout.LabelField("1×3 -> 2×4");
                EditorGUILayout.LabelField("2×2 -> 3×3");
                EditorGUILayout.LabelField("2×3 -> 3×4");
                EditorGUILayout.LabelField("3×3 -> 4×4");
                EditorGUILayout.Space(2f);
                EditorGUILayout.LabelField("任意 N×M -> (N+1)×(M+1)", EditorStyles.miniBoldLabel);
            }
        }

        private static bool IsFilledRectangleCell(int x, int y, int width, int height)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        private static Tile[] ReadTiles(SerializedProperty arrayProperty, int expectedCount)
        {
            var result = new Tile[expectedCount];
            if (arrayProperty == null)
            {
                return result;
            }

            int count = Mathf.Min(arrayProperty.arraySize, expectedCount);
            for (int i = 0; i < count; i++)
            {
                result[i] = arrayProperty.GetArrayElementAtIndex(i).objectReferenceValue as Tile;
            }

            return result;
        }

        private static void ApplyOverrides(Tile[] overrides, Tile[] destination)
        {
            if (overrides == null || destination == null)
            {
                return;
            }

            int count = Mathf.Min(overrides.Length, destination.Length);
            for (int i = 0; i < count; i++)
            {
                if (overrides[i] != null)
                {
                    destination[i] = overrides[i];
                }
            }
        }

        private static void ApplyCanonicalTiles(Tile[] canonicalTiles, Tile[] destination, bool allowAutoRotate)
        {
            if (canonicalTiles == null || destination == null || canonicalTiles.Length == 0)
            {
                return;
            }

            SetIfPresent(destination, 0, canonicalTiles, 0);
            SetIfPresent(destination, 15, canonicalTiles, 5);
            SetOrbit(destination, canonicalTiles, 1, new[] { 1, 2, 4, 8 }, allowAutoRotate);
            SetOrbit(destination, canonicalTiles, 2, new[] { 3, 5, 10, 12 }, allowAutoRotate);
            SetOrbit(destination, canonicalTiles, 3, new[] { 6, 9 }, allowAutoRotate);
            SetOrbit(destination, canonicalTiles, 4, new[] { 7, 11, 13, 14 }, allowAutoRotate);
        }

        private static void CopyIfSized(Tile[] source, Tile[] destination)
        {
            if (source == null || destination == null || source.Length != destination.Length)
            {
                return;
            }

            for (int i = 0; i < destination.Length; i++)
            {
                destination[i] = source[i];
            }
        }

        private static void SetOrbit(Tile[] destination, Tile[] canonicalTiles, int canonicalIndex, int[] orbit, bool allowAutoRotate)
        {
            if (canonicalTiles.Length <= canonicalIndex || canonicalTiles[canonicalIndex] == null)
            {
                return;
            }

            for (int i = 0; i < orbit.Length; i++)
            {
                if (!allowAutoRotate && i > 0)
                {
                    break;
                }

                destination[orbit[i]] = canonicalTiles[canonicalIndex];
            }
        }

        private static void SetIfPresent(Tile[] destination, int destinationIndex, Tile[] source, int sourceIndex)
        {
            if (source.Length > sourceIndex && source[sourceIndex] != null)
            {
                destination[destinationIndex] = source[sourceIndex];
            }
        }

        private static bool HasAnyTile(Tile[] tiles)
        {
            if (tiles == null)
            {
                return false;
            }

            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i] != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountMissingTiles(Tile[] tiles)
        {
            if (tiles == null)
            {
                return DualGridTerrain.TileCount;
            }

            int count = 0;
            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i] == null)
                {
                    count++;
                }
            }

            return count;
        }

        private static void EnsureArraySize(SerializedProperty arrayProperty, int expectedCount)
        {
            if (arrayProperty == null || !arrayProperty.isArray || arrayProperty.arraySize == expectedCount)
            {
                return;
            }

            arrayProperty.arraySize = expectedCount;
        }

        private static void InitializeFamilyArray(SerializedProperty families)
        {
            if (families == null)
            {
                return;
            }

            families.arraySize = DualGridTerrain.MaterialFamilies.Length;
            for (int i = 0; i < families.arraySize; i++)
            {
                SerializedProperty family = families.GetArrayElementAtIndex(i);
                if (family == null)
                {
                    continue;
                }

                SerializedProperty layerId = family.FindPropertyRelative("layerId");
                SerializedProperty enabled = family.FindPropertyRelative("enabled");
                SerializedProperty authoringMode = family.FindPropertyRelative("authoringMode");
                SerializedProperty explicit16Tiles = family.FindPropertyRelative("explicit16Tiles");
                SerializedProperty canonical6Tiles = family.FindPropertyRelative("canonical6Tiles");
                SerializedProperty perIndexOverrides16 = family.FindPropertyRelative("perIndexOverrides16");
                SerializedProperty allowAutoRotateCanonical = family.FindPropertyRelative("allowAutoRotateCanonical");
                if (layerId != null)
                {
                    layerId.enumValueIndex = (int)DualGridTerrain.MaterialFamilies[i];
                }

                if (enabled != null)
                {
                    enabled.boolValue = true;
                }

                if (authoringMode != null)
                {
                    authoringMode.enumValueIndex = (int)DualGridAuthoringMode.Explicit16;
                }

                if (allowAutoRotateCanonical != null)
                {
                    allowAutoRotateCanonical.boolValue = true;
                }

                EnsureArraySize(explicit16Tiles, DualGridTerrain.TileCount);
                EnsureArraySize(canonical6Tiles, CanonicalTileCount);
                EnsureArraySize(perIndexOverrides16, DualGridTerrain.TileCount);
            }
        }

        private static string ResolveLayerLabel(TerrainRenderLayerId layerId)
        {
            return layerId switch
            {
                TerrainRenderLayerId.Floor => "地板",
                TerrainRenderLayerId.Soil => "土层",
                TerrainRenderLayerId.Stone => "石层",
                TerrainRenderLayerId.HardRock => "硬岩",
                TerrainRenderLayerId.UltraHard => "超硬岩",
                TerrainRenderLayerId.Boundary => "边界",
                _ => "未知图层"
            };
        }

        private static PatternDefinition[] BuildExplicitPatterns()
        {
            var result = new PatternDefinition[DualGridTerrain.TileCount];
            for (int index = 0; index < result.Length; index++)
            {
                result[index] = new PatternDefinition(
                    index,
                    $"#{index:00}",
                    DescribePattern(index),
                    (index & 8) != 0,
                    (index & 4) != 0,
                    (index & 2) != 0,
                    (index & 1) != 0);
            }

            return result;
        }

        private static PatternDefinition[] BuildCanonicalPatterns()
        {
            return new[]
            {
                new PatternDefinition(0, "空白", "4 个角都为空。", false, false, false, false),
                new PatternDefinition(1, "单角", "只有 1 个角为实心，可旋转到其它角。", true, false, false, false),
                new PatternDefinition(2, "邻边", "2 个相邻角为实心，可旋转到其它边。", true, true, false, false),
                new PatternDefinition(3, "对角", "2 个对角为实心，可旋转到另一条对角。", true, false, false, true),
                new PatternDefinition(4, "三角", "只有 1 个角为空，可旋转到其它缺角。", true, true, true, false),
                new PatternDefinition(5, "实心", "4 个角都为实心。", true, true, true, true)
            };
        }

        private static SizePreviewDefinition[][] BuildResolvedPreviewRows()
        {
            return new[]
            {
                new[]
                {
                    new SizePreviewDefinition("1×1", 1, 1),
                    new SizePreviewDefinition("1×2", 1, 2),
                    new SizePreviewDefinition("1×3", 1, 3),
                },
                new[]
                {
                    new SizePreviewDefinition("2×2", 2, 2),
                    new SizePreviewDefinition("2×3", 2, 3),
                    new SizePreviewDefinition("3×3", 3, 3),
                }
            };
        }

        private static string DescribePattern(int index)
        {
            var filledCorners = new List<string>(4);
            if ((index & 8) != 0)
            {
                filledCorners.Add("左上");
            }

            if ((index & 4) != 0)
            {
                filledCorners.Add("右上");
            }

            if ((index & 2) != 0)
            {
                filledCorners.Add("左下");
            }

            if ((index & 1) != 0)
            {
                filledCorners.Add("右下");
            }

            return filledCorners.Count == 0
                ? "4 个角都为空。"
                : $"实心角：{string.Join(" / ", filledCorners)}。";
        }


        private readonly struct IndexedTileSetDefinition
        {
            public IndexedTileSetDefinition(string label, SerializedProperty property, Type objectType)
            {
                Label = label;
                Property = property;
                ObjectType = objectType;
            }

            public string Label { get; }
            public SerializedProperty Property { get; }
            public Type ObjectType { get; }
        }

        private readonly struct PatternDefinition
        {
            public PatternDefinition(int tileIndex, string label, string description, bool topLeft, bool topRight, bool bottomLeft, bool bottomRight)
            {
                TileIndex = tileIndex;
                Label = label;
                Description = description;
                TopLeft = topLeft;
                TopRight = topRight;
                BottomLeft = bottomLeft;
                BottomRight = bottomRight;
            }

            public int TileIndex { get; }
            public string Label { get; }
            public string Description { get; }
            public bool TopLeft { get; }
            public bool TopRight { get; }
            public bool BottomLeft { get; }
            public bool BottomRight { get; }
        }

        private readonly struct SizePreviewDefinition
        {
            public SizePreviewDefinition(string label, int sourceWidth, int sourceHeight)
            {
                Label = label;
                SourceWidth = Mathf.Max(1, sourceWidth);
                SourceHeight = Mathf.Max(1, sourceHeight);
            }

            public string Label { get; }
            public int SourceWidth { get; }
            public int SourceHeight { get; }
            public int DisplayWidth => SourceWidth + 1;
            public int DisplayHeight => SourceHeight + 1;
        }
    }
}
