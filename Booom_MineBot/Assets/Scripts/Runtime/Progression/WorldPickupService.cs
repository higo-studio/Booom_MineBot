using System;
using System.Collections.Generic;
using Minebot.Common;
using Minebot.GridMining;
using UnityEngine;

namespace Minebot.Progression
{
    public enum WorldPickupType
    {
        Metal,
        Energy,
        Experience
    }

    public enum WorldPickupSource
    {
        PlayerMining,
        HelperRobotMining,
        RobotRecycle,
        WaveRecycle
    }

    public readonly struct WorldPickupAbsorption
    {
        public WorldPickupAbsorption(WorldPickupState pickup, ResourceAmount reward)
        {
            Pickup = pickup;
            Reward = reward;
        }

        public WorldPickupState Pickup { get; }
        public ResourceAmount Reward { get; }
    }

    public sealed class WorldPickupState
    {
        internal WorldPickupState(int id, WorldPickupType type, int amount, GridPosition origin, Vector2 drift, WorldPickupSource source)
        {
            Id = id;
            Type = type;
            Amount = Mathf.Max(0, amount);
            Origin = origin;
            Drift = drift;
            Source = source;
        }

        public int Id { get; }
        public WorldPickupType Type { get; }
        public int Amount { get; }
        public GridPosition Origin { get; }
        public Vector2 Drift { get; }
        public WorldPickupSource Source { get; }
        public float Age { get; private set; }
        public bool IsAbsorbing { get; private set; }
        public float AbsorbProgress { get; private set; }

        internal void Advance(float deltaTime)
        {
            Age += Mathf.Max(0f, deltaTime);
        }

        internal void BeginAbsorb()
        {
            IsAbsorbing = true;
            AbsorbProgress = 0f;
        }

        internal bool AdvanceAbsorb(float deltaTime, float absorbDuration)
        {
            AbsorbProgress += Mathf.Max(0.0001f, deltaTime) / Mathf.Max(0.01f, absorbDuration);
            return AbsorbProgress >= 1f;
        }
    }

    public sealed class WorldPickupService
    {
        public const float DefaultAutoAbsorbRadius = 1.08f;
        public const float DefaultAbsorbDuration = 0.18f;
        public const float DefaultSpawnGraceSeconds = 0.18f;

        private readonly List<WorldPickupState> activePickups = new List<WorldPickupState>();
        private readonly List<WorldPickupAbsorption> pendingAbsorptions = new List<WorldPickupAbsorption>();
        private readonly float autoAbsorbRadius;
        private readonly float absorbDuration;
        private readonly float spawnGraceSeconds;
        private int nextPickupId = 1;

        public WorldPickupService(
            float autoAbsorbRadius = DefaultAutoAbsorbRadius,
            float absorbDuration = DefaultAbsorbDuration,
            float spawnGraceSeconds = DefaultSpawnGraceSeconds)
        {
            this.autoAbsorbRadius = Mathf.Max(0.1f, autoAbsorbRadius);
            this.absorbDuration = Mathf.Max(0.01f, absorbDuration);
            this.spawnGraceSeconds = Mathf.Max(0f, spawnGraceSeconds);
        }

        public event Action PickupsChanged;
        public event Action<WorldPickupState> PickupSpawned;
        public event Action<WorldPickupAbsorption> PickupAbsorbed;

        public IReadOnlyList<WorldPickupState> ActivePickups => activePickups;

        public bool SpawnReward(GridPosition origin, ResourceAmount reward, WorldPickupSource source)
        {
            bool created = false;
            created |= SpawnSingle(WorldPickupType.Metal, reward.Metal, origin, new Vector2(-0.18f, 0f), source);
            created |= SpawnSingle(WorldPickupType.Energy, reward.Energy, origin, new Vector2(0.18f, 0.08f), source);
            created |= SpawnSingle(WorldPickupType.Experience, reward.Experience, origin, new Vector2(0f, 0.18f), source);
            if (created)
            {
                PickupsChanged?.Invoke();
            }

            return created;
        }

        public ResourceAmount TickAndCollect(float deltaTime, Vector2 playerWorldPosition)
        {
            if (activePickups.Count == 0)
            {
                return ResourceAmount.Zero;
            }

            ResourceAmount collected = ResourceAmount.Zero;
            pendingAbsorptions.Clear();

            for (int i = activePickups.Count - 1; i >= 0; i--)
            {
                WorldPickupState pickup = activePickups[i];
                pickup.Advance(deltaTime);

                if (!pickup.IsAbsorbing && pickup.Age >= spawnGraceSeconds && IsWithinAutoAbsorbRange(playerWorldPosition, pickup))
                {
                    pickup.BeginAbsorb();
                }

                if (pickup.IsAbsorbing && pickup.AdvanceAbsorb(deltaTime, absorbDuration))
                {
                    ResourceAmount reward = ToReward(pickup.Type, pickup.Amount);
                    collected += reward;
                    pendingAbsorptions.Add(new WorldPickupAbsorption(pickup, reward));
                    activePickups.RemoveAt(i);
                    continue;
                }

                activePickups[i] = pickup;
            }

            if (pendingAbsorptions.Count > 0)
            {
                for (int i = 0; i < pendingAbsorptions.Count; i++)
                {
                    PickupAbsorbed?.Invoke(pendingAbsorptions[i]);
                }

                PickupsChanged?.Invoke();
            }

            return collected;
        }

        public void Clear()
        {
            if (activePickups.Count == 0)
            {
                return;
            }

            activePickups.Clear();
            pendingAbsorptions.Clear();
            PickupsChanged?.Invoke();
        }

        private bool SpawnSingle(WorldPickupType type, int amount, GridPosition origin, Vector2 drift, WorldPickupSource source)
        {
            if (amount <= 0)
            {
                return false;
            }

            var pickup = new WorldPickupState(nextPickupId++, type, amount, origin, drift, source);
            activePickups.Add(pickup);
            PickupSpawned?.Invoke(pickup);
            return true;
        }

        private bool IsWithinAutoAbsorbRange(Vector2 playerWorldPosition, WorldPickupState pickup)
        {
            Vector2 center = new Vector2(pickup.Origin.X + 0.5f, pickup.Origin.Y + 0.5f) + pickup.Drift;
            return (playerWorldPosition - center).sqrMagnitude <= autoAbsorbRadius * autoAbsorbRadius;
        }

        private static ResourceAmount ToReward(WorldPickupType type, int amount)
        {
            switch (type)
            {
                case WorldPickupType.Metal:
                    return new ResourceAmount(amount, 0, 0);
                case WorldPickupType.Energy:
                    return new ResourceAmount(0, amount, 0);
                default:
                    return new ResourceAmount(0, 0, amount);
            }
        }
    }
}
