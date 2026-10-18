using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

//标记驱动的输入配置扫描与加载
//只认 InputConfigDefinition + InputConfigAttribute
public static class InputConfigAutoLoader
{
    private static List<Type> _cachedTypes;

    //扫描所有输入配置类型，按 Priority 排序
    public static List<Type> ScanInputConfigTypes()
    {
        if (_cachedTypes != null)return _cachedTypes;

        var result = new List<Type>();
        var asm = Assembly.GetExecutingAssembly();
        foreach (var type in asm.GetTypes() )
        {
            if (type.IsAbstract) continue;
            if (!typeof(InputConfigDefinition).IsAssignableFrom(type)) continue;
            var attr = type.GetCustomAttribute<InputConfigAttribute>();
            if (attr == null) continue;
            result.Add(type);
        }

        result.Sort((a, b) =>
        {
            var pa = GetAttr(a)?.Priority ?? 100;
            var pb = GetAttr(b)?.Priority ?? 100;
            return pa.CompareTo(pb);
        });

        _cachedTypes = result;
        return result;
    }

    public static InputConfigAttribute GetAttr(Type type)
    {
        return type.GetCustomAttribute<InputConfigAttribute>();
    }

    //把加载出来的子配置挂到顶级容器对应字段
    public static void AssignToRoot(InputConfigRoot root, InputConfigDefinition obj)
    {
        if (root == null || obj == null) return;
        switch (obj)
        {
            case InputGlobalConfig g: root.GlobalConfig = g; break;
            case InputBindContainer b: root.BindContainer = b; break;
            case InputInterceptorContainer i: root.InterceptorContainer = i;break;
            case InputStateContainer s: root.StateContainer = s; break;
            case InputEventMapContainer e: root.EventMapContainer = e; break;
            case InputIntentContainer t: root.IntentContainer = t; break;
            case InputAreaModelContainer a : root.AreaModelContainer = a; break;
            case InputScopeContainer sc: root.ScopeContainer = sc; break;
            default:
                Debug.LogWarning($"[InputConfigAutoLoader] 未知配置类型：{obj.GetType().Name}");
                break;
        }
    }
}