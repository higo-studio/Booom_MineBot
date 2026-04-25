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
                    int distance = position.ManhattanDistance(settings.Spawn);
                    HardnessTier hardness = GetHardness(distance, settings.SafeRadius);
                    ResourceAmount reward = terrain == TerrainKind.MineableWall
                        ? GetReward(position, hardness)
                        : ResourceAmount.Zero;
                    cells.Add(new GridCellState(terrain, hardness, CellStaticFlags.None, reward));
                }
            }

            return new LogicalGridState(settings.Size, settings.Spawn, cells);
        }

        private static HardnessTier GetHardness(int distanceFromSpawn, int safeRadius)
        {
            if (distanceFromSpawn > safeRadius + 7)
            {
                return HardnessTier.HardRock;
            }

            if (distanceFromSpawn > safeRadius + 4)
            {
                return HardnessTier.Stone;
            }

            return HardnessTier.Soil;
        }

        private static ResourceAmount GetReward(GridPosition position, HardnessTier hardness)
        {
            bool energyPocket = ((position.X * 31 + position.Y * 17) % 5) == 0;
            switch (hardness)
            {
                case HardnessTier.HardRock:
                    return new ResourceAmount(3, energyPocket ? 2 : 1, 3);
                case HardnessTier.Stone:
                    return new ResourceAmount(2, energyPocket ? 1 : 0, 2);
                default:
                    return new ResourceAmount(1, energyPocket ? 1 : 0, 1);
            }
        }
    }
}
