using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace EventSystemV2
{
    public static class EventUtil
    {
        //便捷发布方法
        public static void EmitEventFromMethod(object instance, string methodName, params object[] args)
        {
            string fullMethodName = $"{instance.GetType().Name}.{methodName}";

            //快速查找事件名
            if (!EventGlobalCache.PublishMethodToEvent.TryGetValue(fullMethodName, out string eventName))
            {
                Debug.LogError($"未找到发布方法配置：{fullMethodName},发布中断");
                return;
            }

            var eventItem = EventGlobalCache.AllEvent[eventName];
            //获取打包KEY列表
            if (!eventItem.PublishKey.TryGetValue(fullMethodName, out var keys))
            {
                Debug.LogError($"未找到发布方法的KEY配置：{fullMethodName}");
                return;
            }

            //自动打包：根据配置中的KEY顺序匹配参数
            var pack = new Package();
            var parameters = GetMethodParameters(instance, methodName);
            for (int i = 0; i < args.Length && i < keys.Count; i++)
            {
                object arg = args[i];
                Type argType = parameters[i].ParameterType;
                string key = keys[i];

                //字符串直接存储，不要序列化
                if (argType == typeof(string))
                {
                    pack.Put(key, arg);
                }
                else if (IsSimpleType(argType) || typeof(UnityEngine.Object).IsAssignableFrom(argType))
                {
                    pack.Put(key, arg);
                }
                else if (arg != null) 
                {
                    string json = SerializeObject(arg);
                    pack.Put(key, json);
                }
                else
                {
                    pack.Put(key, null);
                }
            }
            //创建并发布事件
            var eventType = Type.GetType(eventItem.EventClassFullName);
            var evt = (PackageEvent)Activator.CreateInstance(eventType);
            evt.Package = pack;
            evt.EventClassFullName = eventItem.EventClassFullName;
            evt.QueueType = eventItem.QueueType;
            EmitEvent(evt);
        }

        //查找配置
        private static EventItem FindEventItemByPublishMethod(string className, string methodName)
        {
            foreach (var item in EventGlobalCache.AllEvent.Values)
            {
                //检查发布类名和方法名是否相同
                if (item.PublishClassName.ContainsKey(className) && item.PublishClassName[className] == methodName)
                {
                    return item;
                }

            }
            return null;
        }

        //发布方法
        public static void EmitEvent(PackageEvent evt)
        {
            //实例化管理器
            var em = EventManager.Instance;
            if (em == null) return;
            //检验是否能发布
            if (!EventSystemManager.CanPublish(evt))
            {
                Debug.LogWarning($"系统层配置里不存在{evt.EventClassFullName}");
                return;
            }

            EventQueueType queue = evt.QueueType;

            //判断队列
            switch (queue)
            {
                case EventQueueType.Logic:
                    em.EmitLogic(evt); break;
                case EventQueueType.Render:
                    em.EmitRender(evt); break;
                case EventQueueType.Physics:
                    em.EmitPhysics(evt); break;
                case EventQueueType.Audio:
                    em.EmitAudio(evt); break;
                case EventQueueType.Input:
                    em.EmitInput(evt); break;
                case EventQueueType.Network:
                    em.EmitNetwork(evt); break;
            }
        }

        public static object[] GetKeyValue(PackageEvent evt, Dictionary<string, string> KeyConnection, MethodInfo lisMethod)
        {
            Package pack = evt.Package;
            //获取监听方法所有的形参
            ParameterInfo[] paramInfos = lisMethod.GetParameters();
            //打包成类型组
            object[] invokeArgs = new object[paramInfos.Length];
            //按顺序处理
            for (int i = 0; i < paramInfos.Length; i++)
            {
                ParameterInfo param = paramInfos[i];
                string listenParamName = param.Name;
                Type targetType = param.ParameterType;

                if (!KeyConnection.TryGetValue(listenParamName, out string packKey))
                {
                    Debug.LogWarning($"监听参数【{listenParamName}】未配置映射关系");
                    return Array.Empty<object>();
                }

                if (!pack.data.TryGetValue(packKey, out object packValue))
                {
                    invokeArgs[i] = targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
                    continue;
                }

                // 字符串直接处理
                if (targetType == typeof(string))
                {
                    invokeArgs[i] = packValue?.ToString();
                    continue;
                }

                //Unity.Object直接引用
                if (typeof(UnityEngine.Object).IsAssignableFrom(targetType))
                {
                    if (packValue is UnityEngine.Object unityObj && unityObj == null)
                        invokeArgs[i] = null;
                    else
                    invokeArgs[i] = packValue;
                    continue;
                }

                // 简单类型直接赋值
                if (IsSimpleType(targetType))
                {
                    invokeArgs[i] = packValue;
                    continue;
                }

                // 复杂类型：反序列化
                else if (packValue is string json)
                {
                    try
                    {
                        invokeArgs[i] = DeserializeObject(json, targetType);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"反序列化失败：{packKey}, {e.Message}");
                        invokeArgs[i] = null;
                    }
                }
                else
                {
                    invokeArgs[i] = packValue;
                }
            }
            return invokeArgs;
        }

        //类型判断工具
        private static bool IsSimpleType(Type type)
        {
            // 处理可空类型
            Type underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType != null)
                type = underlyingType;

            if (type.IsPrimitive ||
                type == typeof(string) ||
                type == typeof(decimal) ||
                type == typeof(DateTime) ||
                type == typeof(TimeSpan) ||
                type == typeof(Guid) ||
                type.IsEnum)
            {
                return true;
            }

            if (type == typeof(Vector2) ||
                type == typeof(Vector3) ||
                type == typeof(Vector4) ||
                type == typeof(Quaternion) ||
                type == typeof(Color) ||
                type == typeof(Rect))
            {
                return true;
            }
            return false;
        }

        //序列化复杂对象
        private static string SerializeObject(object obj)
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Formatting.None
            };
            return JsonConvert.SerializeObject(obj, settings);
        }

        //反序列化复杂对象
        private static object DeserializeObject(string json, Type TargetType)
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            return JsonConvert.DeserializeObject(json, TargetType, settings);
        }

        //获取方法的参数信息
        private static ParameterInfo[] GetMethodParameters(object instance, string methodName)
        {
            var method = instance.GetType().GetMethod(methodName);
            return method?.GetParameters() ?? new ParameterInfo[0];
        }
    }
}
