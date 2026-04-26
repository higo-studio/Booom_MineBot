using UnityEngine;
using UnityEngine.Tilemaps;

namespace Minebot.Presentation
{
    internal sealed class MinebotPresentationAssets
    {
        public Tile EmptyTile { get; private set; }
        public Tile SoilWallTile { get; private set; }
        public Tile StoneWallTile { get; private set; }
        public Tile HardRockWallTile { get; private set; }
        public Tile UltraHardWallTile { get; private set; }
        public Tile BoundaryTile { get; private set; }
        public Tile DangerTile { get; private set; }
        public Tile MarkerTile { get; private set; }
        public Tile RepairStationTile { get; private set; }
        public Tile RobotFactoryTile { get; private set; }
        public Tile ScanHintTile { get; private set; }
        public Tile BuildPreviewValidTile { get; private set; }
        public Tile BuildPreviewInvalidTile { get; private set; }
        public Tile[] WallContourTiles { get; private set; }
        public Tile[] DangerContourTiles { get; private set; }
        public Tile[] DangerOutlineTiles { get; private set; }
        public Sprite PlayerSprite { get; private set; }
        public Sprite RobotSprite { get; private set; }
        public Vector2 ScanLabelOffset { get; private set; }
        public Color ScanLabelColor { get; private set; }
        public float ScanLabelFontSize { get; private set; }
        public float PlayerColliderRadius { get; private set; }
        public bool IsUsingConfiguredArtSet { get; private set; }

        public static MinebotPresentationAssets Create(MinebotPresentationArtSet artSet)
        {
            MinebotPresentationAssets fallback = CreateFallback();
            if (artSet == null)
            {
                return fallback;
            }

            return new MinebotPresentationAssets
            {
                EmptyTile = artSet.EmptyTile != null ? artSet.EmptyTile : fallback.EmptyTile,
                SoilWallTile = artSet.SoilWallTile != null ? artSet.SoilWallTile : fallback.SoilWallTile,
                StoneWallTile = artSet.StoneWallTile != null ? artSet.StoneWallTile : fallback.StoneWallTile,
                HardRockWallTile = artSet.HardRockWallTile != null ? artSet.HardRockWallTile : fallback.HardRockWallTile,
                UltraHardWallTile = artSet.UltraHardWallTile != null ? artSet.UltraHardWallTile : fallback.UltraHardWallTile,
                BoundaryTile = artSet.BoundaryTile != null ? artSet.BoundaryTile : fallback.BoundaryTile,
                DangerTile = artSet.DangerTile != null ? artSet.DangerTile : fallback.DangerTile,
                MarkerTile = artSet.MarkerTile != null ? artSet.MarkerTile : fallback.MarkerTile,
                RepairStationTile = artSet.RepairStationTile != null ? artSet.RepairStationTile : fallback.RepairStationTile,
                RobotFactoryTile = artSet.RobotFactoryTile != null ? artSet.RobotFactoryTile : fallback.RobotFactoryTile,
                ScanHintTile = artSet.ScanHintTile != null ? artSet.ScanHintTile : fallback.ScanHintTile,
                BuildPreviewValidTile = artSet.BuildPreviewValidTile != null ? artSet.BuildPreviewValidTile : fallback.BuildPreviewValidTile,
                BuildPreviewInvalidTile = artSet.BuildPreviewInvalidTile != null ? artSet.BuildPreviewInvalidTile : fallback.BuildPreviewInvalidTile,
                WallContourTiles = NormalizeContourTiles(artSet.WallContourTiles, fallback.WallContourTiles),
                DangerContourTiles = NormalizeContourTiles(artSet.DangerContourTiles, fallback.DangerContourTiles),
                DangerOutlineTiles = NormalizeDangerOutlineTiles(artSet.DangerOutlineTiles, fallback.DangerOutlineTiles),
                PlayerSprite = artSet.PlayerSprite != null ? artSet.PlayerSprite : fallback.PlayerSprite,
                RobotSprite = artSet.RobotSprite != null ? artSet.RobotSprite : fallback.RobotSprite,
                ScanLabelOffset = artSet.ScanLabelOffset,
                ScanLabelColor = artSet.ScanLabelColor,
                ScanLabelFontSize = artSet.ScanLabelFontSize,
                PlayerColliderRadius = artSet.PlayerColliderRadius,
                IsUsingConfiguredArtSet = true
            };
        }

        public Tile DangerOutlineTileForWave(int currentWave)
        {
            if (DangerOutlineTiles == null || DangerOutlineTiles.Length == 0)
            {
                return DangerTile;
            }

            int index = Mathf.Clamp(Mathf.Max(0, currentWave), 0, DangerOutlineTiles.Length - 1);
            return DangerOutlineTiles[index] != null ? DangerOutlineTiles[index] : DangerTile;
        }

        public Tile WallContourTileForIndex(int index)
        {
            return TileForContourIndex(WallContourTiles, index);
        }

        public Tile DangerContourTileForIndex(int index)
        {
            return TileForContourIndex(DangerContourTiles, index);
        }

        public Tile WallTileForHardness(Minebot.GridMining.HardnessTier hardness)
        {
            switch (hardness)
            {
                case Minebot.GridMining.HardnessTier.Stone:
                    return StoneWallTile;
                case Minebot.GridMining.HardnessTier.HardRock:
                    return HardRockWallTile;
                case Minebot.GridMining.HardnessTier.UltraHard:
                    return UltraHardWallTile;
                default:
                    return SoilWallTile;
            }
        }

        private static MinebotPresentationAssets CreateFallback()
        {
            return new MinebotPresentationAssets
            {
                EmptyTile = CreateTile("Empty Tile", new Color(0.13f, 0.18f, 0.19f, 1f), new Color(0.07f, 0.09f, 0.1f, 1f)),
                SoilWallTile = CreateTile("Soil Wall Tile", new Color(0.43f, 0.34f, 0.24f, 1f), new Color(0.24f, 0.19f, 0.15f, 1f)),
                StoneWallTile = CreateTile("Stone Wall Tile", new Color(0.36f, 0.36f, 0.34f, 1f), new Color(0.18f, 0.18f, 0.17f, 1f)),
                HardRockWallTile = CreateTile("Hard Rock Wall Tile", new Color(0.24f, 0.26f, 0.28f, 1f), new Color(0.1f, 0.11f, 0.13f, 1f)),
                UltraHardWallTile = CreateTile("Ultra Hard Wall Tile", new Color(0.18f, 0.16f, 0.23f, 1f), new Color(0.08f, 0.07f, 0.11f, 1f)),
                BoundaryTile = CreateTile("Boundary Tile", new Color(0.05f, 0.05f, 0.06f, 1f), new Color(0.17f, 0.17f, 0.18f, 1f)),
                DangerTile = CreateTile("Danger Tile", new Color(0.86f, 0.12f, 0.1f, 0.62f), new Color(1f, 0.36f, 0.22f, 0.82f)),
                MarkerTile = CreateTile("Marker Tile", new Color(0.95f, 0.05f, 0.05f, 0.84f), new Color(1f, 0.85f, 0.25f, 0.92f)),
                RepairStationTile = CreateTile("Repair Station Tile", new Color(0.1f, 0.38f, 0.85f, 1f), new Color(0.62f, 0.88f, 1f, 1f)),
                RobotFactoryTile = CreateTile("Robot Factory Tile", new Color(0.88f, 0.42f, 0.09f, 1f), new Color(1f, 0.78f, 0.2f, 1f)),
                ScanHintTile = CreateTile("Scan Hint Tile", new Color(0.2f, 0.75f, 1f, 0.74f), new Color(0.9f, 1f, 1f, 0.9f)),
                BuildPreviewValidTile = CreateTile("Build Preview Valid Tile", new Color(0.18f, 0.72f, 1f, 0.42f), new Color(0.82f, 0.96f, 1f, 0.92f)),
                BuildPreviewInvalidTile = CreateTile("Build Preview Invalid Tile", new Color(0.86f, 0.12f, 0.1f, 0.36f), new Color(1f, 0.44f, 0.28f, 0.9f)),
                WallContourTiles = CreateContourTileSet("Wall Contour", new Color(0.92f, 0.9f, 0.82f, 0.95f), 2),
                DangerContourTiles = CreateContourTileSet("Danger Contour", new Color(1f, 0.43f, 0.26f, 0.95f), 2),
                DangerOutlineTiles = new[]
                {
                    CreateOutlineTile("Danger Outline Thin Tile", new Color(1f, 0.38f, 0.22f, 0.95f), 1),
                    CreateOutlineTile("Danger Outline Medium Tile", new Color(1f, 0.42f, 0.24f, 0.95f), 2),
                    CreateOutlineTile("Danger Outline Thick Tile", new Color(1f, 0.48f, 0.26f, 0.95f), 3)
                },
                PlayerSprite = CreateSprite("Player Sprite", new Color(1f, 0.86f, 0.22f, 1f), new Color(0.1f, 0.75f, 0.95f, 1f)),
                RobotSprite = CreateSprite("Robot Sprite", new Color(0.34f, 0.94f, 0.38f, 1f), new Color(0.05f, 0.28f, 0.12f, 1f)),
                ScanLabelOffset = new Vector2(0f, 0.62f),
                ScanLabelColor = new Color(1f, 0.95f, 0.58f, 1f),
                ScanLabelFontSize = 4f,
                PlayerColliderRadius = 0.42f
            };
        }

        private static Tile[] NormalizeContourTiles(Tile[] configuredTiles, Tile[] fallbackTiles)
        {
            var normalized = new Tile[DualGridContour.TileCount];
            for (int i = 0; i < normalized.Length; i++)
            {
                Tile configured = configuredTiles != null && i < configuredTiles.Length ? configuredTiles[i] : null;
                Tile fallback = fallbackTiles != null && i < fallbackTiles.Length ? fallbackTiles[i] : null;
                normalized[i] = configured != null ? configured : fallback;
            }

            return normalized;
        }

        private static Tile[] NormalizeDangerOutlineTiles(Tile[] configuredTiles, Tile[] fallbackTiles)
        {
            if (configuredTiles == null || configuredTiles.Length == 0)
            {
                return fallbackTiles;
            }

            var normalized = new Tile[configuredTiles.Length];
            for (int i = 0; i < configuredTiles.Length; i++)
            {
                Tile fallback = fallbackTiles[Mathf.Min(i, fallbackTiles.Length - 1)];
                normalized[i] = configuredTiles[i] != null ? configuredTiles[i] : fallback;
            }

            return normalized;
        }

        private static Tile TileForContourIndex(Tile[] contourTiles, int index)
        {
            if (contourTiles == null || contourTiles.Length == 0)
            {
                return null;
            }

            int safeIndex = Mathf.Clamp(index, 0, contourTiles.Length - 1);
            return contourTiles[safeIndex];
        }

        private static Tile CreateTile(string name, Color fill, Color border)
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = name;
            tile.sprite = CreateSprite(name + " Sprite", fill, border);
            tile.colliderType = Tile.ColliderType.None;
            return tile;
        }

        private static Tile[] CreateContourTileSet(string namePrefix, Color outlineColor, int thickness)
        {
            var tiles = new Tile[DualGridContour.TileCount];
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i] = CreateContourTile($"{namePrefix} {i:X1}", i, outlineColor, thickness);
            }

            return tiles;
        }

        private static Tile CreateContourTile(string name, int contourIndex, Color outlineColor, int thickness)
        {
            const int size = 16;
            const int halfSize = size / 2;
            int safeThickness = Mathf.Clamp(thickness, 1, halfSize);
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name + " Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

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

            texture.Apply(false, true);
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = name;
            tile.sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            tile.colliderType = Tile.ColliderType.None;
            return tile;
        }

        private static Tile CreateOutlineTile(string name, Color outlineColor, int thickness)
        {
            const int size = 16;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name + " Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Color clear = new Color(0f, 0f, 0f, 0f);
            int safeThickness = Mathf.Clamp(thickness, 1, size / 2);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isOutline = x < safeThickness
                        || y < safeThickness
                        || x >= size - safeThickness
                        || y >= size - safeThickness;
                    texture.SetPixel(x, y, isOutline ? outlineColor : clear);
                }
            }

            texture.Apply(false, true);
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = name;
            tile.sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            tile.colliderType = Tile.ColliderType.None;
            return tile;
        }

        private static Sprite CreateSprite(string name, Color fill, Color border)
        {
            const int size = 16;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name + " Texture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isBorder = x == 0 || y == 0 || x == size - 1 || y == size - 1;
                    texture.SetPixel(x, y, isBorder ? border : fill);
                }
            }

            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = name;
            return sprite;
        }
    }
}
