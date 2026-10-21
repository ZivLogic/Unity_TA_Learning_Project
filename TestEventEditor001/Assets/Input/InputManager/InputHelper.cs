using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 输入系统工具方法
/// 新版：只认 Key1/Key2/Key3，不兼容旧字段
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
    /// 取主键（Key1）
    /// </summary>
    public static string GetPrimaryKey(InputKeyConfig cfg)
    {
        if (cfg == null) return null;
        return cfg.Key1;
    }

    /// <summary>
    /// 取备用键（AltKey1）
    /// </summary>
    public static string GetPrimaryAltKey(InputKeyConfig cfg)
    {
        if (cfg == null) return null;
        return cfg.AltKey1;
    }

    /// <summary>
    /// 取所有键位（主键 + 备用键）
    /// </summary>
    public static List<string> GetAllKeys(InputKeyConfig cfg)
    {
        var list = new List<string>();
        if (cfg == null) return list;

        if (!string.IsNullOrEmpty(cfg.Key1)) list.Add(cfg.Key1);
        if (!string.IsNullOrEmpty(cfg.Key2)) list.Add(cfg.Key2);
        if (!string.IsNullOrEmpty(cfg.Key3)) list.Add(cfg.Key3);
        if (!string.IsNullOrEmpty(cfg.AltKey1)) list.Add(cfg.AltKey1);
        if (!string.IsNullOrEmpty(cfg.AltKey2)) list.Add(cfg.AltKey2);
        if (!string.IsNullOrEmpty(cfg.AltKey3)) list.Add(cfg.AltKey3);

        return list;
    }

    /// <summary>
    /// 取组合键（Key1/Key2/Key3，不含备用键）
    /// </summary>
    public static List<string> GetComboKeys(InputKeyConfig cfg)
    {
        var list = new List<string>();
        if (cfg == null) return list;

        if (!string.IsNullOrEmpty(cfg.Key1)) list.Add(cfg.Key1);
        if (!string.IsNullOrEmpty(cfg.Key2)) list.Add(cfg.Key2);
        if (!string.IsNullOrEmpty(cfg.Key3)) list.Add(cfg.Key3);

        return list;
    }

    /// <summary>
    /// 检查按键冲突（同上下文内）
    /// 只检查 Key1/Key2/Key3，不检查备用键（备用键允许冲突）
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
            if (cfg.ActionKey == actionKey) continue;

            bool contextOverlap = cfg.AllowContext == null
                || cfg.AllowContext.Count == 0
                || cfg.AllowContext.Contains(context);
            if (!contextOverlap) continue;

            if (cfg.Key1 == newKeyStr
                || cfg.Key2 == newKeyStr
                || cfg.Key3 == newKeyStr)
                return true;
        }
        return false;
    }
}