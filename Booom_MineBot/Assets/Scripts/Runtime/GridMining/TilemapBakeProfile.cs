using System;
using Minebot.Common;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Minebot.GridMining
{
    [Serializable]
    public sealed class TerrainTileRule
    {
        public TileBase tile;
        public TerrainKind terrainKind = TerrainKind.MineableWall;
        public HardnessTier hardnessTier = HardnessTier.Soil;
        public CellStaticFlags staticFlags;
        public ResourceAmount reward = new ResourceAmount(1, 0, 1);
    }

    [Serializable]
    public sealed class PoiTileRule
    {
        public TileBase tile;
        public MapMarkerKind markerKind;
        public Vector2Int size = Vector2Int.one;
        public bool unique = true;
    }

    [CreateAssetMenu(menuName = "Minebot/Grid Mining/Tilemap Bake Profile")]
    public sealed class TilemapBakeProfile : ScriptableObject
    {
        [SerializeField]
        private TerrainTileRule[] terrainRules = Array.Empty<TerrainTileRule>();

        [SerializeField]
        private PoiTileRule[] poiRules = Array.Empty<PoiTileRule>();

        public TerrainTileRule[] TerrainRules => terrainRules;
        public PoiTileRule[] PoiRules => poiRules;

        public bool TryGetTerrainRule(TileBase tile, out TerrainTileRule rule)
        {
            foreach (TerrainTileRule candidate in terrainRules)
            {
                if (candidate.tile == tile)
                {
                    rule = candidate;
                    return true;
                }
            }

            rule = null;
            return false;
        }

        public bool TryGetPoiRule(TileBase tile, out PoiTileRule rule)
        {
            foreach (PoiTileRule candidate in poiRules)
            {
                if (candidate.tile == tile)
                {
                    rule = candidate;
                    return true;
                }
            }

            rule = null;
            return false;
        }
    }
}
