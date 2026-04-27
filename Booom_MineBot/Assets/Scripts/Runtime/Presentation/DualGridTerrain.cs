using System;
using Minebot.Common;
using Minebot.GridMining;
using UnityEngine;

namespace Minebot.Presentation
{
    public enum TerrainMaterialId : byte
    {
        None = 0,
        Floor = 1,
        Soil = 2,
        Stone = 3,
        HardRock = 4,
        UltraHard = 5,
        Boundary = 6
    }

    public enum TerrainRenderLayerId : byte
    {
        Floor = 0,
        Soil = 1,
        Stone = 2,
        HardRock = 3,
        UltraHard = 4,
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
                TerrainMaterialId material = DualGridTerrain.MaterialForLayer(layerId);
                int atlasIndex = DualGridTerrain.ComputeIndex(
                    sample.TopLeft == material,
                    sample.TopRight == material,
                    sample.BottomLeft == material,
                    sample.BottomRight == material);
                output[i] = new RenderLayerCommand(layerId, atlasIndex, atlasIndex != 0);
            }
        }
    }

    public static class DualGridTerrain
    {
        public const int TileCount = 16;
        public const int RenderLayerCount = DualGridTerrainLayout.RenderLayerCount;
        public static readonly Vector3 DisplayOffset = DualGridTerrainLayout.DefaultDisplayOffset;
        public static readonly TerrainRenderLayerId[] OrderedLayers = DualGridTerrainLayout.OrderedLayers;

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
                    return TerrainRenderLayerId.Soil;
                case TerrainMaterialId.Stone:
                    return TerrainRenderLayerId.Stone;
                case TerrainMaterialId.HardRock:
                    return TerrainRenderLayerId.HardRock;
                case TerrainMaterialId.UltraHard:
                    return TerrainRenderLayerId.UltraHard;
                case TerrainMaterialId.Boundary:
                    return TerrainRenderLayerId.Boundary;
                default:
                    return TerrainRenderLayerId.Floor;
            }
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
