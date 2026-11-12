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
        RegisterIntent("MoveRightAndForward", OnMove);
        RegisterIntent("MoveLeftAndBack", OnMove);
        RegisterIntent("MoveRightAndBack", OnMove);
        RegisterIntent("Inpt_Test_01", Test_Input);
        RegisterIntent("Inpt_Test_02", Test_Input);
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

    // ========== 攻击（枪械射击） ==========
    private void OnAttack(InputOutputPayload output)
    {
        if (output.KeyState != InputKeyState.Down) return;

        var cam = TargetCamera != null ? TargetCamera : Camera.main;
        if (cam == null) return;

        var mainEntity = MasterFactory.Instance.FindByUUID(_mainCharacterUuid);
        if (mainEntity?.LogicObject == null) return;

        // 1. 枪口坐标
        Vector3 muzzlePos = mainEntity.LogicObject.transform.position + Vector3.up * 1.2f;

        // 2. 摄像机中心交点
        Ray camRay = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
        Vector3 centerPoint = camRay.origin + camRay.direction * RayDistance;
        if (Physics.Raycast(camRay, out RaycastHit camHit, RayDistance, SelectLayerMask))
        {
            centerPoint = camHit.point;
        }

        // 3. 从枪口向交点打射线
        Vector3 shootDir = (centerPoint - muzzlePos).normalized;
        List<string> hitUuids = new List<string>();
        Debug.Log($"有交点 = {shootDir}");

        if (Physics.Raycast(muzzlePos, shootDir, out RaycastHit shootHit, RayDistance, SelectLayerMask))
        {
            var mono = EntityMono.FindInParents(shootHit.collider.gameObject);
            if (mono != null && mono.Entity != null && mono.UUID != _mainCharacterUuid)
            {
                hitUuids.Add(mono.UUID); // 打中的人
            }
        }

        // 4. 填入结果：受害者是 hitUuids，接收者是主角
        _result.HasResult = true;
        _result.Action = output.Action; // "Attack"
        _result.Uuids = hitUuids;
        _result.ReceiverUuids.Add(_mainCharacterUuid); // 告诉中转层发给主角
        _result.Frame = output.Frame;
        //Debug.Log("1");
    }

    // ========== 选择（P键，切换主角） ==========
    private void OnSelect(InputOutputPayload output)
    {
        if (output.KeyState != InputKeyState.Down) return;

        var hit = RaycastEntity();
        if (hit != null)
        {
            var mono = EntityMono.FindInParents(hit);
            if (mono != null)
            {
                _mainCharacterUuid = mono.UUID; // 摄像机内部同步，无需中转层干预
                _result.HasResult = true;
                _result.Action = "SelectMainCharacter";
                _result.SingleUuid = mono.UUID;
                _result.Uuids = new List<string> { mono.UUID };
                _result.ReceiverUuids.Add(mono.UUID); // 发给新主角自己
                _result.Frame = output.Frame;
            }
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

    //测试输入
    private void Test_Input(InputOutputPayload output)
    {
        var entities = ResolveByScope(output.Scope);
        var uuids = ToUuids(entities);
        if (uuids.Count == 0) return;

        _result.HasResult = true;
        _result.Action = output.Action;
        _result.Uuids = uuids;
        _result.Frame = output.Frame;
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