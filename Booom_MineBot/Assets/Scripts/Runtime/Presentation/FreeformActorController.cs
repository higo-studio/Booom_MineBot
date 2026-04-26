using Minebot.Common;
using Minebot.GridMining;
using UnityEngine;

namespace Minebot.Presentation
{
    public sealed class FreeformActorController : MonoBehaviour
    {
        private readonly KinematicCharacterMotor2D motor = new KinematicCharacterMotor2D();

        [SerializeField]
        private float moveSpeed = 4f;

        [SerializeField]
        private float collisionRadius = ActorContactProbe.DefaultCollisionRadius;

        [SerializeField]
        private float contactOffset = 0.02f;

        [SerializeField]
        private float minMoveDistance = 0.0005f;

        [SerializeField]
        private int maxMoveIterations = 4;

        [SerializeField]
        private bool enableOverlapRecovery = true;

        public float MoveSpeed => Mathf.Max(0.1f, moveSpeed);
        public float CollisionRadius
        {
            get => Mathf.Clamp(collisionRadius, 0.1f, 0.49f);
            set => collisionRadius = Mathf.Clamp(value, 0.1f, 0.49f);
        }
        public float ContactOffset
        {
            get => Mathf.Clamp(contactOffset, 0.001f, 0.25f);
            set => contactOffset = Mathf.Clamp(value, 0.001f, 0.25f);
        }
        public float MinMoveDistance
        {
            get => Mathf.Max(0.0001f, minMoveDistance);
            set => minMoveDistance = Mathf.Max(0.0001f, value);
        }
        public int MaxMoveIterations
        {
            get => Mathf.Clamp(maxMoveIterations, 1, 8);
            set => maxMoveIterations = Mathf.Clamp(value, 1, 8);
        }
        public bool EnableOverlapRecovery
        {
            get => enableOverlapRecovery;
            set => enableOverlapRecovery = value;
        }

        public Vector2 WorldPosition => transform.position;
        public GridPosition CurrentGridPosition => ActorContactProbe.WorldToGrid(WorldPosition);

        public CharacterMoveResult2D Move(ICharacterCollisionWorld2D collisionWorld, Vector2 direction, float deltaTime)
        {
            Vector2 delta = direction.sqrMagnitude > 0.0001f ? direction.normalized * MoveSpeed * Mathf.Max(0f, deltaTime) : Vector2.zero;
            if (delta == Vector2.zero)
            {
                return new CharacterMoveResult2D(
                    WorldPosition,
                    WorldPosition,
                    Vector2.zero,
                    Vector2.zero,
                    CharacterCollisionFlags2D.None,
                    default,
                    false,
                    default,
                    false,
                    0);
            }

            var request = new CharacterMoveRequest2D(
                WorldPosition,
                delta,
                new CharacterMotorConfig2D(
                    CollisionRadius,
                    ContactOffset,
                    MinMoveDistance,
                    MaxMoveIterations,
                    EnableOverlapRecovery));

            CharacterMoveResult2D result = motor.Move(request, collisionWorld);
            if (result.HasMoved)
            {
                transform.position = new Vector3(result.FinalPosition.x, result.FinalPosition.y, transform.position.z);
            }

            return result;
        }

        public void SnapTo(GridPosition position)
        {
            Vector2 world = ActorContactProbe.GridToWorldCenter(position);
            transform.position = new Vector3(world.x, world.y, transform.position.z);
        }
    }
}
