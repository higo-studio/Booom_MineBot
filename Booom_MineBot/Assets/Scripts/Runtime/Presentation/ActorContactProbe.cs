using Minebot.Common;
using Minebot.GridMining;
using UnityEngine;

namespace Minebot.Presentation
{
    public static class ActorContactProbe
    {
        public static GridPosition WorldToGrid(Vector2 worldPosition)
        {
            return new GridPosition(Mathf.FloorToInt(worldPosition.x), Mathf.FloorToInt(worldPosition.y));
        }

        public static Vector2 GridToWorldCenter(GridPosition position)
        {
            return new Vector2(position.X + 0.5f, position.Y + 0.5f);
        }

        public static GridPosition ResolveContactCell(Vector2 worldPosition, Vector2 moveDirection, float probeDistance = 0.52f)
        {
            Vector2 direction = moveDirection.sqrMagnitude > 0.0001f ? moveDirection.normalized : Vector2.zero;
            return WorldToGrid(worldPosition + direction * Mathf.Max(0f, probeDistance));
        }

        public static bool TryResolveMove(
            LogicalGridState grid,
            Vector2 currentWorld,
            Vector2 desiredDelta,
            out Vector2 resolvedWorld,
            out GridPosition contactCell)
        {
            resolvedWorld = currentWorld;
            contactCell = ResolveContactCell(currentWorld, desiredDelta);
            if (grid == null)
            {
                return false;
            }

            Vector2 desiredWorld = currentWorld + desiredDelta;
            GridPosition desiredCell = WorldToGrid(desiredWorld);
            if (!grid.IsInside(desiredCell))
            {
                contactCell = desiredCell;
                return false;
            }

            GridCellState cell = grid.GetCell(desiredCell);
            if (!cell.IsPassable)
            {
                contactCell = ResolveContactCell(currentWorld, desiredDelta);
                if (!grid.IsInside(contactCell))
                {
                    contactCell = desiredCell;
                }

                return false;
            }

            resolvedWorld = desiredWorld;
            contactCell = desiredCell;
            return true;
        }
    }
}
