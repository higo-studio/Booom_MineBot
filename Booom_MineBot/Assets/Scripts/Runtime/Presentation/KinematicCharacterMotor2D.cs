using System;
using Minebot.Common;
using UnityEngine;

namespace Minebot.Presentation
{
    [Flags]
    public enum CharacterCollisionFlags2D
    {
        None = 0,
        Hit = 1 << 0,
        Blocked = 1 << 1,
        Sliding = 1 << 2,
        InitialOverlap = 1 << 3,
        Depenetrated = 1 << 4
    }

    public readonly struct CharacterMotorConfig2D
    {
        public CharacterMotorConfig2D(
            float collisionRadius,
            float contactOffset,
            float minMoveDistance,
            int maxIterations,
            bool enableOverlapRecovery)
        {
            CollisionRadius = Mathf.Clamp(collisionRadius, 0.1f, 0.49f);
            ContactOffset = Mathf.Clamp(contactOffset, 0.001f, 0.25f);
            MinMoveDistance = Mathf.Max(0.0001f, minMoveDistance);
            MaxIterations = Mathf.Clamp(maxIterations, 1, 8);
            EnableOverlapRecovery = enableOverlapRecovery;
        }

        public float CollisionRadius { get; }
        public float ContactOffset { get; }
        public float MinMoveDistance { get; }
        public int MaxIterations { get; }
        public bool EnableOverlapRecovery { get; }
    }

    public readonly struct CharacterMoveRequest2D
    {
        public CharacterMoveRequest2D(Vector2 startPosition, Vector2 desiredDisplacement, CharacterMotorConfig2D config)
        {
            StartPosition = startPosition;
            DesiredDisplacement = desiredDisplacement;
            Config = config;
        }

        public Vector2 StartPosition { get; }
        public Vector2 DesiredDisplacement { get; }
        public CharacterMotorConfig2D Config { get; }
    }

    public readonly struct CharacterSweepHit2D
    {
        public CharacterSweepHit2D(
            Vector2 position,
            Vector2 normal,
            float fraction,
            float distance,
            GridPosition cell,
            bool initialOverlap)
        {
            Position = position;
            Normal = normal;
            Fraction = Mathf.Clamp01(fraction);
            Distance = Mathf.Max(0f, distance);
            Cell = cell;
            InitialOverlap = initialOverlap;
        }

        public Vector2 Position { get; }
        public Vector2 Normal { get; }
        public float Fraction { get; }
        public float Distance { get; }
        public GridPosition Cell { get; }
        public bool InitialOverlap { get; }
    }

    public readonly struct CharacterMoveResult2D
    {
        public CharacterMoveResult2D(
            Vector2 startPosition,
            Vector2 finalPosition,
            Vector2 requestedDisplacement,
            Vector2 remainingDisplacement,
            CharacterCollisionFlags2D collisionFlags,
            CharacterSweepHit2D primaryHit,
            bool hasPrimaryHit,
            GridPosition stableContactCell,
            bool hasStableContact,
            int hitCount)
        {
            StartPosition = startPosition;
            FinalPosition = finalPosition;
            RequestedDisplacement = requestedDisplacement;
            RemainingDisplacement = remainingDisplacement;
            CollisionFlags = collisionFlags;
            PrimaryHit = primaryHit;
            HasPrimaryHit = hasPrimaryHit;
            StableContactCell = stableContactCell;
            HasStableContact = hasStableContact;
            HitCount = Mathf.Max(0, hitCount);
        }

        public Vector2 StartPosition { get; }
        public Vector2 FinalPosition { get; }
        public Vector2 RequestedDisplacement { get; }
        public Vector2 RemainingDisplacement { get; }
        public CharacterCollisionFlags2D CollisionFlags { get; }
        public CharacterSweepHit2D PrimaryHit { get; }
        public bool HasPrimaryHit { get; }
        public GridPosition StableContactCell { get; }
        public bool HasStableContact { get; }
        public int HitCount { get; }

        public Vector2 ActualDisplacement => FinalPosition - StartPosition;
        public bool HasMoved => ActualDisplacement.sqrMagnitude > 0.0000001f;
        public bool WasBlocked => (CollisionFlags & CharacterCollisionFlags2D.Blocked) != 0;
        public bool WasSliding => (CollisionFlags & CharacterCollisionFlags2D.Sliding) != 0;
        public bool HadInitialOverlap => (CollisionFlags & CharacterCollisionFlags2D.InitialOverlap) != 0;
        public bool WasDepenetrated => (CollisionFlags & CharacterCollisionFlags2D.Depenetrated) != 0;
    }

    public interface ICharacterCollisionWorld2D
    {
        bool Sweep(Vector2 origin, Vector2 displacement, CharacterMotorConfig2D config, out CharacterSweepHit2D hit);

        bool TryDepenetrate(Vector2 position, CharacterMotorConfig2D config, out Vector2 resolvedPosition, out CharacterSweepHit2D hit);
    }

    public sealed class KinematicCharacterMotor2D
    {
        private const float SafeFractionBackoff = 0.0001f;

        public CharacterMoveResult2D Move(in CharacterMoveRequest2D request, ICharacterCollisionWorld2D collisionWorld)
        {
            if (collisionWorld == null)
            {
                return new CharacterMoveResult2D(
                    request.StartPosition,
                    request.StartPosition,
                    request.DesiredDisplacement,
                    request.DesiredDisplacement,
                    CharacterCollisionFlags2D.None,
                    default,
                    false,
                    default,
                    false,
                    0);
            }

            Vector2 current = request.StartPosition;
            Vector2 goal = request.StartPosition + request.DesiredDisplacement;
            Vector2 remaining = request.DesiredDisplacement;
            CharacterCollisionFlags2D flags = CharacterCollisionFlags2D.None;
            CharacterSweepHit2D primaryHit = default;
            bool hasPrimaryHit = false;
            GridPosition stableContactCell = default;
            bool hasStableContact = false;
            int hitCount = 0;

            for (int iteration = 0; iteration < request.Config.MaxIterations; iteration++)
            {
                if (remaining.sqrMagnitude <= request.Config.MinMoveDistance * request.Config.MinMoveDistance)
                {
                    remaining = Vector2.zero;
                    break;
                }

                if (!collisionWorld.Sweep(current, remaining, request.Config, out CharacterSweepHit2D hit))
                {
                    current += remaining;
                    remaining = Vector2.zero;
                    break;
                }

                flags |= CharacterCollisionFlags2D.Hit | CharacterCollisionFlags2D.Blocked;
                hitCount++;
                stableContactCell = hit.Cell;
                hasStableContact = true;
                if (!hasPrimaryHit)
                {
                    primaryHit = hit;
                    hasPrimaryHit = true;
                }

                if (hit.InitialOverlap)
                {
                    flags |= CharacterCollisionFlags2D.InitialOverlap;
                    if (!request.Config.EnableOverlapRecovery
                        || !collisionWorld.TryDepenetrate(current, request.Config, out Vector2 recoveredPosition, out CharacterSweepHit2D overlapHit))
                    {
                        remaining = Vector2.zero;
                        break;
                    }

                    if ((recoveredPosition - current).sqrMagnitude > 0.0000001f)
                    {
                        flags |= CharacterCollisionFlags2D.Depenetrated;
                    }

                    current = recoveredPosition;
                    stableContactCell = overlapHit.Cell;
                    hasStableContact = true;
                    if (!hasPrimaryHit)
                    {
                        primaryHit = overlapHit;
                        hasPrimaryHit = true;
                    }

                    remaining = goal - current;
                    continue;
                }

                float safeFraction = Mathf.Clamp01(hit.Fraction - SafeFractionBackoff);
                if (safeFraction > 0f)
                {
                    current += remaining * safeFraction;
                }

                Vector2 unresolvedToGoal = goal - current;
                float intoNormal = Vector2.Dot(unresolvedToGoal, hit.Normal);
                if (intoNormal < 0f)
                {
                    unresolvedToGoal -= hit.Normal * intoNormal;
                }

                Vector2 nextRemaining = unresolvedToGoal;
                bool hasSlideRemainder = nextRemaining.sqrMagnitude > request.Config.MinMoveDistance * request.Config.MinMoveDistance;
                if (hasSlideRemainder && (nextRemaining - remaining).sqrMagnitude > 0.0000001f)
                {
                    flags |= CharacterCollisionFlags2D.Sliding;
                }

                remaining = hasSlideRemainder ? nextRemaining : Vector2.zero;
            }

            return new CharacterMoveResult2D(
                request.StartPosition,
                current,
                request.DesiredDisplacement,
                remaining,
                flags,
                primaryHit,
                hasPrimaryHit,
                stableContactCell,
                hasStableContact,
                hitCount);
        }
    }
}
