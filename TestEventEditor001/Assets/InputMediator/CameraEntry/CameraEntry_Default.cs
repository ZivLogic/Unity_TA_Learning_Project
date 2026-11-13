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



    // 【新增】供外部（瞄准线、攻击逻辑）读取的准星数据
    public Vector3 CurrentAimPoint { get; private set; }
    public Vector3 CurrentMuzzlePos { get; private set; }

    // 专用于攻击和准星的 LayerMask（防止打到主角自己）
    [Header("射击判定层")]
    public LayerMask ShootLayerMask = ~0;

    // 摄像机自己缓存枪口，不依赖中转层
    private Transform _cachedMuzzlePoint;


    private void LateUpdate()
    {
        var mediator = InputMediator.Instance;
        var mainEntity = mediator?.MainCharacterEntity;

        // 1. 如果主角实体存在，但还没缓存枪口，就去扒
        if (mainEntity != null && _cachedMuzzlePoint == null)
        {
            var attachment = FindAttachment(mainEntity);
            if (attachment != null)
            {
                _cachedMuzzlePoint = attachment.MuzzlePoint;
                Debug.Log("[摄像机] 成功获取主角枪口挂载点");
            }
        }

        // 2. 如果主角还没就绪，空转
        if (mainEntity == null || _cachedMuzzlePoint == null)
            return;

        // 3. 更新枪口坐标
        CurrentMuzzlePos = _cachedMuzzlePoint.position;

        // 4. 更新准星点（摄像机正中心射线）
        var cam = TargetCamera != null ? TargetCamera : Camera.main;
        if (cam != null)
        {
            Ray camRay = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
            if (Physics.Raycast(camRay, out RaycastHit camHit, RayDistance, ShootLayerMask))
                CurrentAimPoint = camHit.point;
            else
                CurrentAimPoint = camRay.origin + camRay.direction * RayDistance;
        }
    }

    // 辅助方法：从主角实体里找挂载点脚本
    private CharacterAttachmentScript FindAttachment(Entity entity)
    {
        if (entity?.LogicScripts == null) return null;
        foreach (var script in entity.LogicScripts)
        {
            if (script is CharacterAttachmentScript attachment)
                return attachment;
        }
        return null;
    }

    // ========== 攻击方法 ==========
    private void OnAttack(InputOutputPayload output)
    {
        if (output.KeyState != InputKeyState.Down) return;
        if (_cachedMuzzlePoint == null) return;

        // 从这里开始，射线判定就和前面写的一样了
        Vector3 muzzlePos = CurrentMuzzlePos;
        Vector3 shootDir = (CurrentAimPoint - muzzlePos).normalized;

        List<string> hitUuids = new List<string>();
        float radius = 0.1f;

        muzzlePos += shootDir * 0.1f; // 起点微推，防止卡碰撞体

        if (Physics.SphereCast(muzzlePos, radius, shootDir, out RaycastHit shootHit, RayDistance, ShootLayerMask, QueryTriggerInteraction.Ignore))
        {
            var mono = EntityMono.FindInParents(shootHit.collider.gameObject);
            if (mono != null && mono.Entity != null && mono.UUID != _mainCharacterUuid)
            {
                hitUuids.Add(mono.UUID);
            }
        }

        _result.HasResult = true;
        _result.Action = output.Action;
        _result.Uuids = hitUuids;
        _result.ReceiverUuids.Add(_mainCharacterUuid);
        _result.Frame = output.Frame;
    }

}