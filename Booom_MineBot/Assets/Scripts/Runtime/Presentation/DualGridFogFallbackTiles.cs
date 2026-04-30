using UnityEngine;
using UnityEngine.Tilemaps;

namespace Minebot.Presentation
{
    public static class DualGridFogFallbackTiles
    {
        private const int TileSize = 16;
        private const int HalfSize = TileSize / 2;

        internal static Tile[] CreateTileSet(DualGridFogBandKind bandKind)
        {
            var tiles = new Tile[DualGridFog.TileCount];
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i] = CreateTile(bandKind, i);
            }

            return tiles;
        }

        public static Texture2D CreateTexture(DualGridFogBandKind bandKind, int atlasIndex, string textureName = null)
        {
            var texture = new Texture2D(TileSize, TileSize, TextureFormat.RGBA32, false)
            {
                name = string.IsNullOrWhiteSpace(textureName)
                    ? $"Tile_DG_Fog{bandKind}_{atlasIndex:00}_Texture"
                    : textureName,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            bool topLeft = (atlasIndex & (1 << 3)) != 0;
            bool topRight = (atlasIndex & (1 << 2)) != 0;
            bool bottomLeft = (atlasIndex & (1 << 1)) != 0;
            bool bottomRight = (atlasIndex & 1) != 0;
            Color clear = new Color(0f, 0f, 0f, 0f);
            FogStyle style = StyleFor(bandKind);

            for (int y = 0; y < TileSize; y++)
            {
                for (int x = 0; x < TileSize; x++)
                {
                    bool isTop = y >= HalfSize;
                    bool isLeft = x < HalfSize;
                    bool isFilled = isTop
                        ? (isLeft ? topLeft : topRight)
                        : (isLeft ? bottomLeft : bottomRight);

                    if (!isFilled)
                    {
                        texture.SetPixel(x, y, clear);
                        continue;
                    }

                    bool drawOutline = false;
                    int xWithin = isLeft ? x : x - HalfSize;
                    int yWithin = isTop ? y - HalfSize : y;
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
                    Color color = drawOutline
                        ? style.Outline
                        : accent ? style.Accent : style.Fill;
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply(false, false);
            return texture;
        }

        private static Tile CreateTile(DualGridFogBandKind bandKind, int atlasIndex)
        {
            var texture = CreateTexture(bandKind, atlasIndex);
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = $"Tile_DG_Fog{bandKind}_{atlasIndex:00}";
            tile.sprite = Sprite.Create(texture, new Rect(0, 0, TileSize, TileSize), new Vector2(0.5f, 0.5f), TileSize);
            tile.colliderType = Tile.ColliderType.None;
            return tile;
        }

        private static FogStyle StyleFor(DualGridFogBandKind bandKind)
        {
            switch (bandKind)
            {
                case DualGridFogBandKind.Deep:
                    return new FogStyle(
                        new Color(0.01f, 0.02f, 0.03f, 0.98f),
                        new Color(0.03f, 0.04f, 0.05f, 1f),
                        new Color(0.12f, 0.15f, 0.17f, 1f));
                default:
                    return new FogStyle(
                        new Color(0.1f, 0.15f, 0.17f, 0.52f),
                        new Color(0.16f, 0.22f, 0.24f, 0.58f),
                        new Color(0.28f, 0.36f, 0.39f, 0.72f));
            }
        }

        private readonly struct FogStyle
        {
            public FogStyle(Color fill, Color accent, Color outline)
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
