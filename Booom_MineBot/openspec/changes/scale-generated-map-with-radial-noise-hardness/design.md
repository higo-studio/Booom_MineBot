## 背景

当前 `MapGenerator` 的行为非常固定：

- 尺寸由调用点直接传 `12x12`
- 出生点固定在中心
- 出生点外的硬度只按曼哈顿距离切成 `Soil / Stone / HardRock`
- 没有 `UltraHard` 的生成路径

这导致两个问题：

1. 地图空间太小，玩家和机器人很快触到边界，无法承载更长的挖掘推进。
2. 硬度分布过于整齐，只有规则的距离环，没有自然的局部起伏。

用户的新要求是：地图显著放大；整体上仍然要体现“越远越硬”的同心圆层次；同时叠加柏林噪声，让每一圈内部出现局部矿脉感；并且在足够远处必须稳定收敛到全 `UltraHard`。

## 目标 / 非目标

**目标：**

- 让默认程序地图从当前尺寸扩展到 `20x` 规模。
- 让地图大小、混合比例、噪声尺度、硬度阶梯阈值和外圈强制超硬半径可配置。
- 保留安全出生区和前期可推进软岩带，不让噪声破坏开局节奏。
- 保持 `MapGenerator` 仍是确定性的纯 C# 规则代码，便于 EditMode 测试。

**非目标：**

- 不在本轮实现地图分块加载、无限地图或流式生成。
- 不修改 authored `MapDefinition` 的编辑/Bake 流程。
- 不改变炸弹布置、奖励拾取、机器人寻路或 dual-grid 表现层的数据权威边界。

## 关键决策

### 1. 配置入口放进 `BootstrapConfig`

这轮只影响“没有 authored map 时”的默认生成行为，因此最直接的入口是给 `BootstrapConfig` 增加 `GeneratedMapConfig` 字段。当 `DefaultMap != null` 时，仍然优先使用 authored map；只有 `DefaultMap == null` 时才读取该配置并调用 `MapGenerator.Generate(...)`。

这样做的好处：

- 继续遵守“数值优先进入 ScriptableObject 配置”的项目约束。
- Scene / Bootstrap 可显式覆盖默认生成参数，而不是把倍率和噪声常量散落在 `MinebotServices`。
- `MapGenerator` 仍保持与 Unity 场景对象解耦，测试可以直接传 settings。

### 2. 生成尺寸采用“基础尺寸 * 倍率”

为兼容“变成现在的 20 倍”这个要求，同时保留后续快速回调空间，配置使用：

- `baseSize`
- `sizeMultiplier`

最终尺寸为：

```text
resolvedSize = baseSize * sizeMultiplier
```

默认值采用：

- `baseSize = (12, 12)`
- `sizeMultiplier = 20`

因此默认生成结果为 `240x240`。

### 3. 硬度由“径向值 + 噪声值”的加权平均决定

对于每个可挖岩体格：

1. 先计算它相对出生点的欧式距离，并除以“出生点到最远内圈角”的距离，得到 `radial01`
2. 对 `radial01` 施加 `radialExponent`，控制同心圆渐变的陡峭程度
3. 采样 `Mathf.PerlinNoise(...)` 得到 `noise01`
4. 使用配置化权重做归一化加权平均：

```text
blend01 = (radial01 * radialWeight + noise01 * noiseWeight) / (radialWeight + noiseWeight)
```

如果总权重为 0，则回退到 `radial01`。

### 4. 用阈值阶梯把混合值映射成 4 档硬度

混合值 `blend01` 用 3 个阈值映射：

- `< stoneThreshold` => `Soil`
- `>= stoneThreshold && < hardRockThreshold` => `Stone`
- `>= hardRockThreshold && < ultraHardThreshold` => `HardRock`
- `>= ultraHardThreshold` => `UltraHard`

阈值在 settings 构造阶段会被自动夹紧并保证单调：

```text
stone <= hardRock <= ultraHard
```

这样既能给设计留出“阶梯”调整空间，也避免 Inspector 输入无效组合。

### 5. 外圈强制全 `UltraHard`

仅靠噪声混合会让远处偶尔出现回落到 `Stone` / `HardRock` 的孤岛，不符合“远到一定程度全是最硬的”要求。因此增加一个独立规则：

- 当 `radial01 >= forcedUltraHardDistanceNormalized`
- 且该格是 `MineableWall`
- 则无视噪声，直接返回 `UltraHard`

这让地图外环有一个稳定、明确的终局硬度带。

### 6. 保留起步软岩带

更大地图 + 噪声如果直接作用到出生区外一圈，会让玩家开局第一圈就抽到高硬度墙，破坏节奏。因此在安全空腔外保留一段固定软岩带：

- `ChebyshevDistance(position, spawn) <= safeRadius + 2`
- 则该可挖岩体直接是 `Soil`

这不是新的策划层可调项，而是为了维持现有前期推进和测试稳定性的安全约束。

### 7. 奖励表补上 `UltraHard`

既然程序生成会稳定产出 `UltraHard`，默认奖励函数也必须覆盖它。做法保持现有“硬度越高，基础奖励越高”的语义，在 `GetReward(...)` 里补上 `UltraHard` 档位。

## 测试策略

- EditMode：
  - 验证默认 `MapGenerationSettings.CreateDefault()` 解析为 `240x240`
  - 验证外圈强制超硬半径之外的可挖岩体为 `UltraHard`
  - 验证混合规则能在默认或定制 settings 下生成多档硬度，而不是退化成单一层
- Unity MCP：
  - 至少跑一次 `unity_compile(exitPlayMode:true)`，确保新的配置类型和生成规则不会破坏运行时/测试程序集编译
