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
            //确保目录存在
            EnsureDirectories();
            //加载配置
            //具体事件配置
            var config = EventGlobalCache.AllEvent;
            //索引配置
            var mappingConfig_Listen = EventGlobalCache.GlobalEventConfig.ListenMethodToEvent;

            //先根据具体配置遍历获取到事件名称，通过事件名称找到监听方法具体列表，在根据父级索引获取所有的kv.Key，在通过事件内具体的具体方法 -> 方法找到
            //普通方法，在通过具体方法名找到对应实例类名和包键值名称还有字段映射联系，最后面集齐实例类名，普通方法名，具体方法的包键值，键值的映射关系
            if (config == null || config.Count == 0)
            {
                Debug.LogWarning("[EventCodeGenerator]没有找到事件配置/配置为空");
                return;
            }
            int generatedCount = 0;

            foreach (var kv in config)
            {
                string eventName = kv.Key;
                EventItem item = kv.Value;

                //生成事件壳类
                GenerateEventClass(eventName, item);

                //生成绑定代码
                GenerateBindingClass(eventName, item);

                generatedCount++;
            }

            AssetDatabase.Refresh();
            Debug.Log($"生成了{generatedCount}个事件代码");
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

        //自动生成事件类外壳
        private static void GenerateEventClass(string eventName, EventItem eventItem)
        {
            string className = GetEventClassName(eventName);
            string filePath = EVENT_PATH + className + ".cs";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using EventSystemV2;");
            sb.AppendLine();
            sb.AppendLine($"public class {className} : PackageEvent");
            sb.AppendLine("{");
            sb.AppendLine($"    public {className} ()");
            sb.AppendLine("     {");
            sb.AppendLine($"        EventClassFullName = \"{className}\";");
            sb.AppendLine($"        QueueType = EventQueueType.{eventItem.QueueType};");
            sb.AppendLine("     }");
            sb.AppendLine("}");

            File.WriteAllText(filePath, sb.ToString());
            Debug.Log($"生成事件类：{filePath}");
        }

        private static void GenerateBindingClass(string eventName, EventItem eventItem)
        {
            string className = GetBindingClassName(eventName);
            string filePath = BINDING_PATH + className + ".cs";
            string eventClassName = GetEventClassName(eventName);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Reflection;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using EventSystemV2;");
            sb.AppendLine();
            sb.AppendLine($"public static class {className}");
            sb.AppendLine("{");
            sb.AppendLine("     private static bool _initialized = false;");
            sb.AppendLine();
            //sb.AppendLine("     [RuntimeInitializeOnLoadMethod]");
            sb.AppendLine($"    public static void Initialize()");
            sb.AppendLine("     {");
            sb.AppendLine("         if (_initialized)return;");
            sb.AppendLine("             _initialized = true;");
            sb.AppendLine();

            //收集所有不同的监听实例
            HashSet<string> processedListeners = new();

            //生成每个监听方的绑定代码
            for (int i = 0; i < eventItem.EventListen.Count; i++)
            {

                //在这里判断需要实例化的对象和对应的监听方法


                string listenFullName = eventItem.EventListen[i];
                string[] pars = listenFullName.Split('.');
                string listenClassName = pars[0];
                string listenMethodName = pars[1];

                //获取映射关系
                string publishFullName = eventItem.EventPublish;
                string mappingKey = $"{publishFullName}_{listenFullName}";
                var keyConnection = eventItem.KeyConnection.GetValueOrDefault(mappingKey);


                //获取监听方法
                MethodInfo listenMethod = GetMethodInfo(listenMethodName);
                string listenerVarName = $"listener_{i}";

                sb.AppendLine($"        //绑定监听：{listenFullName}");
                sb.AppendLine($"        var listener_{i} = BusinessInstanceManager.Get(\"{listenClassName}\");");
                sb.AppendLine($"        if (listener_{i} == null)");
                sb.AppendLine($"        {{");
                sb.AppendLine($"            Debug.LogWarning($\"监听实例{listenClassName}未注册\");");
                sb.AppendLine($"            return;");
                sb.AppendLine($"        }}");
                sb.AppendLine();
                sb.AppendLine($"        Action<{eventClassName}> handler_{i} = (evt) => ");
                sb.AppendLine("         {");
                sb.AppendLine("             try");
                sb.AppendLine("             {");

                if (listenMethod != null)
                {
                    GenerateSafeInvokeCode(sb, listenMethod, keyConnection, listenMethodName, listenerVarName);
                }
                else
                {
                    //降级方案：使用动态调用
                    sb.AppendLine($"                var method = listener_{i}.GetType().GetMethod(\"{listenMethodName}\");");
                    sb.AppendLine($"                if (method != null)");
                    sb.AppendLine($"                {{");
                    sb.AppendLine($"                    Debug.LogWarning($\"监听方法:{listenMethodName}使用动态调用\");");
                    sb.AppendLine($"                    method.Invoke(listener_{i}, null);");
                    sb.AppendLine($"                }}");
                }

                sb.AppendLine("             }");
                sb.AppendLine("             catch (Exception e)");
                sb.AppendLine("             {");
                sb.AppendLine($"                Debug.LogError($\"执行监听{listenMethodName}失败：{{e.Message}}\");");
                sb.AppendLine("             }");
                sb.AppendLine("         };");
                sb.AppendLine($"        EventManager.Instance.Listen<{eventClassName}>(handler_{i});");
                sb.AppendLine();

            }

            sb.AppendLine("     }");
            sb.AppendLine("}");

            File.WriteAllText(filePath, sb.ToString());
        }

        private static void GenerateSafeInvokeCode(StringBuilder sb, MethodInfo method, Dictionary<string, string> keyConnection, string listenMethodName, string listenerVarName)
        {
            var parameters = method.GetParameters();

            if (parameters.Length == 0)
            {
                sb.AppendLine($"                var method = {listenerVarName}.GetType().GetMethod(\"{listenMethodName}\");");
                sb.AppendLine($"                method?.Invoke({listenerVarName}, null);");
                return;
            }

            //生成临时变量参数
            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                string paramName = param.Name;
                string paramType = GetTypeName(param.ParameterType);

                string packKey = param.Name;
                if (keyConnection != null && keyConnection.TryGetValue(paramName, out string mappedKey))
                    packKey = mappedKey;

                sb.AppendLine($"                {paramType} p_{paramName};");
                sb.AppendLine($"                if ( ! evt.Package.Get<{paramType}>(\"{packKey}\", out p_{paramName}))");
                sb.AppendLine($"                {{");
                sb.AppendLine($"                    Debug.LogError($\"获取参数{paramName}失败，KEY:{packKey}\");");
                sb.AppendLine($"                    return;");
                sb.AppendLine($"                }}");
            }

            //构建调用参数
            string args = string.Join(",", System.Array.ConvertAll(parameters, p => $"p_{p.Name}"));
            sb.AppendLine();
            sb.AppendLine($"                var method = {listenerVarName}.GetType().GetMethod(\"{listenMethodName}\");");
            sb.AppendLine($"                method?.Invoke({listenerVarName}, new object[] {{{args}}});");

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
