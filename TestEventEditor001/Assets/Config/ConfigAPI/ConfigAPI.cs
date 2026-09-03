
using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 配置接口：系统访问配置的统一入口
/// </summary>
public static class ConfigAPI
{
    /// <summary>
    /// 获取配置
    /// </summary>
    public static T Get<T>() where T : ConfigDefinition
    {
        return ConfigManager.Instance.GetConfig<T>();
    }

    /// <summary>
    /// 获取配置（按ID）
    /// </summary>
    public static T Get<T>(string configId) where T : ConfigDefinition
    {
        return ConfigManager.Instance.GetConfig<T>(configId);
    }

    /// <summary>
    /// 判断配置是否已加载
    /// </summary>
    public static bool IsLoaded(string configId)
    {
        return ConfigManager.Instance.IsConfigLoaded(configId);
    }

    /// <summary>
    /// 注册配置就绪回调
    /// </summary>
    public static void OnConfigLoaded<T>(Action<T> callback) where T : ConfigDefinition
    {
        var configId = typeof(T).GetCustomAttribute<ConfigAttribute>()?.ConfigId ?? typeof(T).Name;

        if (IsLoaded(configId))
        {
            callback?.Invoke(Get<T>(configId));
        }
        else
        {
            ConfigManager.Instance.OnConfigLoaded += (id, config) =>
            {
                if (id == configId && config is T typedConfig)
                {
                    callback?.Invoke(typedConfig);
                }
            };
        }
    }
}