using Minebot.Common;
using Minebot.GridMining;
using UnityEngine;

namespace Minebot.Presentation
{
    public sealed class FreeformActorController : MonoBehaviour
    {
        [SerializeField]
        private float moveSpeed = 4f;

        public float MoveSpeed => Mathf.Max(0.1f, moveSpeed);
        public Vector2 WorldPosition => transform.position;
        public GridPosition CurrentGridPosition => ActorContactProbe.WorldToGrid(WorldPosition);

        public bool TryMove(LogicalGridState grid, Vector2 direction, float deltaTime, out GridPosition contactCell)
        {
            Vector2 delta = direction.sqrMagnitude > 0.0001f ? direction.normalized * MoveSpeed * Mathf.Max(0f, deltaTime) : Vector2.zero;
            if (delta == Vector2.zero)
            {
                contactCell = CurrentGridPosition;
                return false;
            }

            bool moved = ActorContactProbe.TryResolveMove(grid, WorldPosition, delta, out Vector2 resolvedWorld, out contactCell);
            if (moved)
            {
                transform.position = new Vector3(resolvedWorld.x, resolvedWorld.y, transform.position.z);
            }

            return moved;
        }

        public void SnapTo(GridPosition position)
        {
            Vector2 world = ActorContactProbe.GridToWorldCenter(position);
            transform.position = new Vector3(world.x, world.y, transform.position.z);
        }
    }
}
