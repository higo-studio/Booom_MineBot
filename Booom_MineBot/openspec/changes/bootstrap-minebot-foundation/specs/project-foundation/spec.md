## ADDED Requirements

### Requirement: Bootstrap 流程负责初始化可玩运行时
项目 SHALL 提供一个 Bootstrap 入口流程，在 Gameplay 场景变为可交互之前完成共享配置、输入绑定和核心运行时服务初始化。

#### Scenario: 从项目入口场景启动
- **WHEN** 游戏从项目配置的入口场景启动
- **THEN** Bootstrap 流程会先初始化核心配置和输入服务，再加载 Gameplay 场景

#### Scenario: 进入主玩法循环
- **WHEN** Gameplay 场景完成加载
- **THEN** 玩家循环、UI 表现层和运行时系统都能直接访问已初始化的服务，而不依赖临时场景单例

### Requirement: 代码必须按能力边界划分程序集
仓库 SHALL 将 Runtime、Editor 和 Tests 代码拆分为显式 asmdef 模块，并且这些模块要与 Minebot 的能力边界保持一致。

#### Scenario: 新增玩法代码
- **WHEN** 开发者添加新的玩法系统
- **THEN** 该代码属于具名运行时程序集，而不是默认的全局程序集

#### Scenario: 修改纯 UI 表现代码
- **WHEN** 开发者只修改 UI 表现层代码
- **THEN** 玩法规则程序集不需要反向依赖 UI 程序集即可完成编译

### Requirement: 必须提供基础自动化验证入口
项目 SHALL 为确定性玩法规则和 Bootstrap 烟雾验证提供 EditMode 与 PlayMode 测试入口。

#### Scenario: 验证确定性规则逻辑
- **WHEN** 开发者运行网格、风险或波次规则的 EditMode 测试
- **THEN** 这些测试无需依赖场景专属对象或手动 Inspector 配置即可执行

#### Scenario: 验证启动流程完整性
- **WHEN** 开发者运行 PlayMode 烟雾测试集
- **THEN** 项目能够验证 Bootstrap 流程可无初始化错误地进入 Gameplay 场景

### Requirement: 仓库内必须提供 Agent 项目约束文档
仓库 SHALL 包含本地项目 skill 文档，用于总结策划来源、确认的技术栈、OpenSpec 工作流和后续 Agent 任务的验证路径。

#### Scenario: 开始新的规划或实现任务
- **WHEN** Agent 在此仓库内被要求执行 Minebot 相关工作
- **THEN** 仓库会提供一个项目内 skill，引导 Agent 读取策划来源、模块边界和预期工作流
