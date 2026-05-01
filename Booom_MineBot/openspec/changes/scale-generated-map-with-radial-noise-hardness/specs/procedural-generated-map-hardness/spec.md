## ADDED Requirements

### Requirement: 默认程序地图必须支持可配置的大尺寸

当运行时未提供 authored `MapDefinition` 时，系统 SHALL 根据配置化的程序地图参数生成默认地图尺寸，而不是继续写死当前的小尺寸。

#### Scenario: 使用默认 20x 生成图尺寸

- **GIVEN** 运行时没有提供 `DefaultMap`
- **AND** 生成地图配置的 `baseSize = 12x12`
- **AND** 生成地图配置的 `sizeMultiplier = 20`
- **WHEN** 系统初始化默认程序地图
- **THEN** 生成出的 `LogicalGridState.Size` 等于 `240x240`
- **AND** 玩家出生点位于该尺寸中心附近的安全空腔中

### Requirement: 程序岩体硬度必须支持径向渐变叠加柏林噪声

程序地图中的可挖岩体 SHALL 使用“径向渐变 + Perlin Noise”的混合评分决定硬度，而不是只按固定距离硬切分层。

#### Scenario: 混合评分产生多档硬度分布

- **GIVEN** 一组启用了非零径向权重和非零噪声权重的程序地图配置
- **AND** 配置提供 `Soil / Stone / HardRock / UltraHard` 的阶梯阈值
- **WHEN** 系统生成默认程序地图
- **THEN** 生成出的可挖岩体会按混合评分落入多个硬度档位
- **AND** 同一距离环内允许出现由噪声引起的局部硬度起伏

### Requirement: 足够远的程序岩体必须强制成为超硬岩

当程序岩体距离出生点超过配置指定的归一化半径时，系统 SHALL 直接将其视为 `UltraHard`，不允许被噪声拉回较低硬度。

#### Scenario: 外圈强制超硬覆盖噪声扰动

- **GIVEN** 程序地图配置指定了 `forcedUltraHardDistanceNormalized`
- **WHEN** 某个可挖岩体的径向归一化距离大于或等于该值
- **THEN** 该格的 `HardnessTier` 为 `UltraHard`
- **AND** 该结果不受柏林噪声采样值影响
