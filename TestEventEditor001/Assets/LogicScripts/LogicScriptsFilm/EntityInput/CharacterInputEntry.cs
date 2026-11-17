using UnityEngine;
using EventSystemV2;
using System.Collections.Generic;

public class CharacterInputEntry : EntityInputEntry, INeedEventEntry, IEntityStateMachineProvider
{
    public EventEntryBase EventEntry { get; set; }

    private EntityStateMachineBase _stateMachine;

    public void InjectStateMachine(EntityStateMachineBase stateMachine)
    {
        _stateMachine = stateMachine;
        Debug.Log($"[输入入口]绑定状态机：{stateMachine.GetType().Name}");
    }

    [Header("移动")]
    public float MoveSpeed = 3f;
    public float RotateSpeed = 15f;

    private Vector3 _finalMoveDir = Vector3.zero;

    protected override void RegisterInputs()
    {
        // 移动相关的输入事件我们直接不用管了，全交给 Update 轮询
        // 这里只保留其他瞬时的交互按键（比如测试的按键）
        RegisterInput("Inpt_Test_01", HandleTestClass);
        RegisterInput("Inpt_Test_02", HandleTestClass_003);
        RegisterInput("Attack", HandleAttackResult);
        RegisterInput("Interact", HandleInteractResult);
    }

    private void Update()
    {
        // 1. 每帧向 InputManager 询问真实物理状态
        bool isUp = InputManager.Instance.IsActionHeld("MoveUp");
        bool isDown = InputManager.Instance.IsActionHeld("MoveDown");
        bool isLeft = InputManager.Instance.IsActionHeld("MoveLeft");
        bool isRight = InputManager.Instance.IsActionHeld("MoveRight");

        // 2. 收集基础输入方向
        float h = (isRight ? 1 : 0) - (isLeft ? 1 : 0);
        float v = (isUp ? 1 : 0) - (isDown ? 1 : 0);

        Vector3 localInput = new Vector3(h, 0, v);

        if (localInput.sqrMagnitude < 0.01f)
        {
            _finalMoveDir = Vector3.zero;
            return;
        }

        // 3. 相机相对移动
        var cam = CameraRigScript.ActiveCamera;
        if (cam != null)
        {
            Vector3 camForward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
            Vector3 camRight = Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized;
            _finalMoveDir = (camForward * v + camRight * h).normalized;
        }
        else
        {
            _finalMoveDir = localInput.normalized;
        }

        // 4. 执行移动
        transform.position += _finalMoveDir * MoveSpeed * Time.deltaTime;

        // 5. 角色平滑转身
        Quaternion targetRot = Quaternion.LookRotation(_finalMoveDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, RotateSpeed * Time.deltaTime);
    }

    // ========== 其他保留的测试逻辑 ==========
    private void HandleMoveDown(EntityInputEvent e)
    {
        if (e.KeyState != InputKeyState.Down) return;

        TestEntry testEntry = null;
        foreach (var script in Entity.LogicScripts)
        {
            if (script is TestEntry te) { testEntry = te; break; }
        }

        if (testEntry != null) testEntry.TEST_001();
    }

    private void HandleMoveRight(EntityInputEvent e)
    {
        if (e.KeyState != InputKeyState.Down) return;

        if (EventEntry != null)
        {
            EventEntry.EmitFromExternal(this, nameof(InputMoveToEvent), new List<string> { "Pawn_0005" }, 2, "InputTest");
        }
    }

    [EventPublishAttr]
    public void InputMoveToEvent(List<string> UUIDS, int vle, string name) { }





    private void HandleTestClass(EntityInputEvent e)
    {
        if (e.KeyState != InputKeyState.Down) return;

        var snapshot = new Event_Class_Test_001 { TestName = "TestEventClass" };

        string snapshotJson = JsonUtility.ToJson(snapshot);

        if (EventEntry != null)
        {
            EventEntry.EmitFromExternal(this, nameof(Test_Class_Event), new List<string> { "Pawn_0005" }, snapshotJson);
        }
    }

    [EventPublishAttr]
    public void Test_Class_Event(List<string> UUIDS, string evt) { }





    private void HandleTestClass_002(EntityInputEvent e)
    {
        if (e.KeyState != InputKeyState.Down) return;

        var snapshot = new Event_Class_Test_001 { TestName = "TestEventClass" };

        string snapshotJson = JsonUtility.ToJson(snapshot);

        if (EventEntry != null)
        {
            EventEntry.EmitFromExternal(this, nameof(Test_Class_Event_002), new List<string> { "Pawn_0005" }, snapshot);
        }
    }

    [EventPublishAttr]
    public void Test_Class_Event_002(List<string> UUIDS, Event_Class_Test_001 evt) { }


    private void HandleTestClass_003(EntityInputEvent e)
    {
        if (e.KeyState != InputKeyState.Down) return;

        if (EventEntry != null)
        {
            EventEntry.EmitFromExternal(this, nameof(Test_Class_Event_003), new List<string> { "Pawn_0005" }, "测试成功！");
        }
    }

    [EventPublishAttr]
    public void Test_Class_Event_003(List<string> UUIDS, string TEST) { }



    private void HandleAttackResult(EntityInputEvent e)
    {
        //测试更改状态
        _stateMachine.TryChangeAction(EntityActionState.Attacking);
        //测试添加Buff
        _stateMachine.AddBuff("Burning");

        if (e.KeyState != InputKeyState.Down) return;
        Debug.Log("检测到攻击");

        // 如果 TargetUuids 为空，说明摄像机判定打空了
        if (e.TargetUuids == null || e.TargetUuids.Count == 0)
        {
            Debug.Log("枪打空了！");
            // 这里可以放一个空枪音效或特效
            return;
        }

        Debug.Log($"打中了！目标：{e.TargetUuids[0]}");

        //测试状态发送
        var state = _stateMachine.GetSnapshot();
        //Debug.Log($"ID:{state.UUID}, 生命状态:{state.Lifecycle}, 活动状态:{state.Action}");
        //foreach (var Buff in state.Tags)
        //    Debug.Log($"Buff状态:{Buff}");

        //var state_002 = new EntityStateSnapshot {Tags = new string[] { "Burning" } };

        // 发包给受害者
        if (EventEntry != null)
        {
            EventEntry.EmitFromExternal(this, nameof(OnShootPublish), e.TargetUuids, state);
        }
    }

    [EventPublishAttr]
    public void OnShootPublish(List<string> UUIDS, EntityStateSnapshot snapshot) { }


    // 主角交互脚本里
    private void HandleInteractResult(EntityInputEvent e)
    {
        if (e.KeyState != InputKeyState.Down) return;

        if (e.TargetUuids == null || e.TargetUuids.Count == 0)
        {
            Debug.Log("没选到交互物");
            return;
        }

        string targetUuid = e.TargetUuids[0];
        Debug.Log($"检测到 {e.TargetUuids.Count} 个可交互实体，准备与 {targetUuid} 等交互");

        // 发包给目标实体（使用你的事件系统）
        if (EventEntry != null)
        {
            EventEntry.EmitFromExternal(this, nameof(OnInteractPublish), new List<string> { targetUuid });
        }
    }

    [EventPublishAttr]
    public void OnInteractPublish(List<string> Uuids) { }

    public override void LoadFromEntry(ILogicStateEntry entry) { }
    public override ILogicStateEntry SaveToEntry() => null;
}