using UnityEngine;
using EventSystemV2;
using System.Collections.Generic;

/// <summary>
/// 角色输入逻辑脚本
/// 单键 + 组合斜走
/// </summary>
public class CharacterInputEntry : EntityInputEntry, INeedEventEntry
{
    public EventEntryBase EventEntry { get; set; }

    [Header("移动")]
    public float MoveSpeed = 3f;

    // 单键方向
    private Vector3 _moveDir = Vector3.zero;
    // 组合方向
    private Vector3 _comboDir = Vector3.zero;

    protected override void RegisterInputs()
    {
        // 单键
        RegisterInput("MoveUp", e => HandleSingle(Vector3.forward, e.KeyState));
        RegisterInput("MoveDown", HandleMoveDown);
        RegisterInput("MoveLeft", e => HandleSingle(Vector3.left, e.KeyState));
        RegisterInput("MoveRight", HandleMoveRight);

        // 组合
        RegisterInput("MoveDiagonal", e => HandleDiagonal(e.KeyState));
    }

    // ========== 单键 ==========
    private void HandleSingle(Vector3 dir, InputKeyState state)
    {
        if (state == InputKeyState.Up)
        {
            // 组合中，单键 Up 退出组合
            if (_comboDir != Vector3.zero)
            {
                _comboDir = Vector3.zero;
                Debug.Log($"[{name}] 组合退出");
            }

            if (_moveDir == dir)
            {
                _moveDir = Vector3.zero;
                Debug.Log($"[{name}] 停止 {dir}");
            }
            return;
        }

        // 组合中，单键 Down/Hold 忽略
        if (_comboDir != Vector3.zero) return;

        if (state == InputKeyState.Down || state == InputKeyState.Hold)
        {
            _moveDir = dir;
            Debug.Log($"[{name}] 单键 {dir} {state}");
        }
    }

    // ========== 组合斜走 ==========
    private void HandleDiagonal(InputKeyState state)
    {
        if (state == InputKeyState.Up)
        {
            _comboDir = Vector3.zero;
            Debug.Log($"[{name}] 斜走结束");
            return;
        }

        if (state == InputKeyState.Down || state == InputKeyState.Hold)
        {
            _comboDir = (Vector3.forward + Vector3.left).normalized;
            _moveDir = Vector3.zero;
            Debug.Log($"[{name}] 斜走 {_comboDir} {state}");
        }
    }

    // ========== 每帧移动 ==========
    private void Update()
    {
        Vector3 dir = _comboDir != Vector3.zero ? _comboDir : _moveDir;
        if (dir == Vector3.zero) return;
        transform.position += dir * MoveSpeed * Time.deltaTime;
    }

    // 【新增】专门处理按 S 的测试逻辑
    private void HandleMoveDown(EntityInputEvent e)
    {
        // 只在刚按下的那一帧触发，防止按住一直发
        if (e.KeyState != InputKeyState.Down) return;

        // 1. 从当前实体上找 TestEntry 组件
        // （注意：要求 TestEntry 必须和 CharacterInputEntry 挂在同一个实体上）
        TestEntry testEntry = null;
        foreach (var script in Entity.LogicScripts)
        {
            if (script is TestEntry te)
            {
                testEntry = te;
                break;
            }
        }

        // 2. 找到就调用它的测试方法
        if (testEntry != null)
        {
            Debug.Log("【输入层】按下了 S 键，准备触发 TestEntry 的事件发送逻辑");
            testEntry.TEST_001();
        }
        else
        {
            Debug.LogWarning("【输入层】当前实体上没有找到 TestEntry 组件！");
        }
    }

    private void HandleMoveRight(EntityInputEvent e)
    {
        // 只在刚按下的那一帧触发，防止按住一直发
        if (e.KeyState != InputKeyState.Down) return;

        if (EventEntry != null)
        {
            // 把发送任务“代发”给事件入口
            // 告诉它：用我身上的 InputMoveEvent 的配置发
            EventEntry.EmitFromExternal(this, nameof(InputMoveToEvent), new List<string> { "Pawn_0005" }, 2, "InputTest");
        }
    }

    [EventPublishAttr]
    public void InputMoveToEvent(List<string> UUIDS, int vle, string name)
    {

    }
    

    // ========== 持久化 ==========
    public override void LoadFromEntry(ILogicStateEntry entry) { }
    public override ILogicStateEntry SaveToEntry() => null;
}