using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.VisualScripting;
using UnityEngine;
using DiagDebug = UnityEngine.Debug;

public static class SelectScopeTool
{
    public static SelectScope Single(string entityId = null)
    {
        return new SelectScope
        {
            ScopeId = "Single",
            ScopeType = SelectScopeType.Single.ToString(),
            SingleEntityId = entityId
        };
    }

    public static SelectScope Self()
    {
        return new SelectScope { ScopeId = "Self", ScopeType = SelectScopeType.Self.ToString() };
    }

    public static SelectScope ByCategory(string categoryTag)
    {
        return new SelectScope
        {
            ScopeId = $"Category_{categoryTag}",
            ScopeType = SelectScopeType.Category.ToString(),
            CategoryTag = categoryTag
        };
    }

    public static SelectScope ByValue(string valueKey, float min, float max)
    {
        return new SelectScope
        {
            ScopeType = SelectScopeType.ValueRange.ToString(),
            ValueKey = valueKey,
            MinValue = min,
            MaxValue = max
        };
    }

    public static SelectScope ByArea(string modelId, float radius = 0f,
        float height = 0f, float length = 0f, float width = 0f, float angle = 0f)
    {
        return new SelectScope
        {
            ScopeType = SelectScopeType.Area.ToString(),
            AreaModelId = modelId,
            Radius = radius,
            Height = height,
            Length = length,
            Width = width,
            Angle = angle
        };
    }

    public static SelectScope Global()
    {
        return new SelectScope { ScopeId = "Global", ScopeType = SelectScopeType.Global.ToString() };
    }

    public static bool IsValid(SelectScope scope)
    {
        if (scope == null) return false;

        var scopeType = scope.GetScopeType();

        switch (scopeType)
        {
            case SelectScopeType.Single:
            case SelectScopeType.Self:
            case SelectScopeType.Global:
                return true;
            case SelectScopeType.Category:
                return !string.IsNullOrEmpty(scope.CategoryTag);
            case SelectScopeType.ValueRange:
                return !string.IsNullOrEmpty(scope.ValueKey)
                       && scope.MinValue <= scope.MaxValue;
            case SelectScopeType.Area:
                return !string.IsNullOrEmpty(scope.AreaModelId);
            default:
                return false;
        }
    }

    public static string Describe(SelectScope scope)
    {
        if (scope == null) return "null";

        var scopeType = scope.GetScopeType();

        switch (scopeType)
        {
            case SelectScopeType.Single:
                return $"Single({scope.SingleEntityId ?? "auto"})";
            case SelectScopeType.Self:
                return "Self";
            case SelectScopeType.Category:
                return $"Category({scope.CategoryTag})";
            case SelectScopeType.ValueRange:
                return $"ValueRange({scope.ValueKey} in [{scope.MinValue},{scope.MaxValue}])";
            case SelectScopeType.Area:
                return $"Area({scope.AreaModelId} R={scope.Radius} H={scope.Height} L={scope.Length} W={scope.Width} A={scope.Angle})";
            case SelectScopeType.Global:
                return "Global";
            default:
                return scope.ScopeType.ToString();
        }
    }

    public static SelectScope Clone(SelectScope src)
    {
        if (src == null) return null;
        return new SelectScope
        {
            ScopeId = src.ScopeId,
            ScopeType = src.ScopeType,
            SingleEntityId = src.SingleEntityId,
            CategoryTag = src.CategoryTag,
            CategoryMatchMode = src.CategoryMatchMode,
            ValueKey = src.ValueKey,
            MinValue = src.MinValue,
            MaxValue = src.MaxValue,
            AreaModelId = src.AreaModelId,
            Radius = src.Radius,
            Height = src.Height,
            Length = src.Length,
            Width = src.Width,
            Angle = src.Angle,
            AreaCenter = src.AreaCenter,
            IncludeSelf = src.IncludeSelf,
            Remark = src.Remark
        };
    }
}

/// <summary>
/// 范围校验：结合状态机，判断当前 Scope 是否被允许
/// 只校验"允许不允许"，不查实体
/// </summary>
public static class SelectScopeValidator
{
    public static bool IsAllowed(
        SelectScope scope,
        InputStateConfig state,
        InputAreaModelContainer areaModelContainer)
    {
        if (scope == null) return true;
        if (state == null) return true;

        var scopeType = scope.GetScopeType();

        if (scopeType == SelectScopeType.Self)return true;

        // 1. 范围类型是否允许
        if (state.AllowScopeTypes != null && state.AllowScopeTypes.Count > 0)
        {
            string typeName = scope.ScopeType.ToString();
            if (!state.AllowScopeTypes.Contains(typeName))
            {
                DiagDebug.LogWarning($"[SelectScopeValidator] 范围不允许/不在配置中，范围名：{typeName}");
                return false;
            }
        }

        // 2. Category：类别是否允许
        if (scopeType == SelectScopeType.Category
            && state.AllowCategories != null && state.AllowCategories.Count > 0)
        {
            if (!state.AllowCategories.Contains(scope.CategoryTag))
            {
                DiagDebug.LogWarning($"[SelectScopeValidator] 类别不允许/不在配置中，类别名：{scope.CategoryTag}");
                return false;
            }
        }

        // 3. ValueRange：数值键是否允许
        if (scopeType == SelectScopeType.ValueRange
            && state.AllowValueKeys != null && state.AllowValueKeys.Count > 0)
        {
            if (!state.AllowValueKeys.Contains(scope.ValueKey))
                return false;
        }

        // 4. Area：区域模型是否允许
        if (scopeType == SelectScopeType.Area)
        {
            // 模型ID必须在配置里存在
            if (areaModelContainer != null
                && !areaModelContainer.Dict.ContainsKey(scope.AreaModelId))
            {
                DiagDebug.LogWarning($"[SelectScopeValidator] 区域不在配置中，区域名：{scope.AreaModelId}");
                return false;
            }

            // 状态允许的模型ID
            if (state.AllowAreaModelIds != null && state.AllowAreaModelIds.Count > 0)
            {
                if (!state.AllowAreaModelIds.Contains(scope.AreaModelId))
                {
                    DiagDebug.LogWarning($"[SelectScopeValidator] 区域不允许，区域名：{scope.AreaModelId}");
                    return false;
                }
            }
        }

        return true;
    }
}