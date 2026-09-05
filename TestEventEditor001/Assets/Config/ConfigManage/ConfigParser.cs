// Framework/ConfigParser.cs
using System;
using System.Text.RegularExpressions;
using UnityEngine;

public static class ConfigParser
{
    public static T Parse<T>(string jsonText, ConfigManager configManager = null) where T : ConfigDefinition, new()
    {
        if (string.IsNullOrEmpty(jsonText))
        {
            return new T();
        }

        jsonText = StripComments(jsonText);

        if (configManager != null)
        {
            jsonText = ResolveReferences(jsonText, configManager);

            // 调试：打印替换后的 JSON
            Debug.Log($"[ConfigParser] 替换后的 JSON:\n{jsonText}");
        }

        T config = JsonUtility.FromJson<T>(jsonText);

        return config ?? new T();
    }

    /// <summary>
    /// 解析 JSON 中的所有 @ref 引用（手动解析，避免正则问题）
    /// </summary>
    private static string ResolveReferences(string jsonText, ConfigManager configManager)
    {
        int currentIndex = 0;

        while (true)
        {
            // 查找 @ref:
            int refStart = jsonText.IndexOf("@ref:", currentIndex, StringComparison.Ordinal);
            if (refStart == -1) break;

            // 找到 @ref: 前面的引号位置
            int valueStart = jsonText.LastIndexOf('"', refStart);
            if (valueStart == -1) break;

            // 查找引用结束位置（下一个引号）
            int refEnd = jsonText.IndexOf('"', refStart);
            if (refEnd == -1) break;

            // 提取引用路径
            string refPath = jsonText.Substring(refStart + 5, refEnd - refStart - 5).Trim();

            Debug.Log($"[ConfigParser] 解析引用: {refPath}");

            // 解析引用值
            object value = configManager.ResolveReference(refPath);

            if (value != null)
            {
                string valueJson = ValueToJson(value);

                // 替换整个 "xxx" 部分（包括两边的引号）
                jsonText = jsonText.Remove(valueStart, refEnd - valueStart + 1);
                jsonText = jsonText.Insert(valueStart, valueJson);

                // 更新索引
                currentIndex = valueStart + valueJson.Length;

                Debug.Log($"[ConfigParser] 引用解析成功: {refPath} → {valueJson}");
            }
            else
            {
                Debug.LogWarning($"[ConfigParser] 引用解析失败: {refPath}");
                jsonText = jsonText.Remove(valueStart, refEnd - valueStart + 1);
                jsonText = jsonText.Insert(valueStart, "null");
                currentIndex = valueStart + 4;
            }
        }

        return jsonText;
    }

    /// <summary>
    /// 将 C# 对象转换为 JSON 字符串
    /// </summary>
    private static string ValueToJson(object value)
    {
        if (value == null) return "null";

        // 字符串
        if (value is string str) return $"\"{str}\"";

        // 布尔
        if (value is bool b) return b.ToString().ToLower();

        // 数值类型
        if (value is int || value is float || value is double || value is long)
            return value.ToString();

        // Vector3
        if (value is Vector3 vec3)
            return $"{{\"x\":{vec3.x},\"y\":{vec3.y},\"z\":{vec3.z}}}";

        // Vector2
        if (value is Vector2 vec2)
            return $"{{\"x\":{vec2.x},\"y\":{vec2.y}}}";

        // 其他复杂对象
        return JsonUtility.ToJson(value);
    }

    /// <summary>
    /// 去除 JSON 注释
    /// </summary>
    private static string StripComments(string json)
    {
        json = Regex.Replace(json, @"//.*?$", "", RegexOptions.Multiline);
        json = Regex.Replace(json, @"/\*.*?\*/", "", RegexOptions.Singleline);
        return json;
    }
}