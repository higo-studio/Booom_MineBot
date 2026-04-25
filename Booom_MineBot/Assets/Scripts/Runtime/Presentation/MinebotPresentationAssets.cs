using UnityEngine;
using UnityEngine.Tilemaps;

namespace Minebot.Presentation
{
    internal sealed class MinebotPresentationAssets
    {
        public Tile EmptyTile { get; private set; }
        public Tile WallTile { get; private set; }
        public Tile BoundaryTile { get; private set; }
        public Tile DangerTile { get; private set; }
        public Tile MarkerTile { get; private set; }
        public Tile RepairStationTile { get; private set; }
        public Tile RobotFactoryTile { get; private set; }
        public Tile ScanHintTile { get; private set; }
        public Sprite PlayerSprite { get; private set; }
        public Sprite RobotSprite { get; private set; }

        public static MinebotPresentationAssets Create()
        {
            return new MinebotPresentationAssets
            {
                EmptyTile = CreateTile("Empty Tile", new Color(0.13f, 0.18f, 0.19f, 1f), new Color(0.07f, 0.09f, 0.1f, 1f)),
                WallTile = CreateTile("Wall Tile", new Color(0.43f, 0.34f, 0.24f, 1f), new Color(0.24f, 0.19f, 0.15f, 1f)),
                BoundaryTile = CreateTile("Boundary Tile", new Color(0.05f, 0.05f, 0.06f, 1f), new Color(0.17f, 0.17f, 0.18f, 1f)),
                DangerTile = CreateTile("Danger Tile", new Color(0.86f, 0.12f, 0.1f, 0.62f), new Color(1f, 0.36f, 0.22f, 0.82f)),
                MarkerTile = CreateTile("Marker Tile", new Color(0.95f, 0.05f, 0.05f, 0.84f), new Color(1f, 0.85f, 0.25f, 0.92f)),
                RepairStationTile = CreateTile("Repair Station Tile", new Color(0.1f, 0.38f, 0.85f, 1f), new Color(0.62f, 0.88f, 1f, 1f)),
                RobotFactoryTile = CreateTile("Robot Factory Tile", new Color(0.88f, 0.42f, 0.09f, 1f), new Color(1f, 0.78f, 0.2f, 1f)),
                ScanHintTile = CreateTile("Scan Hint Tile", new Color(0.2f, 0.75f, 1f, 0.74f), new Color(0.9f, 1f, 1f, 0.9f)),
                PlayerSprite = CreateSprite("Player Sprite", new Color(1f, 0.86f, 0.22f, 1f), new Color(0.1f, 0.75f, 0.95f, 1f)),
                RobotSprite = CreateSprite("Robot Sprite", new Color(0.34f, 0.94f, 0.38f, 1f), new Color(0.05f, 0.28f, 0.12f, 1f))
            };
        }

        private static Tile CreateTile(string name, Color fill, Color border)
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = name;
            tile.sprite = CreateSprite(name + " Sprite", fill, border);
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
