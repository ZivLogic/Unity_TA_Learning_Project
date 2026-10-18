using System;
using UnityEngine;

/// <summary>
/// 全局输入模式管理
/// 模式决定：输入系统整体的开放程度
/// ForbidAll / OnlyViewDirect / NormalOperate
/// </summary>
public class InputModeManager
{
    public string CurrentMode { get; private set; }

    public event Action<string, string> OnModeChanged;

    public void Init(string defaultMode)
    {
        CurrentMode = string.IsNullOrEmpty(defaultMode)
            ? "NormalOperate"
            : defaultMode;
        Debug.Log($"[InputModeManager]初始模式：{CurrentMode}");
    }

    public void Switch(string mode)
    {
        if (string.IsNullOrEmpty(mode))
        {
            Debug.LogWarning("[InputModeManager]切换模式为空，忽略");
            return;
        }
        if (mode == CurrentMode) return;

        string old = CurrentMode;
        CurrentMode = mode;
        OnModeChanged?.Invoke(old, mode);
        Debug.Log($"[InputModeManager]模式切换：{old} → {mode}");
    }

    /// <summary>
    /// 是否完全禁止输入
    /// </summary>
    public bool IsForbidAll()
    {
        return CurrentMode == "ForbidAll";
    }

    /// <summary>
    /// 是否只允许特定直通道
    /// </summary>
    public bool IsOnlyViewDirect()
    {
        return CurrentMode == "OnlyViewDirect";
    }
}