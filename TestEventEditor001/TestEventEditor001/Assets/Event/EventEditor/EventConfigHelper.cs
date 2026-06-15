using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace EventSystemV2
{
    //事件编辑器独写工具
    public static class EventConfigHelper
    {
        #region 配置储存路径常量
        //配置总父级文件夹
        public const string BaseConfigDir = "Assets/Resources/EventConfig/";

        //系统ID配置路径
        public static readonly string SysIdPath = BaseConfigDir + "SysIdConfig.json";

        //事件属性配置路径
        public static readonly string EventPath = BaseConfigDir + "EventConfig.json";

        //日志配置路径
        public static readonly string LogPath = BaseConfigDir + "OperateLogConfig.json";

        //版本配置路径
        public static readonly string VersionPath = BaseConfigDir + "VersionConfig.json";

        #endregion

        //路径初始化
        static EventConfigHelper()
        {
            //配置目录不存在自动创建
            if (!Directory.Exists(BaseConfigDir))
            {
                //创建路径
                Directory.CreateDirectory(BaseConfigDir);
                //刷新
                EventGlobalCache.IsRefresh = true;
            }
        }

        #region 保存系列
        //写入方法
        private static void WriteFile(string path, string json)
        {
            File.WriteAllText(path, json);
        }

        public static void SaveSysConfig(SysIdConfigRoot data)
        {
            WriteFile(SysIdPath, JsonConvert.SerializeObject(data, Formatting.Indented));
        }
        public static void SaveEventConfig(EventConfigRoot data)
        {
            //保存前刷新方法实例列表
            EventGlobalCache.RefeshMethodInstances();
            WriteFile(EventPath, JsonConvert.SerializeObject(data, Formatting.Indented));
        }
        public static void SaveLogConfig(LogConfigRoot data) => WriteFile(LogPath, JsonConvert.SerializeObject(data, Formatting.Indented));
        public static void SaveVersionConfig(VersionConfigRoot data) => WriteFile(VersionPath, JsonConvert.SerializeObject(data, Formatting.Indented));
        #endregion
        #region 读取系列
        private static T Load<T>(string path) where T : new()
        {
            if (!File.Exists(path)) return new T();
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<T>(json);
        }

        public static SysIdConfigRoot LoadSysConfig() => Load<SysIdConfigRoot>(SysIdPath);
        public static EventConfigRoot LoadEventConfig() => Load<EventConfigRoot>(EventPath);
        public static LogConfigRoot LoadLogConfig() => Load<LogConfigRoot>(LogPath);
        public static VersionConfigRoot LoadVersionConfig() => Load<VersionConfigRoot>(VersionPath);
        #endregion
        #region 深拷贝
        public static T DeepCopy<T>(T source)
        {
            if (source == null) return default;
            string json = JsonConvert.SerializeObject(source);
            return JsonConvert.DeserializeObject<T>(json);
        }
        #endregion

    }
}