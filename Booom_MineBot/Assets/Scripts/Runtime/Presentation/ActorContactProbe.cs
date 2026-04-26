using Minebot.Common;
using Minebot.GridMining;
using UnityEngine;

namespace Minebot.Presentation
{
    public static class ActorContactProbe
    {
        public const float DefaultCollisionRadius = 0.42f;

        public static GridPosition WorldToGrid(Vector2 worldPosition)
        {
            return new GridPosition(Mathf.FloorToInt(worldPosition.x), Mathf.FloorToInt(worldPosition.y));
        }

        public static Vector2 GridToWorldCenter(GridPosition position)
        {
            return new Vector2(position.X + 0.5f, position.Y + 0.5f);
        }

        public static GridPosition ResolveContactCell(Vector2 worldPosition, Vector2 moveDirection, float collisionRadius = DefaultCollisionRadius)
        {
            Vector2 direction = moveDirection.sqrMagnitude > 0.0001f ? moveDirection.normalized : Vector2.zero;
            float probeDistance = Mathf.Max(0f, collisionRadius + 0.08f);
            return WorldToGrid(worldPosition + direction * probeDistance);
        }

        public static bool TryResolveMove(
            LogicalGridState grid,
            Vector2 currentWorld,
            Vector2 desiredDelta,
            float collisionRadius,
            out Vector2 resolvedWorld,
            out GridPosition contactCell)
        {
            if (grid == null)
            {
                resolvedWorld = currentWorld;
                contactCell = WorldToGrid(currentWorld);
                return false;
            }

            var motor = new KinematicCharacterMotor2D();
            var collisionWorld = new GridCharacterCollisionWorld(grid);
            var request = new CharacterMoveRequest2D(
                currentWorld,
                desiredDelta,
                new CharacterMotorConfig2D(
                    collisionRadius,
                    0.02f,
                    0.0005f,
                    4,
                    true));

            CharacterMoveResult2D result = motor.Move(request, collisionWorld);
            resolvedWorld = result.FinalPosition;
            contactCell = result.HasStableContact
                ? result.StableContactCell
                : collisionWorld.ResolveOccupancyCell(result.FinalPosition, WorldToGrid(currentWorld));
            return result.HasMoved;
        }
    }
}
