using System;
using UnityEngine;
using EventSystemV2;

/// <summary>
/// 事件配置源：把事件系统（EventGlobalCache）适配到统一热更接口。
/// 序列化用 Newtonsoft（事件配置里含大量 Dictionary，JsonUtility 不支持）。
/// 依赖方向：EventConfigSource → EventGlobalCache（单向）。
/// </summary>
public class EventConfigSource : ConfigSourceBase
{
    public override string SourceId => "Event";
    public override string DefaultSubDir => ConfigDirs.EventConfig;
    public override IConfigSerializer Serializer => ConfigSerializers.Newtonsoft;

    public override string[] GetFileNames()
    {
        return new string[]
        {
            "EventConfig.json",
            "SysIdConfig.json",
            "OperateLogConfig.json",
            "VersionConfig.json",
        };
    }

    public override bool ValidateFile(string fileName, string content)
    {
        // 按文件名反序列化到对应容器类型做"真校验"。
        try
        {
            switch (fileName)
            {
                case "EventConfig.json":
                    return Serializer.Deserialize<EventConfigRoot>(content) != null;
                case "SysIdConfig.json":
                    return Serializer.Deserialize<SysIdConfigRoot>(content) != null;
                case "OperateLogConfig.json":
                    return Serializer.Deserialize<LogConfigRoot>(content) != null;
                case "VersionConfig.json":
                    return Serializer.Deserialize<VersionConfigRoot>(content) != null;
                default:
                    Debug.LogWarning($"[{SourceId}] 未注册校验的文件: {fileName}");
                    return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[{SourceId}] {fileName} 校验失败: {e.Message}");
            return false;
        }
    }

    public override void OnHotUpdated()
    {
        // 热更成功后：重载事件配置 + 重建系统层开关。
        EventGlobalCache.ReloadAllFromDisk();
        EventSystemManager.SysManageInit();
    }
}
