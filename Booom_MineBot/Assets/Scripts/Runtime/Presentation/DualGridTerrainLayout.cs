using System;
using UnityEngine;

namespace Minebot.Presentation
{
    [Serializable]
    public struct DualGridTerrainLayoutSettings
    {
        [SerializeField]
        [InspectorLabel("显示偏移")]
        private Vector3 displayOffset;

        [SerializeField]
        [InspectorLabel("基础排序层级")]
        private int sortingOrderBase;

        [SerializeField]
        [InspectorLabel("排序层级步进")]
        private int sortingOrderStep;

        [SerializeField]
        [InspectorLabel("手动配置排序层级")]
        private bool useManualSortingOrders;

        [SerializeField]
        [InspectorLabel("DG Floor 排序层级")]
        private int floorSortingOrder;

        [SerializeField]
        [InspectorLabel("DG Wall 排序层级")]
        private int wallSortingOrder;

        [SerializeField]
        [InspectorLabel("DG Boundary 排序层级")]
        private int boundarySortingOrder;

        public Vector3 DisplayOffset => displayOffset == default ? DualGridTerrainLayout.DefaultDisplayOffset : displayOffset;
        public int SortingOrderBase => sortingOrderBase;
        public int SortingOrderStep => sortingOrderStep == 0 ? 1 : sortingOrderStep;
        public bool UseManualSortingOrders => useManualSortingOrders;
        public int GetManualSortingOrder(TerrainRenderLayerId layerId)
        {
            switch (layerId)
            {
                case TerrainRenderLayerId.Floor:
                    return floorSortingOrder;
                case TerrainRenderLayerId.Soil:
                case TerrainRenderLayerId.Stone:
                case TerrainRenderLayerId.HardRock:
                case TerrainRenderLayerId.UltraHard:
                    return wallSortingOrder;
                case TerrainRenderLayerId.Boundary:
                    return boundarySortingOrder;
                default:
                    return SortingOrderBase;
            }
        }

        public static DualGridTerrainLayoutSettings CreateDefault()
        {
            return new DualGridTerrainLayoutSettings
            {
                displayOffset = DualGridTerrainLayout.DefaultDisplayOffset,
                sortingOrderBase = 0,
                sortingOrderStep = 1,
                useManualSortingOrders = false,
                floorSortingOrder = 0,
                wallSortingOrder = 10,
                boundarySortingOrder = 20
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
            if (settings.UseManualSortingOrders)
            {
                return settings.GetManualSortingOrder(layerId);
            }

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
