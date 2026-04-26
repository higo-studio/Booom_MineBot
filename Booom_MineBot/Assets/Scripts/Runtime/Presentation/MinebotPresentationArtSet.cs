using System;
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

        [SerializeField]
        private Tile[] dangerOutlineTiles = Array.Empty<Tile>();

        [SerializeField]
        private Tile[] wallContourTiles = Array.Empty<Tile>();

        [SerializeField]
        private Tile[] dangerContourTiles = Array.Empty<Tile>();

        [SerializeField]
        private Tile soilDetailTile;

        [SerializeField]
        private Tile stoneDetailTile;

        [SerializeField]
        private Tile hardRockDetailTile;

        [SerializeField]
        private Tile ultraHardDetailTile;

        [SerializeField]
        private Tile buildPreviewValidTile;

        [SerializeField]
        private Tile buildPreviewInvalidTile;

        [SerializeField]
        private Vector2 scanLabelOffset = new Vector2(0f, 0.62f);

        [SerializeField]
        private Color scanLabelColor = new Color(1f, 0.95f, 0.58f, 1f);

        [SerializeField]
        private float scanLabelFontSize = 4f;

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

        [SerializeField]
        private float playerColliderRadius = 0.42f;

        public Tile EmptyTile => emptyTile;
        public Tile SoilWallTile => soilWallTile;
        public Tile StoneWallTile => stoneWallTile;
        public Tile HardRockWallTile => hardRockWallTile;
        public Tile UltraHardWallTile => ultraHardWallTile;
        public Tile BoundaryTile => boundaryTile;
        public Tile DangerTile => dangerTile;
        public Tile MarkerTile => markerTile;
        public Tile ScanHintTile => scanHintTile;
        public Tile[] DangerOutlineTiles => dangerOutlineTiles ?? Array.Empty<Tile>();
        public Tile[] WallContourTiles => wallContourTiles ?? Array.Empty<Tile>();
        public Tile[] DangerContourTiles => dangerContourTiles ?? Array.Empty<Tile>();
        public Tile SoilDetailTile => soilDetailTile;
        public Tile StoneDetailTile => stoneDetailTile;
        public Tile HardRockDetailTile => hardRockDetailTile;
        public Tile UltraHardDetailTile => ultraHardDetailTile;
        public Tile BuildPreviewValidTile => buildPreviewValidTile;
        public Tile BuildPreviewInvalidTile => buildPreviewInvalidTile;
        public Vector2 ScanLabelOffset => scanLabelOffset;
        public Color ScanLabelColor => scanLabelColor.a > 0f ? scanLabelColor : new Color(1f, 0.95f, 0.58f, 1f);
        public float ScanLabelFontSize => Mathf.Max(0.5f, scanLabelFontSize);
        public Tile RepairStationTile => repairStationTile;
        public Tile RobotFactoryTile => robotFactoryTile;
        public Sprite PlayerSprite => playerSprite;
        public Sprite RobotSprite => robotSprite;
        public float PlayerColliderRadius => Mathf.Clamp(playerColliderRadius, 0.1f, 0.49f);

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
            Sprite robot,
            Tile soilDetail = null,
            Tile stoneDetail = null,
            Tile hardRockDetail = null,
            Tile ultraHardDetail = null,
            Tile buildPreviewValid = null,
            Tile buildPreviewInvalid = null,
            Tile[] wallContour = null,
            Tile[] dangerContour = null)
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
            soilDetailTile = soilDetail;
            stoneDetailTile = stoneDetail;
            hardRockDetailTile = hardRockDetail;
            ultraHardDetailTile = ultraHardDetail;
            buildPreviewValidTile = buildPreviewValid;
            buildPreviewInvalidTile = buildPreviewInvalid;
            wallContourTiles = wallContour ?? Array.Empty<Tile>();
            dangerContourTiles = dangerContour ?? Array.Empty<Tile>();
        }
#endif
    }
}
