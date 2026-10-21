using UnityEngine;

/// <summary>
/// 角色输入逻辑脚本
/// 单键 + 组合斜走
/// </summary>
public class CharacterInputEntry : EntityInputEntry
{
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
        RegisterInput("MoveDown", e => HandleSingle(Vector3.back, e.KeyState));
        RegisterInput("MoveLeft", e => HandleSingle(Vector3.left, e.KeyState));
        RegisterInput("MoveRight", e => HandleSingle(Vector3.right, e.KeyState));

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

    // ========== 持久化 ==========
    public override void LoadFromEntry(ILogicStateEntry entry) { }
    public override ILogicStateEntry SaveToEntry() => null;
}