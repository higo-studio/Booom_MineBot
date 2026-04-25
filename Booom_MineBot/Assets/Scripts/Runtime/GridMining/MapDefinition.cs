using System;
using System.Collections.Generic;
using Minebot.Common;
using UnityEngine;

namespace Minebot.GridMining
{
    public enum MapMarkerKind
    {
        Spawn = 0,
        RepairStation = 1,
        RobotFactory = 2
    }

    [Serializable]
    public struct MapCellDefinition
    {
        public TerrainKind terrainKind;
        public HardnessTier hardnessTier;
        public CellStaticFlags staticFlags;
        public ResourceAmount reward;
    }

    [Serializable]
    public struct MapMarkerDefinition
    {
        public MapMarkerKind markerKind;
        public GridPosition position;
        public Vector2Int size;
        public int direction;
    }

    [CreateAssetMenu(menuName = "Minebot/Grid Mining/Map Definition")]
    public sealed class MapDefinition : ScriptableObject
    {
        [SerializeField]
        private string mapId = "default";

        [SerializeField]
        private Vector2Int size = new Vector2Int(12, 12);

        [SerializeField]
        private MapCellDefinition[] cells = Array.Empty<MapCellDefinition>();

        [SerializeField]
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
