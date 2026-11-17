using System;
using System.IO;
using UnityEngine;

/// <summary>
/// 配置源接口：每个配置系统实现一个，声明"从哪读、怎么校验、热更后怎么重载"。
/// 热更服务（ConfigHotUpdater）只依赖此接口，不关心具体是哪个系统，
/// 这样"热更逻辑"和"配置系统"彻底解耦。
/// </summary>
public interface IConfigSource
{
    /// <summary>唯一标识，用于日志，如 "Main" / "Event" / "Input"。</summary>
    string SourceId { get; }

    /// <summary>StreamingAssets 下的相对子目录，如 "EventConfig"。</summary>
    string DefaultSubDir { get; }

    /// <summary>本系统用的序列化器（JsonUtility 或 Newtonsoft）。</summary>
    IConfigSerializer Serializer { get; }

    /// <summary>版本文件名，默认 version.json，一般无需覆盖。</summary>
    string VersionFile { get; }

    // 【新增】路径成员：ConfigHotUpdater 读写需要它们。
    // 由 ConfigSourceBase 统一派生，实现者不用自己写。
    string DefaultDir { get; }
    string HotDir { get; }
    string BackupDir { get; }
    string TmpDir { get; }


    /// <summary>参与热更的业务文件清单（不含 version.json，它由热更服务单独处理）。</summary>
    string[] GetFileNames();

    /// <summary>读当前生效版本号（热更目录优先，回退保底目录）。</summary>
    string GetCurrentVersion();

    /// <summary>校验单个业务文件：JSON 合法性 + 本系统业务规则。返回 false 表示该文件不可用。</summary>
    bool ValidateFile(string fileName, string content);

    /// <summary>热更成功后回调，由各系统自己决定怎么重载（如 ConfigManager.ReloadAll）。</summary>
    void OnHotUpdated();
}

/// <summary>
/// 配置源基类：统一派生目录路径（保底/热更/备份/临时），并提供默认的版本号读取。
/// 子类只需声明 SourceId / DefaultSubDir / Serializer，并实现文件清单、校验、重载。
/// </summary>
public abstract class ConfigSourceBase : IConfigSource
{
    public abstract string SourceId { get; }
    public abstract string DefaultSubDir { get; }
    public abstract IConfigSerializer Serializer { get; }
    public virtual string VersionFile => ConfigDirs.VersionFile;

    // —— 以下路径由 DefaultSubDir 统一派生，子类无需重复写 ——
    public string DefaultDir => Path.Combine(ConfigPaths.DefaultRoot, DefaultSubDir);
    public string HotDir => Path.Combine(ConfigPaths.HotRoot, DefaultSubDir);
    public string BackupDir => HotDir + "_backup";
    public string TmpDir => HotDir + "_tmp";

    public abstract string[] GetFileNames();
    public abstract bool ValidateFile(string fileName, string content);
    public abstract void OnHotUpdated();

    /// <summary>默认实现：热更目录 version.json 优先，回退保底目录，都没有则 "0.0.0"。</summary>
    public virtual string GetCurrentVersion()
    {
        string hotPath = Path.Combine(HotDir, VersionFile);
        if (File.Exists(hotPath))
        {
            try
            {
                var v = Serializer.Deserialize<ConfigVersionInfo>(File.ReadAllText(hotPath));
                if (v != null && !string.IsNullOrEmpty(v.version)) return v.version;
            }
            catch (Exception e) { Debug.LogWarning($"[{SourceId}] 热更版本解析失败: {e.Message}"); }
        }

        string defPath = Path.Combine(DefaultDir, VersionFile);
        if (File.Exists(defPath))
        {
            try
            {
                var v = Serializer.Deserialize<ConfigVersionInfo>(File.ReadAllText(defPath));
                if (v != null && !string.IsNullOrEmpty(v.version)) return v.version;
            }
            catch (Exception e) { Debug.LogWarning($"[{SourceId}] 保底版本解析失败: {e.Message}"); }
        }
        return "0.0.0";
    }

    /// <summary>辅助：读某配置文件的文本，热更目录优先，回退保底目录；都没有返回 null。</summary>
    public virtual string ReadFileText(string fileName)
    {
        string hotPath = Path.Combine(HotDir, fileName);
        if (File.Exists(hotPath)) return File.ReadAllText(hotPath);

        string defPath = Path.Combine(DefaultDir, fileName);
        if (File.Exists(defPath)) return File.ReadAllText(defPath);

        return null;
    }
}

/// <summary>version.json 中的单文件条目（文件名 + 该文件的 md5）。</summary>
[Serializable]
public class ConfigFileEntry
{
    public string name;
    public string md5;
}

/// <summary>version.json 结构：通用热更协议，所有配置源共用。</summary>
[Serializable]
public class ConfigVersionInfo
{
    public string version;
    public string updatedAt;
    public string minAppVersion;
    public string note;
    public ConfigFileEntry[] files;
}
