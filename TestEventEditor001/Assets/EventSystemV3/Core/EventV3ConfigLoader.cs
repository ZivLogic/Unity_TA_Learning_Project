using UnityEngine;
using System;
using System.IO;

namespace EventSystemV3
{
    public static class EventV3ConfigLoader
    {
        /// <summary>
        /// 加载事件定义配置（独立于 ConfigManager）
        /// </summary>
        public static EventDefinitionConfigRoot LoadDefinitions()
        {
            string json = ReadConfigJson(EventV3Const.DefinitionFileName);
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning("[EventV3ConfigLoader] 事件定义配置不存在");
                return new EventDefinitionConfigRoot();
            }

            try
            {
                return JsonUtility.FromJson<EventDefinitionConfigRoot>(json)
                       ?? new EventDefinitionConfigRoot();
            }
            catch (Exception e)
            {
                Debug.LogError($"[EventV3ConfigLoader] 解析失败\n{e}");
                return new EventDefinitionConfigRoot();
            }
        }

        /// <summary>
        /// 编辑器优先工程目录，打包走 Resources
        /// </summary>
        private static string ReadConfigJson(string fileName)
        {
#if UNITY_EDITOR
            string editorPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                EventV3Const.EditorConfigRoot,
                fileName
            );
            if (File.Exists(editorPath))
                return File.ReadAllText(editorPath);
#endif

            string resourcePath = EventV3Const.ResourceConfigRoot
                                  + Path.GetFileNameWithoutExtension(fileName);
            var asset = Resources.Load<TextAsset>(resourcePath);
            return asset?.text;
        }
    }
}