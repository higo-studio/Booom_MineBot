using System.Collections.Generic;
using Minebot.Common;
using UnityEngine;

namespace Minebot.GridMining
{
    public readonly struct MapGenerationSettings
    {
        public MapGenerationSettings(Vector2Int size, GridPosition spawn, int safeRadius)
        {
            Size = size;
            Spawn = spawn;
            SafeRadius = Mathf.Max(0, safeRadius);
        }

        public Vector2Int Size { get; }
        public GridPosition Spawn { get; }
        public int SafeRadius { get; }
    }

    public static class MapGenerator
    {
        public static LogicalGridState Generate(MapGenerationSettings settings)
        {
            var cells = new List<GridCellState>(settings.Size.x * settings.Size.y);
            for (int y = 0; y < settings.Size.y; y++)
            {
                for (int x = 0; x < settings.Size.x; x++)
                {
                    var position = new GridPosition(x, y);
                    bool border = x == 0 || y == 0 || x == settings.Size.x - 1 || y == settings.Size.y - 1;
                    bool safe = position.ManhattanDistance(settings.Spawn) <= settings.SafeRadius;
                    TerrainKind terrain = border ? TerrainKind.Indestructible : safe ? TerrainKind.Empty : TerrainKind.MineableWall;
                    HardnessTier hardness = position.ManhattanDistance(settings.Spawn) > settings.SafeRadius + 4 ? HardnessTier.Stone : HardnessTier.Soil;
                    var reward = terrain == TerrainKind.MineableWall ? new ResourceAmount(1, 0, 1) : ResourceAmount.Zero;
                    cells.Add(new GridCellState(terrain, hardness, CellStaticFlags.None, reward));
                }
            }

            return new LogicalGridState(settings.Size, settings.Spawn, cells);
        }
    }
}
