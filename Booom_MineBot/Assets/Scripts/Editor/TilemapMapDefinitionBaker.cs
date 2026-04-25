using System.Collections.Generic;
using Minebot.Common;
using Minebot.GridMining;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Minebot.Editor
{
    public static class TilemapMapDefinitionBaker
    {
        public static MapDefinition Bake(
            string assetPath,
            string mapId,
            Tilemap terrain,
            Tilemap poi,
            MapBakeOverlay overlay,
            TilemapBakeProfile profile)
        {
            if (terrain == null)
            {
                throw new System.ArgumentNullException(nameof(terrain));
            }

            if (profile == null)
            {
                throw new System.ArgumentNullException(nameof(profile));
            }

            BoundsInt bounds = terrain.cellBounds;
            var cells = new MapCellDefinition[bounds.size.x * bounds.size.y];
            var markers = new List<MapMarkerDefinition>();

            for (int y = 0; y < bounds.size.y; y++)
            {
                for (int x = 0; x < bounds.size.x; x++)
                {
                    Vector3Int tilePosition = new Vector3Int(bounds.xMin + x, bounds.yMin + y, 0);
                    TileBase terrainTile = terrain.GetTile(tilePosition);
                    var cell = new MapCellDefinition
                    {
                        terrainKind = TerrainKind.Empty,
                        hardnessTier = HardnessTier.Soil,
                        staticFlags = CellStaticFlags.None,
                        reward = ResourceAmount.Zero
                    };

                    if (terrainTile != null && profile.TryGetTerrainRule(terrainTile, out TerrainTileRule terrainRule))
                    {
                        cell.terrainKind = terrainRule.terrainKind;
                        cell.hardnessTier = terrainRule.hardnessTier;
                        cell.staticFlags = terrainRule.staticFlags;
                        cell.reward = terrainRule.reward;
                    }

                    cells[y * bounds.size.x + x] = cell;

                    TileBase poiTile = poi != null ? poi.GetTile(tilePosition) : null;
                    if (poiTile != null && profile.TryGetPoiRule(poiTile, out PoiTileRule poiRule))
                    {
                        markers.Add(new MapMarkerDefinition
                        {
                            markerKind = poiRule.markerKind,
                            position = new GridPosition(x, y),
                            size = poiRule.size,
                            direction = 0
                        });
                    }
                }
            }

            if (overlay != null)
            {
                foreach (MapBakeOverlayCell overlayCell in overlay.Cells)
                {
                    int index = overlayCell.position.Y * bounds.size.x + overlayCell.position.X;
                    if (index < 0 || index >= cells.Length)
                    {
                        continue;
                    }

                    cells[index].staticFlags |= overlayCell.flags;
                    if (overlayCell.overrideReward)
                    {
                        cells[index].reward = overlayCell.rewardOverride;
                    }
                }
            }

            var definition = AssetDatabase.LoadAssetAtPath<MapDefinition>(assetPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<MapDefinition>();
                AssetDatabase.CreateAsset(definition, assetPath);
            }

            definition.SetData(mapId, new Vector2Int(bounds.size.x, bounds.size.y), cells, markers.ToArray());
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();
            return definition;
        }
    }
}
