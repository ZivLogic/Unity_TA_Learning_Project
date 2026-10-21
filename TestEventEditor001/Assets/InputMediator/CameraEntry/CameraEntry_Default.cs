using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 默认摄像机入口
/// 具体选择流程：
/// 1. R → 进入选择状态
/// 2. 左键 → 确认选择 → 射线 → 主角
/// 3. 其他输入 → 按范围选择 → 返回 UUID
/// </summary>
public class CameraEntry_Default : CameraEntryBase
{
    public bool IsSelecting { get; private set; }
    public GameObject SelectedEntity { get; private set; }

    protected override void RegisterIntents()
    {
        RegisterIntent("AreaSelect", OnEnterSelect);
        RegisterIntent("Attack", OnAttack);
        RegisterIntent("Interact", OnInteract);
        RegisterIntent("MoveUp", OnMove);
        RegisterIntent("MoveDown", OnMove);
        RegisterIntent("MoveLeft", OnMove);
        RegisterIntent("MoveRight", OnMove);
        RegisterIntent("MoveDiagonal", OnMove);
    }

    // ========== 切换清理 ==========
    public override void OnDeactivated()
    {
        base.OnDeactivated();

        if (IsSelecting)
        {
            IsSelecting = false;
            SelectedEntity = null;
            Log("切走时取消选择");
        }
    }

    public override void OnActivated()
    {
        base.OnActivated();

        // 切回时重置选择状态
        IsSelecting = false;
        SelectedEntity = null;
    }

    // ========== 进入选择 ==========
    private void OnEnterSelect(InputOutputPayload output)
    {
        if (output.KeyState != InputKeyState.Down) return;
        if (IsSelecting) return;

        IsSelecting = true;
        SelectedEntity = null;
        Log("=== 进入选择状态 ===");
    }

    // ========== 攻击 ==========
    private void OnAttack(InputOutputPayload output)
    {
        if (IsSelecting && output.KeyState == InputKeyState.Down)
        {
            Log("=== 确认选择 ===");
            var hit = RaycastEntity();
            EndSelect(hit);
            return;
        }

        if (output.KeyState == InputKeyState.Down)
        {
            var entities = ResolveByScope(output.Scope);
            var uuids = ToUuids(entities);
            Log($"攻击解析到 {uuids.Count} 个实体");

            _result.HasResult = true;
            _result.Action = output.Action;
            _result.Uuids = uuids;
            _result.Frame = output.Frame;
        }
    }

    // ========== 交互 ==========
    private void OnInteract(InputOutputPayload output)
    {
        if (output.KeyState != InputKeyState.Down) return;

        var entities = ResolveByScope(output.Scope);
        var uuids = ToUuids(entities);
        Log($"交互解析到 {uuids.Count} 个实体");

        _result.HasResult = true;
        _result.Action = output.Action;
        _result.Uuids = uuids;
        _result.Frame = output.Frame;
    }

    // ========== 移动 ==========
    private void OnMove(InputOutputPayload output)
    {
        var entities = ResolveByScope(output.Scope);
        var uuids = ToUuids(entities);
        if (uuids.Count == 0) return;

        _result.HasResult = true;
        _result.Action = output.Action;
        _result.Uuids = uuids;
        _result.Frame = output.Frame;
    }

    // ========== 选择结束 ==========
    private void EndSelect(GameObject entity)
    {
        IsSelecting = false;
        SelectedEntity = entity;

        if (entity != null)
        {
            var mono = EntityMono.FindInParents(entity);
            if (mono != null)
            {
                Log($"=== 选择结束：{mono.UUID} ===");
                _mainCharacterUuid = mono.UUID;

                _result.HasResult = true;
                _result.Action = "SelectMainCharacter";
                _result.SingleUuid = mono.UUID;
                _result.Uuids = new List<string> { mono.UUID };
                _result.Frame = Time.frameCount;
            }
        }
        else
        {
            Log("=== 选择结束：无目标 ===");
        }
    }

    private void Update()
    {
        if (IsSelecting && Input.GetKeyDown(KeyCode.Escape))
        {
            IsSelecting = false;
            Log("取消选择");
        }
    }
}