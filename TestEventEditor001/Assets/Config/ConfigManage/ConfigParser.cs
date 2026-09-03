// Framework/ConfigParser.cs
using System;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// 配置解析器：JSON → 配置对象
/// </summary>
public static class ConfigParser
{
    /// <summary>
    /// 解析 JSON 为配置对象
    /// </summary>
    public static T Parse<T>(string jsonText) where T : ConfigDefinition, new()
    {
        if (string.IsNullOrEmpty(jsonText))
        {
            return new T();
        }

        // 1. 去除注释
        jsonText = StripComments(jsonText);

        // 2. 反序列化
        T config = JsonUtility.FromJson<T>(jsonText);

        return config ?? new T();
    }

    /// <summary>
    /// 去除 JSON 注释
    /// </summary>
    private static string StripComments(string json)
    {
        // 去除单行注释
        json = Regex.Replace(json, @"//.*?$", "", RegexOptions.Multiline);

        // 去除多行注释
        json = Regex.Replace(json, @"/\*.*?\*/", "", RegexOptions.Singleline);

        return json;
    }
}