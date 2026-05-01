using System;
using Minebot.Common;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Minebot.GridMining
{
    [Serializable]
    public sealed class TerrainTileRule
    {
        [InspectorLabel("瓦片资源")]
        public TileBase tile;

        [InspectorLabel("地形类型")]
        public TerrainKind terrainKind = TerrainKind.MineableWall;

        [InspectorLabel("硬度档位")]
        public HardnessTier hardnessTier = HardnessTier.Soil;

        [InspectorLabel("静态标记")]
        public CellStaticFlags staticFlags;

        [InspectorLabel("资源奖励")]
        public ResourceAmount reward = new ResourceAmount(1, 0, 1);
    }

    [Serializable]
    public sealed class PoiTileRule
    {
        [InspectorLabel("瓦片资源")]
        public TileBase tile;

        [InspectorLabel("标记类型")]
        public MapMarkerKind markerKind;

        [InspectorLabel("尺寸")]
        public Vector2Int size = Vector2Int.one;

        [InspectorLabel("唯一")]
        public bool unique = true;
    }

    [CreateAssetMenu(menuName = "Minebot/网格挖掘/瓦片地图烘焙配置")]
    public sealed class TilemapBakeProfile : ScriptableObject
    {
        [SerializeField]
        [InspectorLabel("地形规则")]
        private TerrainTileRule[] terrainRules = Array.Empty<TerrainTileRule>();

        [SerializeField]
        [InspectorLabel("标记点规则")]
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
