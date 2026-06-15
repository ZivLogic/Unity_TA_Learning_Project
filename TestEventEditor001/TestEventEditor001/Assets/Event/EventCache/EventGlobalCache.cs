using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

namespace EventSystemV2
{
    //全局缓存
    public static class EventGlobalCache
    {
        #region 所有配置
        public static SysIdConfigRoot GlobalSysConfig { get; set; }
        public static EventConfigRoot GlobalEventConfig { get; set; }
        //{
        //    get => _globalEventConfig;
        //    set
        //    {
        //        Debug.Log($"GlobalEventConfig被修改！旧值：{_globalEventConfig?.GetHashCode()}，新值：{value?.GetHashCode()}");
        //        Debug.Log($"调用堆栈：{Environment.StackTrace}");
        //        _globalEventConfig = value;
        //    }
        //}
        public static LogConfigRoot GlbalEventLogConfig { get; set; }
        public static VersionConfigRoot VersionConfig { get; set; }


        //private static EventConfigRoot _globalEventConfig;




        //方法完整名 -> 参数信息
        public static Dictionary<string, MethodParamInfo> GlobalMethodParams = new();

        //方法完整名 -> 参数名
        public static Dictionary<string, List<string>> MethodToKeyList = new();



        //编译标记：编译中禁止保存/生成代码
        public static bool IsCompileWaiting { get; set; }

        //刷新标记，编辑器每帧节流检测
        public static bool IsRefresh { get; set; }
        #endregion
        #region 从磁盘加载配置
        //编辑器打开以及事件初始化时需要调用
        public static void ReloadAllFromDisk()
        {
            //确保初始化刷新是false
            IsRefresh = false;
            GlobalSysConfig = EventConfigHelper.LoadSysConfig();
            GlobalEventConfig = EventConfigHelper.LoadEventConfig();
            GlbalEventLogConfig = EventConfigHelper.LoadLogConfig();
            VersionConfig = EventConfigHelper.LoadVersionConfig();
            RefeshMethodInstances();

            if (GlobalEventConfig.Items.Count == 0)
            {
                Debug.LogWarning("系统配置为空");
            }
            Debug.Log($"事件容器有值，值个数为：{GlobalEventConfig.Items.Count}");
        }
        #endregion
        #region 保存落地所有JSON
        public static void SaveAllToDisk()
        {
            //确保刷新初始化状态是false
            IsRefresh = false;
            if (IsCompileWaiting) return;
            EventConfigHelper.SaveSysConfig(GlobalSysConfig);
            EventConfigHelper.SaveEventConfig(GlobalEventConfig);
            EventConfigHelper.SaveLogConfig(GlbalEventLogConfig);
            EventConfigHelper.SaveVersionConfig(VersionConfig);
            //刷新
            IsRefresh = true;
        }
        #endregion
        #region 清空所有缓存
        public static void ClearAllMemorCache()
        {
            GlobalSysConfig = new SysIdConfigRoot();
            GlobalEventConfig = new EventConfigRoot();
            GlbalEventLogConfig = new LogConfigRoot();
            VersionConfig = new VersionConfigRoot();
            GlobalMethodParams.Clear();
        }
        #endregion
        #region 快捷取值
        public static Dictionary<string, SysIdItem> SysId_IsEnable => GlobalSysConfig.ItemsIsEnable;
        public static Dictionary<string, SysIdItem> SysId_NoEnable => GlobalSysConfig.ItemsNoEnable;
        public static Dictionary<string, List<string>> SysIDToPubMethod => GlobalSysConfig.SysIDToPublsihMethod;
        public static Dictionary<string, List<string>> SysIDToLstMethod => GlobalSysConfig.SysIDToListenMethod;
        public static Dictionary<string, EventItem> AllEvent => GlobalEventConfig.Items;
        public static Dictionary<string, List<string>> MethodInstances => GlobalEventConfig.MethodInstances;
        public static Dictionary<string, string> PublishMethodToEvent => GlobalEventConfig.PublishMethodToEvent;
        public static Dictionary<string, string> ListenMethodToEvent => GlobalEventConfig.ListenMethodToEvent;
        public static List<OperateLogItem> AllEventLog => GlbalEventLogConfig.Items;
        public static Dictionary<string, VersionItems> AllVersion => VersionConfig.items;
        #endregion
        #region 快捷添加日志
        public static void AddLog(string eventName, string content)
        {
            AllEventLog.Add(new OperateLogItem
            {
                OperateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                TargetEvent = eventName,
                Content = content
            });
        }
        #endregion
        #region 刷新所有业务方法的实例
        public static void RefeshMethodInstances()
        {
            //判断是否有缓存
            if (GlobalEventConfig == null) return;
            GlobalEventConfig.MethodInstances.Clear();
            GlobalMethodParams.Clear();

            //扫描所有发布方法
            foreach (var method in EventReflectTool.GetAllPublish())
            {
                //获取到业务方法的实例名字
                string className = method.DeclaringType.Name;
                //如果没有这个类的列表
                if (!GlobalEventConfig.MethodInstances.ContainsKey(className))
                    //创建列表
                    GlobalEventConfig.MethodInstances[className] = new List<string>();
                //如果这个类列表内没有这个方法记录
                if (!GlobalEventConfig.MethodInstances[className].Contains(method.Name))
                    //添加这个方法
                    GlobalEventConfig.MethodInstances[className].Add(method.Name);


                string fullName = $"{method.DeclaringType.Name}.{method.Name}";

                //缓存参数信息
                var paramInfo = new MethodParamInfo
                {
                    MethodFullName = fullName
                };

                foreach (var param in method.GetParameters())
                {
                    string paramName = param.Name;
                    Type paramType = param.ParameterType;

                    paramInfo.Parameters.Add(new ParamInfo
                    {
                        Name = param.Name,
                        Type = paramType,
                        TypeName = GetTypeNameForStorage(paramType)
                    });
                    paramInfo.ParamNameToType[param.Name] = param.ParameterType;


                    if (!MethodToKeyList.ContainsKey(fullName))
                    {
                        MethodToKeyList[fullName] = new List<string>();
                    }
                    MethodToKeyList[fullName].Add(paramName);
                    Debug.Log($"加载了事件{fullName}的回调");
                }

                GlobalMethodParams[fullName] = paramInfo;


            }
            //扫描所有监听方法
            foreach (var method in EventReflectTool.GetAllListen())
            {
                //获取监听方实例的名字
                string className = method.DeclaringType.Name;
                //如果没有列表
                if (!GlobalEventConfig.MethodInstances.ContainsKey(className))
                    GlobalEventConfig.MethodInstances[className] = new List<string>();
                //如果列表内没有方法
                if (!GlobalEventConfig.MethodInstances[className].Contains(method.Name))
                    GlobalEventConfig.MethodInstances[className].Add(method.Name);

                //填充监听查找方法
                if (!GlobalEventConfig.ListenMethodInstances.ContainsKey(className))
                    GlobalEventConfig.ListenMethodInstances[className] = new List<string>();
                //如果监听内没实例
                if (!GlobalEventConfig.ListenMethodInstances[className].Contains(method.Name))
                    GlobalEventConfig.ListenMethodInstances[className].Add(method.Name);

                string fullName = $"{method.DeclaringType.Name}.{method.Name}";

                //缓存参数信息
                var paramInfo = new MethodParamInfo
                {
                    MethodFullName = fullName
                };

                foreach (var param in method.GetParameters())
                {
                    string paramName = param.Name;
                    Type paramType = param.ParameterType;

                    paramInfo.Parameters.Add(new ParamInfo
                    {
                        Name = paramName,
                        Type = paramType,
                        TypeName = GetTypeNameForStorage(paramType)
                    });
                    paramInfo.ParamNameToType[paramName] = paramType;

                    if (!MethodToKeyList.ContainsKey(fullName))
                    {
                        MethodToKeyList[fullName] = new List<string>();
                    }

                    if (!MethodToKeyList[fullName].Contains(paramName))
                    {
                        MethodToKeyList[fullName].Add(paramName);
                    }
                }

                GlobalMethodParams[fullName] = paramInfo;
            }




            //调用一次方法刷新
            RefeshMethodInstancesToEvent();
        }
        #endregion
        #region 刷新所有方法名对应事件名
        public static void RefeshMethodInstancesToEvent()
        {
            //判断是否有缓存
            if (GlobalEventConfig == null) return;
            GlobalEventConfig.PublishMethodToEvent.Clear();
            GlobalEventConfig.ListenMethodToEvent.Clear();

            foreach (var kv in AllEvent)
            {
                string eventName = kv.Key;
                var eventItem = kv.Value;

                //建立发布方缓存
                if (!string.IsNullOrEmpty(eventItem.EventPublish))
                {
                    PublishMethodToEvent[eventItem.EventPublish] = eventName;
                }

                foreach (string listenFullName in eventItem.EventListen)
                {
                    ListenMethodToEvent[listenFullName] = eventName;
                }
            }
        }
        #endregion
        #region 方法参数获取辅方法
        private static string GetTypeNameForStorage(Type type)
        {
            // 基础类型
            if (type == typeof(int)) return "int";
            if (type == typeof(string)) return "string";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(long)) return "long";
            if (type == typeof(byte)) return "byte";
            if (type == typeof(char)) return "char";

            // Unity 常用类型
            if (type == typeof(Vector2)) return "Vector2";
            if (type == typeof(Vector3)) return "Vector3";
            if (type == typeof(Quaternion)) return "Quaternion";
            if (type == typeof(Color)) return "Color";
            if (type == typeof(Rect)) return "Rect";

            // 枚举
            if (type.IsEnum) return type.Name;

            // 自定义类：返回程序集限定的名称，用于反序列化
            return type.AssemblyQualifiedName ?? type.FullName ?? type.Name;
        }
        #endregion
    }
    //编辑器缓存基类
    public abstract class BasePageCache
    {
        //刷新：从全局缓存页面覆盖
        public abstract void RefreshFromGlobal();
        //提交：页面数据写入全局缓存
        public abstract void CommitToGlobal();
        //清空当前页面数据
        public abstract void ClearPageTemp();
        //}
    }


    public class MethodParamInfo
    {
        public string MethodFullName;                             //方法具体名称
        public List<ParamInfo> Parameters = new();                //参数列表
        public Dictionary<string, Type> ParamNameToType = new();  //参数名 -》类型
    }

    public class ParamInfo
    {
        public string Name;             //参数名
        public Type Type;               //参数类型
        public string TypeName;         //类型字符串（用于生成代码）
    }
}