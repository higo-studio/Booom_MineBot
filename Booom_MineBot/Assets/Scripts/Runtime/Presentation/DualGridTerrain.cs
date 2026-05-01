using System;
using Minebot.Common;
using Minebot.GridMining;
using UnityEngine;

namespace Minebot.Presentation
{
    public enum TerrainMaterialId : byte
    {
        [InspectorName("无")]
        None = 0,
        [InspectorName("地板")]
        Floor = 1,
        [InspectorName("土层")]
        Soil = 2,
        [InspectorName("石层")]
        Stone = 3,
        [InspectorName("硬岩")]
        HardRock = 4,
        [InspectorName("超硬岩")]
        UltraHard = 5,
        [InspectorName("边界")]
        Boundary = 6
    }

    public enum TerrainRenderLayerId : byte
    {
        [InspectorName("地板")]
        Floor = 0,
        [InspectorName("土层")]
        Soil = 1,
        [InspectorName("石层")]
        Stone = 2,
        [InspectorName("硬岩")]
        HardRock = 3,
        [InspectorName("超硬岩")]
        UltraHard = 4,
        [InspectorName("边界")]
        Boundary = 5
    }

    public readonly struct CornerMaterialSample : IEquatable<CornerMaterialSample>
    {
        public CornerMaterialSample(
            TerrainMaterialId topLeft,
            TerrainMaterialId topRight,
            TerrainMaterialId bottomLeft,
            TerrainMaterialId bottomRight)
        {
            TopLeft = topLeft;
            TopRight = topRight;
            BottomLeft = bottomLeft;
            BottomRight = bottomRight;
        }

        public TerrainMaterialId TopLeft { get; }
        public TerrainMaterialId TopRight { get; }
        public TerrainMaterialId BottomLeft { get; }
        public TerrainMaterialId BottomRight { get; }

        public bool Equals(CornerMaterialSample other)
        {
            return TopLeft == other.TopLeft
                && TopRight == other.TopRight
                && BottomLeft == other.BottomLeft
                && BottomRight == other.BottomRight;
        }

        public override bool Equals(object obj)
        {
            return obj is CornerMaterialSample other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)TopLeft;
                hash = (hash * 397) ^ (int)TopRight;
                hash = (hash * 397) ^ (int)BottomLeft;
                hash = (hash * 397) ^ (int)BottomRight;
                return hash;
            }
        }

        public override string ToString()
        {
            return $"{TopLeft}/{TopRight}/{BottomLeft}/{BottomRight}";
        }
    }

    public readonly struct RenderLayerCommand : IEquatable<RenderLayerCommand>
    {
        public RenderLayerCommand(TerrainRenderLayerId layerId, int atlasIndex, bool hasContent)
        {
            LayerId = layerId;
            AtlasIndex = atlasIndex;
            HasContent = hasContent;
        }

        public TerrainRenderLayerId LayerId { get; }
        public int AtlasIndex { get; }
        public bool HasContent { get; }

        public bool Equals(RenderLayerCommand other)
        {
            return LayerId == other.LayerId
                && AtlasIndex == other.AtlasIndex
                && HasContent == other.HasContent;
        }

        public override bool Equals(object obj)
        {
            return obj is RenderLayerCommand other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)LayerId;
                hash = (hash * 397) ^ AtlasIndex;
                hash = (hash * 397) ^ (HasContent ? 1 : 0);
                return hash;
            }
        }

        public override string ToString()
        {
            return HasContent ? $"{LayerId}:{AtlasIndex}" : $"{LayerId}:<empty>";
        }
    }

    public interface IDualGridTerrainResolver
    {
        void Resolve(CornerMaterialSample sample, RenderLayerCommand[] output);
    }

    public sealed class LayeredBinaryTerrainResolver : IDualGridTerrainResolver
    {
        public void Resolve(CornerMaterialSample sample, RenderLayerCommand[] output)
        {
            if (output == null || output.Length < DualGridTerrain.RenderLayerCount)
            {
                throw new ArgumentException($"Output buffer must have length >= {DualGridTerrain.RenderLayerCount}.", nameof(output));
            }

            TerrainRenderLayerId[] orderedLayers = DualGridTerrain.OrderedLayers;
            for (int i = 0; i < orderedLayers.Length; i++)
            {
                TerrainRenderLayerId layerId = orderedLayers[i];
                if (layerId == TerrainRenderLayerId.Soil)
                {
                    output[i] = ResolveWallCommand(sample);
                    continue;
                }

                TerrainMaterialId material = DualGridTerrain.MaterialForLayer(layerId);
                int atlasIndex = DualGridTerrain.ComputeIndex(
                    sample.TopLeft == material,
                    sample.TopRight == material,
                    sample.BottomLeft == material,
                    sample.BottomRight == material);
                output[i] = new RenderLayerCommand(layerId, atlasIndex, atlasIndex != 0);
            }
        }

        private static RenderLayerCommand ResolveWallCommand(CornerMaterialSample sample)
        {
            int atlasIndex = DualGridTerrain.ComputeIndex(
                DualGridTerrain.IsWallMaterial(sample.TopLeft),
                DualGridTerrain.IsWallMaterial(sample.TopRight),
                DualGridTerrain.IsWallMaterial(sample.BottomLeft),
                DualGridTerrain.IsWallMaterial(sample.BottomRight));

            if (atlasIndex == 0)
            {
                return new RenderLayerCommand(TerrainRenderLayerId.Soil, 0, false);
            }

            TerrainRenderLayerId wallFamily = DualGridTerrain.ResolveWallFamilyLayer(sample);
            return new RenderLayerCommand(wallFamily, atlasIndex, true);
        }
    }

    public static class DualGridTerrain
    {
        public const int TileCount = 16;
        public const int RenderLayerCount = DualGridTerrainLayout.RenderLayerCount;
        public static readonly Vector3 DisplayOffset = DualGridTerrainLayout.DefaultDisplayOffset;
        public static readonly TerrainRenderLayerId[] OrderedLayers = DualGridTerrainLayout.OrderedLayers;
        public static readonly TerrainRenderLayerId[] MaterialFamilies =
        {
            TerrainRenderLayerId.Floor,
            TerrainRenderLayerId.Soil,
            TerrainRenderLayerId.Stone,
            TerrainRenderLayerId.HardRock,
            TerrainRenderLayerId.UltraHard,
            TerrainRenderLayerId.Boundary
        };

        public static TerrainMaterialId MaterialForCell(GridCellState cell)
        {
            return MaterialForTerrain(cell.TerrainKind, cell.HardnessTier);
        }

        public static TerrainMaterialId MaterialForTerrain(TerrainKind terrainKind, HardnessTier hardnessTier)
        {
            switch (terrainKind)
            {
                case TerrainKind.Empty:
                    return TerrainMaterialId.Floor;
                case TerrainKind.MineableWall:
                    switch (hardnessTier)
                    {
                        case HardnessTier.Stone:
                            return TerrainMaterialId.Stone;
                        case HardnessTier.HardRock:
                            return TerrainMaterialId.HardRock;
                        case HardnessTier.UltraHard:
                            return TerrainMaterialId.UltraHard;
                        default:
                            return TerrainMaterialId.Soil;
                    }
                case TerrainKind.Indestructible:
                    return TerrainMaterialId.Boundary;
                default:
                    return TerrainMaterialId.None;
            }
        }

        public static TerrainMaterialId MaterialForLayer(TerrainRenderLayerId layerId)
        {
            switch (layerId)
            {
                case TerrainRenderLayerId.Soil:
                    return TerrainMaterialId.Soil;
                case TerrainRenderLayerId.Stone:
                    return TerrainMaterialId.Stone;
                case TerrainRenderLayerId.HardRock:
                    return TerrainMaterialId.HardRock;
                case TerrainRenderLayerId.UltraHard:
                    return TerrainMaterialId.UltraHard;
                case TerrainRenderLayerId.Boundary:
                    return TerrainMaterialId.Boundary;
                default:
                    return TerrainMaterialId.Floor;
            }
        }

        public static TerrainRenderLayerId LayerForMaterial(TerrainMaterialId materialId)
        {
            switch (materialId)
            {
                case TerrainMaterialId.Soil:
                case TerrainMaterialId.Stone:
                case TerrainMaterialId.HardRock:
                case TerrainMaterialId.UltraHard:
                    return TerrainRenderLayerId.Soil;
                case TerrainMaterialId.Boundary:
                    return TerrainRenderLayerId.Boundary;
                default:
                    return TerrainRenderLayerId.Floor;
            }
        }

        public static bool IsWallMaterial(TerrainMaterialId materialId)
        {
            switch (materialId)
            {
                case TerrainMaterialId.Soil:
                case TerrainMaterialId.Stone:
                case TerrainMaterialId.HardRock:
                case TerrainMaterialId.UltraHard:
                    return true;
                default:
                    return false;
            }
        }

        public static TerrainRenderLayerId ResolveWallFamilyLayer(CornerMaterialSample sample)
        {
            int soilCount = CountWallMaterial(sample, TerrainMaterialId.Soil);
            int stoneCount = CountWallMaterial(sample, TerrainMaterialId.Stone);
            int hardRockCount = CountWallMaterial(sample, TerrainMaterialId.HardRock);
            int ultraHardCount = CountWallMaterial(sample, TerrainMaterialId.UltraHard);
            int bestCount = Mathf.Max(soilCount, stoneCount, hardRockCount, ultraHardCount);
            if (bestCount <= 0)
            {
                return TerrainRenderLayerId.Soil;
            }

            if (MatchesPreferredWallMaterial(sample.BottomRight, bestCount, ultraHardCount, hardRockCount, stoneCount, soilCount, out TerrainRenderLayerId preferred))
            {
                return preferred;
            }

            if (MatchesPreferredWallMaterial(sample.TopRight, bestCount, ultraHardCount, hardRockCount, stoneCount, soilCount, out preferred))
            {
                return preferred;
            }

            if (MatchesPreferredWallMaterial(sample.BottomLeft, bestCount, ultraHardCount, hardRockCount, stoneCount, soilCount, out preferred))
            {
                return preferred;
            }

            if (MatchesPreferredWallMaterial(sample.TopLeft, bestCount, ultraHardCount, hardRockCount, stoneCount, soilCount, out preferred))
            {
                return preferred;
            }

            return TerrainRenderLayerId.Soil;
        }

        public static CornerMaterialSample Sample(LogicalGridState grid, int displayX, int displayY)
        {
            return new CornerMaterialSample(
                SampleCell(grid, displayX - 1, displayY),
                SampleCell(grid, displayX, displayY),
                SampleCell(grid, displayX - 1, displayY - 1),
                SampleCell(grid, displayX, displayY - 1));
        }

        public static CornerMaterialSample Sample(IDualGridMaterialSource source, int displayX, int displayY)
        {
            return new CornerMaterialSample(
                SampleCell(source, displayX - 1, displayY),
                SampleCell(source, displayX, displayY),
                SampleCell(source, displayX - 1, displayY - 1),
                SampleCell(source, displayX, displayY - 1));
        }

        public static int ComputeIndex(bool topLeft, bool topRight, bool bottomLeft, bool bottomRight)
        {
            return DualGridContour.ComputeIndex(topLeft, topRight, bottomLeft, bottomRight);
        }

        public static Vector3Int[] GetAffectedDisplayCells(GridPosition cell)
        {
            return DualGridContour.GetAffectedContourCells(cell);
        }

        public static string GetTilemapName(TerrainRenderLayerId layerId)
        {
            return DualGridTerrainLayout.GetTilemapName(layerId);
        }

        public static int GetSortingOrder(TerrainRenderLayerId layerId)
        {
            return DualGridTerrainLayout.GetSortingOrder(layerId, DualGridTerrainLayoutSettings.CreateDefault());
        }

        public static int GetOrderedLayerIndex(TerrainRenderLayerId layerId)
        {
            return DualGridTerrainLayout.GetOrderedLayerIndex(layerId);
        }

        private static int CountWallMaterial(CornerMaterialSample sample, TerrainMaterialId target)
        {
            int count = 0;
            if (sample.TopLeft == target)
            {
                count++;
            }

            if (sample.TopRight == target)
            {
                count++;
            }

            if (sample.BottomLeft == target)
            {
                count++;
            }

            if (sample.BottomRight == target)
            {
                count++;
            }

            return count;
        }

        private static bool MatchesPreferredWallMaterial(
            TerrainMaterialId candidate,
            int bestCount,
            int ultraHardCount,
            int hardRockCount,
            int stoneCount,
            int soilCount,
            out TerrainRenderLayerId preferred)
        {
            preferred = TerrainRenderLayerId.Soil;
            switch (candidate)
            {
                case TerrainMaterialId.UltraHard:
                    preferred = TerrainRenderLayerId.UltraHard;
                    return ultraHardCount == bestCount;
                case TerrainMaterialId.HardRock:
                    preferred = TerrainRenderLayerId.HardRock;
                    return hardRockCount == bestCount;
                case TerrainMaterialId.Stone:
                    preferred = TerrainRenderLayerId.Stone;
                    return stoneCount == bestCount;
                case TerrainMaterialId.Soil:
                    preferred = TerrainRenderLayerId.Soil;
                    return soilCount == bestCount;
                default:
                    return false;
            }
        }

        private static TerrainMaterialId SampleCell(LogicalGridState grid, int x, int y)
        {
            var position = new GridPosition(x, y);
            return grid.IsInside(position) ? MaterialForCell(grid.GetCell(position)) : TerrainMaterialId.None;
        }

        private static TerrainMaterialId SampleCell(IDualGridMaterialSource source, int x, int y)
        {
            return source != null && source.IsInside(x, y) ? source.GetMaterial(x, y) : TerrainMaterialId.None;
        }
    }
}
