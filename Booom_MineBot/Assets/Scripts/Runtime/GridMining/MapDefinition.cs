using System;
using System.Collections.Generic;
using Minebot.Common;
using UnityEngine;

namespace Minebot.GridMining
{
    public enum MapMarkerKind
    {
        [InspectorName("出生点")]
        Spawn = 0,
        [InspectorName("维修站")]
        RepairStation = 1,
        [InspectorName("机器人工厂")]
        RobotFactory = 2
    }

    [Serializable]
    public struct MapCellDefinition
    {
        [InspectorLabel("地形类型")]
        public TerrainKind terrainKind;

        [InspectorLabel("硬度档位")]
        public HardnessTier hardnessTier;

        [InspectorLabel("静态标记")]
        public CellStaticFlags staticFlags;

        [InspectorLabel("资源奖励")]
        public ResourceAmount reward;
    }

    [Serializable]
    public struct MapMarkerDefinition
    {
        [InspectorLabel("标记类型")]
        public MapMarkerKind markerKind;

        [InspectorLabel("位置")]
        public GridPosition position;

        [InspectorLabel("尺寸")]
        public Vector2Int size;

        [InspectorLabel("朝向")]
        public int direction;
    }

    [CreateAssetMenu(menuName = "Minebot/网格挖掘/地图定义")]
    public sealed class MapDefinition : ScriptableObject
    {
        [SerializeField]
        [InspectorLabel("地图标识")]
        private string mapId = "default";

        [SerializeField]
        [InspectorLabel("地图尺寸")]
        private Vector2Int size = new Vector2Int(12, 12);

        [SerializeField]
        [InspectorLabel("格子定义")]
        private MapCellDefinition[] cells = Array.Empty<MapCellDefinition>();

        [SerializeField]
        [InspectorLabel("标记定义")]
        private MapMarkerDefinition[] markers = Array.Empty<MapMarkerDefinition>();

        public string MapId => mapId;
        public Vector2Int Size => size;
        public IReadOnlyList<MapCellDefinition> Cells => cells;
        public IReadOnlyList<MapMarkerDefinition> Markers => markers;

        public LogicalGridState CreateGridState()
        {
            var initialCells = new GridCellState[cells.Length];
            for (int i = 0; i < cells.Length; i++)
            {
                MapCellDefinition cell = cells[i];
                initialCells[i] = new GridCellState(cell.terrainKind, cell.hardnessTier, cell.staticFlags, cell.reward);
            }

            return new LogicalGridState(size, FindSpawn(), initialCells);
        }

        public GridPosition FindSpawn()
        {
            foreach (MapMarkerDefinition marker in markers)
            {
                if (marker.markerKind == MapMarkerKind.Spawn)
                {
                    return marker.position;
                }
            }

            return new GridPosition(size.x / 2, size.y / 2);
        }

        public void SetData(string id, Vector2Int mapSize, MapCellDefinition[] mapCells, MapMarkerDefinition[] mapMarkers)
        {
            mapId = id;
            size = mapSize;
            cells = mapCells ?? Array.Empty<MapCellDefinition>();
            markers = mapMarkers ?? Array.Empty<MapMarkerDefinition>();
        }
    }
}
