using System;
using System.Collections.Generic;
using System.Reflection;


//配置标记：声明配置类与JSON文件的对应关系
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ConfigAttribute : Attribute
{
    //配置ID（唯一标识）
    public string ConfigId { get; }

    //JSON文件路径（相对于 Assets/Configs/ConfigData）
    public string JsonPath {  get; }

    //优先级（越小越先加载）
    public int Priority {  get; }

    //依赖的配置ID列表
    public string[] DependsOn { get; }

    public ConfigAttribute(string configId, string jsonPath = null, int priority = 100, params string[] dependsOn)
    {
        ConfigId = configId;
        JsonPath = jsonPath ?? $"{configId}.Json";
        Priority = priority;
        DependsOn = dependsOn ?? Array.Empty<string>();
    }
}

///配置基础容器：所有配置的基类
public abstract class ConfigDefinition
{
    //配置ID（从标记自动获取）
    public string ConfigId
    {
        get
        {
            var attr = GetType().GetCustomAttribute<ConfigAttribute>();
            return attr?.ConfigId ?? GetType().Name;
        }
    }

    //优先级
    public int Priority
    {
        get
        {
            var attr = GetType().GetCustomAttribute<ConfigAttribute>();
            return attr?.Priority ?? 100;
        }
    }

    //依赖的配置ID
    public string[] DependsOn
    {
        get
        {
            var attr = GetType().GetCustomAttribute<ConfigAttribute>();
            return attr?.DependsOn ?? Array.Empty<string>();
        }
    }
}
