using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// 配置序列化器接口。
/// 不同配置系统底层 JSON 库不同，通过此接口统一调用，避免各系统各写一套读写。
///
/// 为什么不是"全用 JsonUtility"：
///   JsonUtility 不支持 Dictionary 序列化，事件系统的配置里塞满了 Dictionary，
///   硬改 JsonUtility 会导致字典全部丢失。所以底层库允许不同，接口保持统一。
/// </summary>
public interface IConfigSerializer
{
    string Serialize<T>(T data, bool pretty);
    T Deserialize<T>(string json);
}

/// <summary>
/// JsonUtility 实现：主配置 / 输入配置 / 逻辑状态用。
/// 要求 T 是 [Serializable] 强类型，字段为数组或基本类型。
/// </summary>
public sealed class JsonUtilityConfigSerializer : IConfigSerializer
{
    public string Serialize<T>(T data, bool pretty) => JsonUtility.ToJson(data, pretty);
    public T Deserialize<T>(string json) => JsonUtility.FromJson<T>(json);
}

/// <summary>
/// 序列化器全局单例：所有 ConfigSource 从这里取，避免各 source 重复 new。
/// </summary>
public static class ConfigSerializers
{
    public static readonly IConfigSerializer JsonUtility = new JsonUtilityConfigSerializer();
    public static readonly IConfigSerializer Newtonsoft = new NewtonsoftConfigSerializer();
}


/// <summary>
/// Newtonsoft 实现：事件配置用（含 Dictionary / 复杂嵌套）。
/// TypeNameHandling.All 保留自定义类型信息（事件系统跨事件传自定义类依赖它）。
/// </summary>
public sealed class NewtonsoftConfigSerializer : IConfigSerializer
{
    // 配置文件序列化：不写 $type（配置文件应是稳定纯数据，不绑定程序集内部类型名）
    private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    };

    public string Serialize<T>(T data, bool pretty)
        => JsonConvert.SerializeObject(data, pretty ? Formatting.Indented : Formatting.None, Settings);

    public T Deserialize<T>(string json)
        => JsonConvert.DeserializeObject<T>(json, Settings);
}

