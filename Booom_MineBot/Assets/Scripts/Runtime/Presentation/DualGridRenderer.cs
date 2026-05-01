using System;
using System.Collections.Generic;
using Minebot.Common;
using Minebot.GridMining;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Minebot.Presentation
{
    public interface IDualGridMaterialSource
    {
        BoundsInt CellBounds { get; }
        bool IsInside(int x, int y);
        TerrainMaterialId GetMaterial(int x, int y);
    }

    public interface IDualGridRenderTarget
    {
        void ClearAll();
        void WriteDisplayCell(Vector3Int displayPosition, RenderLayerCommand[] commands);
        void CompressBounds();
    }

    public sealed class LogicalGridMaterialSource : IDualGridMaterialSource
    {
        private readonly LogicalGridState grid;
        private readonly BoundsInt cellBounds;

        public LogicalGridMaterialSource(LogicalGridState logicalGrid)
        {
            grid = logicalGrid;
            cellBounds = new BoundsInt(0, 0, 0, logicalGrid.Size.x, logicalGrid.Size.y, 1);
        }

        public BoundsInt CellBounds => cellBounds;

        public bool IsInside(int x, int y)
        {
            return grid.IsInside(new GridPosition(x, y));
        }

        public TerrainMaterialId GetMaterial(int x, int y)
        {
            var position = new GridPosition(x, y);
            return grid.IsInside(position) ? DualGridTerrain.MaterialForCell(grid.GetCell(position)) : TerrainMaterialId.None;
        }
    }

    public sealed class TilemapAuthoringMaterialSource : IDualGridMaterialSource
    {
        private readonly Tilemap terrainTilemap;
        private readonly TilemapBakeProfile bakeProfile;
        private readonly BoundsInt cellBounds;

        public TilemapAuthoringMaterialSource(Tilemap sourceTerrainTilemap, TilemapBakeProfile profile)
        {
            terrainTilemap = sourceTerrainTilemap;
            bakeProfile = profile;
            cellBounds = sourceTerrainTilemap != null ? sourceTerrainTilemap.cellBounds : default;
        }

        public BoundsInt CellBounds => cellBounds;

        public bool IsInside(int x, int y)
        {
            return x >= cellBounds.xMin && x < cellBounds.xMax
                && y >= cellBounds.yMin && y < cellBounds.yMax;
        }

        public TerrainMaterialId GetMaterial(int x, int y)
        {
            if (!IsInside(x, y))
            {
                return TerrainMaterialId.None;
            }

            if (terrainTilemap == null || bakeProfile == null)
            {
                return TerrainMaterialId.Floor;
            }

            TileBase tile = terrainTilemap.GetTile(new Vector3Int(x, y, 0));
            if (tile != null && bakeProfile.TryGetTerrainRule(tile, out TerrainTileRule rule))
            {
                return DualGridTerrain.MaterialForTerrain(rule.terrainKind, rule.hardnessTier);
            }

            return TerrainMaterialId.Floor;
        }
    }

    public sealed class TilemapDualGridRenderTarget : IDualGridRenderTarget
    {
        private readonly IReadOnlyList<Tilemap> tilemaps;
        private readonly MinebotPresentationAssets assets;
        private readonly Dictionary<Vector3Int, TileBase>[] tileCaches;

        public TilemapDualGridRenderTarget(IReadOnlyList<Tilemap> terrainTilemaps, MinebotPresentationAssets presentationAssets)
        {
            tilemaps = terrainTilemaps;
            assets = presentationAssets;
            if (tilemaps != null)
            {
                tileCaches = new Dictionary<Vector3Int, TileBase>[tilemaps.Count];
                for (int i = 0; i < tileCaches.Length; i++)
                {
                    tileCaches[i] = new Dictionary<Vector3Int, TileBase>();
                }
            }
            else
            {
                tileCaches = Array.Empty<Dictionary<Vector3Int, TileBase>>();
            }
        }

        public void ClearAll()
        {
            if (tilemaps == null)
            {
                return;
            }

            for (int i = 0; i < tilemaps.Count; i++)
            {
                tilemaps[i]?.ClearAllTiles();
            }

            for (int i = 0; i < tileCaches.Length; i++)
            {
                tileCaches[i].Clear();
            }
        }

        public void WriteDisplayCell(Vector3Int displayPosition, RenderLayerCommand[] commands)
        {
            if (tilemaps == null || commands == null)
            {
                return;
            }

            int count = Mathf.Min(tilemaps.Count, commands.Length);
            for (int i = 0; i < count; i++)
            {
                Tilemap tilemap = tilemaps[i];
                if (tilemap == null)
                {
                    continue;
                }

                RenderLayerCommand command = commands[i];
                TileBase newTile = command.HasContent ? assets.DualGridTerrainTileFor(command.LayerId, command.AtlasIndex) : null;
                
                // Check if tile is already set to the same value - skip to preserve tile animations
                if (tileCaches[i].TryGetValue(displayPosition, out TileBase existingTile))
                {
                    if (existingTile == newTile)
                    {
                        continue;
                    }
                }

                tileCaches[i][displayPosition] = newTile;
                tilemap.SetTile(displayPosition, newTile);
            }
        }

        public void CompressBounds()
        {
            if (tilemaps == null)
            {
                return;
            }

            for (int i = 0; i < tilemaps.Count; i++)
            {
                tilemaps[i]?.CompressBounds();
            }
        }
    }

    public sealed class DualGridRenderer
    {
        private readonly IDualGridTerrainResolver resolver;
        private readonly RenderLayerCommand[] commandBuffer;

        public DualGridRenderer(IDualGridTerrainResolver terrainResolver)
        {
            resolver = terrainResolver;
            commandBuffer = new RenderLayerCommand[DualGridTerrain.RenderLayerCount];
        }

        public void RebuildAll(IDualGridMaterialSource source, IDualGridRenderTarget target)
        {
            if (source == null || target == null)
            {
                return;
            }

            target.ClearAll();
            BoundsInt bounds = source.CellBounds;
            for (int y = bounds.yMin; y <= bounds.yMax; y++)
            {
                for (int x = bounds.xMin; x <= bounds.xMax; x++)
                {
                    WriteDisplayCell(source, target, new Vector3Int(x, y, 0));
                }
            }
        }

        public void RefreshChanged(IDualGridMaterialSource source, IDualGridRenderTarget target, ICollection<GridPosition> changedCells)
        {
            if (source == null || target == null)
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
                Vector3Int[] affected = DualGridTerrain.GetAffectedDisplayCells(changed);
                for (int i = 0; i < affected.Length; i++)
                {
                    affectedDisplayCells.Add(affected[i]);
                }
            }

            foreach (Vector3Int position in affectedDisplayCells)
            {
                WriteDisplayCell(source, target, position);
            }
        }

        private void WriteDisplayCell(IDualGridMaterialSource source, IDualGridRenderTarget target, Vector3Int displayPosition)
        {
            CornerMaterialSample sample = DualGridTerrain.Sample(source, displayPosition.x, displayPosition.y);
            resolver.Resolve(sample, commandBuffer);
            target.WriteDisplayCell(displayPosition, commandBuffer);
        }
    }
}
