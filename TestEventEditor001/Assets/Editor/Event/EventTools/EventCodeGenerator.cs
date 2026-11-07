using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Reflection;
using System.Linq;
using System;

namespace EventSystemV2
{
    public static class EventCodeGenerator
    {
        //代码生成路径定义

        private const string GENERATED_PATH = "Assets/Scripts/Generated/";

        private const string EVENT_PATH = GENERATED_PATH + "Events/";

        private const string BINDING_PATH = GENERATED_PATH + "Bindings/";

        //配置总父级文件夹
        public const string BaseConfigDir = "Assets/Resources/EventConfig/";

        //必须在Editor文件夹中
        [MenuItem("Tools/事件系统/生成所有事件代码")]
        public static void GenerateAllEventCode()
        {
            EnsureDirectories();
            var config = EventGlobalCache.AllEvent;
            if (config == null || config.Count == 0)
            {
                Debug.LogWarning("[EventCodeGenerator]没有找到事件配置/配置为空");
                return;
            }

            // 【核心修改】按接收方类名分组收集数据
            // Key: 类名 (如 "TestEntry")
            // Value: 该类的所有接收绑定信息
            var classBindings = new Dictionary<string, List<BindingInfo>>();
            int generatedCount = 0;

            foreach (var kv in config)
            {
                string eventName = kv.Key;
                EventItem item = kv.Value;

                // 1. 生成事件壳类 (强类型容器)
                GenerateEventClass(eventName, item);
                generatedCount++;

                // 2. 收集监听方信息
                foreach (var listenFullName in item.EventListen)
                {
                    var parts = listenFullName.Split('.');
                    if (parts.Length < 2) continue;

                    string className = parts[0];
                    string methodName = parts[1];

                    if (!classBindings.ContainsKey(className))
                        classBindings[className] = new List<BindingInfo>();

                    // 去重：防止同一个类的同一个方法绑定了多次
                    if (!classBindings[className].Exists(b => b.MethodName == methodName))
                    {
                        classBindings[className].Add(new BindingInfo
                        {
                            EventClassName = GetEventClassName(eventName),
                            MethodName = methodName,
                            EventItem = item,
                            ListenFullName = listenFullName
                        });
                    }
                }
            }

            // 3. 为每个接收方类生成唯一的 partial 文件
            foreach (var kv in classBindings)
            {
                GeneratePartialBindingClass(kv.Key, kv.Value);
            }

            AssetDatabase.Refresh();
            Debug.Log($"生成了 {generatedCount} 个事件代码，以及 {classBindings.Count} 个类的绑定代码");
        }

        //删除所有生成的代码
        [MenuItem("Tools/事件系统/清理生成的代码")]
        public static void CleanGeneratedCode()
        {
            if (Directory.Exists(GENERATED_PATH))
            {
                Directory.Delete(GENERATED_PATH, true);
                Debug.Log($"已删除目录:{GENERATED_PATH}");
            }

            //删除mate文件
            string matePath = GENERATED_PATH.TrimEnd('/') + ".meta";
            if (File.Exists(matePath))
            {
                File.Delete(matePath);
            }

            AssetDatabase.Refresh();
            Debug.Log("已清理所有生成的代码");
        }

        [MenuItem("Tools/事件系统/清理配置")]
        public static void ClearEventConfig()
        {
            if (Directory.Exists(BaseConfigDir))
            {
                Directory.Delete(BaseConfigDir, true);
                Debug.Log($"已删除目录:{BaseConfigDir}");
            }

            //删除mate文件
            string matePath = BaseConfigDir.TrimEnd('/') + ".meta";
            if (File.Exists(matePath))
            {
                File.Delete(matePath);
            }

            AssetDatabase.Refresh();
            Debug.Log("已清理所有生成的配置");
        }

        //确保目录存在
        private static void EnsureDirectories()
        {
            if (!Directory.Exists(GENERATED_PATH))
                Directory.CreateDirectory(GENERATED_PATH);
            if (!Directory.Exists(EVENT_PATH))
                Directory.CreateDirectory(EVENT_PATH);
            if (!Directory.Exists(BINDING_PATH))
                Directory.CreateDirectory(BINDING_PATH);
        }

        private static void GenerateEventClass(string eventName, EventItem eventItem)
        {
            string className = GetEventClassName(eventName);
            string filePath = EVENT_PATH + className + ".cs";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using EventSystemV2;");
            sb.AppendLine();
            sb.AppendLine($"public class {className} : PackageEvent");
            sb.AppendLine("{");

            // 路由字段
            sb.AppendLine("    public string TargetId;");
            sb.AppendLine("    public List<string> TargetIds;");
            sb.AppendLine();

            // payload 字段（按发布方法参数生成属性，UUID 参数跳过）
            if (eventItem.PublishKey != null &&
                eventItem.PublishKey.TryGetValue(eventItem.EventPublish, out var keys))
            {
                var methodParams = EventGlobalCache.GlobalMethodParams
                    .GetValueOrDefault(eventItem.EventPublish);
                if (methodParams != null)
                {
                    foreach (var key in keys)
                    {
                        if (key == "UUID" || key == "UUIDS") continue;  // UUID 是路由，不进 payload
                        if (methodParams.ParamNameToType.TryGetValue(key, out var t))
                        {
                            sb.AppendLine($"    public {GetTypeName(t)} {key};");
                        }
                    }
                }
            }

            sb.AppendLine();
            sb.AppendLine($"    public {className}()");
            sb.AppendLine("    {");
            sb.AppendLine($"        EventClassFullName = \"{className}\";");
            sb.AppendLine($"        QueueType = EventQueueType.{eventItem.QueueType};");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(filePath, sb.ToString());
            Debug.Log($"生成事件类：{filePath}");
        }

        // 【新增】辅助数据结构，用于按类聚合生成
        private class BindingInfo
        {
            public string EventClassName;
            public string MethodName;
            public string ListenFullName;
            public EventItem EventItem;
        }

        // 【替换】生成唯一的 partial 类文件
        private static void GeneratePartialBindingClass(string className, List<BindingInfo> bindings)
        {
            // 文件名固定为 Gen_类名.cs，保证一个类只有一个生成文件
            string filePath = BINDING_PATH + $"Gen_{className}.cs";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using EventSystemV2;");
            sb.AppendLine();
            sb.AppendLine($"public partial class {className}");
            sb.AppendLine("{");
            sb.AppendLine("    protected override void RegisterEvents()");
            sb.AppendLine("    {");

            foreach (var binding in bindings)
            {
                sb.AppendLine($"        // 绑定事件: {binding.EventClassName}");
                sb.AppendLine($"        RegisterMethod(\"{binding.MethodName}\", evt => ");
                sb.AppendLine($"            {binding.MethodName}({BuildArgList(binding.EventItem, binding.ListenFullName, binding.EventClassName)}));");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(filePath, sb.ToString());
            Debug.Log($"生成绑定 partial 类：{filePath}");
        }

        private static string BuildArgList(EventItem item, string listenFullName, string eventClassName)
        {
            string mappingKey = $"{item.EventPublish}_{listenFullName}";
            var mapping = item.KeyConnection?.GetValueOrDefault(mappingKey);
            var listenParams = EventGlobalCache.GlobalMethodParams.GetValueOrDefault(listenFullName);
            if (listenParams == null) return "";

            var args = new List<string>();
            foreach (var p in listenParams.Parameters)
            {
                string prop = p.Name;
                if (mapping != null && mapping.TryGetValue(p.Name, out var mapped)) prop = mapped;
                args.Add($"(({eventClassName})evt).{prop}");
            }
            return string.Join(", ", args);
        }

        private static void GenerateSafeInvokeCode(StringBuilder sb, MethodInfo method,
        Dictionary<string, string> keyConnection, string listenMethodName, string listenerVarName)
        {
            var parameters = method.GetParameters();

            // 构建参数表达式：优先按 KeyConnection 映射，否则用 evt.同名属性
            var argExprs = new List<string>();
            foreach (var param in parameters)
            {
                string paramName = param.Name;
                string propName = paramName;
                if (keyConnection != null && keyConnection.TryGetValue(paramName, out var mapped))
                    propName = mapped;

                argExprs.Add($"evt.{propName}");
            }

            string args = string.Join(", ", argExprs);
            sb.AppendLine($"                {listenerVarName}.{listenMethodName}({args});");
        }


        //定义规范事件名称
        private static string GetEventClassName(string eventName)
        {
            return $"E_{eventName}";
        }

        //定义规范监听类名
        private static string GetBindingClassName(string eventName)
        {
            return $"Binding_{eventName}";
        }

        //查找监听方法
        private static MethodInfo GetMethodInfo(string methodName)
        {
            //直接调用现有获取方法
            List<MethodInfo> listenMethods = EventReflectTool.GetAllListen();
            return listenMethods.FirstOrDefault(m => m.Name == methodName);
        }

        //匹配验证值类型
        private static string GetTypeName(System.Type type)
        {
            //处理可空类型
            if (Nullable.GetUnderlyingType(type) != null)
            {
                return $"{GetTypeName(Nullable.GetUnderlyingType(type))}?";
            }

            //处理数组
            if (type.IsArray)
            {
                return $"{GetTypeName(type.GetElementType())}[]";
            }

            //处理泛类型
            if (type.IsGenericType)
            {
                string genericTypeName = type.Name.Split('`')[0];
                var genericArgs = type.GetGenericArguments();
                string argsStr = string.Join(",", System.Array.ConvertAll(genericArgs, GetTypeName));

                //特殊处理常用泛型
                if (genericTypeName == "List")
                    return $"System.Collections.Generic.List<{argsStr}>";
                if (genericTypeName == "Dictionary")
                {
                    var keyType = GetTypeName(genericArgs[0]);
                    var valueType = GetTypeName(genericArgs[1]);
                    return $"System.Collections.Generic.Dictionary<{keyType}, {valueType}>";
                }
                if (genericTypeName == "HashSet")
                    return $"System.Collections.Generic.HashSet<{argsStr}>";
                if (genericTypeName == "Queue")
                    return $"System.Collections.Generic.Queue<{argsStr}>";
                if (genericTypeName == "Stack")
                    return $"System.Collections.Generic.Stack<{argsStr}>";

                return $"{genericTypeName}<{argsStr}>";
            }
            return GetFullTypeName(type);
        }

        private static string GetFullTypeName(System.Type type)
        {
            // 基础类型映射
            if (type == typeof(int)) return "int";
            if (type == typeof(string)) return "string";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(long)) return "long";
            if (type == typeof(byte)) return "byte";
            if (type == typeof(char)) return "char";
            if (type == typeof(object)) return "object";
            if (type == typeof(void)) return "void";

            // Unity常用类型
            if (type == typeof(Vector2)) return "UnityEngine.Vector2";
            if (type == typeof(Vector3)) return "UnityEngine.Vector3";
            if (type == typeof(Vector4)) return "UnityEngine.Vector4";
            if (type == typeof(Quaternion)) return "UnityEngine.Quaternion";
            if (type == typeof(Color)) return "UnityEngine.Color";
            if (type == typeof(Rect)) return "UnityEngine.Rect";
            if (type == typeof(Transform)) return "UnityEngine.Transform";
            if (type == typeof(GameObject)) return "UnityEngine.GameObject";

            // 自定义类型：需要包含命名空间
            return type.FullName ?? type.Name;

        }

        //删除方法


    }
}
