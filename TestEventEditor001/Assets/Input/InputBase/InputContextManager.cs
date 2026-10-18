using System;
using UnityEngine;

/// <summary>
/// 输入上下文管理
/// 上下文决定：同一个按键在不同情境下映射到不同功能
/// 只负责记录和切换，不负责判定
/// </summary>
public class InputContextManager
{
    // 当前上下文
    public string CurrentContext { get; private set; }

    // 上下文变更事件（外部可监听）
    public event Action<string, string> OnContextChanged; // (old, new)

    /// <summary>
    /// 初始化
    /// </summary>
    public void Init(string defaultContext)
    {
        CurrentContext = string.IsNullOrEmpty(defaultContext)
            ? "GameWorld"
            : defaultContext;
        Debug.Log($"[InputContextManager]初始上下文：{CurrentContext}");
    }

    /// <summary>
    /// 切换上下文
    /// </summary>
    public void Switch(string context)
    {
        if (string.IsNullOrEmpty(context))
        {
            Debug.LogWarning("[InputContextManager]切换上下文为空，忽略");
            return;
        }
        if (context == CurrentContext) return;

        string old = CurrentContext;
        CurrentContext = context;
        OnContextChanged?.Invoke(old, context);
        Debug.Log($"[InputContextManager]上下文切换：{old} → {context}");
    }

    /// <summary>
    /// 判断配置是否允许当前上下文
    /// </summary>
    public bool IsAllowed(System.Collections.Generic.List<string> allowContext)
    {
        if (allowContext == null || allowContext.Count == 0) return true;
        return allowContext.Contains(CurrentContext);
    }
}