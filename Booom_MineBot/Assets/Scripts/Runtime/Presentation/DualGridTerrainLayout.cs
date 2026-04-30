using System;
using UnityEngine;

namespace Minebot.Presentation
{
    [Serializable]
    public struct DualGridTerrainLayoutSettings
    {
        [SerializeField]
        private Vector3 displayOffset;

        [SerializeField]
        private int sortingOrderBase;

        [SerializeField]
        private int sortingOrderStep;

        public Vector3 DisplayOffset => displayOffset == default ? DualGridTerrainLayout.DefaultDisplayOffset : displayOffset;
        public int SortingOrderBase => sortingOrderBase;
        public int SortingOrderStep => sortingOrderStep == 0 ? 1 : sortingOrderStep;

        public static DualGridTerrainLayoutSettings CreateDefault()
        {
            return new DualGridTerrainLayoutSettings
            {
                displayOffset = DualGridTerrainLayout.DefaultDisplayOffset,
                sortingOrderBase = 0,
                sortingOrderStep = 1
            };
        }
    }

    public static class DualGridTerrainLayout
    {
        public static readonly Vector3 DefaultDisplayOffset = new Vector3(-0.5f, -0.5f, 0f);
        public static readonly TerrainRenderLayerId[] OrderedLayers =
        {
            TerrainRenderLayerId.Floor,
            TerrainRenderLayerId.Soil,
            TerrainRenderLayerId.Boundary
        };

        public const int RenderLayerCount = 3;

        public static string GetTilemapName(TerrainRenderLayerId layerId)
        {
            switch (layerId)
            {
                case TerrainRenderLayerId.Soil:
                case TerrainRenderLayerId.Stone:
                case TerrainRenderLayerId.HardRock:
                case TerrainRenderLayerId.UltraHard:
                    return "DG Wall Tilemap";
                case TerrainRenderLayerId.Boundary:
                    return "DG Boundary Tilemap";
                default:
                    return "DG Floor Tilemap";
            }
        }

        public static int GetSortingOrder(TerrainRenderLayerId layerId, DualGridTerrainLayoutSettings settings)
        {
            int orderedIndex = GetOrderedLayerIndex(layerId);
            return settings.SortingOrderBase + (Math.Max(0, orderedIndex) * settings.SortingOrderStep);
        }

        public static int GetOrderedLayerIndex(TerrainRenderLayerId layerId)
        {
            switch (layerId)
            {
                case TerrainRenderLayerId.Stone:
                case TerrainRenderLayerId.HardRock:
                case TerrainRenderLayerId.UltraHard:
                    layerId = TerrainRenderLayerId.Soil;
                    break;
            }

            for (int i = 0; i < OrderedLayers.Length; i++)
            {
                if (OrderedLayers[i] == layerId)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
