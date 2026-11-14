# 第 7 章 上层逻辑脚本

> 本章讲上层逻辑脚本：**所有 `LogicScriptBase` 子类的职责、依赖、示例**。
> 核心思想：实体自管理——实体不查全局表，因为它的所有信息（预制体部位、音效、碰撞体、逻辑脚本配置）都在 Root 下或由逻辑脚本定义。逻辑脚本是"实体对自身信息的解释器"，配置是"实体对自身信息的定义"。
>
> 配套：《架构优点》A1/B3/B6、《已知问题与待修点》ISSUE-P1-001/002/006、ISSUE-P2-005/006/007/011/012/013/025/026/027。

------

## 7.1 系统定位与职责边界

### 负责什么

| 负责                                                     | 不负责                     |
| :------------------------------------------------------- | :------------------------- |
| 实体业务逻辑（移动/相机/状态机/交互…）                   | 实体创建（工厂管）         |
| 通过 `LogicScriptBase` 三方法接入状态系统                | 状态注入（逻辑状态系统管） |
| 通过 `EventEntryBase` / `EntityInputEntry` 接入事件/输入 | 事件投递（事件系统管）     |
| 自己解释自己的配置（部位倍率、受击音效…）                | 配置加载（配置系统管）     |
| 自己找自己身上的组件（`GetComponent`）                   | 全局查询（实体不查全局表） |

### 边界

- **实体自管理**：实体不查全局表，Root 容器就是实体的"自己的全局表"
- **逻辑脚本是解释器**：实体不知道自己的预制体有什么部位，但逻辑脚本可以定义配置
- **配置通过逻辑状态系统注册**：`LoadFromEntry` 把配置注入脚本
- **需要知道自己身上有什么**：直接去 `Entity.Root` 找（`GetComponent`/`Find`）

------

## 7.2 脚本清单与分类

### 7.2.1 按职责分类

| 类别                 | 脚本                        | ContainerId                     | 职责                                 |
| :------------------- | :-------------------------- | :------------------------------ | :----------------------------------- |
| **无状态**           | `ChestInteractScript`       | 空                              | 交互提示（`IInteractable`）          |
| **无状态**           | `CharacterAttachmentScript` | 空                              | 生成枪口挂载点                       |
| **无状态**           | `PlayerTag`                 | 空                              | 注册主角到 `InputMediator`           |
| **无状态**           | `AimLineScript`             | 空                              | 画枪口→准星线                        |
| **有状态**           | `PawnAttackRange`           | `logic_state_pawn_attack_range` | 攻击范围/等级/经验                   |
| **有状态**           | `CameraRigScript`           | `logic_state_camera_rig`        | 越肩相机（距离/高度/偏移/yaw/pitch） |
| **有状态**           | `StateMachineScript`        | `entity_state_machine`          | 四维状态 + 限时 Tag                  |
| **输入/事件桥接**    | `CharacterInputEntry`       | 空                              | 移动轮询 + 攻击/交互事件             |
| **事件入口**         | `TestEntry`                 | 空                              | 事件监听/发布（测试）                |
| **事件入口（基类）** | `EventEntryBase`            | `"event"`                       | partial 生成 RegisterEvents          |

### 7.2.2 按继承关系

text

```
LogicScriptBase
├─ ChestInteractScript（IInteractable）
├─ CharacterAttachmentScript
├─ PlayerTag
├─ AimLineScript
├─ PawnAttackRange
├─ CameraRigScript
├─ StateMachineScript
├─ EntityInputEntry（抽象基类）
│   └─ CharacterInputEntry（INeedEventEntry）
└─ EventEntryBase（抽象基类）
    └─ TestEntry（partial）
```



### 7.2.3 脚本与系统对接

| 脚本                        | 对接系统                | 对接方式                                                     |
| :-------------------------- | :---------------------- | :----------------------------------------------------------- |
| `PawnAttackRange`           | 逻辑状态                | `LoadFromEntry`/`SaveToEntry`                                |
| `CameraRigScript`           | 逻辑状态 + 配置         | `LoadFromEntry` + `ConfigAPI.Get<CameraRegistryConfig>`      |
| `StateMachineScript`        | 逻辑状态 + 实体四维状态 | `LoadFromEntry`/`SaveToEntry` + `Entity.Lifecycle`           |
| `CharacterInputEntry`       | 输入 + 事件 + 相机      | `IsActionHeld` 轮询 + `EmitFromExternal` + `CameraRigScript.ActiveCamera` |
| `TestEntry`                 | 事件                    | `[EventListenAttr]`/`[EventPublishAttr]`                     |
| `PlayerTag`                 | 输入中转                | `InputMediator.SetMainCharacter`                             |
| `AimLineScript`             | 输入中转 + 相机入口     | `InputMediator.GetCameraEntry` + `CameraEntry_Default.CurrentAimPoint` |
| `CharacterAttachmentScript` | 无（被其他脚本读）      | 暴露 `MuzzlePoint`                                           |
| `ChestInteractScript`       | 输入（`IInteractable`） | `GetInteractionHint`                                         |

------

## 7.3 核心数据结构（`LogicScriptBase` 三方法）

csharp

```c#
public abstract class LogicScriptBase : MonoBehaviour, IEntityAware
{
    public Entity Entity { get; set; }

    public abstract string ContainerId { get; }
    public abstract void LoadFromEntry(ILogicStateEntry entry);
    public abstract ILogicStateEntry SaveToEntry();
    public virtual void OnAllStateInjected() { }
}
```



### 三方法使用场景

| 方法                 | 何时调         | 典型用途                    |
| :------------------- | :------------- | :-------------------------- |
| `LoadFromEntry`      | 状态加载时     | 从父容器读配置，脚本自己拆  |
| `SaveToEntry`        | 状态保存时     | 打包状态到子容器            |
| `OnAllStateInjected` | 全部注入完成后 | 找其他脚本/组件、初始化依赖 |

### `ContainerId` 约定

| 值        | 含义                               | 例子                              |
| :-------- | :--------------------------------- | :-------------------------------- |
| 空串 `""` | 无状态脚本                         | `PlayerTag`/`ChestInteractScript` |
| 非空      | 有状态脚本，值 = 父容器 `ConfigId` | `"logic_state_pawn_attack_range"` |

------

## 7.4 各脚本详解

### 7.4.1 `PawnAttackRange`（有状态，最标准范例）

csharp

```c#
public class PawnAttackRange : LogicScriptBase
{
    public override string ContainerId => "logic_state_pawn_attack_range";

    public Vector2Int attackRange;
    public Vector2Int attackRange2;
    public int level;
    public float experience;

    public override void LoadFromEntry(ILogicStateEntry entry)
    {
        if (entry is not PawnAttackRangeStateContainer.PawnAttackRangeEntry data) return;
        attackRange = data.attackRange;
        attackRange2 = data.attackRange2;
        level = data.level;
        experience = data.experience;
    }

    public override ILogicStateEntry SaveToEntry()
    {
        return new PawnAttackRangeStateContainer.PawnAttackRangeEntry
        {
            uuid = Entity.UUID,
            tag = Entity.Tag,
            attackRange = attackRange,
            attackRange2 = attackRange2,
            level = level,
            experience = experience
        };
    }
}
```



**特点**：

- 最标准的"有状态脚本"范例，可作为"新写一个有状态脚本"的模板
- 数据与逻辑分离：脚本只管存/读，业务逻辑另写
- `LoadFromEntry` 里类型检查 `is not PawnAttackRangeStateContainer.PawnAttackRangeEntry`，防错类型

### 7.4.2 `CameraRigScript`（有状态，越肩相机）

csharp

```c#
public class CameraRigScript : LogicScriptBase
{
    public override string ContainerId => "logic_state_camera_rig";

    public static Camera ActiveCamera { get; private set; }

    // 配置
    public string cameraId = "Camera_Main";
    public float camDistance = 5f;
    public float camHeight = 1.5f;
    public float snapshotDelay = 0.15f;
    public float followLerp = 12f;
    public float shoulderOffset = 2.0f;
    public float mouseSensitivity = 3f;
    public float minPitch = -30f;
    public float maxPitch = 70f;

    // 运行时
    private Transform _character;
    private Camera _cam;
    private float _currentYaw = 0f;
    private float _currentPitch = 20f;

    public override void LoadFromEntry(ILogicStateEntry entry)
    {
        if (entry is not CameraRigStateContainer.CameraRigEntry data) return;
        cameraId = data.cameraId;
        camDistance = data.camDistance;
        camHeight = data.camHeight;
        snapshotDelay = data.snapshotDelay;
        followLerp = data.followLerp;
        _currentYaw = data.camYaw;
        _currentPitch = data.camPitch;
        // ⚠️ 未读 shoulderOffset（ISSUE-P2-005）
    }

    public override ILogicStateEntry SaveToEntry()
    {
        return new CameraRigStateContainer.CameraRigEntry
        {
            uuid = Entity?.UUID,
            tag = Entity?.Tag,
            cameraId = cameraId,
            camDistance = camDistance,
            camYaw = _currentYaw,
            camPitch = _currentPitch,
            camHeight = camHeight,
            snapshotDelay = snapshotDelay,
            followLerp = followLerp,
            shoulderOffset = shoulderOffset
        };
    }

    public override void OnAllStateInjected()
    {
        _character = Entity.LogicObject.transform;
        _cam = FindCamera();
        if (_cam != null) { ActiveCamera = _cam; }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update：鼠标视角旋转 + 快照延迟记录
    // LateUpdate：越肩相机跟随（位置 + 旋转平滑）
}
```



**特点**：

- **`ActiveCamera` 静态属性**：被 `CharacterInputEntry.Update` 读取做相机相对移动（Q62）
- **越肩相机**：`shoulderOffset` 向右偏移，让人物在左边
- **快照延迟跟随**：`_snapshots` 记录角色位置，`LateUpdate` 用 `snapshotDelay` 前的快照（防抖动）
- **鼠标视角**：直接用 `Input.GetAxis("Mouse X")`，**未接入输入系统**（ISSUE-P2-013）
- **`FindCamera`**：从 `CameraRegistryConfig` 按 `cameraId` 找相机，找不到降级 `Camera.main`

### 7.4.3 `StateMachineScript`（有状态，四维状态 + 限时 Tag）

csharp

```c#
public class StateMachineScript : LogicScriptBase
{
    public override string ContainerId => "entity_state_machine";

    private Dictionary<string, float> _timedTags = new();

    public void AddTimedTag(string tag, float duration)
    {
        Entity.AddTag(tag);
        _timedTags[tag] = Time.time + duration;
    }

    private void Update()
    {
        // 清理过期 Tag
    }

    public override void LoadFromEntry(ILogicStateEntry entry)
    {
        if (entry is StateMachineStateContainer.StateMachineEntry data)
        {
            // 用 Enum.TryParse 安全转换（区分大小写，ISSUE-P2-007）
            if (Enum.TryParse(data.Lifecycle, out EntityLifecycleState lifecycle))
                Entity.Lifecycle = lifecycle;

            if (Enum.TryParse(data.GameFlow, out GameFlowState gameFlow))
                Entity.GameFlow = gameFlow;

            if (Enum.TryParse(data.Action, out EntityActionState action))
                Entity.Action = action;

            if (data.Tags != null)
                foreach (var tag in data.Tags) Entity.AddTag(tag);
        }
    }

    public override ILogicStateEntry SaveToEntry()
    {
        return new StateMachineStateContainer.StateMachineEntry
        {
            uuid = Entity.UUID,
            tag = Entity.Tag,
            Lifecycle = Entity.Lifecycle.ToString(),
            GameFlow = Entity.GameFlow.ToString(),
            Action = Entity.Action.ToString(),
            Tags = new List<string>(Entity.GetAllTags())
        };
    }
}
```



**特点**：

- **四维状态管理**：`Lifecycle`/`GameFlow`/`Action`/`Tags`
- **限时 Tag**：`_timedTags` 字典记录剩余时间，`Update` 清理过期（不存盘，ISSUE-P2-006）
- **枚举存字符串**：`Lifecycle`/`GameFlow`/`Action` 存 `ToString()`，防 `JsonUtility` 解析枚举报错
- **`Enum.TryParse`**：区分大小写（ISSUE-P2-007）

### 7.4.4 `CharacterInputEntry`（输入/事件桥接，关键）

csharp

```c#
public class CharacterInputEntry : EntityInputEntry, INeedEventEntry
{
    public EventEntryBase EventEntry { get; set; }

    public float MoveSpeed = 3f;
    public float RotateSpeed = 15f;

    private Vector3 _finalMoveDir;

    protected override void RegisterInputs()
    {
        RegisterInput("Inpt_Test_01", HandleTestClass);
        RegisterInput("Inpt_Test_02", HandleTestClass_002);
        RegisterInput("Attack", HandleAttackResult);
        RegisterInput("Interact", HandleInteractResult);
    }

    private void Update()
    {
        // 1. 轮询 IsActionHeld（持续状态用权威快照）
        bool isUp = InputManager.Instance.IsActionHeld("MoveUp");
        bool isDown = InputManager.Instance.IsActionHeld("MoveDown");
        bool isLeft = InputManager.Instance.IsActionHeld("MoveLeft");
        bool isRight = InputManager.Instance.IsActionHeld("MoveRight");

        // 2. 收集方向
        float h = (isRight ? 1 : 0) - (isLeft ? 1 : 0);
        float v = (isUp ? 1 : 0) - (isDown ? 1 : 0);
        Vector3 localInput = new Vector3(h, 0, v);

        if (localInput.sqrMagnitude < 0.01f) { _finalMoveDir = Vector3.zero; return; }

        // 3. 相机相对移动
        var cam = CameraRigScript.ActiveCamera;
        if (cam != null)
        {
            Vector3 camForward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
            Vector3 camRight = Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized;
            _finalMoveDir = (camForward * v + camRight * h).normalized;
        }
        else _finalMoveDir = localInput.normalized;

        // 4. 移动
        transform.position += _finalMoveDir * MoveSpeed * Time.deltaTime;

        // 5. 转身
        Quaternion targetRot = Quaternion.LookRotation(_finalMoveDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, RotateSpeed * Time.deltaTime);
    }

    private void HandleAttackResult(EntityInputEvent e)
    {
        if (e.KeyState != InputKeyState.Down) return;
        if (e.TargetUuids == null || e.TargetUuids.Count == 0) return;  // 打空

        // 发事件给受害者
        if (EventEntry != null)
            EventEntry.EmitFromExternal(this, nameof(OnShootPublish), e.TargetUuids, 10f);
    }

    [EventPublishAttr]
    public void OnShootPublish(List<string> Uuids, float damage) { }

    private void HandleInteractResult(EntityInputEvent e)
    {
        if (e.KeyState != InputKeyState.Down) return;
        if (e.TargetUuids == null || e.TargetUuids.Count == 0) return;

        if (EventEntry != null)
            EventEntry.EmitFromExternal(this, nameof(OnInteractPublish),
                new List<string> { e.TargetUuids[0] });
    }

    [EventPublishAttr]
    public void OnInteractPublish(List<string> Uuids) { }
}
```



**特点**：

- **两套状态哲学**：移动用 `IsActionHeld` 轮询（持续状态），攻击/交互用 `RegisterInput` 事件（瞬时机）
- **输入 → 事件桥接**：`HandleAttackResult` 里 `EventEntry.EmitFromExternal` 发事件给目标
- **相机相对移动**：读 `CameraRigScript.ActiveCamera`，为 null 时降级 `localInput`
- ⚠️ **移动直接改 transform**：ISSUE-P1-006（Rigidbody 冲突抖动）
- ⚠️ **`ForbidAll` 下仍可移动**：ISSUE-P1-001（轮询绕过模式判断）

### 7.4.5 `PlayerTag`（无状态，主角就绪信号）

csharp

```c#
public class PlayerTag : LogicScriptBase
{
    public static PlayerTag Current { get; private set; }
    public override string ContainerId => "";

    public override void OnAllStateInjected()
    {
        if (Current != null && Current != this) { Debug.LogError("多个主角"); return; }
        Current = this;

        if (InputMediator.Instance != null)
            InputMediator.Instance.SetMainCharacter(Entity.UUID);

        Debug.Log($"[PlayerTag] 自动绑定主角：{Entity.UUID}");
    }

    private void OnDestroy() { if (Current == this) Current = null; }

    public override void LoadFromEntry(ILogicStateEntry entry) { }
    public override ILogicStateEntry SaveToEntry() => null;
}
```



**特点**：

- **主角就绪信号**：`OnAllStateInjected` 里设主角到 `InputMediator`
- **唯一性检查**：`Current != null && Current != this` 报错
- **`OnDestroy` 清理**：销毁时清 `Current`

**启动时序关键点**：`PlayerTag.OnAllStateInjected` 是"主角就绪"触发源，`CameraEntry_Default.LateUpdate` 第一帧可能拿不到主角，第二帧才拿到（Q61）。

### 7.4.6 `AimLineScript`（无状态，瞄准线）

csharp

```c#
public class AimLineScript : LogicScriptBase
{
    public override string ContainerId => "";

    private LineRenderer _lineRenderer;
    private Transform _muzzlePoint;

    public override void OnAllStateInjected()
    {
        _lineRenderer = gameObject.AddComponent<LineRenderer>();
        _lineRenderer.startWidth = 0.1f;
        _lineRenderer.endWidth = 0.0f;
        _lineRenderer.useWorldSpace = true;

        // URP 兼容 Shader
        Shader lineShader = Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Color")
            ?? Shader.Find("Sprites/Default");
        _lineRenderer.material = new Material(lineShader);
        _lineRenderer.material.color = lineColor;

        // 找自己身上的枪口
        foreach (var s in Entity.LogicScripts)
            if (s is CharacterAttachmentScript attachment) { _muzzlePoint = attachment.MuzzlePoint; break; }
    }

    private void LateUpdate()
    {
        if (_lineRenderer == null || _muzzlePoint == null) return;

        var camEntry = InputMediator.Instance.GetCameraEntry() as CameraEntry_Default;
        if (camEntry != null && camEntry.CurrentAimPoint != Vector3.zero)  // ISSUE-P2-012
        {
            _lineRenderer.SetPosition(0, _muzzlePoint.position);
            _lineRenderer.SetPosition(1, camEntry.CurrentAimPoint);
        }
    }
}
```



**特点**：

- **`OnAllStateInjected` 里找 `CharacterAttachmentScript.MuzzlePoint`**：读自己身上的挂载点
- **读 `CameraEntry_Default.CurrentAimPoint`**：准星点
- ⚠️ `!= Vector3.zero` 判断准星有效性（ISSUE-P2-012，打中世界原点误判）

### 7.4.7 `CharacterAttachmentScript`（无状态，枪口挂载点）

csharp

```c#
public class CharacterAttachmentScript : LogicScriptBase
{
    public override string ContainerId => "";
    public Transform MuzzlePoint { get; private set; }

    public override void OnAllStateInjected()
    {
        var muzzleGo = new GameObject("MuzzlePoint");
        muzzleGo.transform.SetParent(Entity.LogicObject.transform, false);
        muzzleGo.transform.localPosition = new Vector3(0f, 1.2f, 1f);
        MuzzlePoint = muzzleGo.transform;
    }
}
```



**特点**：

- **生成挂载点**：在 LogicObject 下生成 `MuzzlePoint`
- **被其他脚本读**：`AimLineScript` 和 `CameraEntry_Default.FindAttachment` 读 `MuzzlePoint`
- **偏移硬编码**：`(0, 1.2, 1)`，可改为配置

### 7.4.8 `ChestInteractScript`（无状态，`IInteractable`）

csharp

```c#
public class ChestInteractScript : LogicScriptBase, IInteractable
{
    public override string ContainerId => "";
    public string GetInteractionHint() => "交互测试_001";  // ISSUE-P2-025 硬编码
    public override void LoadFromEntry(ILogicStateEntry entry) { }
    public override ILogicStateEntry SaveToEntry() => null;
}
```



**特点**：

- **实现 `IInteractable`**：`CameraEntryBase.ResolveTrigger` 会识别
- **交互提示硬编码**：ISSUE-P2-025

### 7.4.9 `TestEntry`（事件入口，测试）

csharp

```c#
public partial class TestEntry : EventEntryBase
{
    [EventListenAttr]
    public void TestEventListen(int value, string msg) { Debug.Log($"收到：{value}, {msg}"); }

    [EventPublishAttr]
    public void TestEventPublish(List<string> UUIDS, int value, string msg)
    {
        EventUtil.EmitToTargets(this, nameof(TestEventPublish), UUIDS, value, msg);
    }
}
```



**特点**：

- **测试代码**：正式版移除（ISSUE-P2-026）
- **partial 生成绑定**：`Gen_TestEntry.cs` 里 `RegisterMethod("TestEventListen", ...)`

------

## 7.5 怪物受击完整示例（贯穿第 3 章 + 第 5 章）

**场景**：怪物复杂实体，预制体有多个部位，逻辑脚本定义"部位 → 伤害倍率"表。

**Step 1：怪物蓝图**

json

```json
{
  "blueprintId": "monster",
  "tag": "Monster",
  "modelPath": "TestGameObject/MonsterPrefab",
  "physicsConfigId": "",   // 留空，预制体自带碰撞体
  "materialId": "",        // 留空
  "logicScriptIds": ["monster_hit_script"],
  "poolId": "monster_pool"
}
```



**Step 2：逻辑脚本定义配置**

csharp

```c#
public class MonsterHitScript : LogicScriptBase
{
    public override string ContainerId => "monster_hit_state";

    private Dictionary<string, float> _partMultipliers = new();

    public override void LoadFromEntry(ILogicStateEntry entry)
    {
        if (entry is not MonsterHitStateContainer.MonsterHitEntry data) return;
        _partMultipliers.Clear();
        for (int i = 0; i < data.partNames.Count; i++)
            _partMultipliers[data.partNames[i]] = data.partMultipliers[i];
    }

    public override ILogicStateEntry SaveToEntry()
    {
        return new MonsterHitStateContainer.MonsterHitEntry
        {
            uuid = Entity.UUID,
            tag = Entity.Tag,
            partNames = new List<string>(_partMultipliers.Keys),
            partMultipliers = new List<float>(_partMultipliers.Values)
        };
    }

    [EventListenAttr]
    public void OnShootListen(string hitPart, float damage)
    {
        float multiplier = _partMultipliers.GetValueOrDefault(hitPart, 1.0f);
        float finalDamage = damage * multiplier;

        // 需要受击音效：直接去自己的 Root 容器找
        var audio = Entity.Root.GetComponentInChildren<AudioSource>();
        audio?.Play();

        Debug.Log($"[MonsterHit] 部位={hitPart}, 倍率={multiplier}, 最终伤害={finalDamage}");
    }
}
```



**Step 3：父容器配置（逻辑状态系统）**

json

```json
{
  "entries": [
    {
      "uuid": "Monster_0001",
      "tag": "Monster",
      "partNames": ["弱点", "普通", "护甲"],
      "partMultipliers": [2.0, 1.0, 0.5]
    }
  ]
}
```



**Step 4：完整链路**

1. **玩家攻击**：`InputManager` 输出 `Attack` → `InputMediator` → `CameraEntry_Default.OnAttack` → 射线检测到碰撞体 → `hitPart = "弱点"` → `_result.ReceiverUuids.Add(主角UUID)`
2. **主角发事件**：`CharacterInputEntry.HandleAttackResult` → `EventEntry.EmitFromExternal(OnShootPublish, 目标UUID, "弱点", 10f)`
3. **事件投递**：`EventManager.DispatchToTargets` → 怪物 `EventEntryBase.Receive("MonsterHitScript.OnShootListen", evt)`
4. **怪物自解释**：`MonsterHitScript.OnShootListen` → 查 `_partMultipliers["弱点"] = 2.0` → 最终伤害 20 → 去 `Entity.Root` 找音效播放

**核心洞察**：怪物**不知道自己的预制体有什么部位**，但它的逻辑脚本定义了"部位 → 倍率"配置（通过逻辑状态系统注册）；事件只把"弱点"这个**信息**送到怪物面前，怪物**自己解释**。

------

## 7.6 扩展指南

### 加一个无状态脚本

csharp

```c#
public class MyNoStateScript : LogicScriptBase
{
    public override string ContainerId => "";
    public override void LoadFromEntry(ILogicStateEntry entry) { }
    public override ILogicStateEntry SaveToEntry() => null;

    public override void OnAllStateInjected()
    {
        // 找自己身上的组件、初始化
    }
}
```



### 加一个有状态脚本

参考 7.4.1 `PawnAttackRange`，三步：

1. 父容器配置（第 3 章 3.5.1）
2. 注册到 `logic_state_registry`
3. 脚本实现 `LoadFromEntry`/`SaveToEntry`

### 加一个输入行为

csharp

```c#
public class MyInputEntry : EntityInputEntry
{
    protected override void RegisterInputs()
    {
        RegisterInput("Attack", OnAttack);
    }

    private void OnAttack(EntityInputEvent e)
    {
        if (e.KeyState != InputKeyState.Down) return;
        // 业务逻辑
    }
}
```



### 加一个事件监听

csharp

```c#
public class MyEventListener : EventEntryBase
{
    [EventListenAttr]
    public void MyListen(int value, string msg) { /* 业务逻辑 */ }
}
```



然后在编辑器配置事件 + 生成代码（第 5 章 5.6）。

### 需要知道自己身上的组件

**不要查全局表**，直接去 `Entity.Root` 找：

csharp

```c#
var audio = Entity.Root.GetComponentInChildren<AudioSource>();
var collider = Entity.LogicObject.GetComponent<Collider>();
var renderer = Entity.ViewObject.GetComponentInChildren<Renderer>();
```



**这就是"实体自管理"**——Root 容器就是实体自己的"全局表"。

------

## 7.7 已知限制与注意事项

| 编号         | 问题                                            | 优先级 |
| :----------- | :---------------------------------------------- | :----- |
| ISSUE-P1-001 | `ForbidAll` 下 `CharacterInputEntry` 仍可移动   | P1     |
| ISSUE-P1-002 | 脚本挂载顺序依赖蓝图数组顺序（隐性依赖）        | P1     |
| ISSUE-P1-006 | 移动直接改 transform + Rigidbody 抖动           | P1     |
| ISSUE-P2-005 | `CameraRigScript` 的 `shoulderOffset` 未 Load   | P2     |
| ISSUE-P2-006 | `StateMachineScript` 限时 Buff 时间戳不存盘     | P2     |
| ISSUE-P2-007 | `StateMachineScript` 枚举 `TryParse` 区分大小写 | P2     |
| ISSUE-P2-011 | `CameraEntry_Default.OnSelect` 是旧测试代码     | P2     |
| ISSUE-P2-012 | `AimLineScript` 用 `!= Vector3.zero` 判断准星   | P2     |
| ISSUE-P2-013 | 摄像机绕过输入系统直接读 Unity Input            | P2     |
| ISSUE-P2-025 | `ChestInteractScript` 交互提示硬编码            | P2     |
| ISSUE-P2-026 | `TestEntry` 硬编码 UUID                         | P2     |
| ISSUE-P2-027 | `CharacterInputEntry` 未注册方法                | P2     |

### 设计约定

- **实体自管理**：实体不查全局表，Root 容器就是实体自己的"全局表"
- **逻辑脚本是解释器**：实体不知道自己的预制体有什么部位，但逻辑脚本可以定义配置
- **需要知道自己身上的组件**：直接 `Entity.Root.GetComponentInChildren` 找
- **两套状态哲学**：持续状态轮询 `IsActionHeld`，瞬时机事件投递
- **`PlayerTag.OnAllStateInjected` 是主角就绪信号**
- **`CameraRigScript.ActiveCamera` 是静态属性**，供 `CharacterInputEntry` 读
- **`OnAllStateInjected` 是"第二步完成"回调**，给需要"读完配置才能初始化"的脚本用

### 隐性依赖

- **`PlayerTag` 设置主角 → `InputMediator.MainCharacterEntity` 就绪**：`CameraEntry_Default.LateUpdate` 第一帧可能拿不到主角（Q61）
- **`CameraRigScript.ActiveCamera` → `CharacterInputEntry.Update` 读它做相机相对移动**：为 null 时降级用 `localInput`（Q62）
- **`CharacterAttachmentScript.MuzzlePoint` → `AimLineScript`/`CameraEntry_Default` 读它**：`OnAllStateInjected` 顺序不保证，可能第一帧空转
- **`CharacterInputEntry` 依赖 `EventEntryBase` 先挂**（ISSUE-P1-002）

### 未完成/待补

- 摄像机接入输入系统（ISSUE-P2-013 待改）
- `TestEntry` 等测试代码清理（ISSUE-P2-026/027）
- `CameraEntry_Default.OnSelect` 旧测试代码清理（ISSUE-P2-011）
- 移动物理冲突优化（ISSUE-P1-006）
- `ForbidAll` 移动拦截（ISSUE-P1-001）

------

## 7.8 依赖关系

text

```
上层逻辑脚本（LogicScriptBase 子类）
    │
    ├─ 依赖：
    │   ├─ 逻辑状态系统（LoadFromEntry/SaveToEntry/OnAllStateInjected）
    │   ├─ 输入系统（IsActionHeld / GetStandardOutput）
    │   ├─ 事件系统（EmitFromExternal / RegisterMethod）
    │   ├─ 配置系统（ConfigAPI.Get）
    │   ├─ 工厂系统（MasterFactory.FindByUUID / FindAllByTag）
    │   └─ 输入中转（InputMediator.SetMainCharacter / GetCameraEntry）
    │
    ├─ 被依赖：
    │   ├─ 逻辑状态系统（GetScriptsByType 查脚本）
    │   ├─ 事件系统（RegisterTarget 注册入口）
    │   └─ 输入中转（FindInputEntry 找实体输入入口）
    │
    └─ 对外接口：
        LogicScriptBase.Entity / ContainerId
        LoadFromEntry / SaveToEntry / OnAllStateInjected
        EventEntryBase.EmitFromExternal / RegisterMethod
        EntityInputEntry.RegisterInput / ReceiveInput
```



**被谁依赖**：逻辑状态系统、事件系统、输入中转

**依赖谁**：逻辑状态系统、输入系统、事件系统、配置系统、工厂系统、输入中转

------

**第 7 章结束。**