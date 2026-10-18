using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 输入中转层
/// 职责：
/// 1. 拉输入系统标准输出
/// 2. 按范围类型决定是否射线
/// 3. Self → 直接发主角
/// 4. Global → 全场景
/// 5. Single/Category/ValueRange/Area → 摄像机射线解析
/// 6. 预筛 → 组装事件 → 实体系统
/// </summary>
public class InputMediator : MonoBehaviour
{
    [Header("摄像机")]
    public CameraTestScript TargetCamera;

    [Header("调试")]
    public bool DebugLog = true;

    // 当前主角 UUID
    private string _mainCharacterUuid;

    private int _lastFrame = -1;

    // ========== 主角注册 ==========
    public void SetMainCharacter(string uuid)
    {
        _mainCharacterUuid = uuid;
        Log($"主角注册：{uuid}");
    }

    public string GetMainCharacter()
    {
        return _mainCharacterUuid;
    }

    // ========== 主循环 ==========
    private void Update()
    {
        var output = InputManager.Instance.GetStandardOutput();
        if (output == null || !output.HasOutput) return;
        if (output.Frame == _lastFrame) return;
        _lastFrame = output.Frame;

        Log($"收到：{output.Action} {output.KeyState} " +
            $"Scope={output.Scope.ScopeId}");

        //SelectScopeTool.Describe(output.Scope)

        var uuidList = ResolveScope(output);
        if (uuidList == null || uuidList.Count == 0)
        {
            Log("无有效实体");
            return;
        }
        Log($"解析到 {uuidList.Count} 个实体");

        var evt = BuildEvent(output, uuidList);
        PushEvent(evt);





        // 收到 AreaSelect 且按下 → 通知摄像机进入选择模式
        if (output.Action == "AreaSelect" && output.KeyState == InputKeyState.Down)
        {
            var camera = SelectCamera(output);
            if (camera != null)
                camera.BeginSelect();
        }

        // InputMediator.Update 里
        Debug.Log($"[Mediator] Action={output.Action} Scope={output.Scope?.ScopeType} " +
                  $"TargetUuids=[{string.Join(",", uuidList)}]");
    }

    // ========== 按范围解析 ==========
    private List<string> ResolveScope(InputOutputPayload output)
    {
        var scope = output.Scope;
        if (scope == null)
        {
            // 没范围，默认当 Self
            return ResolveSelf();
        }

        var scopeType = scope.GetScopeType();

        switch (scopeType)
        {
            case SelectScopeType.Self:
                return ResolveSelf();

            case SelectScopeType.Global:
                return ResolveGlobal();

            case SelectScopeType.Single:
            case SelectScopeType.Category:
            case SelectScopeType.ValueRange:
            case SelectScopeType.Area:
                return ResolveByCamera(scope, output);

            default:
                Log($"未知范围类型：{scope.ScopeType}");
                return new List<string>();
        }
    }

    // ========== Self：直接主角 ==========
    private List<string> ResolveSelf()
    {
        if (string.IsNullOrEmpty(_mainCharacterUuid))
        {
            Log("Self：主角未注册");
            return new List<string>();
        }

        // 验证工厂有这个实体
        if (MasterFactory.Instance.FindByUUID(_mainCharacterUuid) == null)
        {
            Log($"Self：工厂无主角 {_mainCharacterUuid}");
            return new List<string>();
        }

        Log($"Self：主角 {_mainCharacterUuid}");
        return new List<string> { _mainCharacterUuid };
    }

    // ========== Global：全场景 ==========
    private List<string> ResolveGlobal()
    {
        var all = MasterFactory.Instance.GetAllEntities();
        var result = new List<string>();
        foreach (var e in all)
        {
            if (e == null || string.IsNullOrEmpty(e.UUID)) continue;
            if (!result.Contains(e.UUID))
                result.Add(e.UUID);
        }
        Log($"Global：{result.Count} 个");
        return result;
    }

    // ========== 需要射线的范围 ==========
    private List<string> ResolveByCamera(SelectScope scope, InputOutputPayload output)
    {
        var camera = SelectCamera(output);
        if (camera == null)
        {
            Log("无摄像机");
            return new List<string>();
        }

        var entities = camera.ResolveScope(scope, output);
        if (entities == null || entities.Count == 0)
        {
            Log("摄像机未解析到");
            return new List<string>();
        }

        return PreFilterAndGetUuids(entities);
    }

    // ========== 选摄像机 ==========
    private CameraTestScript SelectCamera(InputOutputPayload output)
    {
        if (TargetCamera != null) return TargetCamera;
        return FindObjectOfType<CameraTestScript>();
    }

    // ========== 预筛 ==========
    private List<string> PreFilterAndGetUuids(List<GameObject> entities)
    {
        var result = new List<string>();
        foreach (var go in entities)
        {
            if (go == null) continue;

            var mono = EntityMono.FindInParents(go);
            if (mono == null)
            {
                Log($"跳过无 EntityMono：{go.name}");
                continue;
            }

            if (string.IsNullOrEmpty(mono.UUID))
            {
                Log($"跳过 UUID 空：{go.name}");
                continue;
            }

            if (MasterFactory.Instance.FindByUUID(mono.UUID) == null)
            {
                Log($"工厂无此 UUID：{mono.UUID}");
                continue;
            }

            if (!result.Contains(mono.UUID))
                result.Add(mono.UUID);
        }
        return result;
    }

    // ========== 组装事件 ==========
    private EntityInputEvent BuildEvent(InputOutputPayload output, List<string> uuids)
    {
        return new EntityInputEvent
        {
            Action = output.Action,
            ActionKey = output.ActionKey,
            Context = output.Context,
            KeyState = output.KeyState,
            Device = output.Device,
            Param = output.Param,
            Frame = output.Frame,
            TargetUuids = uuids
        };
    }

    private void PushEvent(EntityInputEvent evt)
    {
        if (EntitySystem.Instance == null)
        {
            Debug.LogError("[Mediator] EntitySystem.Instance 为空");
            return;
        }
        EntitySystem.Instance.InjectInput(evt);
    }

    // InputMediator 里加
    private void Start()
    {
        // 场景里如果有 Tag=Player 的实体，默认设为主角
        var player = MasterFactory.Instance.FindFirstByTag("Player");
        if (player != null)
        {
            SetMainCharacter(player.UUID);
            Log($"启动默认主角：{player.UUID}");
        }
    }

    private void Log(string msg)
    {
        if (DebugLog) Debug.Log($"[Mediator] {msg}");
    }
}