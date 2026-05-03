using UnityEngine;

namespace Minebot.GridMining
{
    [CreateAssetMenu(menuName = "Minebot/挖掘/挖掘规则")]
    public sealed class MiningRules : ScriptableObject
    {
        public const float DefaultPlayerMiningTickIntervalSeconds = 0.1f;
        public const float DefaultMiningDisengageGraceSeconds = 0.5f;
        public const int DefaultPlayerBaseAttack = 0;

        public const int DefaultSoilMaxHealth = 1;
        public const int DefaultStoneMaxHealth = 2;
        public const int DefaultHardRockMaxHealth = 3;
        public const int DefaultUltraHardMaxHealth = 4;

        public const int DefaultSoilDefense = 0;
        public const int DefaultStoneDefense = 2;
        public const int DefaultHardRockDefense = 4;
        public const int DefaultUltraHardDefense = 6;

        public const int DefaultSoilAttackBonus = 1;
        public const int DefaultStoneAttackBonus = 3;
        public const int DefaultHardRockAttackBonus = 5;
        public const int DefaultUltraHardAttackBonus = 7;

        [SerializeField]
        [InspectorLabel("玩家挖掘 tick 间隔（秒）")]
        private float playerMiningTickIntervalSeconds = DefaultPlayerMiningTickIntervalSeconds;

        [SerializeField]
        [InspectorLabel("挖掘中断宽限（秒）")]
        private float miningDisengageGraceSeconds = DefaultMiningDisengageGraceSeconds;

        [SerializeField]
        [InspectorLabel("玩家基础攻击力")]
        private int playerBaseAttack = DefaultPlayerBaseAttack;

        [Header("土层")]
        [SerializeField]
        [InspectorLabel("生命值")]
        private int soilMaxHealth = DefaultSoilMaxHealth;

        [SerializeField]
        [InspectorLabel("防御力")]
        private int soilDefense = DefaultSoilDefense;

        [SerializeField]
        [InspectorLabel("钻头攻击加值")]
        private int soilAttackBonus = DefaultSoilAttackBonus;

        [Header("石层")]
        [SerializeField]
        [InspectorLabel("生命值")]
        private int stoneMaxHealth = DefaultStoneMaxHealth;

        [SerializeField]
        [InspectorLabel("防御力")]
        private int stoneDefense = DefaultStoneDefense;

        [SerializeField]
        [InspectorLabel("钻头攻击加值")]
        private int stoneAttackBonus = DefaultStoneAttackBonus;

        [Header("硬岩")]
        [SerializeField]
        [InspectorLabel("生命值")]
        private int hardRockMaxHealth = DefaultHardRockMaxHealth;

        [SerializeField]
        [InspectorLabel("防御力")]
        private int hardRockDefense = DefaultHardRockDefense;

        [SerializeField]
        [InspectorLabel("钻头攻击加值")]
        private int hardRockAttackBonus = DefaultHardRockAttackBonus;

        [Header("超硬岩")]
        [SerializeField]
        [InspectorLabel("生命值")]
        private int ultraHardMaxHealth = DefaultUltraHardMaxHealth;

        [SerializeField]
        [InspectorLabel("防御力")]
        private int ultraHardDefense = DefaultUltraHardDefense;

        [SerializeField]
        [InspectorLabel("钻头攻击加值")]
        private int ultraHardAttackBonus = DefaultUltraHardAttackBonus;

        public float PlayerMiningTickIntervalSeconds => Mathf.Max(0.02f, playerMiningTickIntervalSeconds);
        public float MiningDisengageGraceSeconds => Mathf.Max(0f, miningDisengageGraceSeconds);
        public int PlayerBaseAttack => Mathf.Max(0, playerBaseAttack);

        public int MaxHealthFor(HardnessTier tier) => tier switch
        {
            HardnessTier.Soil => Mathf.Max(1, soilMaxHealth),
            HardnessTier.Stone => Mathf.Max(1, stoneMaxHealth),
            HardnessTier.HardRock => Mathf.Max(1, hardRockMaxHealth),
            HardnessTier.UltraHard => Mathf.Max(1, ultraHardMaxHealth),
            _ => Mathf.Max(1, soilMaxHealth)
        };

        public int DefenseFor(HardnessTier tier) => tier switch
        {
            HardnessTier.Soil => Mathf.Max(0, soilDefense),
            HardnessTier.Stone => Mathf.Max(0, stoneDefense),
            HardnessTier.HardRock => Mathf.Max(0, hardRockDefense),
            HardnessTier.UltraHard => Mathf.Max(0, ultraHardDefense),
            _ => Mathf.Max(0, soilDefense)
        };

        public int AttackBonusFor(HardnessTier tier) => tier switch
        {
            HardnessTier.Soil => Mathf.Max(0, soilAttackBonus),
            HardnessTier.Stone => Mathf.Max(0, stoneAttackBonus),
            HardnessTier.HardRock => Mathf.Max(0, hardRockAttackBonus),
            HardnessTier.UltraHard => Mathf.Max(0, ultraHardAttackBonus),
            _ => Mathf.Max(0, soilAttackBonus)
        };

        public int EffectiveAttackFor(HardnessTier tier, bool includePlayerBaseAttack)
        {
            int attack = AttackBonusFor(tier);
            if (includePlayerBaseAttack)
            {
                attack += PlayerBaseAttack;
            }

            return Mathf.Max(0, attack);
        }

        public static int DefaultMaxHealthFor(HardnessTier tier) => tier switch
        {
            HardnessTier.Soil => DefaultSoilMaxHealth,
            HardnessTier.Stone => DefaultStoneMaxHealth,
            HardnessTier.HardRock => DefaultHardRockMaxHealth,
            HardnessTier.UltraHard => DefaultUltraHardMaxHealth,
            _ => DefaultSoilMaxHealth
        };

        public static int DefaultDefenseFor(HardnessTier tier) => tier switch
        {
            HardnessTier.Soil => DefaultSoilDefense,
            HardnessTier.Stone => DefaultStoneDefense,
            HardnessTier.HardRock => DefaultHardRockDefense,
            HardnessTier.UltraHard => DefaultUltraHardDefense,
            _ => DefaultSoilDefense
        };

        public static int DefaultAttackBonusFor(HardnessTier tier) => tier switch
        {
            HardnessTier.Soil => DefaultSoilAttackBonus,
            HardnessTier.Stone => DefaultStoneAttackBonus,
            HardnessTier.HardRock => DefaultHardRockAttackBonus,
            HardnessTier.UltraHard => DefaultUltraHardAttackBonus,
            _ => DefaultSoilAttackBonus
        };

        public static int DefaultEffectiveAttackFor(HardnessTier tier, bool includePlayerBaseAttack)
        {
            int attack = DefaultAttackBonusFor(tier);
            if (includePlayerBaseAttack)
            {
                attack += DefaultPlayerBaseAttack;
            }

            return Mathf.Max(0, attack);
        }
    }
}
