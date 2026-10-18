using System;
using System.Reflection;

//输入配置标记：声明输入配置类与Json文件的对应关系
//与原配置系统独立

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class InputConfigAttribute : Attribute
{
    public string ConfigId { get; }
    public string JsonFile { get; }
    public int Priority { get; }
    public string[] DependsOn { get; }

    public InputConfigAttribute(
       string configId,
       string jsonFile = null,
       int priority = 100,
       params string[] dependsOn)
    {
        ConfigId = configId;
        JsonFile = jsonFile ?? $"{configId}.json";
        Priority = priority;
        DependsOn = dependsOn ?? Array.Empty<string>();
    }
}

//输入配置类：只服务输入系统
public abstract class InputConfigDefinition
{
    public string ConfigId
    {
        get
        {
            var attr = GetType().GetCustomAttribute<InputConfigAttribute>();
            return attr?.ConfigId ?? GetType().Name;
        }
    }

    public int Priority
    {
        get
        {
            var attr = GetType().GetCustomAttribute<InputConfigAttribute>();
            return attr?.Priority ?? 100;
        }
    }

    public string[] DependsOn
    {
        get
        {
            var attr = GetType().GetCustomAttribute<InputConfigAttribute>();
            return attr?.DependsOn ?? Array.Empty<string>();
        }
    }
}