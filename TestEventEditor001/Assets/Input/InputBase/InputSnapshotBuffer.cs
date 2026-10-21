using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 双帧快照缓冲
/// 维护上一帧和当前帧的硬件状态
/// 提供三态判断：Down / Hold / Up / None
/// </summary>
public class InputSnapshotBuffer
{
    // 键盘
    private Dictionary<KeyCode, bool> _keyCurr = new Dictionary<KeyCode, bool>();
    private Dictionary<KeyCode, bool> _keyPrev = new Dictionary<KeyCode, bool>();

    // 鼠标（0左 1右 2中）
    private bool[] _mouseCurr = new bool[3];
    private bool[] _mousePrev = new bool[3];

    // 通用轴
    public Vector2 MoveAxis { get; private set; }
    public Vector2 LookAxis { get; private set; }

    // 需要采样的按键集合（由配置决定）
    private HashSet<KeyCode> _sampleKeys = new HashSet<KeyCode>();

    /// <summary>
    /// 注册需要采样的按键
    /// </summary>
    public void RegisterSampleKey(KeyCode key)
    {
        if (key != KeyCode.None)
            _sampleKeys.Add(key);
    }

    public void ClearSampleKeys()
    {
        _sampleKeys.Clear();
    }

    /// <summary>
    /// 每帧刷新快照
    /// </summary>
    public void Refresh()
    {
        // 键盘：当前帧 → 上一帧
        _keyPrev.Clear();
        foreach (var kv in _keyCurr)
            _keyPrev[kv.Key] = kv.Value;

        // 鼠标
        for (int i = 0; i < 3; i++)
            _mousePrev[i] = _mouseCurr[i];

        // 刷新当前帧
        _keyCurr.Clear();
        foreach (var key in _sampleKeys)
            _keyCurr[key] = Input.GetKey(key);

        _mouseCurr[0] = Input.GetMouseButton(0);
        _mouseCurr[1] = Input.GetMouseButton(1);
        _mouseCurr[2] = Input.GetMouseButton(2);

        // 轴
        MoveAxis = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical"));
        LookAxis = new Vector2(
            Input.GetAxisRaw("Mouse X"),
            Input.GetAxisRaw("Mouse Y"));

    }

    /// <summary>
    /// 键盘三态
    /// </summary>
    public InputKeyState GetKeyState(KeyCode key)
    {
        bool curr = _keyCurr.TryGetValue(key, out var c) && c;
        bool prev = _keyPrev.TryGetValue(key, out var p) && p;
        return CalcState(curr, prev);
    }

    /// <summary>
    /// 鼠标三态
    /// </summary>
    public InputKeyState GetMouseState(int mouseIndex)
    {
        if (mouseIndex < 0 || mouseIndex >= 3) return InputKeyState.None;
        return CalcState(_mouseCurr[mouseIndex], _mousePrev[mouseIndex]);
    }

    private InputKeyState CalcState(bool curr, bool prev)
    {
        if (curr && !prev) return InputKeyState.Down;
        if (curr && prev) return InputKeyState.Hold;
        if (!curr && prev) return InputKeyState.Up;
        return InputKeyState.None;
    }
}