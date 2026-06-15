using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;

namespace EventSystemV2
{
    public static class EventRuntimeInitializer
    {
        private static bool _initialized = false;

        public static void Initialize()
        {
            //Debug.Log("正确调用注册");
            if (_initialized) return;
            if (EventManager.Instance == null)
            {
                Debug.Log("[EventRuntimeInitializer]事件加载器还未加载");
                return;
            }
            _initialized = true;

            //从配置加载并自动化床建实例
            AutoCreateInstancesFromConfig();

            //根据配置建立事件绑定
            BindEventsFromConfig();
        }

        //根据配置加载并自动化创建实例
        private static void AutoCreateInstancesFromConfig()
        {
            var dictCfg = EventGlobalCache.AllEvent;
            if (dictCfg == null) return;

            foreach (var kv in dictCfg)
            {
                foreach (var pub in kv.Value.PublishClassName)
                {
                    string className = pub.Value;
                    if (BusinessInstanceManager.HasInstance(className))
                    {
                        Debug.Log("已有实例，无需再创建");
                        continue;
                    }

                    //尝试创建实例
                    object instance = TryCreateInstance(className);
                    if (instance != null)
                    {
                        BusinessInstanceManager.Register(className, instance);
                        Debug.Log($"自动创建并注册：{className}");
                    }
                }
                foreach (var lis in kv.Value.ListenClassName)
                {
                    string className = lis.Value;
                    if (BusinessInstanceManager.HasInstance(className))
                    {
                        Debug.Log("已有实例，无需再创建");
                        continue;
                    }

                    //尝试创建实例
                    object instance = TryCreateInstance(className);
                    if (instance != null)
                    {
                        BusinessInstanceManager.Register(className, instance);
                        Debug.Log($"自动创建并注册：{className}");
                    }
                }

            }
        }

        //创建实例
        private static object TryCreateInstance(string className)
        {
            //获取所有程序集
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                //获取所有定义好的类型
                foreach (var type in assembly.GetTypes())
                {
                    //判断是否相等
                    if (type.Name == className)
                    {
                        //如果定义的类型不为空，则创建
                        var ctor = type.GetConstructor(Type.EmptyTypes);
                        if (ctor != null)
                            return Activator.CreateInstance(type);
                        else
                            Debug.LogWarning($"{className}没有无参构造，无法自动创建");
                    }
                }
            }
            return null;
        }

        //绑定映射
        private static void BindEventsFromConfig()
        {
            var config = EventGlobalCache.AllEvent;
            if (config == null) return;

            foreach (var eventItem in config.Values)
            {
                string publishFullName = eventItem.EventPublish;
                string[] publishParts = publishFullName.Split('.');
                string publishClassName = publishParts[0];      // 发布方类名
                string publishMethodName = publishParts[1];     // 发布方方法名（如果需要）

                var publisher = BusinessInstanceManager.Get(publishClassName);
                if (publisher == null) continue;

                foreach (string listenFullName in eventItem.EventListen)
                {
                    string[] listenParts = listenFullName.Split('.');
                    string listenClassName = listenParts[0];
                    string listenMethodName = listenParts[1];

                    var listener = BusinessInstanceManager.Get(listenClassName);
                    if (listener == null) continue;

                    string mappingKey = $"{publishFullName}_{listenFullName}";

                    if (eventItem.KeyConnection?.TryGetValue(mappingKey, out var mapping) == true)
                    {
                        RegisterListener(eventItem, listener, listenMethodName, mapping);
                    }
                }
            }
        }

        private static Dictionary<string, Action<PackageEvent>> _registeredHandlers = new();

        //注册监听关系
        private static void RegisterListener(EventItem eventItem, object listener, string methodName, Dictionary<string, string> mapping)
        {
            //获取具体监听方法
            var method = listener.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null) return;

            //使用EventManager的强类型监听
            var eventType = Type.GetType(eventItem.EventClassFullName);
            if (eventType == null) return;

            //生成唯一键
            string key = $"{eventItem.EventName}_{listener.GetType().Name}_{methodName}";

            // 如果已经注册过，先取消
            if (_registeredHandlers.ContainsKey(key))
            {
                var oldHandler = _registeredHandlers[key];
                var unlistenMethod = typeof(EventManager).GetMethod("Unlisten").MakeGenericMethod(eventType);
                unlistenMethod.Invoke(EventManager.Instance, new object[] { oldHandler });
                _registeredHandlers.Remove(key);

                Debug.Log($"重新注册监听: {key}");
            }
            else
            {
                Debug.Log($"首次注册监听: {key}");  // 添加日志
            }

            //创建适配器委托
            Action<PackageEvent> handler = (evt) =>
            {
                var parameters = EventUtil.GetKeyValue(evt, mapping, method);
                method.Invoke(listener, parameters);
            };

            //调用Listen<T>方法
            var listenMethod = typeof(EventManager).GetMethod("Listen").MakeGenericMethod(eventType);
            listenMethod.Invoke(EventManager.Instance, new object[] { handler });

            //保存
            _registeredHandlers[key] = handler;
        }
    }
}

