using UnityEngine;

/// <summary>
/// 主配置源：把主配置系统（ConfigManager）适配到统一热更接口。
/// 依赖方向：MainConfigSource → ConfigManager（单向），ConfigManager 不反向依赖 source。
/// 路径由基类从 ConfigDirs.MainConfig 派生，与 ConfigManager 内部加载路径共用同一常量，不会漂移。
/// </summary>
public class MainConfigSource : ConfigSourceBase
{
    public override string SourceId => "Main";
    public override string DefaultSubDir => ConfigDirs.MainConfig;
    public override IConfigSerializer Serializer => ConfigSerializers.JsonUtility;

    public override string[] GetFileNames()
    {
        // 返回磁盘上"实际存在"的配置文件名（与 ConfigVersionGenerator 扫描结果对齐）。
        // 用"实际存在"而非"所有配置类"，是为了排除 map_cache / ui_prefabs 这类"有类无文件"的测试残留。
        return ConfigManager.Instance.GetExistingConfigFileNames();
    }

    public override bool ValidateFile(string fileName, string content)
    {
        // 校验交给 ConfigManager：它知道 fileName → 配置类型 的映射，能反序列化到强类型做"真校验"。
        // 注意不能写 Serializer.Deserialize<object>(content)——JsonUtility 不支持 object，永远返回 null。
        return ConfigManager.Instance.ValidateConfigFile(fileName, content);
    }

    public override void OnHotUpdated()
    {
        // 热更成功后重载所有配置。
        ConfigManager.Instance.ReloadAll();
    }
}
