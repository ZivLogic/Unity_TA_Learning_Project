using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// 标准JSON配置生成器
/// 支持：普通类型、Dictionary嵌套、List列表、数组
/// 完全兼容 Newtonsoft.Json
/// </summary>
public class ConfigBuilder
{
    // 顶层容器：总配置名 - 子配置字典
    private readonly Dictionary<string, Dictionary<string, object>> _rootConfig;
    private string _currentGroup;

    public ConfigBuilder()
    {
        _rootConfig = new Dictionary<string, Dictionary<string, object>>();
        _currentGroup = string.Empty;
    }

    #region 分组 & 添加键值
    public ConfigBuilder Group(string groupName)
    {
        if (!_rootConfig.ContainsKey(groupName))
        {
            _rootConfig[groupName] = new Dictionary<string, object>();
        }
        _currentGroup = groupName;
        return this;
    }

    public ConfigBuilder Add(string key, object value)
    {
        if (string.IsNullOrEmpty(_currentGroup))
        {
            Group("DefaultGroup");
        }
        var curGroupDict = _rootConfig[_currentGroup];
        curGroupDict[key] = value;
        return this;
    }
    #endregion

    #region 生成标准JSON文本
    // 压缩单行JSON
    public string BuildMinify()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("{");
        List<string> groupStrList = new List<string>();
        foreach (var groupPair in _rootConfig)
        {
            string inner = BuildDictMinify(groupPair.Value);
            groupStrList.Add($"\"{groupPair.Key}\":{inner}");
        }
        sb.AppendJoin(",", groupStrList);
        sb.Append("}");
        return sb.ToString();
    }

    // 格式化缩进JSON
    public string BuildFormat()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("{");
        int groupIdx = 0;
        foreach (var groupPair in _rootConfig)
        {
            groupIdx++;
            sb.AppendLine($"  \"{groupPair.Key}\": {{");
            var dict = groupPair.Value;
            int keyIdx = 0;
            foreach (var kv in dict)
            {
                keyIdx++;
                string end = keyIdx == dict.Count ? "" : ",";
                sb.AppendLine($"    \"{kv.Key}\": {GetObjStr(kv.Value)}{end}");
            }
            string groupEnd = groupIdx == _rootConfig.Count ? "" : ",";
            sb.AppendLine($"  }}{groupEnd}");
        }
        sb.Append("}");
        return sb.ToString();
    }

    // 内部字典压缩序列化
    private string BuildDictMinify(Dictionary<string, object> dict)
    {
        List<string> itemList = new List<string>();
        foreach (var kv in dict)
        {
            itemList.Add($"\"{kv.Key}\":{GetObjStr(kv.Value)}");
        }
        return $"{{{string.Join(",", itemList)}}}";
    }

    // 核心：适配 字符串/bool/字典/List/数组/普通数值
    private string GetObjStr(object val)
    {
        // 字符串
        if (val is string str)
            return $"\"{str}\"";

        // 布尔小写 true/false
        if (val is bool b)
            return b.ToString().ToLower();

        // 嵌套字典
        if (val is Dictionary<string, object> subDict)
            return BuildDictMinify(subDict);

        // List / 数组 等集合（排除string）
        if (val is IEnumerable enumerable && !(val is string))
        {
            List<string> arrItems = new List<string>();
            foreach (var item in enumerable)
            {
                arrItems.Add(GetObjStr(item));
            }
            return $"[{string.Join(",", arrItems)}]";
        }

        // 数字、其他基础类型
        return val.ToString();
    }
    #endregion

    #region 导出文件
    // 强制覆盖写入
    public void ExportToFileMinify(string savePath)
    {
        WriteFile(savePath, BuildMinify());
    }
    public void ExportToFileFormat(string savePath)
    {
        WriteFile(savePath, BuildFormat());
    }

    // 文件存在则跳过
    public void ExportToFileMinifyIfNotExist(string savePath)
    {
        if (File.Exists(savePath))
        {
            Debug.LogWarning($"[Config] 文件已存在，跳过生成：{savePath}");
            return;
        }
        ExportToFileMinify(savePath);
    }
    public void ExportToFileFormatIfNotExist(string savePath)
    {
        if (File.Exists(savePath))
        {
            Debug.LogWarning($"[Config] 文件已存在，跳过生成：{savePath}");
            return;
        }
        ExportToFileFormat(savePath);
    }
    #endregion

    #region 删除 / 一键更新
    /// <summary>删除指定配置文件</summary>
    public void DeleteConfigFile(string savePath)
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log($"[Config] 已删除旧配置：{savePath}");
        }
    }

    /// <summary>一键更新：先删旧文件，再写入格式化JSON</summary>
    public void ExportToFileFormat_Overwrite(string savePath)
    {
        DeleteConfigFile(savePath);
        ExportToFileFormat(savePath);
        Debug.Log($"[Config] 配置一键更新完成");
    }

    /// <summary>一键更新：先删旧文件，再写入压缩JSON</summary>
    public void ExportToFileMinify_Overwrite(string savePath)
    {
        DeleteConfigFile(savePath);
        ExportToFileMinify(savePath);
        Debug.Log($"[Config] 配置一键更新完成");
    }
    #endregion

    #region 内部写入
    private void WriteFile(string path, string content)
    {
        string dir = Path.GetDirectoryName(path);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        File.WriteAllText(path, content, Encoding.UTF8);
    }
    #endregion
}