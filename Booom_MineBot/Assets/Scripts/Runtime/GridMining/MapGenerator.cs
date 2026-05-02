using System.Collections.Generic;
using Minebot.Common;
using UnityEngine;

namespace Minebot.GridMining
{
    public readonly struct MapGenerationSettings
    {
        public MapGenerationSettings(Vector2Int size, GridPosition spawn, int safeRadius)
            : this(
                size,
                spawn,
                safeRadius,
                GeneratedMapConfig.DefaultRadialWeight,
                GeneratedMapConfig.DefaultNoiseWeight,
                GeneratedMapConfig.DefaultNoiseScale,
                GeneratedMapConfig.DefaultNoiseOffset,
                GeneratedMapConfig.DefaultRadialExponent,
                GeneratedMapConfig.DefaultForcedUltraHardDistanceNormalized,
                GeneratedMapConfig.DefaultStoneThreshold,
                GeneratedMapConfig.DefaultHardRockThreshold,
                GeneratedMapConfig.DefaultUltraHardThreshold)
        {
        }

        public MapGenerationSettings(
            Vector2Int size,
            GridPosition spawn,
            int safeRadius,
            float radialWeight,
            float noiseWeight,
            Vector2 noiseScale,
            Vector2 noiseOffset,
            float radialExponent,
            float forcedUltraHardDistanceNormalized,
            float stoneThreshold,
            float hardRockThreshold,
            float ultraHardThreshold)
        {
            Size = new Vector2Int(Mathf.Max(5, size.x), Mathf.Max(5, size.y));
            Spawn = new GridPosition(
                Mathf.Clamp(spawn.X, 1, Size.x - 2),
                Mathf.Clamp(spawn.Y, 1, Size.y - 2));
            SafeRadius = Mathf.Max(0, safeRadius);
            RadialWeight = Mathf.Max(0f, radialWeight);
            NoiseWeight = Mathf.Max(0f, noiseWeight);
            NoiseScale = new Vector2(Mathf.Max(0.0001f, Mathf.Abs(noiseScale.x)), Mathf.Max(0.0001f, Mathf.Abs(noiseScale.y)));
            NoiseOffset = noiseOffset;
            RadialExponent = Mathf.Max(0.1f, radialExponent);
            ForcedUltraHardDistanceNormalized = Mathf.Clamp01(forcedUltraHardDistanceNormalized);

            float clampedStoneThreshold = Mathf.Clamp01(stoneThreshold);
            float clampedHardRockThreshold = Mathf.Clamp(hardRockThreshold, clampedStoneThreshold, 1f);
            float clampedUltraHardThreshold = Mathf.Clamp(ultraHardThreshold, clampedHardRockThreshold, 1f);
            StoneThreshold = clampedStoneThreshold;
            HardRockThreshold = clampedHardRockThreshold;
            UltraHardThreshold = clampedUltraHardThreshold;
        }

        public Vector2Int Size { get; }
        public GridPosition Spawn { get; }
        public int SafeRadius { get; }
        public float RadialWeight { get; }
        public float NoiseWeight { get; }
        public Vector2 NoiseScale { get; }
        public Vector2 NoiseOffset { get; }
        public float RadialExponent { get; }
        public float ForcedUltraHardDistanceNormalized { get; }
        public float StoneThreshold { get; }
        public float HardRockThreshold { get; }
        public float UltraHardThreshold { get; }

        public static MapGenerationSettings CreateDefault()
        {
            return GeneratedMapConfig.CreateDefaultSettings();
        }
    }

    public static class MapGenerator
    {
        private const int StarterSoilBandThickness = 2;

        public static LogicalGridState Generate(MapGenerationSettings settings)
        {
            return Generate(settings, null);
        }

        public static LogicalGridState Generate(MapGenerationSettings settings, RewardConfig? rewardConfig = null)
        {
            var random = rewardConfig.HasValue ? new DeterministicRandom(settings.Spawn.X * 31 + settings.Spawn.Y) : null;
            var cells = new List<GridCellState>(settings.Size.x * settings.Size.y);
            float maxRadialDistance = MaxDistanceToInnerCorner(settings);
            for (int y = 0; y < settings.Size.y; y++)
            {
                for (int x = 0; x < settings.Size.x; x++)
                {
                    var position = new GridPosition(x, y);
                    bool border = x == 0 || y == 0 || x == settings.Size.x - 1 || y == settings.Size.y - 1;
                    int chebyshevDistance = ChebyshevDistance(position, settings.Spawn);
                    bool safe = chebyshevDistance <= settings.SafeRadius;
                    TerrainKind terrain = border ? TerrainKind.Indestructible : safe ? TerrainKind.Empty : TerrainKind.MineableWall;
                    HardnessTier hardness = terrain == TerrainKind.MineableWall
                        ? GetHardness(position, chebyshevDistance, settings, maxRadialDistance)
                        : HardnessTier.Soil;
                    ResourceAmount reward = terrain == TerrainKind.MineableWall
                        ? GetReward(position, hardness, rewardConfig, random)
                        : ResourceAmount.Zero;
                    cells.Add(new GridCellState(terrain, hardness, CellStaticFlags.None, reward));
                }
            }

            return new LogicalGridState(settings.Size, settings.Spawn, cells);
        }

        private static int ChebyshevDistance(GridPosition a, GridPosition b)
        {
            return Mathf.Max(Mathf.Abs(a.X - b.X), Mathf.Abs(a.Y - b.Y));
        }

        private static HardnessTier GetHardness(
            GridPosition position,
            int chebyshevDistanceFromSpawn,
            MapGenerationSettings settings,
            float maxRadialDistance)
        {
            if (chebyshevDistanceFromSpawn <= settings.SafeRadius + StarterSoilBandThickness)
            {
                return HardnessTier.Soil;
            }

            float radial01 = SampleRadialDistance01(position, settings, maxRadialDistance);
            if (radial01 >= settings.ForcedUltraHardDistanceNormalized)
            {
                return HardnessTier.UltraHard;
            }

            float noise01 = Mathf.PerlinNoise(
                position.X * settings.NoiseScale.x + settings.NoiseOffset.x,
                position.Y * settings.NoiseScale.y + settings.NoiseOffset.y);
            float blend01 = BlendHardness(radial01, noise01, settings.RadialWeight, settings.NoiseWeight);

            if (blend01 >= settings.UltraHardThreshold)
            {
                return HardnessTier.UltraHard;
            }

            if (blend01 >= settings.HardRockThreshold)
            {
                return HardnessTier.HardRock;
            }

            if (blend01 >= settings.StoneThreshold)
            {
                return HardnessTier.Stone;
            }

            return HardnessTier.Soil;
        }

        private static float SampleRadialDistance01(GridPosition position, MapGenerationSettings settings, float maxRadialDistance)
        {
            if (maxRadialDistance <= 0.001f)
            {
                return 1f;
            }

            float dx = position.X - settings.Spawn.X;
            float dy = position.Y - settings.Spawn.Y;
            float distance = Mathf.Sqrt(dx * dx + dy * dy);
            return Mathf.Pow(Mathf.Clamp01(distance / maxRadialDistance), settings.RadialExponent);
        }

        private static float MaxDistanceToInnerCorner(MapGenerationSettings settings)
        {
            return Mathf.Max(
                DistanceTo(settings.Spawn, 1, 1),
                DistanceTo(settings.Spawn, settings.Size.x - 2, 1),
                DistanceTo(settings.Spawn, 1, settings.Size.y - 2),
                DistanceTo(settings.Spawn, settings.Size.x - 2, settings.Size.y - 2));
        }

        private static float DistanceTo(GridPosition origin, int x, int y)
        {
            float dx = origin.X - x;
            float dy = origin.Y - y;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        private static float BlendHardness(float radial01, float noise01, float radialWeight, float noiseWeight)
        {
            float totalWeight = radialWeight + noiseWeight;
            if (totalWeight <= 0.0001f)
            {
                return radial01;
            }

            return Mathf.Clamp01(((radial01 * radialWeight) + (noise01 * noiseWeight)) / totalWeight);
        }

        private static ResourceAmount GetReward(GridPosition position, HardnessTier hardness, RewardConfig? rewardConfig, DeterministicRandom random)
        {
            // 如果没有配置，使用默认奖励（保持向后兼容）
            if (!rewardConfig.HasValue)
            {
                bool energyPocket = ((position.X * 31 + position.Y * 17) % 5) == 0;
                return hardness switch
                {
                    HardnessTier.UltraHard => new ResourceAmount(4, energyPocket ? 2 : 1, 4),
                    HardnessTier.HardRock => new ResourceAmount(3, energyPocket ? 2 : 1, 3),
                    HardnessTier.Stone => new ResourceAmount(2, energyPocket ? 1 : 0, 2),
                    _ => new ResourceAmount(1, energyPocket ? 1 : 0, 1)
                };
            }

            // 使用配置的范围
            var config = rewardConfig.Value;
            int metal = RandomRange(random, position, config.GetMetalRange(hardness));
            int energy = RandomRange(random, position + new GridPosition(100, 0), config.GetEnergyRange(hardness));
            int experience = RandomRange(random, position + new GridPosition(0, 100), config.GetExperienceRange(hardness));
            return new ResourceAmount(metal, energy, experience);
        }

        private static int RandomRange(DeterministicRandom random, GridPosition position, Vector2Int range)
        {
            if (random == null)
            {
                // 无随机器时返回范围中间值
                return (range.x + range.y) / 2;
            }

            // 使用确定性随机生成器，确保同一位置每次生成相同结果
            int seed = position.X * 31 + position.Y * 17;
            int count = range.y - range.x + 1;
            if (count <= 1)
            {
                return range.x;
            }

            int value = (seed * 12345) % count;
            return range.x + value;
        }
    }
}
