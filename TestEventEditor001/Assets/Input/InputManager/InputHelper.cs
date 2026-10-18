using System;
using UnityEngine;

/// <summary>
/// 输入系统工具方法
/// </summary>
public static class InputHelper
{
    /// <summary>
    /// 字符串 → KeyCode
    /// </summary>
    public static KeyCode StringToKeyCode(string str)
    {
        if (string.IsNullOrEmpty(str)) return KeyCode.None;
        if (Enum.TryParse<KeyCode>(str, true, out var key))
            return key;
        return KeyCode.None;
    }

    /// <summary>
    /// KeyCode → 字符串
    /// </summary>
    public static string KeyCodeToString(KeyCode key)
    {
        return key.ToString();
    }

    /// <summary>
    /// 检查按键冲突（同上下文内）
    /// </summary>
    public static bool CheckKeyConflict(
        KeyCode newKey,
        string actionKey,
        string context,
        InputBindContainer container)
    {
        if (container == null) return false;
        string newKeyStr = KeyCodeToString(newKey);

        foreach (var cfg in container.Items)
        {
            if (cfg == null || !cfg.IsEnable) continue;
            if (cfg.ActionKey == actionKey) continue; // 自己不算冲突

            // 上下文是否重叠
            bool contextOverlap = cfg.AllowContext == null
                || cfg.AllowContext.Count == 0
                || cfg.AllowContext.Contains(context);
            if (!contextOverlap) continue;

            // 按键是否相同
            if (cfg.KeyBindCode == newKeyStr || cfg.AltKeyBindCode == newKeyStr)
                return true;
        }
        return false;
    }
}