using System.Collections.Generic;
using Minebot.Common;
using Minebot.GridMining;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Minebot.Presentation
{
    public sealed class DualGridFogRenderer
    {
        public void RebuildAll(LogicalGridState grid, Tilemap fogNearTilemap, Tilemap fogDeepTilemap, MinebotPresentationAssets assets)
        {
            if (grid == null || assets == null)
            {
                return;
            }

            fogNearTilemap?.ClearAllTiles();
            fogDeepTilemap?.ClearAllTiles();
            for (int y = 0; y <= grid.Size.y; y++)
            {
                for (int x = 0; x <= grid.Size.x; x++)
                {
                    WriteDisplayCell(grid, fogNearTilemap, fogDeepTilemap, assets, new Vector3Int(x, y, 0));
                }
            }
        }

        public void RefreshChanged(
            LogicalGridState grid,
            Tilemap fogNearTilemap,
            Tilemap fogDeepTilemap,
            MinebotPresentationAssets assets,
            ICollection<GridPosition> changedCells)
        {
            if (grid == null || assets == null)
            {
                return;
            }

            if (changedCells == null || changedCells.Count == 0)
            {
                return;
            }

            var affectedDisplayCells = new HashSet<Vector3Int>();
            foreach (GridPosition changed in changedCells)
            {
                Vector3Int[] affected = DualGridFog.GetAffectedDisplayCells(changed);
                for (int i = 0; i < affected.Length; i++)
                {
                    affectedDisplayCells.Add(affected[i]);
                }
            }

            foreach (Vector3Int displayPosition in affectedDisplayCells)
            {
                WriteDisplayCell(grid, fogNearTilemap, fogDeepTilemap, assets, displayPosition);
            }
        }

        private static void WriteDisplayCell(
            LogicalGridState grid,
            Tilemap fogNearTilemap,
            Tilemap fogDeepTilemap,
            MinebotPresentationAssets assets,
            Vector3Int displayPosition)
        {
            int nearIndex = DualGridFog.ComputeIndex(grid, displayPosition.x, displayPosition.y, DualGridFogBandKind.Near);
            int deepIndex = DualGridFog.ComputeIndex(grid, displayPosition.x, displayPosition.y, DualGridFogBandKind.Deep);
            fogNearTilemap?.SetTile(displayPosition, nearIndex != 0 ? assets.FogNearDualGridTileForIndex(nearIndex) : null);
            fogDeepTilemap?.SetTile(displayPosition, deepIndex != 0 ? assets.FogDeepDualGridTileForIndex(deepIndex) : null);
        }
    }
}
