# 配置标记容器

## 1.标记定义

### 核心定义

- 定义了唯一配置ID标识【ConfigID】用于默认路径识别和唯一配置指定；
- 定义了JSON配置文件路径（暂且只支持Assets/Configs/ConfigData指定路径）
- 定义了优先级加载，优先级越小越先加载
- 定义了依赖配置表，用于做配置依赖关系验证

### 代码示例

```c#
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
```

## 2.标准规范

### 代码示例

```c#
using System;

[Serializable]                                       //第一步，标记可序列化
[Config(                                             //第二步，挂配置标记，填写关键标记信息
    configId: "audio_configs",
    priority: 100
)]
public class AudioConfigs : ConfigDefinition         //第三步，定义容器，继承基类
{
    public AudioData[] audioList;                    //第五步，定义父容器

    [Serializable]
    public class AudioData                           //第四步，定义子容器
    {
        public string audioId;  // 字典键             //第六步，定义子容器内的具体字段
        public string clipPath; // 音频片段路径
        ...
    }
}
```

### 一图流

