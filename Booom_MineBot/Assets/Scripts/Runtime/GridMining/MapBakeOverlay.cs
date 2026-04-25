using System;
using Minebot.Common;
using UnityEngine;

namespace Minebot.GridMining
{
    [Serializable]
    public struct MapBakeOverlayCell
    {
        public GridPosition position;
        public CellStaticFlags flags;
        public ResourceAmount rewardOverride;
        public bool overrideReward;
    }

    public sealed class MapBakeOverlay : MonoBehaviour
    {
        [SerializeField]
        private MapBakeOverlayCell[] cells = Array.Empty<MapBakeOverlayCell>();

        public MapBakeOverlayCell[] Cells => cells;
    }
}
