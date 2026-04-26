using System;
using System.Collections.Generic;
using Minebot.Common;
using Minebot.GridMining;
using UnityEngine;

namespace Minebot.Progression
{
    public enum BuildingPlacementFailure
    {
        None,
        MissingDefinition,
        OutOfBounds,
        TerrainBlocked,
        Occupied,
        InsufficientResources
    }

    public sealed class BuildingInstance
    {
        public BuildingInstance(BuildingDefinition definition, GridPosition origin)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Origin = origin;
        }

        public BuildingDefinition Definition { get; }
        public GridPosition Origin { get; }
        public string Id => Definition.Id;
    }

    public sealed class BuildingPlacementService
    {
        private readonly LogicalGridState grid;
        private readonly PlayerEconomy economy;
        private readonly List<BuildingInstance> buildings = new List<BuildingInstance>();

        public BuildingPlacementService(LogicalGridState grid, PlayerEconomy economy)
        {
            this.grid = grid ?? throw new ArgumentNullException(nameof(grid));
            this.economy = economy ?? throw new ArgumentNullException(nameof(economy));
        }

        public IReadOnlyList<BuildingInstance> Buildings => buildings;

        public bool CanPlace(BuildingDefinition definition, GridPosition origin, out BuildingPlacementFailure failure)
        {
            failure = ValidateFootprint(definition, origin);
            if (failure != BuildingPlacementFailure.None)
            {
                return false;
            }

            if (!economy.Resources.CanAfford(definition.Cost))
            {
                failure = BuildingPlacementFailure.InsufficientResources;
                return false;
            }

            return true;
        }

        public bool TryPlace(BuildingDefinition definition, GridPosition origin, out BuildingInstance instance, out BuildingPlacementFailure failure)
        {
            instance = null;
            if (!CanPlace(definition, origin, out failure))
            {
                return false;
            }

            if (!economy.TrySpend(definition.Cost))
            {
                failure = BuildingPlacementFailure.InsufficientResources;
                return false;
            }

            instance = RegisterInitialBuilding(definition, origin, out failure);
            return instance != null;
        }

        public BuildingInstance RegisterInitialBuilding(BuildingDefinition definition, GridPosition origin, out BuildingPlacementFailure failure)
        {
            failure = ValidateFootprint(definition, origin);
            if (failure != BuildingPlacementFailure.None)
            {
                return null;
            }

            var instance = new BuildingInstance(definition, origin);
            foreach (GridPosition cellPosition in FootprintCells(definition, origin))
            {
                ref GridCellState cell = ref grid.GetCellRef(cellPosition);
                cell.IsOccupiedByBuilding = true;
                cell.OccupyingBuildingId = definition.Id;
            }

            buildings.Add(instance);
            return instance;
        }

        public IEnumerable<GridPosition> FootprintCells(BuildingDefinition definition, GridPosition origin)
        {
            if (definition == null)
            {
                yield break;
            }

            Vector2Int size = definition.FootprintSize;
            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    yield return new GridPosition(origin.X + x, origin.Y + y);
                }
            }
        }

        private BuildingPlacementFailure ValidateFootprint(BuildingDefinition definition, GridPosition origin)
        {
            if (definition == null)
            {
                return BuildingPlacementFailure.MissingDefinition;
            }

            foreach (GridPosition cellPosition in FootprintCells(definition, origin))
            {
                if (!grid.IsInside(cellPosition))
                {
                    return BuildingPlacementFailure.OutOfBounds;
                }

                GridCellState cell = grid.GetCell(cellPosition);
                if (cell.TerrainKind != definition.AllowedTerrain || cell.IsDangerZone)
                {
                    return BuildingPlacementFailure.TerrainBlocked;
                }

                if (cell.IsOccupiedByBuilding)
                {
                    return BuildingPlacementFailure.Occupied;
                }
            }

            return BuildingPlacementFailure.None;
        }
    }
}
