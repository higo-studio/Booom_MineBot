using System;
using System.Collections.Generic;
using Minebot.Common;
using Minebot.Progression;
using UnityEngine;

namespace Minebot.Presentation
{
    public sealed class MinebotPickupRenderer : MonoBehaviour
    {
        private const int PickupSortingOrder = 34;

        private struct HoverPickupVisual
        {
            public int Id;
            public Vector3 OriginWorld;
            public Vector2 Drift;
            public float Age;
            public int SyncVersion;
        }

        private readonly struct HoverPickupLocation
        {
            public HoverPickupLocation(WorldPickupType type, int index)
            {
                Type = type;
                Index = index;
            }

            public WorldPickupType Type { get; }
            public int Index { get; }
        }

        private sealed class PickupBucket
        {
            public PickupBucket(WorldPickupType type)
            {
                Type = type;
            }

            public WorldPickupType Type { get; }
            public List<HoverPickupVisual> Visuals { get; } = new List<HoverPickupVisual>();
        }

        private readonly PickupBucket metalBucket = new PickupBucket(WorldPickupType.Metal);
        private readonly PickupBucket energyBucket = new PickupBucket(WorldPickupType.Energy);
        private readonly PickupBucket experienceBucket = new PickupBucket(WorldPickupType.Experience);
        private readonly Dictionary<int, HoverPickupLocation> hoverLocations = new Dictionary<int, HoverPickupLocation>();
        private readonly Dictionary<int, MinebotPickupView> fallbackHoverViews = new Dictionary<int, MinebotPickupView>();
        private readonly List<MinebotPickupView> activeAbsorbViews = new List<MinebotPickupView>();
        private readonly Queue<MinebotPickupView> pooledViews = new Queue<MinebotPickupView>();
        private MinebotPresentationAssets assets;
        private int syncVersion;

        public bool UsesBatchRendererGroup => false;
        public int HoverVisualCount => hoverLocations.Count;
        public int AbsorbingVisualCount => activeAbsorbViews.Count;
        public int TotalVisualCount => HoverVisualCount + AbsorbingVisualCount;

        public void Configure(MinebotPresentationAssets presentationAssets)
        {
            assets = presentationAssets;
            PushHoverVisuals();
        }

        public void SyncActivePickups(IReadOnlyList<WorldPickupState> activePickups, Func<GridPosition, Vector3> gridToWorld)
        {
            syncVersion++;
            if (activePickups != null)
            {
                for (int i = 0; i < activePickups.Count; i++)
                {
                    WorldPickupState pickup = activePickups[i];
                    UpsertHoverPickup(pickup, gridToWorld != null ? gridToWorld(pickup.Origin) : Vector3.zero);
                }
            }

            RemoveStaleHoverPickups(metalBucket);
            RemoveStaleHoverPickups(energyBucket);
            RemoveStaleHoverPickups(experienceBucket);
            PushHoverVisuals();
        }

        public void BeginAbsorb(WorldPickupState pickup, Vector3 playerWorldPosition)
        {
            if (assets == null)
            {
                return;
            }

            Sprite icon = assets.PickupIconFor(pickup.Type);
            Vector3 startPosition = ComputeHoverPosition(pickup.Id, pickup.Age, GridToWorldCenter(pickup.Origin), pickup.Drift);
            if (TryRemoveHoverPickup(pickup.Id, out HoverPickupVisual visual))
            {
                startPosition = ComputeHoverPosition(visual.Id, visual.Age, visual.OriginWorld, visual.Drift);
            }

            MinebotPickupView view = null;
            if (fallbackHoverViews.TryGetValue(pickup.Id, out MinebotPickupView hoverView))
            {
                view = hoverView;
                fallbackHoverViews.Remove(pickup.Id);
            }

            view ??= AcquireView(pickup.Type);
            view.BeginAbsorbVisual(icon, startPosition, playerWorldPosition, PickupSortingOrder);
            activeAbsorbViews.Add(view);
            PushHoverVisuals();
        }

        public void ClearAll()
        {
            hoverLocations.Clear();
            metalBucket.Visuals.Clear();
            energyBucket.Visuals.Clear();
            experienceBucket.Visuals.Clear();

            foreach (KeyValuePair<int, MinebotPickupView> pair in fallbackHoverViews)
            {
                ReleaseView(pair.Value);
            }

            fallbackHoverViews.Clear();

            for (int i = activeAbsorbViews.Count - 1; i >= 0; i--)
            {
                ReleaseView(activeAbsorbViews[i]);
            }

            activeAbsorbViews.Clear();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            AdvanceHoverAges(metalBucket, deltaTime);
            AdvanceHoverAges(energyBucket, deltaTime);
            AdvanceHoverAges(experienceBucket, deltaTime);
            PushHoverVisuals();
            TickAbsorbViews(deltaTime);
        }

        private void OnDestroy()
        {
            DisposeViews();
        }

        private void DisposeViews()
        {
            while (pooledViews.Count > 0)
            {
                MinebotPickupView view = pooledViews.Dequeue();
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }

            foreach (KeyValuePair<int, MinebotPickupView> pair in fallbackHoverViews)
            {
                if (pair.Value != null)
                {
                    Destroy(pair.Value.gameObject);
                }
            }

            fallbackHoverViews.Clear();

            for (int i = 0; i < activeAbsorbViews.Count; i++)
            {
                if (activeAbsorbViews[i] != null)
                {
                    Destroy(activeAbsorbViews[i].gameObject);
                }
            }

            activeAbsorbViews.Clear();
            hoverLocations.Clear();
            metalBucket.Visuals.Clear();
            energyBucket.Visuals.Clear();
            experienceBucket.Visuals.Clear();
        }

        private void UpsertHoverPickup(WorldPickupState pickup, Vector3 originWorld)
        {
            if (hoverLocations.TryGetValue(pickup.Id, out HoverPickupLocation location))
            {
                List<HoverPickupVisual> visuals = BucketFor(location.Type).Visuals;
                HoverPickupVisual visual = visuals[location.Index];
                visual.OriginWorld = originWorld;
                visual.Drift = pickup.Drift;
                visual.Age = pickup.Age;
                visual.SyncVersion = syncVersion;
                visuals[location.Index] = visual;
                return;
            }

            PickupBucket bucket = BucketFor(pickup.Type);
            bucket.Visuals.Add(new HoverPickupVisual
            {
                Id = pickup.Id,
                OriginWorld = originWorld,
                Drift = pickup.Drift,
                Age = pickup.Age,
                SyncVersion = syncVersion
            });
            hoverLocations[pickup.Id] = new HoverPickupLocation(pickup.Type, bucket.Visuals.Count - 1);
        }

        private void RemoveStaleHoverPickups(PickupBucket bucket)
        {
            List<HoverPickupVisual> visuals = bucket.Visuals;
            for (int i = visuals.Count - 1; i >= 0; i--)
            {
                if (visuals[i].SyncVersion == syncVersion)
                {
                    continue;
                }

                int removedId = visuals[i].Id;
                if (fallbackHoverViews.TryGetValue(removedId, out MinebotPickupView view))
                {
                    fallbackHoverViews.Remove(removedId);
                    ReleaseView(view);
                }

                RemoveHoverAt(bucket, i);
            }
        }

        private bool TryRemoveHoverPickup(int pickupId, out HoverPickupVisual visual)
        {
            visual = default;
            if (!hoverLocations.TryGetValue(pickupId, out HoverPickupLocation location))
            {
                return false;
            }

            PickupBucket bucket = BucketFor(location.Type);
            visual = bucket.Visuals[location.Index];
            RemoveHoverAt(bucket, location.Index);
            return true;
        }

        private void RemoveHoverAt(PickupBucket bucket, int index)
        {
            List<HoverPickupVisual> visuals = bucket.Visuals;
            HoverPickupVisual removed = visuals[index];
            int lastIndex = visuals.Count - 1;
            if (index != lastIndex)
            {
                HoverPickupVisual swapped = visuals[lastIndex];
                visuals[index] = swapped;
                hoverLocations[swapped.Id] = new HoverPickupLocation(bucket.Type, index);
            }

            visuals.RemoveAt(lastIndex);
            hoverLocations.Remove(removed.Id);
        }

        private static void AdvanceHoverAges(PickupBucket bucket, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            List<HoverPickupVisual> visuals = bucket.Visuals;
            for (int i = 0; i < visuals.Count; i++)
            {
                HoverPickupVisual visual = visuals[i];
                visual.Age += deltaTime;
                visuals[i] = visual;
            }
        }

        private void PushHoverVisuals()
        {
            if (assets == null)
            {
                return;
            }

            PushFallbackVisuals(metalBucket);
            PushFallbackVisuals(energyBucket);
            PushFallbackVisuals(experienceBucket);
        }

        private void PushFallbackVisuals(PickupBucket bucket)
        {
            List<HoverPickupVisual> visuals = bucket.Visuals;
            Sprite icon = assets.PickupIconFor(bucket.Type);
            for (int i = 0; i < visuals.Count; i++)
            {
                HoverPickupVisual visual = visuals[i];
                if (!fallbackHoverViews.TryGetValue(visual.Id, out MinebotPickupView view) || view == null)
                {
                    view = AcquireView(bucket.Type);
                    fallbackHoverViews[visual.Id] = view;
                }

                view.ShowHoverVisual(icon, ComputeHoverPosition(visual.Id, visual.Age, visual.OriginWorld, visual.Drift), PickupSortingOrder);
            }
        }

        private void TickAbsorbViews(float deltaTime)
        {
            for (int i = activeAbsorbViews.Count - 1; i >= 0; i--)
            {
                MinebotPickupView view = activeAbsorbViews[i];
                if (view == null)
                {
                    activeAbsorbViews.RemoveAt(i);
                    continue;
                }

                if (!view.TickAbsorb(deltaTime))
                {
                    continue;
                }

                activeAbsorbViews.RemoveAt(i);
                ReleaseView(view);
            }
        }

        private MinebotPickupView AcquireView(WorldPickupType type)
        {
            while (pooledViews.Count > 0)
            {
                MinebotPickupView pooled = pooledViews.Dequeue();
                if (pooled != null)
                {
                    pooled.gameObject.SetActive(true);
                    return pooled;
                }
            }

            GameObject prefab = assets != null ? assets.PickupPrefabFor(type) : null;
            GameObject instance = prefab != null ? Instantiate(prefab, transform, false) : new GameObject($"Pickup {type}", typeof(MinebotPickupView));
            instance.transform.SetParent(transform, false);
            MinebotPickupView view = instance.GetComponent<MinebotPickupView>();
            if (view == null)
            {
                view = instance.AddComponent<MinebotPickupView>();
            }

            return view;
        }

        private void ReleaseView(MinebotPickupView view)
        {
            if (view == null)
            {
                return;
            }

            view.HideForPool();
            view.transform.SetParent(transform, false);
            pooledViews.Enqueue(view);
        }

        private PickupBucket BucketFor(WorldPickupType type)
        {
            switch (type)
            {
                case WorldPickupType.Energy:
                    return energyBucket;
                case WorldPickupType.Experience:
                    return experienceBucket;
                default:
                    return metalBucket;
            }
        }

        private static Vector3 ComputeHoverPosition(int pickupId, float age, Vector3 originWorld, Vector2 drift)
        {
            float launch = Mathf.Clamp01(age / 0.24f);
            float hover = 0.16f + Mathf.Sin((age + pickupId * 0.17f) * 6.2f) * 0.04f;
            return originWorld + new Vector3(drift.x, Mathf.Lerp(0.02f, 0.28f, launch) + hover, 0f);
        }

        private static Vector3 GridToWorldCenter(GridPosition position)
        {
            return new Vector3(position.X + 0.5f, position.Y + 0.5f, 0f);
        }
    }
}
