using System;
using System.Collections.Generic;
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

        private static readonly AssetEntry[] TileEntries =
        {
            new("Tile_FloorCave", "Assets/Art/Minebot/Sprites/Tiles/tile_floor_cave.png", "Assets/Art/Minebot/Tiles/Tile_FloorCave.asset"),
            new("Tile_WallSoil", "Assets/Art/Minebot/Sprites/Tiles/tile_wall_soil.png", "Assets/Art/Minebot/Tiles/Tile_WallSoil.asset"),
            new("Tile_WallStone", "Assets/Art/Minebot/Sprites/Tiles/tile_wall_stone.png", "Assets/Art/Minebot/Tiles/Tile_WallStone.asset"),
            new("Tile_WallHardRock", "Assets/Art/Minebot/Sprites/Tiles/tile_wall_hard_rock.png", "Assets/Art/Minebot/Tiles/Tile_WallHardRock.asset"),
            new("Tile_WallUltraHard", "Assets/Art/Minebot/Sprites/Tiles/tile_wall_ultra_hard.png", "Assets/Art/Minebot/Tiles/Tile_WallUltraHard.asset"),
            new("Tile_Boundary", "Assets/Art/Minebot/Sprites/Tiles/tile_boundary.png", "Assets/Art/Minebot/Tiles/Tile_Boundary.asset"),
            new("Tile_OverlayDanger", "Assets/Art/Minebot/Sprites/Tiles/tile_overlay_danger.png", "Assets/Art/Minebot/Tiles/Tile_OverlayDanger.asset"),
            new("Tile_OverlayMarker", "Assets/Art/Minebot/Sprites/Tiles/tile_overlay_marker.png", "Assets/Art/Minebot/Tiles/Tile_OverlayMarker.asset"),
            new("Tile_HintScan", "Assets/Art/Minebot/Sprites/Tiles/tile_hint_scan.png", "Assets/Art/Minebot/Tiles/Tile_HintScan.asset"),
            new("Tile_FacilityRepairStation", "Assets/Art/Minebot/Sprites/Tiles/tile_facility_repair_station.png", "Assets/Art/Minebot/Tiles/Tile_FacilityRepairStation.asset"),
            new("Tile_FacilityRobotFactory", "Assets/Art/Minebot/Sprites/Tiles/tile_facility_robot_factory.png", "Assets/Art/Minebot/Tiles/Tile_FacilityRobotFactory.asset")
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

            EnsureTextureImporters();
            Tile[] tiles = EnsureTileAssets();
            EnsureArtSet(tiles);
            EnsurePalettePrefab(tiles);
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

        private static Tile[] EnsureTileAssets()
        {
            var tiles = new Tile[TileEntries.Length];
            for (int i = 0; i < TileEntries.Length; i++)
            {
                AssetEntry entry = TileEntries[i];
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
                tiles[i] = tile;
            }

            return tiles;
        }

        private static void EnsureArtSet(Tile[] tiles)
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
                tiles[0],
                tiles[1],
                tiles[2],
                tiles[3],
                tiles[4],
                tiles[5],
                tiles[6],
                tiles[7],
                tiles[8],
                tiles[9],
                tiles[10],
                player,
                robot);
            EditorUtility.SetDirty(artSet);
        }

        private static void EnsurePalettePrefab(Tile[] tiles)
        {
            var root = new GameObject("Minebot Tile Palette");
            root.AddComponent<Grid>();
            var tilemapObject = new GameObject("Core Tiles", typeof(Tilemap), typeof(TilemapRenderer));
            tilemapObject.transform.SetParent(root.transform, false);
            var tilemap = tilemapObject.GetComponent<Tilemap>();

            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i] != null)
                {
                    tilemap.SetTile(new Vector3Int(i, 0, 0), tiles[i]);
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, PalettePrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
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
    }
}
