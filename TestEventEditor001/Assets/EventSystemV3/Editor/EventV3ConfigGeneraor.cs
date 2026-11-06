#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace EventSystemV3
{
    public static class EventV3ConfigGenerator
    {
        [MenuItem("Tools/EventSystemV3/生成事件定义配置")]
        public static void Generate()
        {
            var entries = new List<EventDefinitionEntry>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var asm in assemblies)
            {
                foreach (var type in asm.GetTypes())
                {
                    if (type.IsAbstract) continue;

                    var methods = type.GetMethods(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    foreach (var method in methods)
                    {
                        var attr = method.GetCustomAttribute<EventDefinitionAttribute>();
                        if (attr == null) continue;

                        if (entries.Exists(e => e.DefinitionId == attr.DefinitionId)) continue;

                        entries.Add(new EventDefinitionEntry
                        {
                            DefinitionId = attr.DefinitionId,
                            Description = attr.Description,
                            PayloadTypeName = attr.PayloadType?.FullName ?? "",
                            Enable = true,
                            QueueType = attr.QueueType,
                            Cooldown = attr.Cooldown,
                            PublisherTag = "",
                            PublisherBlueprintId = "",
                            PublisherMethodFullName = $"{type.Name}.{method.Name}",
                            Listeners = Array.Empty<ListenEntry>()
                        });
                    }
                }
            }

            SaveToFile(entries);
            Debug.Log($"[EventV3ConfigGenerator] 生成 {entries.Count} 个事件定义");
        }

        [MenuItem("Tools/EventSystemV3/导出到 Resources")]
        public static void ExportToResources()
        {
            string src = Path.Combine(Directory.GetCurrentDirectory(), EventV3Const.EditorConfigRoot, EventV3Const.DefinitionFileName);
            if (!File.Exists(src))
            {
                Debug.LogError($"[EventV3ConfigGenerator] 源文件不存在: {src}");
                return;
            }

            string dstDir = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Assets/Resources",
                EventV3Const.ResourceConfigRoot);

            Directory.CreateDirectory(dstDir);
            string dst = Path.Combine(dstDir, EventV3Const.DefinitionFileName);
            File.Copy(src, dst, true);

            AssetDatabase.Refresh();
            Debug.Log($"[EventV3ConfigGenerator] 已导出到: {dst}");
        }

        private static void SaveToFile(List<EventDefinitionEntry> entries)
        {
            var root = new EventDefinitionConfigRoot { entries = entries.ToArray() };
            string json = JsonUtility.ToJson(root, true);

            string path = Path.Combine(
                Directory.GetCurrentDirectory(),
                EventV3Const.EditorConfigRoot,
                EventV3Const.DefinitionFileName
            );

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, json);
            AssetDatabase.Refresh();
        }
    }
}
#endif