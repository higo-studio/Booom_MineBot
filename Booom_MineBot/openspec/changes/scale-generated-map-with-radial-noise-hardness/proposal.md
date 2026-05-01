## Why

当前默认生成地图仍然是 `12x12` 的小尺寸，并且岩体硬度只按出生点曼哈顿距离做简单三档分层。这套规则足够做早期挖掘原型，但已经不适合现在的玩法目标：地图太小，远近层次太快耗尽；硬度分布过于规则，缺少“同心圆总体递进 + 局部矿脉噪声起伏”的探索感。现在需要把默认生成图放大到当前的 `20x`，并把硬度生成改成“径向渐变叠加柏林噪声”的可配置模型，同时保证足够远处一定进入 `UltraHard` 外圈。

## What Changes

- 将默认生成地图从当前尺寸扩展为“基础尺寸 * 倍率”的可配置结果，默认采用当前 `12x12` 的 `20x` 倍率。
- 在 `BootstrapConfig` 中增加生成地图配置，使地图大小、径向权重、噪声权重、噪声采样尺度、硬度阶梯阈值和“远处强制全超硬”半径都可调。
- 将 `MapGenerator` 的硬度分布从固定距离分档改为“同心圆渐变 + Perlin Noise”的混合评分。
- 在指定归一化半径之外，所有可挖岩体强制生成 `UltraHard`，不再受噪声扰动回落。
- 保留出生安全空腔和起步可挖软岩带，避免大地图和噪声分布直接破坏前期推进节奏。

## Capabilities

### New Capabilities

- `procedural-generated-map-hardness`: 支持大尺寸程序地图、径向/噪声混合硬度分布、外圈强制超硬带和配置化地图尺寸。

### Modified Capabilities

- `grid-mining-loop`: 默认生成地图不再使用固定曼哈顿分档，而改为可配置的大地图硬度场。
- `project-foundation`: `BootstrapConfig` 在未指定 `DefaultMap` 时，改为驱动新的程序地图生成配置。

## Impact

- 受影响代码主要在 `Assets/Scripts/Runtime/GridMining`、`Bootstrap` 和对应 EditMode 测试。
- 不改变 `MapDefinition -> LogicalGridState` 的真相边界；仅改变“未提供 authored map 时”的默认生成规则。
- 不在本轮引入新的地图编辑器或运行时分块流送；只改程序生成参数和默认生成结果。
