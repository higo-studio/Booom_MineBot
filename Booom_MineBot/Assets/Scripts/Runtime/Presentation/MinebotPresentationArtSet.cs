using Minebot.GridMining;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Minebot.Presentation
{
    [CreateAssetMenu(menuName = "Minebot/Presentation/Presentation Art Set")]
    public sealed class MinebotPresentationArtSet : ScriptableObject
    {
        [Header("Terrain")]
        [SerializeField]
        private Tile emptyTile;

        [SerializeField]
        private Tile soilWallTile;

        [SerializeField]
        private Tile stoneWallTile;

        [SerializeField]
        private Tile hardRockWallTile;

        [SerializeField]
        private Tile ultraHardWallTile;

        [SerializeField]
        private Tile boundaryTile;

        [Header("Overlay")]
        [SerializeField]
        private Tile dangerTile;

        [SerializeField]
        private Tile markerTile;

        [SerializeField]
        private Tile scanHintTile;

        [Header("Facilities")]
        [SerializeField]
        private Tile repairStationTile;

        [SerializeField]
        private Tile robotFactoryTile;

        [Header("Actors")]
        [SerializeField]
        private Sprite playerSprite;

        [SerializeField]
        private Sprite robotSprite;

        public Tile EmptyTile => emptyTile;
        public Tile SoilWallTile => soilWallTile;
        public Tile StoneWallTile => stoneWallTile;
        public Tile HardRockWallTile => hardRockWallTile;
        public Tile UltraHardWallTile => ultraHardWallTile;
        public Tile BoundaryTile => boundaryTile;
        public Tile DangerTile => dangerTile;
        public Tile MarkerTile => markerTile;
        public Tile ScanHintTile => scanHintTile;
        public Tile RepairStationTile => repairStationTile;
        public Tile RobotFactoryTile => robotFactoryTile;
        public Sprite PlayerSprite => playerSprite;
        public Sprite RobotSprite => robotSprite;

        public Tile TileForHardness(HardnessTier hardness)
        {
            switch (hardness)
            {
                case HardnessTier.Stone:
                    return stoneWallTile != null ? stoneWallTile : soilWallTile;
                case HardnessTier.HardRock:
                    return hardRockWallTile != null ? hardRockWallTile : soilWallTile;
                case HardnessTier.UltraHard:
                    return ultraHardWallTile != null ? ultraHardWallTile : hardRockWallTile != null ? hardRockWallTile : soilWallTile;
                default:
                    return soilWallTile;
            }
        }

#if UNITY_EDITOR
        public void Configure(
            Tile empty,
            Tile soilWall,
            Tile stoneWall,
            Tile hardRockWall,
            Tile ultraHardWall,
            Tile boundary,
            Tile danger,
            Tile marker,
            Tile scanHint,
            Tile repairStation,
            Tile robotFactory,
            Sprite player,
            Sprite robot)
        {
            emptyTile = empty;
            soilWallTile = soilWall;
            stoneWallTile = stoneWall;
            hardRockWallTile = hardRockWall;
            ultraHardWallTile = ultraHardWall;
            boundaryTile = boundary;
            dangerTile = danger;
            markerTile = marker;
            scanHintTile = scanHint;
            repairStationTile = repairStation;
            robotFactoryTile = robotFactory;
            playerSprite = player;
            robotSprite = robot;
        }
#endif
    }
}
