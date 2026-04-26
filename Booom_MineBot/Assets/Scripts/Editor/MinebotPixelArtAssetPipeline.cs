using System;
using System.Collections.Generic;
using System.IO;
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
        private const string ArtSetPath = "Assets/Resources/Minebot/MinebotPresentationArtSet_Default.asset";
        private const string PalettePrefabPath = "Assets/Art/Minebot/Palettes/MinebotTilePalette.prefab";
        private const string DualGridSpriteDirectory = "Assets/Art/Minebot/Sprites/Tiles/DualGridTerrain";
        private const string DualGridTileDirectory = "Assets/Art/Minebot/Tiles/DualGridTerrain";
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

        private static readonly AssetEntry[] TileEntries = CreateTileEntries();
        private static readonly string[] PaletteTilePaths = CreatePaletteTilePaths();
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
            EnsureTextureImporters();
            EnsureTileAssets();
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

            foreach (TerrainRenderLayerId layerId in DualGridFamilies)
            {
                for (int i = 0; i < DualGridTerrain.TileCount; i++)
                {
                    ValidateTexture(GetDualGridSpritePath(layerId, i), TilePixelsPerUnit, errors);
                }
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

            foreach (TerrainRenderLayerId layerId in DualGridFamilies)
            {
                for (int i = 0; i < DualGridTerrain.TileCount; i++)
                {
                    ConfigureTextureImporter(GetDualGridSpritePath(layerId, i), TilePixelsPerUnit);
                }
            }
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
                LoadDualGridTiles(TerrainRenderLayerId.Floor),
                LoadDualGridTiles(TerrainRenderLayerId.Soil),
                LoadDualGridTiles(TerrainRenderLayerId.Stone),
                LoadDualGridTiles(TerrainRenderLayerId.HardRock),
                LoadDualGridTiles(TerrainRenderLayerId.UltraHard),
                LoadDualGridTiles(TerrainRenderLayerId.Boundary));
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
                new("Tile_DetailUltraHard", "Assets/Art/Minebot/Sprites/Tiles/tile_detail_ultra_hard.png", UltraHardDetailTilePath)
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
                RepairStationTilePath,
                RobotFactoryTilePath
            };
        }
    }
}
