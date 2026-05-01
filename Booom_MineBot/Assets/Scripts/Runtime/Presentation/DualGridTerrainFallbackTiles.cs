#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Minebot.Presentation
{
    public static class DualGridTerrainFallbackTiles
    {
        private const int TileSize = 16;
        private const int HalfSize = TileSize / 2;

        internal static Tile[] CreateTileSet(TerrainRenderLayerId layerId)
        {
            var tiles = new Tile[DualGridTerrain.TileCount];
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i] = CreateTile(layerId, i);
            }

            return tiles;
        }

        public static Texture2D CreateTexture(TerrainRenderLayerId layerId, int atlasIndex, string textureName = null)
        {
            FamilyStyle style = StyleFor(layerId);
            var texture = new Texture2D(TileSize, TileSize, TextureFormat.RGBA32, false)
            {
                name = string.IsNullOrWhiteSpace(textureName)
                    ? $"Tile_DG_{layerId}_{atlasIndex:00}_Texture"
                    : textureName,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            bool topLeft = (atlasIndex & (1 << 3)) != 0;
            bool topRight = (atlasIndex & (1 << 2)) != 0;
            bool bottomLeft = (atlasIndex & (1 << 1)) != 0;
            bool bottomRight = (atlasIndex & 1) != 0;
            Color clear = new Color(0f, 0f, 0f, 0f);

            for (int y = 0; y < TileSize; y++)
            {
                for (int x = 0; x < TileSize; x++)
                {
                    bool isTop = y >= HalfSize;
                    bool isLeft = x < HalfSize;
                    int xWithin = isLeft ? x : x - HalfSize;
                    int yWithin = isTop ? y - HalfSize : y;
                    bool isFilled = isTop
                        ? (isLeft ? topLeft : topRight)
                        : (isLeft ? bottomLeft : bottomRight);

                    if (!isFilled)
                    {
                        texture.SetPixel(x, y, clear);
                        continue;
                    }

                    bool drawOutline = false;
                    if (isTop && isLeft)
                    {
                        drawOutline = (topLeft != topRight && xWithin >= HalfSize - 1)
                            || (topLeft != bottomLeft && yWithin < 1);
                    }
                    else if (isTop)
                    {
                        drawOutline = (topRight != topLeft && xWithin < 1)
                            || (topRight != bottomRight && yWithin < 1);
                    }
                    else if (isLeft)
                    {
                        drawOutline = (bottomLeft != bottomRight && xWithin >= HalfSize - 1)
                            || (bottomLeft != topLeft && yWithin >= HalfSize - 1);
                    }
                    else
                    {
                        drawOutline = (bottomRight != bottomLeft && xWithin < 1)
                            || (bottomRight != topRight && yWithin >= HalfSize - 1);
                    }

                    bool accent = ((x + y + atlasIndex) & 3) == 0;
                    Color fill = accent ? style.Accent : style.Fill;
                    texture.SetPixel(x, y, drawOutline ? style.Outline : fill);
                }
            }

            texture.Apply(false, false);
            return texture;
        }

        private static Tile CreateTile(TerrainRenderLayerId layerId, int atlasIndex)
        {
            var texture = CreateTexture(layerId, atlasIndex);
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = $"Tile_DG_{layerId}_{atlasIndex:00}";
            tile.sprite = Sprite.Create(texture, new Rect(0, 0, TileSize, TileSize), new Vector2(0.5f, 0.5f), TileSize);
            tile.colliderType = Tile.ColliderType.None;
            return tile;
        }

        private static FamilyStyle StyleFor(TerrainRenderLayerId layerId)
        {
            switch (layerId)
            {
                case TerrainRenderLayerId.Soil:
                    return new FamilyStyle(
                        new Color(0.43f, 0.34f, 0.24f, 0.92f),
                        new Color(0.55f, 0.44f, 0.31f, 0.96f),
                        new Color(0.24f, 0.19f, 0.15f, 1f));
                case TerrainRenderLayerId.Stone:
                    return new FamilyStyle(
                        new Color(0.36f, 0.36f, 0.34f, 0.92f),
                        new Color(0.54f, 0.54f, 0.51f, 0.96f),
                        new Color(0.18f, 0.18f, 0.17f, 1f));
                case TerrainRenderLayerId.HardRock:
                    return new FamilyStyle(
                        new Color(0.24f, 0.26f, 0.28f, 0.94f),
                        new Color(0.34f, 0.38f, 0.41f, 0.98f),
                        new Color(0.1f, 0.11f, 0.13f, 1f));
                case TerrainRenderLayerId.UltraHard:
                    return new FamilyStyle(
                        new Color(0.18f, 0.16f, 0.23f, 0.96f),
                        new Color(0.28f, 0.24f, 0.35f, 0.98f),
                        new Color(0.08f, 0.07f, 0.11f, 1f));
                case TerrainRenderLayerId.Boundary:
                    return new FamilyStyle(
                        new Color(0.05f, 0.05f, 0.06f, 0.98f),
                        new Color(0.17f, 0.17f, 0.18f, 1f),
                        new Color(0.32f, 0.32f, 0.34f, 1f));
                default:
                    return new FamilyStyle(
                        new Color(0.13f, 0.18f, 0.19f, 0.92f),
                        new Color(0.18f, 0.24f, 0.25f, 0.96f),
                        new Color(0.07f, 0.09f, 0.1f, 1f));
            }
        }

        private readonly struct FamilyStyle
        {
            public FamilyStyle(Color fill, Color accent, Color outline)
            {
                Fill = fill;
                Accent = accent;
                Outline = outline;
            }

            public Color Fill { get; }
            public Color Accent { get; }
            public Color Outline { get; }
        }
    }
}
#endif
