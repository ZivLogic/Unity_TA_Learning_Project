# 已知问题与待修点

> 本文档汇总项目当前所有**已知问题、隐患、待补功能、设计关注点**，作为技术债看板和演进路线图。
>
> **使用方式**：每条带唯一编号，可直接在"状态"列勾选（待修 / 已确认设计 / 已修 / 暂缓）。新增问题按系统追加编号，不改旧编号。
>
> **优先级定义**：
>
> - **P0 阻塞级**：影响功能正确性或发布（打包失效、配置笔误导致功能不可用）
> - **P1 重要级**：当前能跑但存在隐患，或影响体验/性能，建议近期处理
> - **P2 优化级**：边界情况、命名规范、未使用字段、待完善细节
>
> **状态定义**：`待修` / `已确认设计`（不是 bug，是约定）/ `已修` / `暂缓`（随某功能一起做）

------

## 一、P0 阻塞级（发布前必须解决）

### ISSUE-P0-001 事件配置打包后读取失效

| 项       | 内容                                                         |
| :------- | :----------------------------------------------------------- |
| 系统     | 事件系统                                                     |
| 现象     | `EventConfigHelper.Load<T>` 用 `File.Exists/File.ReadAllText` 读 `Assets/Resources/EventConfig/*.json`，打包后该路径的原始文件不存在（被打进 `resources.assets`），`File.Exists` 返回 false，`Load<T>` 返回 `new T()`，所有事件配置为空 |
| 代码位置 | E_003 `EventConfigHelper.Load<T>`、E_007 `EventManager.Awake` → `ReloadAllFromDisk` |
| 影响     | 打包后事件系统完全不可用                                     |
| 建议方案 | 参考输入系统 `InputConfigLoader.ReadDefaultJson` 模式：编辑器读工程目录，运行时读 `Resources.Load<TextAsset>`；或改为 Addressables |
| 优先级   | P0                                                           |
| 状态     | 待修（当前仅编辑器验证阶段，接触打包时处理）                 |

------

### ISSUE-P0-002 输入系统 Area 范围模型 ID 命名不一致

| 项       | 内容                                                         |
| :------- | :----------------------------------------------------------- |
| 系统     | 输入系统（配置数据）                                         |
| 现象     | `input_area_models.json`（I_025）的 `ModelId` 是 `Sphere`/`Box`/`Cylinder`/`Cone`，但 `input_states.json`（I_031）里 `AllowAreaModelIds` 写的是 `SphereRadius`/`BoxSize`，两处命名不一致 |
| 代码位置 | I_025、I_031；校验逻辑在 I_008 `SelectScopeValidator.IsAllowed` |
| 影响     | `Area` 范围在状态允许列表里永远匹配不上，Area 选择被拦；`Peace`/`Battle`/`Home` 状态的 Area 范围实际不可用 |
| 建议方案 | 统一命名：`input_states.json` 的 `AllowAreaModelIds` 改为 `Sphere`/`Box`/`Cylinder`/`Cone`；并在文档中写死"`AllowAreaModelIds` 引用 `input_area_models.ModelId`"的依赖关系 |
| 优先级   | P0（配置数据错误，改 JSON 即可）                             |
| 状态     | 待修                                                         |

------

## 二、P1 重要级（当前能跑但有隐患）

### ISSUE-P1-001 ForbidAll 模式下实体仍可移动

| 项       | 内容                                                         |
| :------- | :----------------------------------------------------------- |
| 系统     | 输入系统 ↔ 上层脚本                                          |
| 现象     | `InputManager.Update` 里 `if (_modeMgr.IsForbidAll()) return;` 只拦截事件输出；但 `CharacterInputEntry.Update` 直接轮询 `InputManager.Instance.IsActionHeld(...)` 改 `transform.position`，**绕过模式判断**，ForbidAll 下角色仍会移动 |
| 代码位置 | I_018 `InputManager.Update`、S_005 `CharacterInputEntry.Update` |
| 影响     | 暂停/过场/菜单打开时角色仍可移动                             |
| 建议方案 | `CharacterInputEntry.Update` 开头加 `if (InputManager.Instance.ModeMgr.IsForbidAll()) return;`；或在 `IsActionHeld` 内部统一查模式 |
| 优先级   | P1                                                           |
| 状态     | 待修                                                         |

------

### ISSUE-P1-002 脚本挂载顺序依赖蓝图数组顺序（隐性依赖）

| 项       | 内容                                                         |
| :------- | :----------------------------------------------------------- |
| 系统     | 工厂系统                                                     |
| 现象     | `LogicFactory.Process` 里 `eventEntry.Initialize()` + `INeedEventEntry` 注入在 `foreach (logicScriptIds)` 循环内，靠"后续轮次补救"——如果 `CharacterInputEntry` 比 `EventEntryBase` 先挂，第一轮注入失败，挂 `EventEntryBase` 时重新遍历才补上。若实体只有 `INeedEventEntry` 没有 `EventEntryBase`，则永远拿不到入口 |
| 代码位置 | F_006 `LogicFactory.Process` 第 6、7 步                      |
| 影响     | 蓝图 `logicScriptIds` 数组顺序错会导致事件入口注入失败，`EmitFromExternal` 静默不投递 |
| 建议方案 | 循环外只 `Initialize()` 一次；或显式声明脚本依赖并拓扑排序；或文档写死"`EventEntryBase` 必须在数组里排第一位" |
| 优先级   | P1                                                           |
| 状态     | 待修（先文档约定，后续考虑拓扑排序）                         |

------

### ISSUE-P1-003 PhysicsFactory 网格碰撞体获取 Entity 方式错误

| 项       | 内容                                                         |
| :------- | :----------------------------------------------------------- |
| 系统     | 工厂系统                                                     |
| 现象     | `PhysicsFactory.AddMeshCollider` 里 `obj.GetComponentInParent<Entity>()` —— `Entity` 是纯 C# 类不是 `Component`，该行能编译但不报错（返回 null），实际拿不到网格，`MeshCollider.sharedMesh` 永远为空 |
| 代码位置 | F_008 `PhysicsFactory.AddMeshCollider`                       |
| 影响     | 配了 `type: "mesh"` 的物理配置不会生效                       |
| 建议方案 | 改为通过 `EntityMono.FindInParents(obj)` 拿到 `EntityMono`，再取 `mono.Entity.ViewObject.GetComponentInChildren<MeshFilter>()` |
| 优先级   | P1                                                           |
| 状态     | 待修                                                         |

------

### ISSUE-P1-004 事件系统 CanPublish 缓存永不命中

| 项       | 内容                                                         |
| :------- | :----------------------------------------------------------- |
| 系统     | 事件系统                                                     |
| 现象     | `EventSystemManager.CanPublishDict` 用 `Dictionary<PackageEvent, bool>` 做缓存，key 是 `PackageEvent` 实例；每次 `EmitEvent` 都是 `Activator.CreateInstance` 新实例，`GetHashCode` 不同，缓存**永远不命中** |
| 代码位置 | E_011 `EventSystemManager.CanPublish`                        |
| 影响     | 每次发布都走完整判断链（性能隐患，非功能错误）               |
| 建议方案 | key 改为 `evt.EventClassFullName`（string）                  |
| 优先级   | P1                                                           |
| 状态     | 待修（旧代码遗留，随事件系统迭代一起改）                     |

------

### ISSUE-P1-005 事件代码生成对"不使用"映射仍生成代码

| 项       | 内容                                                         |
| :------- | :----------------------------------------------------------- |
| 系统     | 事件系统（编辑器）                                           |
| 现象     | `EventCodeGenerator.BuildArgList` 里 `if (mapping != null && mapping.TryGetValue(p.Name, out var mapped)) prop = mapped;`，如果监听参数没配映射，`prop` 保持参数名 `p.Name`，生成 `((E_XXX)evt).pName`；但事件类字段来自发布方法参数，若监听参数名与发布参数名不同且没映射，生成的代码引用不存在的字段，**编译报错** |
| 代码位置 | E_013 `BuildArgList`、E_012 `IsValid`                        |
| 影响     | 用户漏配映射时编译失败（靠编译报错提示，不算静默，但体验差） |
| 建议方案 | `IsValid` 里加校验：无映射的参数必须有同名字段，否则拦截保存 |
| 优先级   | P1                                                           |
| 状态     | 待修                                                         |

------

### ISSUE-P1-006 移动直接改 transform 与 Rigidbody 冲突导致抖动

| 项       | 内容                                                         |
| :------- | :----------------------------------------------------------- |
| 系统     | 工厂系统 ↔ 上层脚本                                          |
| 现象     | `CharacterInputEntry.Update` 直接 `transform.position += ...`，但 `PhysicsFactory` 给 `LogicObject` 加了 `Rigidbody`，Rigidbody 下直接改 transform 可能引起物理抖动/警告 |
| 代码位置 | S_005 `CharacterInputEntry.Update`、F_008 `AddRigidbody`     |
| 影响     | 移动时碰撞抖动（已确认实际存在）                             |
| 建议方案 | 移动改用 `Rigidbody.MovePosition`；或移动类实体 `isKinematic=true` 只走 transform；或物理配置区分"逻辑实体"和"物理实体" |
| 优先级   | P1                                                           |
| 状态     | 待修（设计上"统一走 LogicObject transform"成立，代价是抖动，后续优化） |

------

### ISSUE-P1-007 退出保存顺序不保证

| 项       | 内容                                                         |
| :------- | :----------------------------------------------------------- |
| 系统     | 总控 ↔ 逻辑状态系统                                          |
| 现象     | `MasterFactory.OnApplicationQuit` 调 `LogicStateManager.Instance.SaveAllStates()`，但 Unity 不保证各 MonoBehaviour 的 `OnApplicationQuit` 顺序；若 `LogicStateManager` 先销毁，`Instance` 为 null，保存不执行 |
| 代码位置 | F_012 `MasterFactory.OnApplicationQuit`、L_002 `LogicStateManager` |
| 影响     | 退出时状态可能丢失                                           |
| 建议方案 | 用 `Application.wantsToQuit` 统一编排退出流程；或 `GameManager.OnApplicationQuit` 里按顺序显式调用各系统保存 |
| 优先级   | P1                                                           |
| 状态     | 待修                                                         |

------

### ISSUE-P1-008 输入配置打包导出未自动化

| 项       | 内容                                                         |
| :------- | :----------------------------------------------------------- |
| 系统     | 输入系统                                                     |
| 现象     | `InputConfigLoader.ExportDefaultToResources` 是编辑器工具，需**手动**把工程目录 `Assets/Input/ConfigData/Default` 复制到 `Resources/InputConfig/Default`；无 Build 前 Hook 自动执行 |
| 代码位置 | I_014 `ExportDefaultToResources`                             |
| 影响     | 打包前忘记导出会导致运行时读不到保底配置                     |
| 建议方案 | 加 `IPreprocessBuildWithReport` 在打包前自动调用；或文档写死"打包前必须执行 Tools 菜单" |
| 优先级   | P1（随打包流程一起做）                                       |
| 状态     | 待修                                                         |

------

### ISSUE-P1-009 GameManager 初始化硬编码 0.5 秒延迟

| 项       | 内容                                                         |
| :------- | :----------------------------------------------------------- |
| 系统     | 总控                                                         |
| 现象     | `GameManager.Start` 里 `Invoke("InitGame", 0.5f)`，硬编码延迟"等配置加载完" |
| 代码位置 | G_001 `GameManager.Start`                                    |
| 影响     | 配置加载慢则不够，快则浪费 0.5 秒                            |
| 建议方案 | 改为事件驱动（`ConfigManager.OnAllConfigsLoaded` 回调触发）；或引入异步资源加载器后统一编排 |
| 优先级   | P1（随异步加载器一起做）                                     |
| 状态     | 待修                                                         |

------

## 三、P2 优化级（边界情况 / 命名规范 / 未使用字段）

### ISSUE-P2-001 ConfigAttribute 默认 jsonPath 大小写

| 项       | 内容                                                         |
| :------- | :----------------------------------------------------------- |
| 系统     | 配置系统                                                     |
| 现象     | `ConfigAttribute` 构造里 `JsonPath = jsonPath ?? $"{configId}.Json"`（大写 J），实际因构造时已赋值，`??` 右边不执行；Windows 不区分大小写能跑，Linux/Mac 打包会出问题 |
| 代码位置 | C_003 `ConfigAttribute` 构造                                 |
| 建议     | 统一改小写 `.json`                                           |
| 状态     | 待修                                                         |

------

### ISSUE-P2-002 ConfigManager.FindByKey 只找第一个数组字段

| 项       | 内容                                                         |
| :------- | :----------------------------------------------------------- |
| 系统     | 配置系统                                                     |
| 现象     | `FindByKey` 用 `config.GetType().GetFields().FirstOrDefault(f => f.FieldType.IsArray)`，配置类有多个数组字段时会取错 |
| 代码位置 | C_005 `ConfigManager.FindByKey`                              |
| 现状     | 当前所有配置类只有一个数组字段，暂无影响                     |
| 建议     | 后续多数组配置时改为按 keyField 反射找对应数组               |
| 状态     | 暂缓（观察是否出现多数组配置）                               |

------

### ISSUE-P2-003 ConfigParser @ref: 语法预留未使用

| 项       | 内容                                                         |
| :------- | :----------------------------------------------------------- |
| 系统     | 配置系统                                                     |
| 现象     | `ConfigParser.ResolveReferences` 支持 `@ref:configId.field[index].field`，但实际项目未使用 |
| 代码位置 | C_006 `ConfigParser`、C_005 `ConfigManager.ResolveReference` |
| 建议     | 文档标"预留能力"，暂不删除                                   |
| 状态     | 已确认设计（预留）                                           |

------

### ISSUE-P2-004 地图缓存 / 地形配置预留未启用

| 项       | 内容                                                         |
| :------- | :----------------------------------------------------------- |
| 系统     | 配置系统 / 工厂系统                                          |
| 现象     | `map_cache` 配置有定义无使用；`terrain_configs` 计划删除（地形改预制体化）；`MapFactory` 整体不启用（`GameManager.InitGame` 里 `LoadMap` 被注释） |
| 代码位置 | C_001 `MapCacheConfig`/`TerrainConfigs`、F_007 `MapFactory`、G_001 `GameManager.InitGame` |
| 建议     | 随地图编辑器一起重做：地形预制体化、地图编辑器记录位置/属性、`map_cache` 缓存化、`EntityId` 不再覆盖而是"蓝图ID + 配置表决定位置" |
| 状态     | 暂缓（随地图编辑器）                                         |

------

### ISSUE-P2-005 CameraRigScript shoulderOffset 未 Load

| 项       | 内容                                                         |
| :------- | :----------------------------------------------------------- |
| 系统     | 逻辑状态系统 / 上层脚本                                      |
| 现象     | `CameraRigScript.SaveToEntry` 写了 `shoulderOffset`，但 `LoadFromEntry` 没读，每次用脚本默认值 2.0 |
| 代码位置 | S_002 `CameraRigScript.LoadFromEntry`、S_003 `CameraRigStateContainer.CameraRigEntry` |
| 建议     | `LoadFromEntry` 加 `shoulderOffset = data.shoulderOffset;`；容器已定义该字段 |
| 状态     | 待修                                                         |

------

### ISSUE-P2-006 StateMachineScript 限时 Buff 不存盘

| 项       | 内容                                                         |
| :------- | :----------------------------------------------------------- |
| 系统     | 逻辑状态系统 / 上层脚本                                      |
| 现象     | `_timedTags` 剩余时间不存盘，只存 `Tags`；`SaveToEntry` 不存时间戳 |
| 代码位置 | S_011 `StateMachineScript`                                   |
| 现状     | 注释写明"重启游戏一般直接清空"；示例状态机                   |
| 建议     | 后续加时间戳字段（`List<(string tag, float remain)>`）       |
| 状态     | 暂缓（示例状态机，正式版按需加）                             |

------

### ISSUE-P2-007 StateMachineScript 枚举 TryParse 区分大小写

| 项       | 内容                                                         |
| :------- | :----------------------------------------------------------- |
| 系统     | 逻辑状态系统 / 上层脚本                                      |
| 现象     | `Enum.TryParse(data.Lifecycle, out ...)` 默认区分大小写；`SaveToEntry` 存 PascalCase 能匹配，手改 JSON 写小写会解析失败走保底 |
| 代码位置 | S_011 `StateMachineScript.LoadFromEntry`                     |
| 建议     | 改 `Enum.TryParse(data.Lifecycle, true, out ...)` 忽略大小写；或文档约定"手改配置必须 PascalCase" |
| 状态     | 待修                                                         |

------

### ISSUE-P2-008 InputConfigAutoLoader 只扫当前程序集

| 项       | 内容                                                         |
| :------- | :----------------------------------------------------------- |
| 系统     | 输入系统                                                     |
| 现象     | `ScanInputConfigTypes` 只扫 `Assembly.GetExecutingAssembly()`，主配置系统扫所有程序集 |
| 代码位置 | I_011 `InputConfigAutoLoader.ScanInputConfigTypes`           |
| 现状     | 当前输入配置类都在 Assembly-CSharp，无影响；拆 asmdef 后会漏 |
| 建议     | 改为扫 `AppDomain.CurrentDomain.GetAssemblies()`，与主配置系统一致 |
| 状态     | 暂缓（拆 asmdef 时处理）                                     |

------

### ISSUE-P2-009 InputManager.WriteOutput 里冗余 Clear

| 项       | 内容                                                         |
| :------- | :----------------------------------------------------------- |
| 系统     | 输入系统                                                     |
| 现象     | `Update` 开头 `_currentOutput.Clear()`，`WriteOutput` 里又 Clear 一次 |
| 代码位置 | I_018 `InputManager.Update`/`WriteOutput`                    |
| 建议     | 保留（防御性保底，防止意外调用），无需改                     |
| 状态     | 已确认设计                                                   |

------

### ISSUE-P2-010 EntityInputEvent 字段未填全

| 项       | 内容                                                         |
| :------- | :----------------------------------------------------------- |
| 系统     | 输入系统 ↔ 事件系统                                          |
| 现象     | `InputMediator.Update` 组装 `EntityInputEvent` 只填 `Action/KeyState/Frame/TargetUuids`，`ActionKey/Context/Device/Param` 没填 |
| 代码位置 | I_024 `InputMediator.Update`、I_023 `EntityInputEvent`       |
| 现状     | 业务侧（`CharacterInputEntry`）只用 Action/KeyState/TargetUuids，够用 |
| 建议     | 后续有需要再补，注意 `output` 是单例复用，要拷贝值           |
| 状态     | 暂缓                                                         |

------

### ISSUE-P2-011 CameraEntry_Default OnSelect / 区域选择是旧测试代码

| 项       | 内容                                                         |
| :------- | :----------------------------------------------------------- |
| 系统     | 输入系统                                                     |
| 现象     | `OnSelect` 定义了但没在 `RegisterIntents` 注册；`OnEnterSelect` 只设 `IsSelecting = true` 无消费方；`EndSelect` 定义了没被调用。P 键切换主角是旧测试逻辑 |
| 代码位置 | I_019 `CameraEntry_Default`                                  |
| 现状     | 主角靠 `PlayerTag` 自动声明（Q33），不再需要主动选择         |
| 建议     | 正式版移除 `OnSelect`/`OnEnterSelect`/`EndSelect`/`IsSelecting`/`SelectedEntity` |
| 状态     | 待清理                                                       |

------

### ISSUE-P2-012 AimLineScript 用 != Vector3.zero 判断准星

| 项       | 内容                                                         |
| :------- | :----------------------------------------------------------- |
| 系统     | 上层脚本                                                     |
| 现象     | `AimLineScript.LateUpdate` 用 `camEntry.CurrentAimPoint != Vector3.zero` 判断准星有效性；射线打中世界原点时 `CurrentAimPoint` 为零，误判为"无准星" |
| 代码位置 | S_001 `AimLineScript.LateUpdate`                             |
| 建议     | 加 `HasAimPoint` 布尔字段，或 `CameraEntry_Default` 暴露有效性标记 |
| 状态     | 待修                                                         |

------

### ISSUE-P2-013 摄像机绕过输入系统直接读 Unity Input

| 项       | 内容                                                         |
| :------- | :----------------------------------------------------------- |
| 系统     | 输入系统 ↔ 上层脚本                                          |
| 现象     | `CameraRigScript.Update` 直接用 `Input.GetAxis("Mouse X")`，`InputSnapshotBuffer.Refresh` 里也 `Input.GetAxisRaw`，两套采样独立，`CameraRigScript` 不走输入系统的 `LookAxis` |
| 代码位置 | S_002 `CameraRigScript.Update`、I_006 `InputSnapshotBuffer.Refresh` |
| 现状     | 视角控制顺畅，未接入输入系统                                 |
| 建议     | 后续若要做"输入统一管理/改键/灵敏度配置"，把 `LookAxis` 接入 `CameraRigScript` |
| 状态     | 暂缓                                                         |

------

### ISSUE-P2-014 EventUtil.BuildEvent payloadIndex 大小写敏感

| 项       | 内容                                                         |
| :------- | :----------------------------------------------------------- |
| 系统     | 事件系统                                                     |
| 现象     | `BuildEvent` 里 `if (p.Name == "UUID" || p.Name == "UUIDS") continue;` 大小写敏感；写成 `uuids` 小写不跳过，payloadIndex 错位 |
| 代码位置 | E_009 `BuildEvent`                                           |
| 现状     | 项目规范参数名大写 `UUID`/`UUIDS`，无影响                    |
| 建议     | 文档写死规范；或代码改为 `p.Name.Equals("UUID", StringComparison.OrdinalIgnoreCase)` |
| 状态     | 已确认设计（规范约束）                                       |

------

### ISSUE-P2-015 EventEntryBase.Receive key 依赖 GetType().Name

| 项       | 内容                                                         |
| :------- | :----------------------------------------------------------- |
| 系统     | 事件系统                                                     |
| 现象     | `RegisterMethod` 用 `$"{GetType().Name}.{methodName}"` 建表，配置里 `EventListen` 也是 `"类名.方法名"` 不含命名空间；若两个命名空间有同名类会冲突 |
| 代码位置 | E_005 `EventEntryBase.RegisterMethod`、E_017 配置            |
| 现状     | 全项目不用命名空间，靠"类名全局唯一"保证                     |
| 建议     | 文档写死"类名全局唯一"规范                                   |
| 状态     | 已确认设计（规范约束）                                       |

------

### ISSUE-P2-016 EventEditorWindow DrawPublishSelector KeyConnection key 不一致

| 项       | 内容                                                         |
| :------- | :----------------------------------------------------------- |
| 系统     | 事件系统（编辑器）                                           |
| 现象     | 切换发布方时 `EditingEvent.KeyConnection[newPublishMethod] = new Dictionary...`，key 用 `newPublishMethod`；但 `DrawFieldMappings` 用 `$"{EventPublish}_{listenMethod}"`，两处不一致。可能切换发布方后映射 UI 显示旧数据或空 |
| 代码位置 | E_012 `DrawPublishSelector`、`DrawFieldMappings`             |
| 现状     | 正常使用编辑器时可正常显示，未复现                           |
| 建议     | 后续若复现，统一 mappingKey 格式                             |
| 状态     | 待观察（暂未复现）                                           |

------

### ISSUE-P2-017 EventRuntimeInitializer _initialized 静态标记 Domain Reload 残留

| 项       | 内容                                                         |
| :------- | :----------------------------------------------------------- |
| 系统     | 事件系统                                                     |
| 现象     | `_initialized` 是静态标记，只初始化一次；关闭 Domain Reload（Enter Play Mode Options）时静态字段不重置，第二次进播放可能不初始化 |
| 代码位置 | E_008 `EventRuntimeInitializer`                              |
| 现状     | 未接触 Domain Reload 概念，当前默认开启 Domain Reload 无影响 |
| 建议     | 加 `[RuntimeInitializeOnLoadMethod]` 重置 `_initialized`     |
| 状态     | 待修（了解 Domain Reload 后处理）                            |

------

### ISSUE-P2-018 VersionConfigRoot 有定义无写入

| 项       | 内容                                                         |
| :------- | :----------------------------------------------------------- |
| 系统     | 事件系统                                                     |
| 现象     | `VersionConfigRoot` 有 Load/Save，但没有任何代码写入版本快照 |
| 代码位置 | E_002 `VersionConfigRoot`、E_003 `EventConfigHelper`         |
| 建议     | 预留未完成，后续做版本管理时补                               |
| 状态     | 暂缓                                                         |

------

### ISSUE-P2-019 地图工厂 EntityId 覆盖错误

| 项       | 内容                                                         |
| :------- | :----------------------------------------------------------- |
| 系统     | 工厂系统                                                     |
| 现象     | `MapFactory.PlaceEntity` 里 `entity.EntityId = placement.placementId`，覆盖了蓝图 ID；设计上实体应该只有一个蓝图 ID，位置由配置表决定 |
| 代码位置 | F_007 `MapFactory.PlaceEntity`                               |
| 现状     | 地图工厂不启用                                               |
| 建议     | 随地图编辑器重做：去掉 EntityId 覆盖，改为"蓝图ID + 配置表决定位置" |
| 状态     | 暂缓（随地图工厂）                                           |

------

### ISSUE-P2-020 ProductionLauncher 预热模式边界保护

| 项       | 内容                                                         |
| :------- | :----------------------------------------------------------- |
| 系统     | 工厂系统                                                     |
| 现象     | 预热模式 `Mathf.Min(entry.spawnCount, prewarmedEntities.Count)` 有保护；直接创建模式 `spawnCount` 多大就建多少个，无保护 |
| 代码位置 | F_013 `ProductionLauncher.ProduceWithPrewarm`/`ProduceDirectly` |
| 建议     | 直接模式加同样保护，或文档约定"spawnCount ≤ count"           |
| 状态     | 待修                                                         |

------

### ISSUE-P2-021 输入系统改键 / 存档上限未迭代

| 项       | 内容                                                         |
| :------- | :----------------------------------------------------------- |
| 系统     | 输入系统                                                     |
| 现象     | 改键只改 `Key1` 不改 `AltKey1`；存档上限 5 个、超出删最旧是简化实践，未实际使用 |
| 代码位置 | I_018 `StartRecordKey`/`FinishRecordKey`、I_015 `CommitModify` |
| 现状     | 改键逻辑基本未用，有错误一般是 bug（未迭代）                 |
| 建议     | 真正用到改键时再迭代                                         |
| 状态     | 暂缓                                                         |

------

### ISSUE-P2-022 UI 工厂 / 对象池工厂属性语义未明确

| 项       | 内容                                                         |
| :------- | :----------------------------------------------------------- |
| 系统     | 工厂系统                                                     |
| 现象     | `UIBlueprint.sortingOrder` 实际是 SiblingIndex 不是 Canvas.sortingOrder（F_011 注释写明）；`SetUILayer` 和 `SetSortingOrder` 会互相覆盖；`MaterialData.lodLevel` 未读取；对象池的"特效/UI 入池"、"池状态与逻辑状态关系"未设计 |
| 代码位置 | F_011 `UIFactory`、F_009 `PoolFactory`、C_001 `MaterialDatabaseConfig` |
| 现状     | UI/对象池工厂验证阶段，属性语义未完全理解                    |
| 建议     | 后续逐属性明确语义并写文档；`sortingOrder` 改名 `siblingIndex` 避免歧义 |
| 状态     | 待梳理                                                       |

------

### ISSUE-P2-023 EventManager.DispatchTypedToEntry 过渡代码无人调

| 项       | 内容                                                         |
| :------- | :----------------------------------------------------------- |
| 系统     | 事件系统                                                     |
| 现象     | `DispatchTypedToEntry` 注释"阶段一过渡用，阶段三后走 UUID 路由"，目前无人调用 |
| 代码位置 | E_007 `EventManager.DispatchTypedToEntry`                    |
| 建议     | 确认无人调用后删除；或保留但文档标"过渡"                     |
| 状态     | 待清理                                                       |

------

### ISSUE-P2-024 EventSystemManager.CanListen 未接入

| 项       | 内容                                                         |
| :------- | :----------------------------------------------------------- |
| 系统     | 事件系统                                                     |
| 现象     | `CanListen` 定义了但派发时没调用，监听侧系统开关未生效       |
| 代码位置 | E_011 `EventSystemManager.CanListen`、E_007 `EventManager` 各 Dispatch |
| 现状     | 预留能力，当前事件少直接改事件更快                           |
| 建议     | 后续事件多时补圆                                             |
| 状态     | 暂缓                                                         |

------

### ISSUE-P2-025 ChestInteractScript 交互提示硬编码

| 项       | 内容                                              |
| :------- | :------------------------------------------------ |
| 系统     | 上层脚本                                          |
| 现象     | `GetInteractionHint` 返回 `"交互测试_001"` 硬编码 |
| 代码位置 | S_004 `ChestInteractScript`                       |
| 建议     | 后续走配置                                        |
| 状态     | 暂缓                                              |

------

### ISSUE-P2-026 TestEntry 硬编码 UUID

| 项       | 内容                                        |
| :------- | :------------------------------------------ |
| 系统     | 上层脚本（测试）                            |
| 现象     | `TEST_001` 硬编码 `"Pawn_0003"/"Pawn_0004"` |
| 代码位置 | S_006 `TestEntry.TEST_001`                  |
| 建议     | 测试代码，正式版移除或改通用                |
| 状态     | 待清理                                      |

------

### ISSUE-P2-027 CharacterInputEntry 未注册方法

| 项       | 内容                                                         |
| :------- | :----------------------------------------------------------- |
| 系统     | 上层脚本（测试）                                             |
| 现象     | `HandleMoveDown`/`HandleMoveRight` 定义了但没在 `RegisterInputs` 注册 |
| 代码位置 | S_005 `CharacterInputEntry`                                  |
| 建议     | 测试代码，正式版移除                                         |
| 状态     | 待清理                                                       |

------

### ISSUE-P2-028 InputMediator.Start 默认主角代码被注释

| 项       | 内容                                                         |
| :------- | :----------------------------------------------------------- |
| 系统     | 输入系统                                                     |
| 现象     | 默认主角 `FindFirstByTag("Player")` 整段注释，主角靠 `PlayerTag.OnAllStateInjected` 设置 |
| 代码位置 | I_024 `InputMediator.Start`                                  |
| 现状     | 有意设计（Q33），主角由 `PlayerTag` 声明                     |
| 建议     | 删除注释代码或写明"主角由 PlayerTag 声明"                    |
| 状态     | 已确认设计                                                   |

------

## 四、待补功能（规划中，非 bug）

### FEAT-001 地图编辑器 + 地图工厂重做

- **现状**：`MapFactory` 整体不启用，地形配置计划删除，`map_cache` 预留
- **目标**：地形预制体化；地图编辑器直接记录位置/属性/碰撞体；`map_cache` 缓存化；`EntityId` 不再覆盖
- **涉及**：`MapFactory`、`MapCacheConfig`、`TerrainConfigs`、`MapPlacementsConfig`、`MapOverviewConfig`

### FEAT-002 输入系统打包逻辑

- **现状**：`ExportDefaultToResources` 手动，未接 Build Hook
- **目标**：打包前自动导出；搞清哪些资源会被编译/打进包，为热更做准备

### FEAT-003 改键完整化

- **现状**：只改 `Key1`，不改 `AltKey1`；存档上限 5 个简化
- **目标**：支持主键 + 备用键改键；存档版本管理

### FEAT-004 事件系统 CanListen 接入

- **现状**：`CanListen` 定义未调用
- **目标**：监听侧系统开关生效

### FEAT-005 逻辑状态"保底配置 + 运行时状态"双选

- **现状**：只有运行时状态（`SaveAllStates`）
- **目标**：工具主动保存调好的状态为默认；加载时按配置/状态选运行时还是保底；类似"游戏运行时保存 + 游戏重置"

### FEAT-006 对象池扩展（特效/UI 入池、池状态与逻辑状态关系）

- **现状**：当前都挂完组件再入池，逻辑状态与池状态关系未设计
- **目标**：明确"特效/UI 能否入池"、"入池时逻辑状态怎么处理"

### FEAT-007 异步资源加载器

- **现状**：`GameManager.Invoke("InitGame", 0.5f)` 硬编码延迟
- **目标**：异步加载器 + 工厂联动，事件驱动初始化

### FEAT-008 输入系统"两套状态"统一

- **现状**：持续状态用快照轮询，瞬时机用事件；`ForbidAll` 下轮询绕过拦截（ISSUE-P1-001）
- **目标**：轮询侧也查模式/上下文，或提供统一的"状态查询 + 拦截"接口

------

## 五、关注点台账（开发中标记的"留一个关注点"）

> 来自开发过程中作者标记的关注点，按系统归类，供后续追踪。

| 编号     | 系统         | 关注点                                                       | 关联 ISSUE              |
| :------- | :----------- | :----------------------------------------------------------- | :---------------------- |
| NOTE-001 | 配置系统     | `ConfigAttribute` 默认 `.Json` 大写，Linux/Mac 打包隐患      | ISSUE-P2-001            |
| NOTE-002 | 配置系统     | `FindByKey` 只找第一个数组字段，多数组时取错                 | ISSUE-P2-002            |
| NOTE-003 | 配置系统     | 父容器 ConfigId 必须与配置文件名一致（隐性路径依赖）         | —                       |
| NOTE-004 | 配置系统     | 父容器数组字段必须叫 `entries`（硬约定）                     | —                       |
| NOTE-005 | 工厂系统     | 脚本挂载顺序依赖蓝图数组顺序（隐性依赖）                     | ISSUE-P1-002            |
| NOTE-006 | 工厂系统     | `PhysicsFactory.AddMeshCollider` 的 `GetComponentInParent<Entity>()` 是 bug | ISSUE-P1-003            |
| NOTE-007 | 工厂系统     | 地图工厂 EntityId 覆盖错误，后续重做                         | ISSUE-P2-019            |
| NOTE-008 | 工厂系统     | 预热模式边界情况未考虑                                       | ISSUE-P2-020            |
| NOTE-009 | 工厂系统     | UI/对象池工厂属性语义未完全理解                              | ISSUE-P2-022            |
| NOTE-010 | 逻辑状态系统 | `CameraRigScript` 的 `shoulderOffset` 未 Load                | ISSUE-P2-005            |
| NOTE-011 | 逻辑状态系统 | 限时 Buff 时间戳不存盘                                       | ISSUE-P2-006            |
| NOTE-012 | 输入系统     | `InputConfigAutoLoader` 只扫当前程序集                       | ISSUE-P2-008            |
| NOTE-013 | 输入系统     | `ForbidAll` 下实体仍可移动                                   | ISSUE-P1-001            |
| NOTE-014 | 输入系统     | 输入配置跨文件依赖（state 引用 area_models），改一处忘另一处 | ISSUE-P0-002            |
| NOTE-015 | 输入系统     | 输入配置打包逻辑未做                                         | ISSUE-P1-008 / FEAT-002 |
| NOTE-016 | 输入系统     | 改键/存档上限是简化实践                                      | ISSUE-P2-021 / FEAT-003 |
| NOTE-017 | 事件系统     | 事件配置打包后 `File.ReadAllText` 失效                       | ISSUE-P0-001            |
| NOTE-018 | 事件系统     | `CanPublish` 缓存用实例做 key 永不命中                       | ISSUE-P1-004            |
| NOTE-019 | 事件系统     | `BuildArgList` 对"不使用"映射仍生成代码                      | ISSUE-P1-005            |
| NOTE-020 | 事件系统     | `DrawPublishSelector` KeyConnection key 不一致               | ISSUE-P2-016            |
| NOTE-021 | 事件系统     | `CanListen` 未接入                                           | ISSUE-P2-024 / FEAT-004 |
| NOTE-022 | 事件系统     | Domain Reload 关闭时 `_initialized` 残留                     | ISSUE-P2-017            |
| NOTE-023 | 总控         | 退出保存顺序不保证                                           | ISSUE-P1-007            |
| NOTE-024 | 总控         | `Invoke("InitGame", 0.5f)` 硬编码延迟                        | ISSUE-P1-009 / FEAT-007 |
| NOTE-025 | 上层脚本     | 移动直接改 transform + Rigidbody 抖动                        | ISSUE-P1-006            |
| NOTE-026 | 上层脚本     | 摄像机绕过输入系统直接读 Unity Input                         | ISSUE-P2-013            |
| NOTE-027 | 上层脚本     | `AimLineScript` 用 `!= Vector3.zero` 判断准星                | ISSUE-P2-012            |

------

## 六、统计与优先级总览

| 优先级    | 数量 | 编号范围           |
| :-------- | :--- | :----------------- |
| P0 阻塞级 | 2    | ISSUE-P0-001 ~ 002 |
| P1 重要级 | 9    | ISSUE-P1-001 ~ 009 |
| P2 优化级 | 28   | ISSUE-P2-001 ~ 028 |
| 待补功能  | 8    | FEAT-001 ~ 008     |
| 关注点    | 27   | NOTE-001 ~ 027     |

**建议处理顺序**：

1. 先修 **P0**（打包失效、配置笔误），否则发布/Area 功能不可用
2. 再修 **P1**（ForbidAll 移动、MeshCollider、退出保存），影响正确性
3. **P2** 按迭代节奏逐步处理，不必一次清完
4. **FEAT** 随对应功能（地图编辑器/打包/改键/异步加载器）一起做

------

**文档结束。**

## 



## 新增清单2026.11.17

# 项目痛点清单（外部审查版）

> 生成方式：基于对配置/工厂/输入/事件/上层逻辑五大系统的完整代码审查 + 与作者自有《已知问题与待修点》交叉比对。
> 标注约定：**【新】** = 作者台账未覆盖或未明确；**【已知】** = 台账已有（P0/P1/P2 编号）。
> 严重度沿用作者体系：🔴 P0 阻塞 / 🟠 P1 重要 / 🟡 P2 优化。
> 说明：作者已在修的部分（如事件配置打包读取、Area ModelId 命名）不再重复列为待办，仅在涉及处备注。

------

## 0. 最致命（先看这两条，都是【新】）

### 🔴 ISSUE-N-001 状态机规则让玩家初始状态锁死

- **位置**：`player_rule_config` / `ai_rule_config` 的 `ActionRules`；`EntityStateMachineBase.TryChangeAction`
- **现象**：规则为 `SourceState:"Idle" → BlockedActions:["Moving","Attacking"]`。状态机初始 `_currentAction = Idle`，`TryChangeAction(Moving/Attacking)` 时 `_actionRules[Idle].Contains(新动作)` 命中 → 拦截。
- **影响**：**玩家一出生就既不能移动也不能攻击**，攻击状态切换也永远失败。这是当前最致命的逻辑断点，比输入层问题更早卡死一切行为。
- **备注**：这不是配置错位，是**规则语义写反**——Idle 应"允许"进入 Moving/Attacking，而不是"禁止"。之前我一度记成字段错位，已修正为"语义反了"。

### 🔴 ISSUE-N-002 输入幽灵状态的完整根因链（作者已确认"没想通"）

- **位置**：`InputManager.Update` / `InputSnapshotBuffer` / `InputMediator.Update`
- **根因（四件事叠加）**：
  1. `_currentOutput` 是**单槽位**——组合 `Up` 写入后 fallthrough 到单键又 `WriteOutput` 覆盖，组合 Up 丢失；
  2. `Down` 在双帧快照里**只存在恰好一帧**；
  3. `InputManager.Update` 与 `InputMediator.Update` **执行顺序未定义**，消费方可能晚一帧读到已推进成 Hold/None 的快照；
  4. 移动轮询快照、攻击走事件 KeyState，**两个权威不同源**。
- **影响**：瞬时事件丢失、重复触发、状态残留。`RELEASE_BUFFER_TIME` 是治标（只改"放行"），没改变"单槽位"和"双权威"两个根。
- **备注**：作者已用"移动改轮询"绕开，但绕开 ≠ 解决，反而扩大了双轨分裂。

------

## 1. 配置系统

### 🟠 ISSUE-N-101 热更 JSON 校验形同虚设【新】

- **位置**：`ConfigUpdater.DownloadAndApply` 第 4 步
- **现象**：用 `JsonUtility.FromJson<object>(json)` 校验，但 JsonUtility 无法反序列化到 `object`，既不抛错也不拦坏 JSON。
- **影响**：下载的损坏配置会"通过校验"，替换后才在加载时崩。

### 🟠 ISSUE-N-102 `ConfigAPI.OnConfigLoaded<T>` 匿名 lambda 永不退订【新】

- **位置**：`ConfigAPI.OnConfigLoaded`
- **现象**：`ConfigManager.Instance.OnConfigLoaded += (id,config)=>{...}` 无 `-=` 机会；`ReloadAll` 后叠加。
- **影响**：事件泄漏 + 重复回调，热更次数越多越明显。

### 🟠 ISSUE-N-103 热更 `ReloadAll` 后旧引用不失效【新】

- **位置**：`ConfigManager.ReloadAll`
- **现象**：重建 `_loadedConfigs`，但 Factory/LogicStateManager/已实例化实体仍持有旧配置对象引用，无失效通知。
- **影响**：热更后新旧配置共存，逻辑状态管理器尤其明显（其本身未接热更）。

### 🟡 ISSUE-N-104 `@ref` 按字符串索引数组不支持【新】

- **位置**：`ConfigManager.ResolveIndexedField`
- **现象**：数组分支只认 `int.TryParse`，注释示例 `materials["bread_tree"].path` 是字符串索引 → 返回 null。
- **影响**：引用机制与实际数据结构（数组而非字典）不匹配。

### 🟡 ISSUE-N-105 `CompareVersion` 非数字段当 0【新】

- **位置**：`ConfigUpdater.CompareVersion`
- **现象**：`1.2.3-beta` 的 `beta` 被 `TryParse` 失败后当 0。
- **影响**：带后缀的版本号比较结果不可预期。

### 🟡 ISSUE-N-106 `ConfigBuilder` 序列化不严谨【新】

- **位置**：`ConfigBuilder.GetObjStr`
- **现象**：字符串不转义（引号/换行/反斜杠会生成非法 JSON）；`float.ToString()` 受区域文化影响（部分语言环境小数点是逗号）。
- **影响**：生成的配置 JSON 潜在非法。

### 🟡 ISSUE-N-107 远程文件名路径穿越【新】

- **位置**：`ConfigUpdater.DownloadAndApply`
- **现象**：`fileName` 来自远程 `version.json` 未校验，`Path.Combine(TmpDir, "../x")` 可越界写。
- **影响**：安全/健壮性隐患（作品集展示时应体现已处理）。

### 🟡 ISSUE-N-108 `md5` 字段生成了但从不校验【新】

- **位置**：`ConfigVersionGenerator` 生成 `md5`，`ConfigUpdater` 下载后不比对。
- **影响**：字段成了摆设，文件损坏无法发现。

### 🟡 ISSUE-N-109 配置类字段与生成 JSON 不一致【新】

- **位置**：`CreatConfig` vs `TerrainConfigs`/`MaterialDatabaseConfig`
- **现象**：`TerrainData` 缺 `diffuseTexture/normalTexture/tileSize/groundColor`；`MaterialData` 缺 `lodLevel`。
- **影响**：JsonUtility 缺字段静默给默认值，难发现。

------

## 2. 工厂系统

### 🔴 ISSUE-N-201 PoolFactory 作流水线第 6 步 → "创建即入池"【新，最高危】

- **位置**：`MasterFactory.RegisterFactories`（PoolFactory 排第 6）
- **现象**：所有带 `poolId` 且非预热模式的实体，`CreateEntity` 走完流水线就被 `Push` 入池（`SetActive(false)`）。
- **影响**：`MapFactory.PlaceEntity`（注释写"不走对象池"，实际走）和 `ProduceDirectly`（prewarm=false 且带 poolId）都会**一出生就隐藏**，后面设 position 设到了不可见物体上。这是"创建"与"入池"被混为一步的架构性缺陷。

### 🟠 ISSUE-N-202 `RenderFactory` 默认材质用 `Standard` Shader【新】

- **位置**：`RenderFactory.GetDefaultMaterial`
- **现象**：`Shader.Find("Standard")` 在 URP 下不存在 → `new Material(null)` 报错。
- **影响**：材质加载失败的兜底路径在 URP 项目里本身就会崩。

### 🟠 ISSUE-N-203 `MapFactory.ClearMap` 绕过 `DestroyEntity` + 脚本注册表残留【新】

- **位置**：`MapFactory.ClearMap` / `MasterFactory.OnEntityMonoDestroyed`
- **现象**：`ClearMap` 用 `GameObject.Destroy` 直删，走 `OnEntityMonoDestroyed` 保底路径；而该路径只清实体索引表和池表，**不 `UnregisterScript`**。
- **影响**：`MasterFactory._scriptsByType` 残留已销毁脚本引用，之后 `SaveAllStates` 对已销毁 MonoBehaviour 调 `SaveToEntry` → MissingReference。

### 🟠 ISSUE-N-204 `EventEntryBase.Initialize()` 重复调用【已知 ISSUE-P1-002，补现象】

- **位置**：`LogicFactory.Process`
- **现象**：`Initialize()` 在 `foreach(logicScriptIds)` 循环内，每挂一个脚本就重扫并重 `Initialize()` 一次，`INeedEventEntry` 重复注入。
- **影响**：N 个脚本 → 初始化 N 次。

### 🟠 ISSUE-N-205 逻辑状态存档路径是编辑器路径【新】

- **位置**：`LogicStateManager.WriteContainerToJson` + 注册表 `savePath`
- **现象**：`savePath` 为 `"Assets/Config/ConfigData/..."`，打包后不存在。
- **影响**：真机存档失效（同事件系统 P0 同性质问题，作者已修事件侧，逻辑状态侧尚未）。

### 🟠 ISSUE-N-206 UUID 不持久，存读档靠 UUID 匹配会错位【已知 FEAT-005，补影响】

- **位置**：`MasterFactory.GenerateUUID`（`tag_计数` 全局递增）
- **现象**：跨会话从 0001 重来，而 `LogicStateManager` 靠 UUID 匹配实体。
- **影响**：重启后"恢复场景"错位/失配。

### 🟡 ISSUE-N-207 池化实体仍留在索引表【新】

- **位置**：`MasterFactory.RegisterEntity`
- **现象**：`FindAllByTag/FindFirstByTag` 会混入 inactive 池化体；`Pop` 后 position/状态不重置。
- **影响**：查询"活跃实体"结果不准确。

### 🟡 ISSUE-N-208 `GetBlueprint` 每次线性遍历两个配置数组【新】

- **位置**：`MasterFactory.GetBlueprint`
- **影响**：蓝图查找 O(n)，且多实体共享同一 blueprint 引用，运行时改动会广播。

------

## 3. 输入系统

### 🔴 ISSUE-N-301 同帧多键输入被"单槽位 + return"吃掉【新】

- **位置**：`InputManager.Update` 单键梯队
- **现象**：命中第一个单键就 `return`，`_currentOutput` 单槽。按住 W 再按 A（斜向）或移动中攻击，同帧只有一个能输出。
- **影响**：斜向移动、移动+攻击等组合输入丢失。

### 🟠 ISSUE-N-302 备用键（AltKey）是死代码【新】

- **位置**：`InputManager.GetSingleState` / `GetComboState`
- **现象**：`RebuildBindGroups` 注册了 `AltKey1` 参与采样，但检测只读 `Key1`（单键）/`Key1/2/3`（组合）。
- **影响**：`input_binds.json` 里配置的方向键 `AltKey1` 永远触发不了。

### 🟠 ISSUE-N-303 `_comboReleaseTimers` 是全局开关而非精确匹配【新】

- **位置**：`InputManager.Update`
- **现象**：只要 `Count > 0`（任意组合释放过），`isReleaseBufferActive` 就对**所有**单键 Up 生效。
- **影响**：一个组合释放污染 50ms 内所有单键 Up 行为。

### 🟠 ISSUE-N-304 `MaxTimeWindow`/`TriggerMode` 声明了但未生效【新】

- **位置**：`InputKeyConfig` / `CheckCombo`
- **现象**：`CheckCombo` 只判断"所有键都按住"，`_heldKeys` 时间戳从未被消费。
- **影响**：组合"按下时间窗"约束是空配置，隔很久先后按下也算一次组合。

### 🟠 ISSUE-N-305 `SelectScopeValidator` 只校验基础状态【新】

- **位置**：`InputManager.ProcessBind`
- **现象**：`stateCfg = GetCurrentBaseConfig()`，Hit/UIMenu 等附加状态的范围限制完全不参与校验。
- **影响**：附加状态的范围约束失效。

### 🟠 ISSUE-N-306 主角"双源真相"【新】

- **位置**：`CameraEntry_Default.OnSelect` vs `InputMediator.SetMainCharacter`
- **现象**：OnSelect 直接改 camera 的 `_mainCharacterUuid`（注释"无需中转层干预"），中介层的 `_mainCharacterUuid`/`MainCharacterEntity` 不同步。
- **影响**：摄像机认定的人和中介层认定的人脱节。

### 🟡 ISSUE-N-307 `Peace` 状态没挂 `PeaceLimit` 拦截器【新，配置数据】

- **位置**：`input_states.json`
- **现象**：Peace 的 `InterceptorList` 只有 `["Global"]`（只拦 OpenDebugPanel），而 `PeaceLimit`（拦 Attack）定义了却**没被任何状态引用**。
- **影响**：Peace 模式（备注"不可攻击"）实际**能攻击**。

### 🟡 ISSUE-N-308 选择流程半成品【新】

- **位置**：`CameraEntry_Default`
- **现象**：`OnSelect` 定义但未注册；`EndSelect` 无调用方；`OnEnterSelect` 进入选择态后无"左键确认"接线。
- **影响**：AreaSelect 流程跑不通。

------

## 4. 事件系统

### 🟠 ISSUE-N-401 系统ID事件状态 Add/Delete 用不同 key【新】

- **位置**：`EventEditorWindow.UpdateSysIdEventStatus` vs `DeleteEvent`
- **现象**：写入用 `EventClassFullName`（`"E_xxx"`），删除用 `EventName`（不带前缀）。
- **影响**：删事件后系统ID里残留脏数据删不掉。

### 🟠 ISSUE-N-402 `AllToConfig` 空 `EventPublish` 越界崩溃【新】

- **位置**：`EventEditorWindow.AllToConfig`
- **现象**：`EventPublish.Split('.')[1]` 在发布方为空时长度 1，`[1]` 抛 `IndexOutOfRangeException`。
- **影响**：发布方为空时点保存崩溃（HelpBox 只提示不拦截）。

### 🟠 ISSUE-N-403 路由字段两套命名【新】

- **位置**：`EventUtil.BuildEvent` vs `EventCodeGenerator`
- **现象**：`BuildEvent` 写 `TargetId/TargetIds`，跳过 payload 判断用 `"UUID"/"UUIDS"`；生成器同样两套。
- **影响**：发布方法参数名恰为 UUID 时行为错位。

### 🟠 ISSUE-N-404 `DynamicInvoke` 不捕获异常【新】

- **位置**：`EventManager.DispatchTyped`
- **现象**：一个监听器抛异常中断整个派发循环。
- **影响**：同一事件后续监听器全部收不到。

### 🟠 ISSUE-N-405 `Package.Get<T>` 对已销毁 Unity 对象崩溃【新】

- **位置**：`Package.Get<T>`
- **现象**：`obj is UnityEngine.Object unityObj && typeof(T).IsAssignableFrom(unityObj.GetType())`，已销毁对象 `GetType()` 抛 `MissingReferenceException`。

### 🟠 ISSUE-N-406 三套派发通路并存【新，架构级】

- **位置**：`EventManager` + `EventUtil`
- **现象**：①老弱类型 `On/Off/Dispatch` + 6 个 `*Queue`（**无生产者，死代码**）；②强类型 `typedListeners` + 6 个 `*TypedQueue`；③`EmitToTargets`/`EmitGlobal` 同步直投（**绕过 CanPublish/CanListen 校验**）。
- **影响**：时序语义不统一，校验时有时无。

### 🟠 ISSUE-N-407 两套 `GlobalMethodParams` 并存【新】

- **位置**：`EventEditorWindow` vs `EventGlobalCache`
- **现象**：窗口有自己的 `GlobalMethodParams` 字段，但 `CheckTypeMetch` 用全局的。
- **影响**：窗口私有副本与全局缓存可能不一致。

### 🟠 ISSUE-N-408 编辑器窗口与运行时共享静态 `EventGlobalCache`【新】

- **影响**：窗口打开时改的未保存脏数据会被运行时读到，编辑器态污染运行态。

### 🟡 ISSUE-N-409 6 个协程空转 + 队列无上限 + `EmitGlobal` O(n) 广播【新】

- **位置**：`EventManager.Start` / 各 `Process*` / `DispatchGlobal`
- **影响**：无事件也每帧/定时轮询；事件堆积无上限；全局广播随实体数线性增长。

------

## 5. 上层逻辑脚本

### 🟠 ISSUE-N-501 `HandleAttackResult` 状态变更在 KeyState 判断之前【新】

- **位置**：`CharacterInputEntry.HandleAttackResult`
- **现象**：`TryChangeAction` 和 `AddBuff` 在 `if (e.KeyState != Down) return` **之前**执行；攻击 `ListenState=["Down","Hold"]`。
- **影响**：Hold 每 0.1s 重复触发 `AddBuff("Burning")` 和状态切换。

### 🟠 ISSUE-N-502 `_stateMachine` 注入时序有 null 风险【新】

- **位置**：`CharacterInputEntry.HandleAttackResult`
- **现象**：`_stateMachine` 靠 `IEntityStateMachineProvider.InjectStateMachine` 注入，注入发生在状态机 `OnAllStateInjected` 里，而 `NotifyAllScriptsReady` 遍历字典顺序不保证。
- **影响**：输入事件早于注入到达 → NRE。

### 🟠 ISSUE-N-503 `Event_Class_Test_001` 缺 `[Serializable]`【新】

- **位置**：`CharacterInputEntry.HandleTestClass` / `S_006`
- **现象**：`JsonUtility.ToJson` 对未标 `[Serializable]` 的类输出空 `{}`。
- **影响**：自定义类型事件链路（001 路径）实际断掉。

### 🟠 ISSUE-N-504 `OnAllStateInjected` 非幂等【新】

- **位置**：`AimLineScript` / `CharacterAttachmentScript`
- **现象**：每次 `AddComponent<LineRenderer>` + `new Material`；每次 `new GameObject("MuzzlePoint")`。
- **影响**：热更后 `ReloadAll→LoadAllStates→NotifyAllScriptsReady` 重复注入，叠加组件 + 材质泄漏。

### 🟠 ISSUE-N-505 硬编码 UUID【新】

- **位置**：`TestEntry.TEST_001`（"Pawn_0003/0004"）/ `HandleMoveRight`（"Pawn_0005"）
- **影响**：UUID 是运行时 `tag_计数` 生成，顺序一改全部错位。

### 🟠 ISSUE-N-506 `CameraRigScript` 绕过输入系统【已知 ISSUE-P2-013，补根因】

- **位置**：`CameraRigScript.Update`
- **现象**：`Input.GetAxis("Mouse X/Y")`、`Input.GetKeyDown(Escape)` 直读 Unity 原始输入。
- **影响**：输入系统"统一权威"在相机视角处失效，双份采样。

### 🟠 ISSUE-N-507 移动直接改带 Rigidbody 物体的 transform【已知 ISSUE-P1-006】

- **位置**：`CharacterInputEntry.Update`
- **影响**：绕开物理引擎，抖动/穿模。

### 🟠 ISSUE-N-508 `LoadPlayerState` 两份复制粘贴【新】

- **位置**：`PlayerStateMachineScript` / `AIStateMachineScript`
- **现象**：Buff 字符串解析逻辑完全重复，连方法名 `LoadPlayerState` 都拷进了 AI 类。
- **影响**：改一处忘一处即隐性不一致。

### 🟠 ISSUE-N-509 `PlayerTag.OnDestroy` 只清静态引用【新】

- **现象**：不通知 `InputMediator` 清空主角。
- **影响**：销毁主角后中介层仍持悬空 `MainCharacterEntity`。

### 🟡 ISSUE-N-510 池化与 Buff 计时交互【新】

- **位置**：`EntityStateMachineBase.UpdateBuffTimers`
- **现象**：实体 `SetActive(false)` 后计时停跑，但 `EndTime` 用 `Time.time`，池化期间时间照走。
- **影响**：Pop 出来后 Buff 一次性过期。

------

## 附：与作者台账的关系

| 类型                         | 说明                                                         |
| :--------------------------- | :----------------------------------------------------------- |
| **已修/在修**                | 事件配置打包读取（P0-001）、Area ModelId 命名（P0-002）、配置热更改造 |
| **台账已覆盖、本文补充根因** | ForbidAll 移动未拦（P1-001）、脚本挂载隐性依赖（P1-002）、MeshCollider（P1-003）、CanPublish 缓存（P1-004）、Rigidbody 抖动（P1-006）、CameraRig 绕过输入（P2-013） |
| **本文新增（台账未覆盖）**   | ISSUE-N-001（Idle 锁死）、N-002（幽灵状态根因链）、N-201（创建即入池）、N-202（URP 材质兜底崩）、N-203（脚本注册表残留）、N-401/402（事件脏数据/崩溃）、以及各系统 N-1xx~N-5xx 标注【新】的条目 |

------

