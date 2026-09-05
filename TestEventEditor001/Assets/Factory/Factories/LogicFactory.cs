// Framework/Factories/LogicFactory.cs
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 逻辑工厂：流水线第三步
/// 职责：根据蓝图配置，给逻辑体挂载逻辑脚本
/// </summary>
public class LogicFactory : IFactoryStep
{
    /// <summary>
    /// 处理实体
    /// </summary>
    public void Process(Entity entity)
    {
        if (entity?.Blueprint == null) return;

        // 获取逻辑脚本ID列表
        var logicScriptIds = entity.Blueprint.logicScriptIds;

        if (logicScriptIds == null || logicScriptIds.Length == 0)
        {
            Debug.Log($"[LogicFactory] {entity.Blueprint.blueprintId} 没有逻辑脚本，跳过");
            return;
        }

        Debug.Log($"[LogicFactory] 开始挂载逻辑脚本: {entity.Blueprint.blueprintId} (共 {logicScriptIds.Length} 个)");

        // 遍历每个脚本ID
        foreach (var scriptId in logicScriptIds)
        {
            // 1. 通过字典键查找脚本配置
            var scriptConfig = ConfigManager.Instance.FindLogicScript(scriptId);

            if (scriptConfig == null)
            {
                Debug.LogError($"[LogicFactory] 找不到逻辑脚本配置: {scriptId}");
                continue;
            }

            // 2. 挂载脚本
            var component = AttachScript(entity.LogicObject, scriptConfig);

            if (component != null)
            {
                // 3. 记录到实体
                entity.LogicScripts.Add(component);

                // 4. 如果脚本实现了 IEntityAware 接口，注入实体引用
                if (component is IEntityAware entityAware)
                {
                    entityAware.Entity = entity;
                    Debug.Log($"[LogicFactory] 注入实体引用到: {scriptConfig.scriptName}");
                }

                // 5. 如果脚本有参数，设置参数
                if (scriptConfig.parameters != null && scriptConfig.parameters.Length > 0)
                {
                    ApplyScriptParameters(component, scriptConfig.parameters);
                }
            }
        }
    }

    #region 脚本挂载

    /// <summary>
    /// 挂载脚本到逻辑体
    /// </summary>
    private MonoBehaviour AttachScript(GameObject logicObj, LogicScriptsConfig.ScriptData scriptConfig)
    {
        // 1. 获取脚本类型
        Type scriptType = FindScriptType(scriptConfig.scriptName);

        if (scriptType == null)
        {
            Debug.LogError($"[LogicFactory] 找不到脚本类型: {scriptConfig.scriptName}");
            return null;
        }

        // 2. 检查是否已经存在
        var existing = logicObj.GetComponent(scriptType);
        if (existing != null)
        {
            Debug.LogWarning($"[LogicFactory] 脚本已存在，跳过: {scriptConfig.scriptName}");
            return existing as MonoBehaviour;
        }

        // 3. 添加脚本
        var component = logicObj.AddComponent(scriptType) as MonoBehaviour;

        if (component != null)
        {
            Debug.Log($"[LogicFactory] 挂载脚本成功: {scriptConfig.scriptName}");
        }

        return component;
    }

    /// <summary>
    /// 查找脚本类型
    /// </summary>
    private Type FindScriptType(string scriptName)
    {
        // 1. 先尝试直接获取类型
        var type = Type.GetType(scriptName);
        if (type != null) return type;

        // 2. 在所有程序集中查找
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(scriptName);
            if (type != null) return type;
        }

        // 3. 尝试添加命名空间
        string[] commonNamespaces = {
            "LogicScripts.",
            "Scripts.Logic.",
            "GameLogic.",
            ""
        };

        foreach (var ns in commonNamespaces)
        {
            type = Type.GetType(ns + scriptName);
            if (type != null) return type;
        }

        return null;
    }

    #endregion

    #region 参数设置

    /// <summary>
    /// 应用脚本参数
    /// </summary>
    private void ApplyScriptParameters(MonoBehaviour component, string[] parameters)
    {
        foreach (var paramStr in parameters)
        {
            // 参数格式: "参数名=参数值"
            string[] parts = paramStr.Split('=');
            if (parts.Length != 2) continue;

            string paramName = parts[0].Trim();
            string paramValue = parts[1].Trim();

            // 通过反射设置字段
            var field = component.GetType().GetField(paramName);
            if (field != null)
            {
                SetFieldValue(component, field, paramValue);
                Debug.Log($"[LogicFactory] 设置参数: {component.GetType().Name}.{paramName} = {paramValue}");
            }

            // 通过反射设置属性
            var property = component.GetType().GetProperty(paramName);
            if (property != null && property.CanWrite)
            {
                SetPropertyValue(component, property, paramValue);
                Debug.Log($"[LogicFactory] 设置属性: {component.GetType().Name}.{paramName} = {paramValue}");
            }
        }
    }

    /// <summary>
    /// 设置字段值（类型转换）
    /// </summary>
    private void SetFieldValue(object obj, System.Reflection.FieldInfo field, string value)
    {
        try
        {
            object convertedValue = ConvertValue(value, field.FieldType);
            field.SetValue(obj, convertedValue);
        }
        catch (Exception e)
        {
            Debug.LogError($"[LogicFactory] 设置字段失败: {field.Name} = {value}\n{e}");
        }
    }

    /// <summary>
    /// 设置属性值（类型转换）
    /// </summary>
    private void SetPropertyValue(object obj, System.Reflection.PropertyInfo property, string value)
    {
        try
        {
            object convertedValue = ConvertValue(value, property.PropertyType);
            property.SetValue(obj, convertedValue);
        }
        catch (Exception e)
        {
            Debug.LogError($"[LogicFactory] 设置属性失败: {property.Name} = {value}\n{e}");
        }
    }

    /// <summary>
    /// 类型转换
    /// </summary>
    private object ConvertValue(string value, Type targetType)
    {
        if (targetType == typeof(string))
            return value;

        if (targetType == typeof(int))
            return int.Parse(value);

        if (targetType == typeof(float))
            return float.Parse(value);

        if (targetType == typeof(bool))
            return bool.Parse(value);

        if (targetType == typeof(Vector3))
        {
            // 格式: (x,y,z)
            value = value.Trim('(', ')');
            string[] parts = value.Split(',');
            if (parts.Length == 3)
            {
                return new Vector3(
                    float.Parse(parts[0]),
                    float.Parse(parts[1]),
                    float.Parse(parts[2])
                );
            }
        }

        if (targetType == typeof(Vector2))
        {
            // 格式: (x,y)
            value = value.Trim('(', ')');
            string[] parts = value.Split(',');
            if (parts.Length == 2)
            {
                return new Vector2(
                    float.Parse(parts[0]),
                    float.Parse(parts[1])
                );
            }
        }

        return null;
    }

    #endregion
}