using System.IO;
using UnityEngine;

namespace EventSystemV2
{
    /// <summary>
    /// 事件配置读写工具。
    /// 三层路径：
    ///   保底 DefaultDir = StreamingAssets/EventConfig（编辑器读写这里，打包后只读）
    ///   热更 HotDir    = persistentDataPath/EventConfig（运行时读优先）
    /// 读：热更优先，回退保底；写：只写保底目录（编辑器保存时用）。
    /// </summary>
    public static class EventConfigHelper
    {
        #region 路径与文件名
        public static readonly string DefaultDir =
            Path.Combine(ConfigPaths.DefaultRoot, ConfigDirs.EventConfig);
        public static readonly string HotDir =
            Path.Combine(ConfigPaths.HotRoot, ConfigDirs.EventConfig);

        public const string SysIdFile = "SysIdConfig.json";
        public const string EventFile = "EventConfig.json";
        public const string LogFile = "OperateLogConfig.json";
        public const string VersionFile = "VersionConfig.json";
        #endregion

        // 确保保底目录存在（编辑器保存需要）
        static EventConfigHelper()
        {
            EnsureDir(DefaultDir);
        }

        private static void EnsureDir(string dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                EventGlobalCache.IsRefresh = true;
            }
        }

        /// <summary>读路径解析：热更优先，回退保底。</summary>
        private static string ResolveReadPath(string fileName)
        {
            string hot = Path.Combine(HotDir, fileName);
            if (File.Exists(hot)) return hot;
            return Path.Combine(DefaultDir, fileName);
        }

        #region 保存系列（写保底目录，编辑器用）
        private static void WriteFile(string path, string json)
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(path, json);
        }

        public static void SaveSysConfig(SysIdConfigRoot data)
            => WriteFile(Path.Combine(DefaultDir, SysIdFile),
                ConfigSerializers.Newtonsoft.Serialize(data, true));

        public static void SaveEventConfig(EventConfigRoot data)
        {
            EventGlobalCache.RefeshMethodInstances();
            WriteFile(Path.Combine(DefaultDir, EventFile),
                ConfigSerializers.Newtonsoft.Serialize(data, true));
        }

        public static void SaveLogConfig(LogConfigRoot data)
            => WriteFile(Path.Combine(DefaultDir, LogFile),
                ConfigSerializers.Newtonsoft.Serialize(data, true));

        public static void SaveVersionConfig(VersionConfigRoot data)
            => WriteFile(Path.Combine(DefaultDir, VersionFile),
                ConfigSerializers.Newtonsoft.Serialize(data, true));
        #endregion

        #region 读取系列（热更优先）
        private static T Load<T>(string fileName) where T : new()
        {
            string path = ResolveReadPath(fileName);
            if (!File.Exists(path)) return new T();
            string json = File.ReadAllText(path);
            return ConfigSerializers.Newtonsoft.Deserialize<T>(json);
        }

        public static SysIdConfigRoot LoadSysConfig() => Load<SysIdConfigRoot>(SysIdFile);
        public static EventConfigRoot LoadEventConfig() => Load<EventConfigRoot>(EventFile);
        public static LogConfigRoot LoadLogConfig() => Load<LogConfigRoot>(LogFile);
        public static VersionConfigRoot LoadVersionConfig() => Load<VersionConfigRoot>(VersionFile);
        #endregion

        #region 深拷贝
        public static T DeepCopy<T>(T source)
        {
            if (source == null) return default;
            string json = ConfigSerializers.Newtonsoft.Serialize(source, false);
            return ConfigSerializers.Newtonsoft.Deserialize<T>(json);
        }
        #endregion
    }
}
