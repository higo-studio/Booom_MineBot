using System;
using Minebot.Common;
using UnityEngine;

namespace Minebot.GridMining
{
    public enum TerrainKind
    {
        Empty = 0,
        MineableWall = 1,
        Indestructible = 2
    }

    public enum HardnessTier
    {
        Soil = 0,
        Stone = 1,
        HardRock = 2,
        UltraHard = 3
    }

    [Flags]
    public enum CellStaticFlags
    {
        None = 0,
        Bomb = 1 << 0,
        PreserveSafePath = 1 << 1,
        CollapseBlocked = 1 << 2,
        SpecialScanRule = 1 << 3
    }

    [Serializable]
    public struct GridCellState
    {
        [SerializeField]
        private TerrainKind terrainKind;

        [SerializeField]
        private HardnessTier hardnessTier;

        [SerializeField]
        private CellStaticFlags staticFlags;

        [SerializeField]
        private ResourceAmount reward;

        [SerializeField]
        private bool isMarked;

        [SerializeField]
        private bool isDangerZone;

        [SerializeField]
        private bool isRevealed;

        [SerializeField]
        private bool isOccupiedByBuilding;

        [SerializeField]
        private string occupyingBuildingId;

        public GridCellState(TerrainKind terrainKind, HardnessTier hardnessTier, CellStaticFlags staticFlags, ResourceAmount reward)
        {
            this.terrainKind = terrainKind;
            this.hardnessTier = hardnessTier;
            this.staticFlags = staticFlags;
            this.reward = reward;
            isMarked = false;
            isDangerZone = false;
            isRevealed = terrainKind == TerrainKind.Empty;
            isOccupiedByBuilding = false;
            occupyingBuildingId = string.Empty;
        }

        public TerrainKind TerrainKind
        {
            get => terrainKind;
            set => terrainKind = value;
        }

        public HardnessTier HardnessTier
        {
            get => hardnessTier;
            set => hardnessTier = value;
        }

        public CellStaticFlags StaticFlags
        {
            get => staticFlags;
            set => staticFlags = value;
        }

        public ResourceAmount Reward
        {
            get => reward;
            set => reward = value;
        }

        public bool IsMarked
        {
            get => isMarked;
            set => isMarked = value;
        }

        public bool IsDangerZone
        {
            get => isDangerZone;
            set => isDangerZone = value;
        }

        public bool IsRevealed
        {
            get => isRevealed;
            set => isRevealed = value;
        }

        public bool IsOccupiedByBuilding
        {
            get => isOccupiedByBuilding;
            set
            {
                isOccupiedByBuilding = value;
                if (!value)
                {
                    occupyingBuildingId = string.Empty;
                }
            }
        }

        public string OccupyingBuildingId
        {
            get => occupyingBuildingId;
            set => occupyingBuildingId = value ?? string.Empty;
        }

        public bool HasBomb => (StaticFlags & CellStaticFlags.Bomb) != 0;
        public bool IsPassable => TerrainKind == TerrainKind.Empty && !IsDangerZone && !IsOccupiedByBuilding;
        public bool IsMineable => TerrainKind == TerrainKind.MineableWall;

        public void ClearBomb()
        {
            staticFlags &= ~CellStaticFlags.Bomb;
        }
    }
}
