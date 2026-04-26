using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Minebot.Presentation;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Minebot.Editor
{
    [InitializeOnLoad]
    public static class MinebotPixelArtAssetPipeline
    {
        private const int TilePixelsPerUnit = 16;
        private const int ActorPixelsPerUnit = 32;
        private const int GlyphPixelsPerUnit = 32;
        private const float DefaultScanLabelFontSize = 4f;
        private const string ArtSetPath = "Assets/Resources/Minebot/MinebotPresentationArtSet_Default.asset";
        private const string BitmapGlyphFontAssetPath = "Assets/Resources/Minebot/MinebotBitmapGlyphFont_Default.asset";
        private const string PalettePrefabPath = "Assets/Art/Minebot/Palettes/MinebotTilePalette.prefab";
        private const string DualGridSpriteDirectory = "Assets/Art/Minebot/Sprites/Tiles/DualGridTerrain";
        private const string DualGridTileDirectory = "Assets/Art/Minebot/Tiles/DualGridTerrain";
        private const string HologramSpriteDirectory = "Assets/Art/Minebot/Sprites/UI/Hologram";
        private const string HologramGlyphDirectory = "Assets/Art/Minebot/Sprites/UI/Hologram/Glyphs";
        private const string HologramOverlayAtlasPath = "Assets/Art/Minebot/Sprites/UI/Hologram/hologram_overlay_atlas.png";
        private const string BitmapGlyphAtlasPath = "Assets/Art/Minebot/Sprites/UI/Hologram/hologram_bmfont_digits.png";
        private const string BitmapGlyphDescriptorPath = "Assets/Art/Minebot/Sprites/UI/Hologram/hologram_bmfont_digits.fnt";
        private const string HologramPromptPath = "Assets/Art/Minebot/Generated/Prompts/minebot-hologram-feedback-batch-001.md";
        private const string HologramManifestPath = "Assets/Art/Minebot/Generated/Selected/minebot-hologram-asset-manifest-001.md";
        private const string HologramRecordTemplatePath = "Assets/Art/Minebot/Docs/holographic-feedback-record-template.md";
        private const string FloorTilePath = "Assets/Art/Minebot/Tiles/Tile_FloorCave.asset";
        private const string SoilWallTilePath = "Assets/Art/Minebot/Tiles/Tile_WallSoil.asset";
        private const string StoneWallTilePath = "Assets/Art/Minebot/Tiles/Tile_WallStone.asset";
        private const string HardRockWallTilePath = "Assets/Art/Minebot/Tiles/Tile_WallHardRock.asset";
        private const string UltraHardWallTilePath = "Assets/Art/Minebot/Tiles/Tile_WallUltraHard.asset";
        private const string BoundaryTilePath = "Assets/Art/Minebot/Tiles/Tile_Boundary.asset";
        private const string DangerTilePath = "Assets/Art/Minebot/Tiles/Tile_OverlayDanger.asset";
        private const string MarkerTilePath = "Assets/Art/Minebot/Tiles/Tile_OverlayMarker.asset";
        private const string ScanHintTilePath = "Assets/Art/Minebot/Tiles/Tile_HintScan.asset";
        private const string RepairStationTilePath = "Assets/Art/Minebot/Tiles/Tile_FacilityRepairStation.asset";
        private const string RobotFactoryTilePath = "Assets/Art/Minebot/Tiles/Tile_FacilityRobotFactory.asset";
        private const string BuildPreviewValidTilePath = "Assets/Art/Minebot/Tiles/Tile_BuildPreviewValid.asset";
        private const string BuildPreviewInvalidTilePath = "Assets/Art/Minebot/Tiles/Tile_BuildPreviewInvalid.asset";
        private const string SoilDetailTilePath = "Assets/Art/Minebot/Tiles/Tile_DetailSoil.asset";
        private const string StoneDetailTilePath = "Assets/Art/Minebot/Tiles/Tile_DetailStone.asset";
        private const string HardRockDetailTilePath = "Assets/Art/Minebot/Tiles/Tile_DetailHardRock.asset";
        private const string UltraHardDetailTilePath = "Assets/Art/Minebot/Tiles/Tile_DetailUltraHard.asset";
        private const string DangerOutlineThinTilePath = "Assets/Art/Minebot/Tiles/Tile_DangerOutlineThin.asset";
        private const string DangerOutlineMediumTilePath = "Assets/Art/Minebot/Tiles/Tile_DangerOutlineMedium.asset";
        private const string DangerOutlineThickTilePath = "Assets/Art/Minebot/Tiles/Tile_DangerOutlineThick.asset";

        private static readonly char[] BitmapGlyphDigits = "0123456789".ToCharArray();
        private static readonly AssetEntry[] TileEntries = CreateTileEntries();
        private static readonly string[] PaletteTilePaths = CreatePaletteTilePaths();
        private static readonly TextureEntry[] HologramEntries = CreateHologramEntries();
        private static readonly TerrainRenderLayerId[] DualGridFamilies =
        {
            TerrainRenderLayerId.Floor,
            TerrainRenderLayerId.Soil,
            TerrainRenderLayerId.Stone,
            TerrainRenderLayerId.HardRock,
            TerrainRenderLayerId.UltraHard,
            TerrainRenderLayerId.Boundary
        };

        private static readonly TextureEntry[] ActorEntries =
        {
            new("Assets/Art/Minebot/Sprites/Actors/actor_player_minebot.png", ActorPixelsPerUnit),
            new("Assets/Art/Minebot/Sprites/Actors/actor_helper_robot.png", ActorPixelsPerUnit)
        };

        static MinebotPixelArtAssetPipeline()
        {
            EditorApplication.delayCall += EnsureDefaultAssets;
        }

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            EditorApplication.delayCall += EnsureDefaultAssets;
        }

        [MenuItem("Minebot/Art/Rebuild Pixel Art Assets")]
        public static void EnsureDefaultAssets()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            EnsureDualGridTerrainSprites();
            EnsureHologramSupportFiles();
            MinebotPrefabGameplayArtSupport.EnsureGeneratedFiles();
            EnsureTextureImporters();
            EnsureTileAssets();
            EnsureBitmapGlyphFontAsset();
            MinebotPrefabGameplayArtSupport.EnsureGeneratedAssets();
            EnsureArtSet();
            EnsurePalettePrefab();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Minebot/Art/Validate Pixel Art Import Settings")]
        public static void ValidateImportSettings()
        {
            var errors = new List<string>();
            foreach (AssetEntry entry in TileEntries)
            {
                ValidateTexture(entry.SpritePath, TilePixelsPerUnit, errors);
            }

            foreach (TextureEntry entry in ActorEntries)
            {
                ValidateTexture(entry.SpritePath, entry.PixelsPerUnit, errors);
            }

            foreach (TextureEntry entry in HologramEntries)
            {
                ValidateTexture(entry.SpritePath, entry.PixelsPerUnit, errors);
            }

            foreach (TerrainRenderLayerId layerId in DualGridFamilies)
            {
                for (int i = 0; i < DualGridTerrain.TileCount; i++)
                {
                    ValidateTexture(GetDualGridSpritePath(layerId, i), TilePixelsPerUnit, errors);
                }
            }

            MinebotPrefabGameplayArtSupport.ValidateImportSettings(errors);

            if (AssetDatabase.LoadAssetAtPath<TextAsset>(BitmapGlyphDescriptorPath) == null)
            {
                errors.Add($"{BitmapGlyphDescriptorPath}: missing descriptor");
            }

            if (AssetDatabase.LoadAssetAtPath<BitmapGlyphFontDefinition>(BitmapGlyphFontAssetPath) == null)
            {
                errors.Add($"{BitmapGlyphFontAssetPath}: missing bitmap glyph font asset");
            }

            if (errors.Count == 0)
            {
                Debug.Log("Minebot pixel art import settings are valid.");
                return;
            }

            Debug.LogError("Minebot pixel art import settings failed:\n" + string.Join("\n", errors));
        }

        private static void EnsureTextureImporters()
        {
            foreach (AssetEntry entry in TileEntries)
            {
                ConfigureTextureImporter(entry.SpritePath, TilePixelsPerUnit);
            }

            foreach (TextureEntry entry in ActorEntries)
            {
                ConfigureTextureImporter(entry.SpritePath, entry.PixelsPerUnit);
            }

            foreach (TextureEntry entry in HologramEntries)
            {
                ConfigureTextureImporter(entry.SpritePath, entry.PixelsPerUnit);
            }

            foreach (TerrainRenderLayerId layerId in DualGridFamilies)
            {
                for (int i = 0; i < DualGridTerrain.TileCount; i++)
                {
                    ConfigureTextureImporter(GetDualGridSpritePath(layerId, i), TilePixelsPerUnit);
                }
            }

            MinebotPrefabGameplayArtSupport.EnsureTextureImporters();
        }

        private static void ConfigureTextureImporter(string assetPath, int pixelsPerUnit)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                changed = true;
            }

            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                changed = true;
            }

            if (!Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerUnit))
            {
                importer.spritePixelsPerUnit = pixelsPerUnit;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static void EnsureTileAssets()
        {
            foreach (AssetEntry entry in TileEntries)
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(entry.SpritePath);
                if (sprite == null)
                {
                    continue;
                }

                Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(entry.TilePath);
                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    tile.name = entry.Name;
                    AssetDatabase.CreateAsset(tile, entry.TilePath);
                }

                tile.sprite = sprite;
                tile.colliderType = Tile.ColliderType.None;
                EditorUtility.SetDirty(tile);
            }

            EnsureDualGridTerrainTileAssets();
        }

        private static BitmapGlyphFontDefinition EnsureBitmapGlyphFontAsset()
        {
            BitmapGlyphFontDefinition font = AssetDatabase.LoadAssetAtPath<BitmapGlyphFontDefinition>(BitmapGlyphFontAssetPath);
            if (font == null)
            {
                font = ScriptableObject.CreateInstance<BitmapGlyphFontDefinition>();
                AssetDatabase.CreateAsset(font, BitmapGlyphFontAssetPath);
            }

            Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(BitmapGlyphAtlasPath);
            TextAsset descriptor = AssetDatabase.LoadAssetAtPath<TextAsset>(BitmapGlyphDescriptorPath);
            var glyphDefinitions = new BitmapGlyphFontDefinition.GlyphDefinition[BitmapGlyphDigits.Length];
            Dictionary<char, float> advances = ParseGlyphAdvances(descriptor);
            for (int i = 0; i < BitmapGlyphDigits.Length; i++)
            {
                char digit = BitmapGlyphDigits[i];
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GetBitmapGlyphSpritePath(digit));
                float advance = advances.TryGetValue(digit, out float parsedAdvance) ? parsedAdvance : 10f;
                glyphDefinitions[i] = new BitmapGlyphFontDefinition.GlyphDefinition(digit, sprite, advance);
            }

            font.Configure(atlas, descriptor, 16f, DefaultScanLabelFontSize, glyphDefinitions);
            EditorUtility.SetDirty(font);
            return font;
        }

        private static void EnsureArtSet()
        {
            MinebotPresentationArtSet artSet = AssetDatabase.LoadAssetAtPath<MinebotPresentationArtSet>(ArtSetPath);
            if (artSet == null)
            {
                artSet = ScriptableObject.CreateInstance<MinebotPresentationArtSet>();
                AssetDatabase.CreateAsset(artSet, ArtSetPath);
            }

            Sprite player = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Minebot/Sprites/Actors/actor_player_minebot.png");
            Sprite robot = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Minebot/Sprites/Actors/actor_helper_robot.png");
            BitmapGlyphFontDefinition bitmapGlyphFont = EnsureBitmapGlyphFontAsset();
            artSet.Configure(
                LoadTile(FloorTilePath),
                LoadTile(SoilWallTilePath),
                LoadTile(StoneWallTilePath),
                LoadTile(HardRockWallTilePath),
                LoadTile(UltraHardWallTilePath),
                LoadTile(BoundaryTilePath),
                LoadTile(DangerTilePath),
                LoadTile(MarkerTilePath),
                LoadTile(ScanHintTilePath),
                LoadTile(RepairStationTilePath),
                LoadTile(RobotFactoryTilePath),
                player,
                robot,
                LoadTile(SoilDetailTilePath),
                LoadTile(StoneDetailTilePath),
                LoadTile(HardRockDetailTilePath),
                LoadTile(UltraHardDetailTilePath),
                LoadTile(BuildPreviewValidTilePath),
                LoadTile(BuildPreviewInvalidTilePath),
                LoadIndexedTiles("Assets/Art/Minebot/Tiles/Tile_WallContour_{0:00}.asset"),
                LoadIndexedTiles("Assets/Art/Minebot/Tiles/Tile_DangerContour_{0:00}.asset"),
                LoadDangerOutlineTiles(),
                LoadDualGridTiles(TerrainRenderLayerId.Floor),
                LoadDualGridTiles(TerrainRenderLayerId.Soil),
                LoadDualGridTiles(TerrainRenderLayerId.Stone),
                LoadDualGridTiles(TerrainRenderLayerId.HardRock),
                LoadDualGridTiles(TerrainRenderLayerId.UltraHard),
                LoadDualGridTiles(TerrainRenderLayerId.Boundary),
                bitmapGlyphFont,
                AssetDatabase.LoadAssetAtPath<Texture2D>(BitmapGlyphAtlasPath),
                AssetDatabase.LoadAssetAtPath<TextAsset>(BitmapGlyphDescriptorPath),
                AssetDatabase.LoadAssetAtPath<Texture2D>(HologramOverlayAtlasPath),
                new Vector2(0f, 0.7f),
                new Color(0.62f, 1f, 0.96f, 1f),
                DefaultScanLabelFontSize,
                35);
            MinebotPrefabGameplayArtSupport.ConfigureArtSet(artSet);
            EditorUtility.SetDirty(artSet);
        }

        private static void EnsurePalettePrefab()
        {
            var root = new GameObject("Minebot Tile Palette");
            root.AddComponent<Grid>();
            var tilemapObject = new GameObject("Core Tiles", typeof(Tilemap), typeof(TilemapRenderer));
            tilemapObject.transform.SetParent(root.transform, false);
            var tilemap = tilemapObject.GetComponent<Tilemap>();

            for (int i = 0; i < PaletteTilePaths.Length; i++)
            {
                Tile tile = LoadTile(PaletteTilePaths[i]);
                if (tile != null)
                {
                    tilemap.SetTile(new Vector3Int(i, 0, 0), tile);
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, PalettePrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static Tile LoadTile(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Tile>(path);
        }

        private static Tile[] LoadIndexedTiles(string tilePathFormat)
        {
            var tiles = new Tile[DualGridContour.TileCount];
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i] = LoadTile(string.Format(tilePathFormat, i));
            }

            return tiles;
        }

        private static Tile[] LoadDualGridTiles(TerrainRenderLayerId layerId)
        {
            var tiles = new Tile[DualGridTerrain.TileCount];
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i] = LoadTile(GetDualGridTilePath(layerId, i));
            }

            return tiles;
        }

        private static Tile[] LoadDangerOutlineTiles()
        {
            return new[]
            {
                LoadTile(DangerOutlineThinTilePath),
                LoadTile(DangerOutlineMediumTilePath),
                LoadTile(DangerOutlineThickTilePath)
            };
        }

        private static void EnsureDualGridTerrainSprites()
        {
            EnsureAssetDirectory(DualGridSpriteDirectory);
            foreach (TerrainRenderLayerId layerId in DualGridFamilies)
            {
                for (int i = 0; i < DualGridTerrain.TileCount; i++)
                {
                    string spriteAssetPath = GetDualGridSpritePath(layerId, i);
                    string fullPath = AssetPathToFullPath(spriteAssetPath);
                    using var scope = new GeneratedTextureScope(DualGridTerrainFallbackTiles.CreateTexture(
                        layerId,
                        i,
                        $"{GetDualGridSpriteFileName(layerId, i)}_Texture"));
                    File.WriteAllBytes(fullPath, scope.Texture.EncodeToPNG());
                }
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }

        private static void EnsureHologramSupportFiles()
        {
            EnsureAssetDirectory(HologramSpriteDirectory);
            EnsureAssetDirectory(HologramGlyphDirectory);
            EnsureGeneratedTexture(HologramOverlayAtlasPath, CreateHologramOverlayAtlas());
            EnsureGeneratedTexture(BitmapGlyphAtlasPath, CreateBitmapGlyphAtlas());

            for (int i = 0; i < BitmapGlyphDigits.Length; i++)
            {
                EnsureGeneratedTexture(GetBitmapGlyphSpritePath(BitmapGlyphDigits[i]), CreateBitmapGlyphTexture(BitmapGlyphDigits[i]));
            }

            EnsureGeneratedTexture("Assets/Art/Minebot/Sprites/Tiles/tile_overlay_danger.png", CreateDangerBaseTexture());
            EnsureGeneratedTexture("Assets/Art/Minebot/Sprites/Tiles/tile_overlay_marker.png", CreateMarkerTexture());
            EnsureGeneratedTexture("Assets/Art/Minebot/Sprites/Tiles/tile_hint_scan.png", CreateScanHintTexture());
            EnsureGeneratedTexture("Assets/Art/Minebot/Sprites/Tiles/tile_build_preview_invalid.png", CreateBuildPreviewInvalidTexture());

            for (int i = 0; i < DualGridContour.TileCount; i++)
            {
                EnsureGeneratedTexture($"Assets/Art/Minebot/Sprites/Tiles/tile_danger_contour_{i:00}.png", CreateDangerContourTexture(i));
            }

            EnsureGeneratedTexture("Assets/Art/Minebot/Sprites/Tiles/tile_danger_outline_thin.png", CreateDangerOutlineTexture(1));
            EnsureGeneratedTexture("Assets/Art/Minebot/Sprites/Tiles/tile_danger_outline_medium.png", CreateDangerOutlineTexture(2));
            EnsureGeneratedTexture("Assets/Art/Minebot/Sprites/Tiles/tile_danger_outline_thick.png", CreateDangerOutlineTexture(3));

            EnsureGeneratedText(BitmapGlyphDescriptorPath, CreateBitmapGlyphDescriptor());
            EnsureGeneratedText(HologramPromptPath, CreateHologramPromptTemplate());
            EnsureGeneratedText(HologramManifestPath, CreateHologramManifestTemplate());
            EnsureGeneratedText(HologramRecordTemplatePath, CreateHologramRecordTemplate());
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }

        private static void EnsureDualGridTerrainTileAssets()
        {
            EnsureAssetDirectory(DualGridTileDirectory);
            foreach (TerrainRenderLayerId layerId in DualGridFamilies)
            {
                for (int i = 0; i < DualGridTerrain.TileCount; i++)
                {
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GetDualGridSpritePath(layerId, i));
                    if (sprite == null)
                    {
                        continue;
                    }

                    string tilePath = GetDualGridTilePath(layerId, i);
                    Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
                    if (tile == null)
                    {
                        tile = ScriptableObject.CreateInstance<Tile>();
                        AssetDatabase.CreateAsset(tile, tilePath);
                    }

                    tile.name = GetDualGridTileAssetName(layerId, i);
                    tile.sprite = sprite;
                    tile.colliderType = Tile.ColliderType.None;
                    EditorUtility.SetDirty(tile);
                }
            }
        }

        private static void EnsureAssetDirectory(string assetDirectoryPath)
        {
            Directory.CreateDirectory(AssetPathToFullPath(assetDirectoryPath));
        }

        private static string AssetPathToFullPath(string assetPath)
        {
            if (string.Equals(assetPath, "Assets", StringComparison.Ordinal))
            {
                return Application.dataPath;
            }

            return assetPath.StartsWith("Assets/", StringComparison.Ordinal)
                ? Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length))
                : Path.GetFullPath(assetPath);
        }

        private static string GetDualGridSpritePath(TerrainRenderLayerId layerId, int index)
        {
            return $"{DualGridSpriteDirectory}/{GetDualGridSpriteFileName(layerId, index)}.png";
        }

        private static string GetDualGridTilePath(TerrainRenderLayerId layerId, int index)
        {
            return $"{DualGridTileDirectory}/{GetDualGridTileAssetName(layerId, index)}.asset";
        }

        private static string GetDualGridSpriteFileName(TerrainRenderLayerId layerId, int index)
        {
            return $"tile_dg_{GetDualGridFamilyToken(layerId)}_{index:00}";
        }

        private static string GetDualGridTileAssetName(TerrainRenderLayerId layerId, int index)
        {
            return $"Tile_DG_{GetDualGridFamilyAssetToken(layerId)}_{index:00}";
        }

        private static string GetDualGridFamilyToken(TerrainRenderLayerId layerId)
        {
            switch (layerId)
            {
                case TerrainRenderLayerId.HardRock:
                    return "hard_rock";
                case TerrainRenderLayerId.UltraHard:
                    return "ultra_hard";
                default:
                    return layerId.ToString().ToLowerInvariant();
            }
        }

        private static string GetDualGridFamilyAssetToken(TerrainRenderLayerId layerId)
        {
            switch (layerId)
            {
                case TerrainRenderLayerId.HardRock:
                    return "HardRock";
                case TerrainRenderLayerId.UltraHard:
                    return "UltraHard";
                default:
                    return layerId.ToString();
            }
        }

        private static void ValidateTexture(string assetPath, int pixelsPerUnit, ICollection<string> errors)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                errors.Add($"{assetPath}: missing TextureImporter");
                return;
            }

            if (importer.textureType != TextureImporterType.Sprite)
            {
                errors.Add($"{assetPath}: Texture Type must be Sprite");
            }

            if (importer.filterMode != FilterMode.Point)
            {
                errors.Add($"{assetPath}: Filter Mode must be Point");
            }

            if (importer.mipmapEnabled)
            {
                errors.Add($"{assetPath}: Mipmap must be disabled");
            }

            if (!Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerUnit))
            {
                errors.Add($"{assetPath}: Pixels Per Unit must be {pixelsPerUnit}");
            }
        }

        private static void EnsureGeneratedTexture(string assetPath, Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            string directoryPath = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(directoryPath))
            {
                EnsureAssetDirectory(directoryPath);
            }

            try
            {
                File.WriteAllBytes(AssetPathToFullPath(assetPath), texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void EnsureGeneratedText(string assetPath, string content)
        {
            string directoryPath = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(directoryPath))
            {
                EnsureAssetDirectory(directoryPath);
            }

            string fullPath = AssetPathToFullPath(assetPath);
            if (File.Exists(fullPath) && string.Equals(File.ReadAllText(fullPath), content, StringComparison.Ordinal))
            {
                return;
            }

            File.WriteAllText(fullPath, content, new UTF8Encoding(false));
        }

        private static Dictionary<char, float> ParseGlyphAdvances(TextAsset descriptor)
        {
            var result = new Dictionary<char, float>();
            if (descriptor == null || string.IsNullOrEmpty(descriptor.text))
            {
                return result;
            }

            string[] lines = descriptor.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (!line.StartsWith("char ", StringComparison.Ordinal))
                {
                    continue;
                }

                int charId = 0;
                int xAdvance = 10;
                string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                for (int partIndex = 0; partIndex < parts.Length; partIndex++)
                {
                    string part = parts[partIndex];
                    if (TryParseIntToken(part, "id", out int parsedId))
                    {
                        charId = parsedId;
                    }
                    else if (TryParseIntToken(part, "xadvance", out int parsedAdvance))
                    {
                        xAdvance = parsedAdvance;
                    }
                }

                if (charId > 0)
                {
                    result[(char)charId] = Mathf.Max(1, xAdvance);
                }
            }

            return result;
        }

        private static bool TryParseIntToken(string token, string key, out int value)
        {
            string prefix = key + "=";
            if (token.StartsWith(prefix, StringComparison.Ordinal) && int.TryParse(token.Substring(prefix.Length), out value))
            {
                return true;
            }

            value = 0;
            return false;
        }

        private static string GetBitmapGlyphSpritePath(char digit)
        {
            return $"{HologramGlyphDirectory}/hologram_digit_{digit}.png";
        }

        private static Texture2D CreateBitmapGlyphTexture(char digit)
        {
            const int width = 10;
            const int height = 16;
            Texture2D texture = CreateTransparentTexture($"Hologram Glyph {digit}", width, height);
            bool[] segments = SevenSegmentMaskFor(digit);
            Color core = new Color(0.72f, 1f, 0.96f, 0.98f);
            Color glow = new Color(0.22f, 0.86f, 1f, 0.42f);
            Color clear = new Color(0f, 0f, 0f, 0f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool lit = IsSevenSegmentPixelLit(segments, x, y, width, height);
                    texture.SetPixel(x, y, lit ? ((x + y) % 3 == 0 ? glow : core) : clear);
                }
            }

            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D CreateBitmapGlyphAtlas()
        {
            const int glyphWidth = 10;
            const int glyphHeight = 16;
            Texture2D atlas = CreateTransparentTexture("Hologram Bitmap Glyph Atlas", glyphWidth * BitmapGlyphDigits.Length, glyphHeight);
            for (int i = 0; i < BitmapGlyphDigits.Length; i++)
            {
                Texture2D glyphTexture = CreateBitmapGlyphTexture(BitmapGlyphDigits[i]);
                try
                {
                    BlitTexture(glyphTexture, atlas, i * glyphWidth, 0);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(glyphTexture);
                }
            }

            atlas.Apply(false, false);
            return atlas;
        }

        private static Texture2D CreateHologramOverlayAtlas()
        {
            Texture2D atlas = CreateTransparentTexture("Hologram Overlay Atlas", 48, 16);
            Texture2D danger = CreateDangerBaseTexture();
            Texture2D marker = CreateMarkerTexture();
            Texture2D scan = CreateScanHintTexture();
            try
            {
                BlitTexture(danger, atlas, 0, 0);
                BlitTexture(marker, atlas, 16, 0);
                BlitTexture(scan, atlas, 32, 0);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(danger);
                UnityEngine.Object.DestroyImmediate(marker);
                UnityEngine.Object.DestroyImmediate(scan);
            }

            atlas.Apply(false, false);
            return atlas;
        }

        private static Texture2D CreateDangerBaseTexture()
        {
            Texture2D texture = CreateTransparentTexture("Danger Base", 16, 16);
            Color fill = new Color(1f, 0.14f, 0.18f, 0.2f);
            Color accent = new Color(1f, 0.36f, 0.24f, 0.95f);
            Color highlight = new Color(1f, 0.8f, 0.76f, 0.95f);
            Color clear = new Color(0f, 0f, 0f, 0f);

            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    bool trimmedCorner = (x < 2 && y < 2) || (x > 13 && y < 2) || (x < 2 && y > 13) || (x > 13 && y > 13);
                    if (trimmedCorner)
                    {
                        texture.SetPixel(x, y, clear);
                        continue;
                    }

                    bool ring = x == 1 || y == 1 || x == 14 || y == 14;
                    bool innerRing = x == 3 || y == 3 || x == 12 || y == 12;
                    bool scanline = x > 2 && x < 13 && y > 2 && y < 13 && ((x + y) % 4 == 0);
                    Color color = clear;
                    if (ring)
                    {
                        color = accent;
                    }
                    else if (innerRing)
                    {
                        color = highlight;
                    }
                    else if (scanline)
                    {
                        color = fill;
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D CreateBuildPreviewInvalidTexture()
        {
            Texture2D texture = CreateTransparentTexture("Build Preview Invalid", 16, 16);
            Color fill = new Color(1f, 0.12f, 0.16f, 0.16f);
            Color accent = new Color(1f, 0.3f, 0.22f, 0.95f);
            Color highlight = new Color(1f, 0.78f, 0.72f, 0.95f);
            Color clear = new Color(0f, 0f, 0f, 0f);

            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    bool trimmedCorner = (x < 2 && y < 2) || (x > 13 && y < 2) || (x < 2 && y > 13) || (x > 13 && y > 13);
                    if (trimmedCorner)
                    {
                        texture.SetPixel(x, y, clear);
                        continue;
                    }

                    bool outerRing = x == 1 || y == 1 || x == 14 || y == 14;
                    bool diagonalA = Mathf.Abs(x - y) <= 1 && x > 2 && x < 13 && y > 2 && y < 13;
                    bool diagonalB = Mathf.Abs((x + y) - 15) <= 1 && x > 2 && x < 13 && y > 2 && y < 13;
                    bool scanline = x > 3 && x < 12 && y > 3 && y < 12 && ((x + y) % 5 == 0);
                    Color color = clear;
                    if (outerRing)
                    {
                        color = accent;
                    }
                    else if (diagonalA || diagonalB)
                    {
                        color = highlight;
                    }
                    else if (scanline)
                    {
                        color = fill;
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D CreateMarkerTexture()
        {
            Texture2D texture = CreateTransparentTexture("Marker Overlay", 16, 16);
            Color core = new Color(0.64f, 1f, 0.96f, 0.92f);
            Color glow = new Color(0.18f, 0.82f, 1f, 0.34f);
            Color clear = new Color(0f, 0f, 0f, 0f);

            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    int distance = Mathf.Abs(x - 7) + Mathf.Abs(y - 7);
                    bool diamond = distance <= 5;
                    bool highlight = diamond && (distance >= 4 || x == 7 || y == 7);
                    texture.SetPixel(x, y, diamond ? (highlight ? core : glow) : clear);
                }
            }

            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D CreateScanHintTexture()
        {
            Texture2D texture = CreateTransparentTexture("Scan Hint Overlay", 16, 16);
            Color ring = new Color(0.72f, 1f, 0.96f, 0.95f);
            Color pulse = new Color(0.18f, 0.82f, 1f, 0.28f);
            Color clear = new Color(0f, 0f, 0f, 0f);

            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    int dx = x - 7;
                    int dy = y - 7;
                    int distanceSq = dx * dx + dy * dy;
                    bool outer = distanceSq >= 20 && distanceSq <= 30;
                    bool inner = distanceSq >= 6 && distanceSq <= 10;
                    bool crosshair = (x == 7 || y == 7) && distanceSq <= 20;
                    Color color = clear;
                    if (outer)
                    {
                        color = ring;
                    }
                    else if (inner || crosshair)
                    {
                        color = pulse;
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D CreateDangerContourTexture(int contourIndex)
        {
            return CreateContourTexture($"Danger Contour {contourIndex:00}", contourIndex, new Color(1f, 0.42f, 0.34f, 0.95f), 2);
        }

        private static Texture2D CreateDangerOutlineTexture(int thickness)
        {
            Texture2D texture = CreateTransparentTexture($"Danger Outline {thickness}", 16, 16);
            Color outline = thickness switch
            {
                1 => new Color(1f, 0.62f, 0.54f, 0.95f),
                2 => new Color(1f, 0.46f, 0.36f, 0.95f),
                _ => new Color(1f, 0.28f, 0.22f, 0.95f)
            };
            Color clear = new Color(0f, 0f, 0f, 0f);

            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    bool isOutline = x < thickness || y < thickness || x >= 16 - thickness || y >= 16 - thickness;
                    texture.SetPixel(x, y, isOutline ? outline : clear);
                }
            }

            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D CreateContourTexture(string name, int contourIndex, Color outlineColor, int thickness)
        {
            const int size = 16;
            const int halfSize = size / 2;
            Texture2D texture = CreateTransparentTexture(name, size, size);
            int safeThickness = Mathf.Clamp(thickness, 1, halfSize);

            bool topLeft = (contourIndex & (1 << 3)) != 0;
            bool topRight = (contourIndex & (1 << 2)) != 0;
            bool bottomLeft = (contourIndex & (1 << 1)) != 0;
            bool bottomRight = (contourIndex & 1) != 0;
            Color clear = new Color(0f, 0f, 0f, 0f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isTop = y >= halfSize;
                    bool isLeft = x < halfSize;
                    int xWithin = isLeft ? x : x - halfSize;
                    int yWithin = isTop ? y - halfSize : y;

                    bool isFilled = isTop
                        ? (isLeft ? topLeft : topRight)
                        : (isLeft ? bottomLeft : bottomRight);

                    if (!isFilled)
                    {
                        texture.SetPixel(x, y, clear);
                        continue;
                    }

                    bool draw = false;
                    if (isTop && isLeft)
                    {
                        draw = (topLeft != topRight && xWithin >= halfSize - safeThickness)
                            || (topLeft != bottomLeft && yWithin < safeThickness);
                    }
                    else if (isTop)
                    {
                        draw = (topRight != topLeft && xWithin < safeThickness)
                            || (topRight != bottomRight && yWithin < safeThickness);
                    }
                    else if (isLeft)
                    {
                        draw = (bottomLeft != bottomRight && xWithin >= halfSize - safeThickness)
                            || (bottomLeft != topLeft && yWithin >= halfSize - safeThickness);
                    }
                    else
                    {
                        draw = (bottomRight != bottomLeft && xWithin < safeThickness)
                            || (bottomRight != topRight && yWithin >= halfSize - safeThickness);
                    }

                    texture.SetPixel(x, y, draw ? outlineColor : clear);
                }
            }

            texture.Apply(false, false);
            return texture;
        }

        private static Texture2D CreateTransparentTexture(string name, int width, int height)
        {
            return new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
        }

        private static void BlitTexture(Texture2D source, Texture2D destination, int offsetX, int offsetY)
        {
            Color[] pixels = source.GetPixels();
            destination.SetPixels(offsetX, offsetY, source.width, source.height, pixels);
        }

        private static bool[] SevenSegmentMaskFor(char digit)
        {
            switch (digit)
            {
                case '0': return new[] { true, true, true, false, true, true, true };
                case '1': return new[] { false, false, true, false, false, true, false };
                case '2': return new[] { true, false, true, true, true, false, true };
                case '3': return new[] { true, false, true, true, false, true, true };
                case '4': return new[] { false, true, true, true, false, true, false };
                case '5': return new[] { true, true, false, true, false, true, true };
                case '6': return new[] { true, true, false, true, true, true, true };
                case '7': return new[] { true, false, true, false, false, true, false };
                case '8': return new[] { true, true, true, true, true, true, true };
                case '9': return new[] { true, true, true, true, false, true, true };
                default: return new[] { false, false, false, false, false, false, false };
            }
        }

        private static bool IsSevenSegmentPixelLit(bool[] segments, int x, int y, int width, int height)
        {
            bool top = segments[0] && y >= height - 3 && x >= 2 && x <= width - 3;
            bool upperLeft = segments[1] && x <= 2 && y >= height / 2 && y <= height - 4;
            bool upperRight = segments[2] && x >= width - 3 && y >= height / 2 && y <= height - 4;
            bool middle = segments[3] && y >= height / 2 - 1 && y <= height / 2 && x >= 2 && x <= width - 3;
            bool lowerLeft = segments[4] && x <= 2 && y >= 2 && y < height / 2 - 1;
            bool lowerRight = segments[5] && x >= width - 3 && y >= 2 && y < height / 2 - 1;
            bool bottom = segments[6] && y <= 2 && x >= 2 && x <= width - 3;
            return top || upperLeft || upperRight || middle || lowerLeft || lowerRight || bottom;
        }

        private static string CreateBitmapGlyphDescriptor()
        {
            var builder = new StringBuilder();
            builder.AppendLine("info face=\"MinebotHologramDigits\" size=16 bold=0 italic=0 charset=\"\" unicode=1 stretchH=100 smooth=0 aa=1 padding=0,0,0,0 spacing=0,0");
            builder.AppendLine("common lineHeight=16 base=12 scaleW=100 scaleH=16 pages=1 packed=0");
            builder.AppendLine("page id=0 file=\"hologram_bmfont_digits.png\"");
            builder.AppendLine($"chars count={BitmapGlyphDigits.Length}");
            for (int i = 0; i < BitmapGlyphDigits.Length; i++)
            {
                int characterId = BitmapGlyphDigits[i];
                builder.AppendLine($"char id={characterId} x={i * 10} y=0 width=10 height=16 xoffset=0 yoffset=0 xadvance=10 page=0 chnl=0");
            }

            return builder.ToString();
        }

        private static string CreateHologramPromptTemplate()
        {
            return
@"# Minebot 全息反馈批次 001

## Prompt

Use case: stylized-concept
Asset type: holographic bitmap glyph atlas and overlay symbols for a Unity top-down mining game
Primary request: Create a compact hologram feedback kit for BOOOM Minebot covering scan digits, warning overlays, marker symbols, and supporting glyph sheets.
Subject: cyan-forward holographic UI language with subtle warm warning accents, readable over dark cave terrain.
Required assets: digit atlas for 0-9, warning overlay symbol, marker symbol, scan hint symbol, optional contour-friendly warning edge variants.
Style: crisp pixel art, no anti-aliased blur, transparent background, no text labels or watermarks outside the glyphs themselves.
Layout: evenly spaced atlas or sheet with explicit padding and a stable slicing plan for Unity import.

## 筛选说明

- 数字在 1x 下必须能和岩壁中心对应，不能依赖系统字体抗锯齿。
- danger / marker / scan 必须共享同一套全息语言，而不是一红一黄一白各自为政。
- 警告资源允许带暖色强调，但底层仍要保留 cyan hologram 质感。
";
        }

        private static string CreateHologramManifestTemplate()
        {
            return
@"# Minebot 全息反馈资产台账 001

## 采用结果

- overlay atlas: `Assets/Art/Minebot/Sprites/UI/Hologram/hologram_overlay_atlas.png`
- BMFont atlas: `Assets/Art/Minebot/Sprites/UI/Hologram/hologram_bmfont_digits.png`
- BMFont descriptor: `Assets/Art/Minebot/Sprites/UI/Hologram/hologram_bmfont_digits.fnt`
- digit sprites: `Assets/Art/Minebot/Sprites/UI/Hologram/Glyphs/hologram_digit_0.png` - `hologram_digit_9.png`
- bitmap glyph font asset: `Assets/Resources/Minebot/MinebotBitmapGlyphFont_Default.asset`
- runtime art set: `Assets/Resources/Minebot/MinebotPresentationArtSet_Default.asset`

## 绑定关系

- `bitmapGlyphFont` -> `MinebotBitmapGlyphFont_Default.asset`
- `bitmapGlyphAtlas` -> `hologram_bmfont_digits.png`
- `bitmapGlyphDescriptor` -> `hologram_bmfont_digits.fnt`
- `hologramOverlayAtlas` -> `hologram_overlay_atlas.png`
- `dangerTile` / `markerTile` / `scanHintTile` / `dangerOutlineTiles` / `dangerContourTiles` 使用同一批次的全息几何语言
";
        }

        private static string CreateHologramRecordTemplate()
        {
            return
@"# 全息反馈资源记录模板

## 批次

- 批次编号：
- 负责人：
- 日期：

## Prompt

-

## 筛选说明

-

## Glyph 映射

| 字符 | 最终 Sprite | xAdvance | 说明 |
| --- | --- | --- | --- |
| 0 |  |  |  |

## 资产路径

- source sheet:
- selected atlas:
- BMFont descriptor:
- overlay atlas:
- ArtSet 引用:

## 审查结论

- 视觉一致性：
- 可读性：
- 需要回炉项：
";
        }

        private sealed class GeneratedTextureScope : IDisposable
        {
            public GeneratedTextureScope(Texture2D texture)
            {
                Texture = texture;
            }

            public Texture2D Texture { get; }

            public void Dispose()
            {
                if (Texture != null)
                {
                    UnityEngine.Object.DestroyImmediate(Texture);
                }
            }
        }

        private readonly struct AssetEntry
        {
            public AssetEntry(string name, string spritePath, string tilePath)
            {
                Name = name;
                SpritePath = spritePath;
                TilePath = tilePath;
            }

            public string Name { get; }
            public string SpritePath { get; }
            public string TilePath { get; }
        }

        private readonly struct TextureEntry
        {
            public TextureEntry(string spritePath, int pixelsPerUnit)
            {
                SpritePath = spritePath;
                PixelsPerUnit = pixelsPerUnit;
            }

            public string SpritePath { get; }
            public int PixelsPerUnit { get; }
        }

        private static AssetEntry[] CreateTileEntries()
        {
            var entries = new List<AssetEntry>
            {
                new("Tile_FloorCave", "Assets/Art/Minebot/Sprites/Tiles/tile_floor_cave.png", FloorTilePath),
                new("Tile_WallSoil", "Assets/Art/Minebot/Sprites/Tiles/tile_wall_soil.png", SoilWallTilePath),
                new("Tile_WallStone", "Assets/Art/Minebot/Sprites/Tiles/tile_wall_stone.png", StoneWallTilePath),
                new("Tile_WallHardRock", "Assets/Art/Minebot/Sprites/Tiles/tile_wall_hard_rock.png", HardRockWallTilePath),
                new("Tile_WallUltraHard", "Assets/Art/Minebot/Sprites/Tiles/tile_wall_ultra_hard.png", UltraHardWallTilePath),
                new("Tile_Boundary", "Assets/Art/Minebot/Sprites/Tiles/tile_boundary.png", BoundaryTilePath),
                new("Tile_OverlayDanger", "Assets/Art/Minebot/Sprites/Tiles/tile_overlay_danger.png", DangerTilePath),
                new("Tile_OverlayMarker", "Assets/Art/Minebot/Sprites/Tiles/tile_overlay_marker.png", MarkerTilePath),
                new("Tile_HintScan", "Assets/Art/Minebot/Sprites/Tiles/tile_hint_scan.png", ScanHintTilePath),
                new("Tile_FacilityRepairStation", "Assets/Art/Minebot/Sprites/Tiles/tile_facility_repair_station.png", RepairStationTilePath),
                new("Tile_FacilityRobotFactory", "Assets/Art/Minebot/Sprites/Tiles/tile_facility_robot_factory.png", RobotFactoryTilePath),
                new("Tile_BuildPreviewValid", "Assets/Art/Minebot/Sprites/Tiles/tile_build_preview_valid.png", BuildPreviewValidTilePath),
                new("Tile_BuildPreviewInvalid", "Assets/Art/Minebot/Sprites/Tiles/tile_build_preview_invalid.png", BuildPreviewInvalidTilePath),
                new("Tile_DetailSoil", "Assets/Art/Minebot/Sprites/Tiles/tile_detail_soil.png", SoilDetailTilePath),
                new("Tile_DetailStone", "Assets/Art/Minebot/Sprites/Tiles/tile_detail_stone.png", StoneDetailTilePath),
                new("Tile_DetailHardRock", "Assets/Art/Minebot/Sprites/Tiles/tile_detail_hard_rock.png", HardRockDetailTilePath),
                new("Tile_DetailUltraHard", "Assets/Art/Minebot/Sprites/Tiles/tile_detail_ultra_hard.png", UltraHardDetailTilePath),
                new("Tile_DangerOutlineThin", "Assets/Art/Minebot/Sprites/Tiles/tile_danger_outline_thin.png", DangerOutlineThinTilePath),
                new("Tile_DangerOutlineMedium", "Assets/Art/Minebot/Sprites/Tiles/tile_danger_outline_medium.png", DangerOutlineMediumTilePath),
                new("Tile_DangerOutlineThick", "Assets/Art/Minebot/Sprites/Tiles/tile_danger_outline_thick.png", DangerOutlineThickTilePath)
            };

            for (int i = 0; i < DualGridContour.TileCount; i++)
            {
                entries.Add(new(
                    $"Tile_WallContour_{i:00}",
                    $"Assets/Art/Minebot/Sprites/Tiles/tile_wall_contour_{i:00}.png",
                    $"Assets/Art/Minebot/Tiles/Tile_WallContour_{i:00}.asset"));
                entries.Add(new(
                    $"Tile_DangerContour_{i:00}",
                    $"Assets/Art/Minebot/Sprites/Tiles/tile_danger_contour_{i:00}.png",
                    $"Assets/Art/Minebot/Tiles/Tile_DangerContour_{i:00}.asset"));
            }

            return entries.ToArray();
        }

        private static TextureEntry[] CreateHologramEntries()
        {
            var entries = new List<TextureEntry>
            {
                new(HologramOverlayAtlasPath, TilePixelsPerUnit),
                new(BitmapGlyphAtlasPath, GlyphPixelsPerUnit)
            };

            for (int i = 0; i < BitmapGlyphDigits.Length; i++)
            {
                entries.Add(new TextureEntry(GetBitmapGlyphSpritePath(BitmapGlyphDigits[i]), GlyphPixelsPerUnit));
            }

            return entries.ToArray();
        }

        private static string[] CreatePaletteTilePaths()
        {
            return new[]
            {
                GetDualGridTilePath(TerrainRenderLayerId.Floor, 15),
                GetDualGridTilePath(TerrainRenderLayerId.Soil, 15),
                GetDualGridTilePath(TerrainRenderLayerId.Stone, 15),
                GetDualGridTilePath(TerrainRenderLayerId.HardRock, 15),
                GetDualGridTilePath(TerrainRenderLayerId.UltraHard, 15),
                GetDualGridTilePath(TerrainRenderLayerId.Boundary, 15),
                DangerTilePath,
                MarkerTilePath,
                BuildPreviewValidTilePath,
                BuildPreviewInvalidTilePath,
                DangerOutlineMediumTilePath,
                RepairStationTilePath,
                RobotFactoryTilePath
            };
        }
    }
}
