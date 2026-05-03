using System.Collections.Generic;
using Minebot.Common;
using UnityEngine;

namespace Minebot.GridMining
{
    public enum MineInteractionResult
    {
        InvalidTarget,
        Moved,
        BlockedByTerrain,
        DrillTooWeak,
        MiningInProgress,
        Mined,
        TriggeredBomb
    }

    public readonly struct MineClearedCell
    {
        public MineClearedCell(GridPosition position, ResourceAmount reward, bool wasBomb)
        {
            Position = position;
            Reward = reward;
            WasBomb = wasBomb;
        }

        public GridPosition Position { get; }
        public ResourceAmount Reward { get; }
        public bool WasBomb { get; }
    }

    public readonly struct MiningProgressSnapshot
    {
        public MiningProgressSnapshot(GridPosition position, int currentHealth, int maxHealth)
        {
            Position = position;
            CurrentHealth = Mathf.Max(0, currentHealth);
            MaxHealth = Mathf.Max(1, maxHealth);
        }

        public GridPosition Position { get; }
        public int CurrentHealth { get; }
        public int MaxHealth { get; }
        public bool IsValid => MaxHealth > 0;
        public float HealthNormalized => MaxHealth > 0 ? Mathf.Clamp01((float)CurrentHealth / MaxHealth) : 0f;
        public float DamageNormalized => 1f - HealthNormalized;
    }

    public readonly struct MineResolution
    {
        public MineResolution(
            MineInteractionResult result,
            IReadOnlyList<MineClearedCell> clearedCells,
            ResourceAmount totalReward,
            MiningProgressSnapshot progressSnapshot = default,
            int damageDealt = 0)
        {
            Result = result;
            ClearedCells = clearedCells ?? System.Array.Empty<MineClearedCell>();
            TotalReward = totalReward;
            ProgressSnapshot = progressSnapshot;
            DamageDealt = Mathf.Max(0, damageDealt);
        }

        public MineInteractionResult Result { get; }
        public IReadOnlyList<MineClearedCell> ClearedCells { get; }
        public ResourceAmount TotalReward { get; }
        public MiningProgressSnapshot ProgressSnapshot { get; }
        public int DamageDealt { get; }
        public bool HasProgressSnapshot => ProgressSnapshot.IsValid;
    }

    public sealed class PlayerMiningState
    {
        public PlayerMiningState(GridPosition position, HardnessTier drillTier)
        {
            Position = position;
            DrillTier = drillTier;
        }

        public GridPosition Position { get; private set; }
        public HardnessTier DrillTier { get; set; }

        public void Teleport(GridPosition position)
        {
            Position = position;
        }
    }

    public sealed class MiningService
    {
        private sealed class MiningProgressState
        {
            public MiningProgressState(int maxHealth)
            {
                MaxHealth = Mathf.Max(1, maxHealth);
                CurrentHealth = MaxHealth;
                TimeSinceLastInteraction = 0f;
            }

            public int MaxHealth { get; }
            public int CurrentHealth { get; set; }
            public float TimeSinceLastInteraction { get; set; }
        }

        private readonly LogicalGridState grid;
        private readonly MiningRules rules;
        private readonly Dictionary<GridPosition, MiningProgressState> progressByCell = new Dictionary<GridPosition, MiningProgressState>();
        private readonly List<MiningProgressSnapshot> activeSnapshotsBuffer = new List<MiningProgressSnapshot>();
        private readonly List<GridPosition> expiredProgressBuffer = new List<GridPosition>();

        public MiningService(LogicalGridState grid)
            : this(grid, null)
        {
        }

        public MiningService(LogicalGridState grid, MiningRules rules)
        {
            this.grid = grid;
            this.rules = rules;
        }

        public float PlayerMiningTickIntervalSeconds => rules != null
            ? rules.PlayerMiningTickIntervalSeconds
            : MiningRules.DefaultPlayerMiningTickIntervalSeconds;

        public float MiningDisengageGraceSeconds => rules != null
            ? rules.MiningDisengageGraceSeconds
            : MiningRules.DefaultMiningDisengageGraceSeconds;

        public IReadOnlyList<MiningProgressSnapshot> ActiveProgressSnapshots
        {
            get
            {
                activeSnapshotsBuffer.Clear();
                foreach (KeyValuePair<GridPosition, MiningProgressState> pair in progressByCell)
                {
                    if (!grid.IsInside(pair.Key) || !grid.GetCell(pair.Key).IsMineable)
                    {
                        continue;
                    }

                    activeSnapshotsBuffer.Add(CreateSnapshot(pair.Key, pair.Value));
                }

                activeSnapshotsBuffer.Sort(CompareSnapshotsByPosition);
                return activeSnapshotsBuffer;
            }
        }

        public MineInteractionResult Move(PlayerMiningState player, GridPosition direction)
        {
            GridPosition target = player.Position + direction;
            if (!grid.IsInside(target))
            {
                return MineInteractionResult.InvalidTarget;
            }

            if (!grid.GetCell(target).IsPassable)
            {
                return MineInteractionResult.BlockedByTerrain;
            }

            player.Teleport(target);
            return MineInteractionResult.Moved;
        }

        public MineInteractionResult TryMine(PlayerMiningState player, GridPosition target, out ResourceAmount reward)
        {
            MineResolution resolution = TryMineDetailed(player, target);
            reward = resolution.TotalReward;
            return resolution.Result;
        }

        public MineInteractionResult TryMineFrom(GridPosition actorPosition, HardnessTier drillTier, GridPosition target, out ResourceAmount reward)
        {
            MineResolution resolution = TryMineDetailedFrom(actorPosition, drillTier, target);
            reward = resolution.TotalReward;
            return resolution.Result;
        }

        public MineResolution TryMineDetailed(PlayerMiningState player, GridPosition target)
        {
            return TryMineDetailedFrom(player.Position, player.DrillTier, target, includePlayerBaseAttack: true);
        }

        public MineResolution TryMineDetailedFrom(
            GridPosition actorPosition,
            HardnessTier drillTier,
            GridPosition target,
            bool includePlayerBaseAttack = true)
        {
            if (!grid.IsInside(target) || actorPosition.ManhattanDistance(target) != 1)
            {
                return CreateResolution(MineInteractionResult.InvalidTarget);
            }

            ref GridCellState cell = ref grid.GetCellRef(target);
            if (!cell.IsMineable)
            {
                return CreateResolution(MineInteractionResult.BlockedByTerrain);
            }

            int attack = EffectiveAttackFor(drillTier, includePlayerBaseAttack);
            int defense = DefenseFor(cell.HardnessTier);
            if (attack <= defense)
            {
                if (progressByCell.TryGetValue(target, out MiningProgressState existingState))
                {
                    existingState.TimeSinceLastInteraction = 0f;
                    return CreateResolution(MineInteractionResult.DrillTooWeak, progressSnapshot: CreateSnapshot(target, existingState));
                }

                return CreateResolution(MineInteractionResult.DrillTooWeak);
            }

            MiningProgressState state = GetOrCreateProgressState(target, MaxHealthFor(cell.HardnessTier));
            state.TimeSinceLastInteraction = 0f;
            int damage = Mathf.Max(0, attack - defense);
            state.CurrentHealth = Mathf.Max(0, state.CurrentHealth - damage);

            if (state.CurrentHealth > 0)
            {
                return CreateResolution(
                    MineInteractionResult.MiningInProgress,
                    progressSnapshot: CreateSnapshot(target, state),
                    damageDealt: damage);
            }

            progressByCell.Remove(target);
            var clearedCells = new List<MineClearedCell>();
            bool wasBomb = cell.HasBomb;
            ResourceAmount reward = OpenCell(target, clearBomb: wasBomb);
            clearedCells.Add(new MineClearedCell(target, reward, wasBomb));
            return CreateResolution(
                wasBomb ? MineInteractionResult.TriggeredBomb : MineInteractionResult.Mined,
                clearedCells,
                reward,
                damageDealt: damage);
        }

        public bool TickMiningRecovery(float deltaTime)
        {
            if (progressByCell.Count == 0)
            {
                return false;
            }

            float elapsed = Mathf.Max(0f, deltaTime);
            float grace = MiningDisengageGraceSeconds;
            expiredProgressBuffer.Clear();
            foreach (KeyValuePair<GridPosition, MiningProgressState> pair in progressByCell)
            {
                if (!grid.IsInside(pair.Key) || !grid.GetCell(pair.Key).IsMineable)
                {
                    expiredProgressBuffer.Add(pair.Key);
                    continue;
                }

                pair.Value.TimeSinceLastInteraction += elapsed;
                if (pair.Value.TimeSinceLastInteraction >= grace)
                {
                    expiredProgressBuffer.Add(pair.Key);
                }
            }

            if (expiredProgressBuffer.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < expiredProgressBuffer.Count; i++)
            {
                progressByCell.Remove(expiredProgressBuffer[i]);
            }

            return true;
        }

        public bool CanDamageTarget(GridPosition position, HardnessTier drillTier, bool includePlayerBaseAttack = true)
        {
            if (!grid.IsInside(position))
            {
                return false;
            }

            GridCellState cell = grid.GetCell(position);
            if (!cell.IsMineable)
            {
                return false;
            }

            return EffectiveAttackFor(drillTier, includePlayerBaseAttack) > DefenseFor(cell.HardnessTier);
        }

        public int EffectiveAttackFor(HardnessTier drillTier, bool includePlayerBaseAttack = true)
        {
            return rules != null
                ? rules.EffectiveAttackFor(drillTier, includePlayerBaseAttack)
                : MiningRules.DefaultEffectiveAttackFor(drillTier, includePlayerBaseAttack);
        }

        private MineResolution CreateResolution(
            MineInteractionResult result,
            IReadOnlyList<MineClearedCell> clearedCells = null,
            ResourceAmount? totalReward = null,
            MiningProgressSnapshot progressSnapshot = default,
            int damageDealt = 0)
        {
            return new MineResolution(result, clearedCells, totalReward ?? ResourceAmount.Zero, progressSnapshot, damageDealt);
        }

        private MiningProgressState GetOrCreateProgressState(GridPosition target, int maxHealth)
        {
            if (!progressByCell.TryGetValue(target, out MiningProgressState state))
            {
                state = new MiningProgressState(maxHealth);
                progressByCell.Add(target, state);
            }

            return state;
        }

        private MiningProgressSnapshot CreateSnapshot(GridPosition position, MiningProgressState state)
        {
            return new MiningProgressSnapshot(position, state.CurrentHealth, state.MaxHealth);
        }

        private static int CompareSnapshotsByPosition(MiningProgressSnapshot left, MiningProgressSnapshot right)
        {
            if (left.Position.Y != right.Position.Y)
            {
                return left.Position.Y.CompareTo(right.Position.Y);
            }

            return left.Position.X.CompareTo(right.Position.X);
        }

        private int MaxHealthFor(HardnessTier tier)
        {
            return rules != null
                ? rules.MaxHealthFor(tier)
                : MiningRules.DefaultMaxHealthFor(tier);
        }

        private int DefenseFor(HardnessTier tier)
        {
            return rules != null
                ? rules.DefenseFor(tier)
                : MiningRules.DefaultDefenseFor(tier);
        }

        private ResourceAmount OpenCell(GridPosition position, bool clearBomb)
        {
            ref GridCellState cell = ref grid.GetCellRef(position);
            ResourceAmount reward = cell.Reward;
            cell.TerrainKind = TerrainKind.Empty;
            cell.IsRevealed = true;
            cell.IsMarked = false;
            if (clearBomb)
            {
                cell.ClearBomb();
            }

            return reward;
        }
    }
}
