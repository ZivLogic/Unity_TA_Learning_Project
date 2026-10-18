using UnityEngine;

/// <summary>
/// 角色输入逻辑脚本
/// 挂在角色实体的逻辑脚本配置里
/// </summary>
public class CharacterInputEntry : EntityInputEntry
{
    public float MoveSpeed = 3f;

    private Vector3 _moveDir = Vector3.zero;

    protected override void RegisterInputs()
    {
        RegisterInput("MoveUp", e => HandleMove(Vector3.forward, e.KeyState));
        RegisterInput("MoveDown", e => HandleMove(Vector3.back, e.KeyState));
        RegisterInput("MoveLeft", e => HandleMove(Vector3.left, e.KeyState));
        RegisterInput("MoveRight", e => HandleMove(Vector3.right, e.KeyState));
    }

    private void HandleMove(Vector3 dir, InputKeyState state)
    {
        if (state == InputKeyState.Up)
        {
            _moveDir = Vector3.zero;
            Debug.Log($"[{name}] 停止移动");
            return;
        }

        if (state == InputKeyState.Down || state == InputKeyState.Hold)
        {
            _moveDir = dir;
            Debug.Log($"[{name}] 移动 {dir} {state}");
        }
    }

    private void Update()
    {
        if (_moveDir == Vector3.zero) return;
        Debug.Log($"[{name}] Update 移动 {_moveDir}");
        transform.position += _moveDir * MoveSpeed * Time.deltaTime;
    }

    // ========== 持久化 ==========
    public override void LoadFromEntry(ILogicStateEntry entry)
    {
        // 输入逻辑一般无持久状态，需要时扩展
    }

    public override ILogicStateEntry SaveToEntry()
    {
        return null;
    }
}