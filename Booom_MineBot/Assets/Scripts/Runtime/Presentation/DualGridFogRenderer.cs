using System.Collections.Generic;
using Minebot.Common;
using Minebot.GridMining;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Minebot.Presentation
{
    public sealed class DualGridFogRenderer
    {
        public void RebuildAll(
            Vector2Int gridSize,
            bool[] fogNearMask,
            bool[] fogDeepMask,
            Tilemap fogNearTilemap,
            Tilemap fogDeepTilemap,
            MinebotPresentationAssets assets)
        {
            if (assets == null || fogNearMask == null || fogDeepMask == null)
            {
                return;
            }

            fogNearTilemap?.ClearAllTiles();
            fogDeepTilemap?.ClearAllTiles();
            for (int y = 0; y <= gridSize.y; y++)
            {
                for (int x = 0; x <= gridSize.x; x++)
                {
                    WriteDisplayCell(gridSize, fogNearMask, fogDeepMask, fogNearTilemap, fogDeepTilemap, assets, new Vector3Int(x, y, 0));
                }
            }
        }

        public void RefreshChanged(
            Vector2Int gridSize,
            bool[] fogNearMask,
            bool[] fogDeepMask,
            Tilemap fogNearTilemap,
            Tilemap fogDeepTilemap,
            MinebotPresentationAssets assets,
            ICollection<GridPosition> changedCells)
        {
            if (assets == null || fogNearMask == null || fogDeepMask == null)
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
                WriteDisplayCell(gridSize, fogNearMask, fogDeepMask, fogNearTilemap, fogDeepTilemap, assets, displayPosition);
            }
        }

        private static void WriteDisplayCell(
            Vector2Int gridSize,
            bool[] fogNearMask,
            bool[] fogDeepMask,
            Tilemap fogNearTilemap,
            Tilemap fogDeepTilemap,
            MinebotPresentationAssets assets,
            Vector3Int displayPosition)
        {
            int nearIndex = DualGridFog.ComputeIndex(gridSize, fogNearMask, displayPosition.x, displayPosition.y);
            int deepIndex = DualGridFog.ComputeIndex(gridSize, fogDeepMask, displayPosition.x, displayPosition.y);
            fogNearTilemap?.SetTile(displayPosition, nearIndex != 0 ? assets.FogNearDualGridTileForIndex(nearIndex) : null);
            fogDeepTilemap?.SetTile(displayPosition, deepIndex != 0 ? assets.FogDeepDualGridTileForIndex(deepIndex) : null);
        }
    }
}
