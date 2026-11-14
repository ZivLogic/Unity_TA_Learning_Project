------

# 架构优点

> 本文档从**技术可验证**的角度，梳理本项目架构的强势点。
> 分三层：**结构性优点**（架构级，最根本）→ **模块级优点**（各系统技术亮点）→ **工程化优点**（开发/维护收益）。
> 每条均注明代码依据，可直接对照源码查证。
>
> 深层设计心得（"事件=显式信息，意义=加密信息"等认知）不在本文档，见作者的《开发历程 / 个人心得》。

------

## 一、结构性优点（架构级，最根本）

这四条是整套架构的"骨架"，决定了其他所有模块怎么长。**它们比任何单个模块的技术亮点都更根本。**

### A1. 实体被动式接收：自己管理自己，没有全局上帝表

**是什么**
实体不主动去任何"全局查询表"里查信息。实体自己记录自己的状态，通道只负责把信息**送到实体面前**，实体被选中/被注册/被投递时才响应。

**代码依据**

- `Entity`（F_001）不持有任何"全局查询接口"，四维状态（`Lifecycle`/`GameFlow`/`Action`/`StateTags`）是实体自己的字段
- `MasterFactory` 的四张索引表（`_entitiesByUUID`/`_entitiesByTag`/`_entitiesByBlueprint`/`_entitiesByType`）是**工厂内部索引**，不是"实体查询全局表"——实体自己不会去查，是外部系统（`InputMediator`/`LogicStateManager`/`CameraEntryBase`）在需要时查
- `InputMediator.Update`（I_024）：拉 `InputManager.GetStandardOutput()` → 给 `CameraEntry` → 拿 `OutputResultBase` → **按 `ReceiverUuids` 精准转发**给实体，实体不轮询
- `EventManager.DispatchToTargets`（E_007）：按 UUID 找 `EventEntryBase` → `entry.Receive(key, evt)`，**实体不主动查事件**
- `LogicStateManager.LoadContainer`（L_002）：按 UUID 找实体 → 找脚本 → `LoadFromEntry`，**实体不主动拉状态**

**对比反模式**

- ❌ 全局 `GameManager.GetEntity(id)`：实体每帧去查"我是谁、我该干嘛"
- ❌ 实体 Update 里 `foreach (var e in AllEntities) if (e.Tag == ...)`：实体自己遍历全局表过滤
- ❌ 全局 `EventBus` 广播，实体收到后 `if (evt.EventName == "XXX")` 全量判断

**收益**

- 实体职责单一：**只管自己的状态和逻辑**，不关心"全局有什么"
- 无全局表 = 无全局锁/无全局遍历 = 天然并行友好、天然可单元测试
- 实体可独立创建/销毁/池化，不影响其他实体

------

### A2. 入口注册：保证实体精准投递

**是什么**
只有**注册过**的实体才会被投递事件/输入。未注册的实体完全不参与事件流，连"收到后判断不是自己"的开销都没有。

**代码依据**

- `EventEntryBase.Initialize`（E_005）：`EventManager.Instance?.RegisterTarget(Entity.UUID, this)` → `EventManager._uuidIndex[uuid] = entry`
- `EventManager.DispatchToTargets`（E_007）：`FindEntry(uuid)` 查 `_uuidIndex`，**没注册的 UUID 直接 `LogWarning` 跳过**
- `EventManager.DispatchGlobal`（E_007）：只遍历 `_globalEntries`（注册过的全局入口），**未注册实体不在列表里**
- `MasterFactory.DestroyEntity` → `entry.Shutdown()` → `UnregisterTarget(UUID)`，**销毁即反注册**

**对比反模式**

- ❌ 广播给所有实体，实体自己 `if (uuid == targetUuid)` 过滤
- ❌ 全局监听器列表，每次派发遍历所有监听者（不管是否相关）

**收益**

- **投递成本与实体总数无关**，只与目标数有关（单投 O(1)，多投 O(n_targets)）
- 未注册实体**零开销**（不在任何索引里）
- 注册/反注册与实体生命周期绑定（`Initialize`/`Shutdown`），不会出现"已销毁实体还收到事件"

------

### A3. 方法注册：保证逻辑精准投递

**是什么**
入口自己注册"**我能处理什么**"，通道来了查表命中才执行。未注册的方法**永远不会被调用**，没有全量 `switch`/`if-else`。

**代码依据**

- `EventEntryBase.RegisterMethod`（E_005）：`_table[$"{GetType().Name}.{methodName}"] = handler`
- `EventEntryBase.Receive`（E_005）：`_table.TryGetValue(key, out var handler)`，命中才 `handler.Invoke(evt)`
- partial 生成代码（E_015 `Gen_TestEntry`）：`RegisterMethod("TestEventListen", evt => TestEventListen(...))` —— **编译期就把"方法名 → 调用"绑好**
- `EntityInputEntry.RegisterInput`（I_022）：`_inputTable[action] = handler`，`ReceiveInput` 查表
- `CameraEntryBase.RegisterIntent`（I_020）：`_intentTable[action] = handler`，`ReceiveInput` 查表

**对比反模式**

- ❌ `void OnEvent(PackageEvent evt) { switch (evt.EventName) { case "A": ... case "B": ... } }`
- ❌ `if (action == "Attack") ... else if (action == "Interact") ...`
- ❌ 每个实体都实现全量 `IEventHandler` 接口，大量空方法

**收益**

- **加一个处理方法 = 加一个 `RegisterMethod`/`RegisterInput` + 一个方法**，不改入口主逻辑
- 查表是 O(1) 字典查找，**与处理方法数量无关**
- 未注册方法**零开销**，不会进任何分支判断
- partial 生成代码让"配置的方法"和"实际方法"在**编译期对齐**，写错方法名编译就报错

------

### A4. 三条合起来：彻底避免"全局上帝"

A1 + A2 + A3 = **"三不"**：

| 不                | 对应优点    | 代码体现                                             |
| :---------------- | :---------- | :--------------------------------------------------- |
| **不查全局表**    | A1 被动接收 | 实体不调 `MasterFactory.FindXxx`，由外部系统查后投递 |
| **不广播过滤**    | A2 入口注册 | 只投递 `_uuidIndex`/`_globalEntries` 里的实体        |
| **不全量 switch** | A3 方法注册 | 查 `_table`/`_inputTable`/`_intentTable`，命中才执行 |

**这是本项目最根本的结构性优势**：没有"上帝对象"知道所有实体、没有"全局总线"广播一切、没有"巨型 switch"处理一切。每个实体自己管理自己、自己记录自己、自己声明能处理什么，通道只做精准投递。

**对比常见的"全局上帝"反模式**：

- 全局 `GameManager` 持有所有实体列表 + 所有系统引用 → 上帝对象
- 全局 `EventBus.Publish("XXX")` → 所有订阅者收到 → 广播过滤
- 实体 `Update` 里查全局状态 → 实体主动拉

------

### A5. 单一权威来源

**是什么**
每个关键操作只有一个权威入口，杜绝"两个地方都能改同一份数据"。

**代码依据**

| 操作     | 权威来源                                   |
| :------- | :----------------------------------------- |
| 实体销毁 | `MasterFactory.DestroyEntity`（F_012）     |
| 实体查询 | `MasterFactory` 的四张索引表               |
| 脚本注册 | `MasterFactory.RegisterScript`（F_012）    |
| 事件发布 | 一个事件只有一个发布方法（Q49）            |
| 状态保存 | `LogicStateManager.SaveAllStates`（L_002） |
| 配置加载 | `ConfigManager.LoadAllConfigs`（C_005）    |
| 输入输出 | `InputManager.GetStandardOutput`（I_018）  |
| 退出标记 | `AppLifecycle.MarkQuitting`（G_002）       |

**收益**

- 数据流单向，容易追踪
- 不会出现"两个系统改同一份数据导致状态不一致"
- 调试时只需在一个地方打断点

**代价**

- 权威来源必须被严格遵守，一旦有地方绕过就出 bug（如 Q31 `CharacterInputEntry.Update` 绕过输入状态判断）

------

### A6. 分层清晰，系统间低耦合高内聚

**是什么**
七个系统（配置/工厂/逻辑状态/输入/事件/总控/上层脚本）各司其职，系统间通过**窄接口**对接，没有系统直接持有另一个系统的内部数据结构。

**代码依据**

| 对接            | 接口                                                         |
| :-------------- | :----------------------------------------------------------- |
| 工厂 ↔ 配置     | `ConfigAPI.Get<T>()` / `ConfigManager.FindByKey`             |
| 工厂 ↔ 事件     | `EventEntryBase.Initialize` / `Shutdown`                     |
| 输入 ↔ 工厂     | `MasterFactory.FindByUUID` / `FindAllByTag`                  |
| 逻辑状态 ↔ 工厂 | `MasterFactory.GetScriptsByType` / `FindByUUID`              |
| 输入 ↔ 事件     | `EntityInputEntry` + `INeedEventEntry` 注入 `EventEntryBase` |

**收益**

- 加一个新系统（如网络）只需挂到 `GameManager.CreateSystem`，不侵入现有代码
- 单个系统可独立测试（如 `EventManager` 不依赖 `MasterFactory` 也能跑）
- 依赖关系集中在 `GameManager` 一处，不散落

------

## 二、模块级优点（各系统技术亮点）

### B1. 配置系统：元配置驱动，零注册

**亮点**

- `[Config(configId, jsonPath, priority, dependsOn)]` 元信息描述配置类，`ConfigManager` 自动扫描全程序集发现配置，**零手工注册**
- `ValidateDependencies` 做**三件事**：依赖存在性 + 优先级合法性 + 循环依赖 DFS 检测（很多项目只做前两者甚至不做）
- `dependsOn` + `Priority` 自动排序加载顺序
- 配置引用配置（蓝图引用 physicsConfigId/materialId/logicScriptIds），**数据驱动数据**

**代码依据**：C_003 `ConfigAttribute`、C_005 `ConfigManager.ScanAllConfigClasses/ValidateDependencies/SortByPriority`

**收益**：加一个配置类 = 加 `[Config]` + 写 JSON，不改任何管理代码

------

### B2. 工厂系统：声明式配置 + 流水线

**亮点**

- 蓝图（`entity_blueprints`/`ui_blueprints`）声明"要什么"，8 步工厂流水线按固定顺序"怎么造"
- `IFactoryStep.Process(Entity)` 统一接口，`MasterFactory` 用 `List<IFactoryStep>` 顺序执行，**步骤可插拔**
- `EntityBlueprintBase` 抽共性（blueprintId/tag/defaultHeight/logicScriptIds/physicsConfigId/materialId/audioIds/poolId），子类加特质（`EntityBlueprint.modelPath` / `UIBlueprint.prefabPath`）
- 普通实体和 UI 实体**走同一套流水线**，部分步骤按蓝图类型跳过

**代码依据**：F_012 `MasterFactory.RegisterFactories`、F_005/F_008/F_006/F_010/F_004/F_009/F_011/F_007 各工厂

**收益**：加一种新实体 = 加一条蓝图配置 + 可选加一个工厂步骤，不改核心流程

------

### B3. 四层实体结构 + 四维状态黑板

**亮点**

- Root → Logic → View + Audio 四层，物理/逻辑挂 Logic，渲染挂 View，音频挂 Audio，Root 做统一标识和变换
- `Entity` 四维状态正交拆分：`Lifecycle`（生死）/ `GameFlow`（流程）/ `Action`（动作）/ `StateTags`（Buff）
- `CanAct` / `IsAlive` 快捷组合判断，避免业务层到处写 `if (alive && !stunned && ...)`
- `Action` 变更触发 `OnActionChanged` 事件，外部（动画/AI）可监听

**代码依据**：F_012 `CreateEntitySkeleton`、F_001 `Entity`、F_002 枚举

**收益**：关注点分离到极致；状态变更可监听；组合判断集中

------

### B4. 输入系统：配置驱动状态机 + 权威快照双轨

**亮点**

- 五层解耦：**上下文**（GameWorld/ShopPanel）× **模式**（ForbidAll/OnlyViewDirect/NormalOperate）× **状态机**（Base 互斥 + Additive 叠加）× **拦截器**（白/黑名单 AND）× **意图**（Intent 批量变更）
- `InputSnapshotBuffer` 双帧快照提供 Down/Hold/Up/None 三态
- `ProcessBind` 四道关卡：`ListenState` → `Context` → `Interceptor` → `Scope` → `CD`
- **组合优先 + 单键兜底 + 50ms 释放缓冲 + `AllowPassthrough` 第三键穿透**
- **两套状态哲学**：持续状态（移动/瞄准）用权威快照轮询 `IsActionHeld`，瞬时机（攻击/交互）用事件投递 `InputOutputPayload`
- `SelectScope` 只表达"选什么范围"，不选实体，选实体由 `CameraEntryBase.ResolveByScope` 完成

**代码依据**：I_002~I_008、I_018 `InputManager`、I_020 `CameraEntryBase`

**收益**：同一按键不同情境映射不同功能，改配置即改行为；权威快照解决"幽灵状态残留"

------

### B5. 事件系统：编辑器驱动 + 代码生成 + 双轨投递

**亮点**

- `EventEditorWindow`（4 Tab：总览/编辑/系统ID/日志）编辑器配置事件
- `EventCodeGenerator` 一键生成 `E_事件名.cs`（强类型 payload）+ `Gen_类名.cs`（partial override RegisterEvents）
- `EventReflectTool` 扫 `[EventPublishAttr]`/`[EventListenAttr]`
- `EventSystemManager` 系统层开关（CanPublish/CanListen，后者待接入）
- `KeyConnection` 字段映射让**发布参数名和监听参数名可以不同**（`value` ↔ `vle`）
- `TypeNameHandling.All` 支持**自定义类型跨事件传递**（S_005/S_006 验证）
- **双轨投递**：`EmitToTargets`（UUID 定点）+ `EmitGlobal`（全局广播）
- **编辑器时计算，运行时轻量**：配置、代码生成、映射校验都在编辑器完成，运行时只读配置 + 反射调用

**代码依据**：E_012 `EventEditorWindow`、E_013 `EventCodeGenerator`、E_007 `EventManager`、E_009 `EventUtil`

**收益**：加一个事件 = 编辑器配 + 一键生成 + 编译即用；发布方和监听方参数名解耦

------

### B6. 逻辑状态系统：脚本自管数据 + 统一注入

**亮点**

- `LogicScriptBase` 抽象 `LoadFromEntry`/`SaveToEntry`/`OnAllStateInjected`
- 管理器只负责**传输**，不关心数据结构，**脚本自己拆数据**
- 父容器配置（`PawnAttackRangeStateContainer` 等）实现 `ConfigDefinition` + 内嵌 `ILogicStateEntry`
- `NotifyAllScriptsReady` 统一回调 `OnAllStateInjected`，给需要"读完配置才能初始化"的脚本明确就绪时机
- 加一个有状态脚本 = 加父容器配置 + 脚本两方法 + 注册表一行

**代码依据**：L_001 `LogicScriptBase`、L_002 `LogicStateManager`

**收益**：数据与逻辑分离；状态可独立存档/热更/回滚

------

### B7. 对象池：预热 + 差量入池 + 批量操作

**亮点**

- `PoolFactory` 的 `ObjectPool`（Queue，maxSize/autoExpand）
- `BeginPrewarm`/`EndPrewarm` + `ProductionLauncher.ProduceWithPrewarm`：建 count 个，前 spawnCount 个激活，其余入池
- `_entityPoolMap` 双表（池字典 + 实体归属映射），实体销毁和池同步解耦
- 批量操作按 UUID/Tag/Blueprint 三种维度
- `EntityManager` 作为业务层不直接碰 `MasterFactory` 的中间层

**代码依据**：F_009 `PoolFactory`、F_013 `ProductionLauncher`、F_014 `EntityManager`

**收益**：预热 + 差量入池避免运行时 Instantiate 卡顿

------

### B8. 退出安全：AppLifecycle 全局标记

**亮点**

- `AppLifecycle.IsQuitting` 全局标记，所有涉及"销毁/重建"的代码先查它
- `EntityMono.OnDestroy` / `MasterFactory.OnEntityMonoDestroyed` 里 `if (AppLifecycle.IsQuitting) return;`
- `MasterFactory.OnApplicationQuit` 里 `MarkQuitting` + 保存状态 + 清理池
- 解决"退出时单例被销毁又重建"的经典坑

**代码依据**：G_002 `AppLifecycle`、F_003 `EntityMono`、F_012 `MasterFactory`

**收益**：退出阶段状态不异常

------

### B9. 总控：统一编排启动顺序

**亮点**

- `GameManager.Awake` 里 `CreateSystem<T>()` 按依赖顺序创建 9 个系统
- 依赖关系集中在一处，不散落在各系统 `Awake`
- 顺序：`AppLifecycleInitializer → EventManager → InputManager → ConfigManager → MasterFactory → LogicStateManager → ProductionLauncher → EntityManager → InputMediator`

**代码依据**：G_001 `GameManager`

**收益**：启动顺序显式化，加系统只需在 `GameManager` 加一行

------

## 三、工程化优点（开发/维护收益）

### C1. 渐进式验证，先跑通再优化

- UI 工厂/对象池/改键逻辑还在验证阶段，先跑通核心链路
- 优点：快速验证核心链路，避免过早优化
- 代价：留了大量"关注点"（见《已知问题与待修点》）

### C2. 规范优先，防御性编程

- 对无法用类型系统约束的约定（`UUID`/`UUIDS` 大写、`entries` 字段名、类名全局唯一），用"硬规范 + 文档约束"
- 优点：简单直接
- 代价：静默失败（未配事件的方法不发布、未映射的字段生成代码会编译报错）

### C3. 编辑器是系统的一部分

- 事件系统的编辑器（`EventEditorWindow` + `EventCodeGenerator`）不是辅助工具，是核心组件
- 配置、代码生成、方法扫描、映射校验都在编辑器完成，运行时只做"读配置 + 反射调用"
- 优点：运行时轻量，加事件零手写
- 对比：很多"运行时反射一切"的事件系统

### C4. 双模式共存（蓝图组装 + 预制体组装）

- 简单实体走"蓝图 + 工厂流水线"（配置驱动）
- 复杂实体（人有多模型/材质/碰撞体）走"预制体 + 工厂第 1 步"（预制体驱动），后续配置留空，脚本运行时 `GetComponent` 自取
- 优点：简单实体配置化，复杂实体灵活化

### C5. 平行体系，互不污染

- 主配置系统（`ConfigAttribute`/`ConfigManager`）和输入配置系统（`InputConfigAttribute`/`InputConfigLoader`）**完全独立**
- 优点：输入系统的"保底配置 + 玩家存档 + 改键"不会污染主配置系统
- 代价：两套扫描/加载逻辑，有重复代码

### C6. 测试脚本与正式脚本隔离

- `TestEntry`/`TestPublish`/`TestListen`/`EVent_TEst` 等测试脚本，`CharacterInputEntry` 里 `HandleMoveDown`/`HandleMoveRight` 未注册
- 优点：测试代码可快速验证事件/自定义类型传递，不影响正式逻辑
- 代价：正式版需清理

------

## 四、优点总览（一图速查）

text

```
结构性优点（最根本）
├─ A1 实体被动式接收，无全局上帝表
├─ A2 入口注册 → 实体精准投递
├─ A3 方法注册 → 逻辑精准投递
├─ A4 三不：不查全局表 / 不广播过滤 / 不全量 switch
├─ A5 单一权威来源
└─ A6 分层清晰，低耦合高内聚

模块级优点（技术亮点）
├─ B1 配置：元配置驱动，零注册
├─ B2 工厂：声明式配置 + 流水线
├─ B3 实体：四层结构 + 四维状态
├─ B4 输入：状态机 + 权威快照双轨
├─ B5 事件：编辑器驱动 + 代码生成 + 双轨投递
├─ B6 逻辑状态：脚本自管数据 + 统一注入
├─ B7 对象池：预热 + 差量入池 + 批量
├─ B8 退出安全：AppLifecycle 全局标记
└─ B9 总控：统一编排启动顺序

工程化优点（开发/维护收益）
├─ C1 渐进式验证
├─ C2 规范优先
├─ C3 编辑器是系统的一部分
├─ C4 双模式共存
├─ C5 平行体系，互不污染
└─ C6 测试脚本与正式脚本隔离
```



------

## 五、四条结构性优点的"一句话总结"

如果只能用四句话向别人介绍这套架构：

1. **实体不查全局表**——通道把信息送到实体面前，实体自己管理自己（A1）
2. **只有注册过的实体才收投递**——没注册的实体零开销（A2）
3. **只有注册过的方法才执行**——没注册的方法零开销（A3）
4. **每条数据只有一个权威来源**——不会两个地方改同一份数据（A5）

这四条合起来 = **"彻底避免全局上帝"**。

------

**文档结束。**