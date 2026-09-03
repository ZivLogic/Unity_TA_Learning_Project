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
/// 两种模式：
///   1. 无组模式：直接 Add，JSON 顶层就是字段名
///   2. 有组模式：先 Group 再 Add，JSON 顶层是组名
/// </summary>
public class ConfigBuilder
{
    // 顶层容器：总配置名 - 子配置字典
    private readonly Dictionary<string, Dictionary<string, object>> _rootConfig;
    private string _currentGroup;

    // 新增：标记是否使用无组模式
    private bool _isRootMode = false;

    public ConfigBuilder()
    {
        _rootConfig = new Dictionary<string, Dictionary<string, object>>();
        _currentGroup = string.Empty;
    }

    #region 分组 & 添加键值

    /// <summary>
    /// 切换到指定分组（有组模式）
    /// </summary>
    public ConfigBuilder Group(string groupName)
    {
        if (!_rootConfig.ContainsKey(groupName))
        {
            _rootConfig[groupName] = new Dictionary<string, object>();
        }
        _currentGroup = groupName;
        _isRootMode = false;
        return this;
    }

    /// <summary>
    /// 直接添加顶层字段（无组模式）
    /// 调用此方法后，后续 Add 都是顶层字段
    /// </summary>
    public ConfigBuilder AddRoot(string key, object value)
    {
        _isRootMode = true;
        _currentGroup = "__root__";

        if (!_rootConfig.ContainsKey("__root__"))
        {
            _rootConfig["__root__"] = new Dictionary<string, object>();
        }

        _rootConfig["__root__"][key] = value;
        return this;
    }

    /// <summary>
    /// 添加键值对
    /// 如果之前调用过 AddRoot，则添加到顶层
    /// 如果之前调用过 Group，则添加到对应组
    /// 如果都没调用过，默认添加到顶层（无组模式）
    /// </summary>
    public ConfigBuilder Add(string key, object value)
    {
        // 如果还没有设置组，且没有使用过 AddRoot，默认使用无组模式
        if (string.IsNullOrEmpty(_currentGroup) && !_isRootMode)
        {
            _isRootMode = true;
            _currentGroup = "__root__";
        }

        // 确保组存在
        if (!_rootConfig.ContainsKey(_currentGroup))
        {
            _rootConfig[_currentGroup] = new Dictionary<string, object>();
        }

        _rootConfig[_currentGroup][key] = value;
        return this;
    }

    #endregion

    #region 生成标准JSON文本

    /// <summary>
    /// 压缩单行JSON
    /// </summary>
    public string BuildMinify()
    {
        // 如果是无组模式，直接输出 __root__ 的内容
        if (_isRootMode && _rootConfig.ContainsKey("__root__") && _rootConfig.Count == 1)
        {
            return BuildDictMinify(_rootConfig["__root__"]);
        }

        // 原有逻辑：有组模式
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

    /// <summary>
    /// 格式化缩进JSON
    /// </summary>
    public string BuildFormat()
    {
        // 如果是无组模式，直接输出 __root__ 的内容
        if (_isRootMode && _rootConfig.ContainsKey("__root__") && _rootConfig.Count == 1)
        {
            var rootDict = _rootConfig["__root__"];
            StringBuilder rootSb = new StringBuilder();
            rootSb.AppendLine("{");

            int idx = 0;
            foreach (var kv in rootDict)
            {
                idx++;
                string end = idx == rootDict.Count ? "" : ",";
                rootSb.AppendLine($"  \"{kv.Key}\": {GetObjStr(kv.Value)}{end}");
            }

            rootSb.Append("}");
            return rootSb.ToString();
        }

        // 原有逻辑：有组模式
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
    public void DeleteConfigFile(string savePath)
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log($"[Config] 已删除旧配置：{savePath}");
        }
    }

    public void ExportToFileFormat_Overwrite(string savePath)
    {
        DeleteConfigFile(savePath);
        ExportToFileFormat(savePath);
        Debug.Log($"[Config] 配置一键更新完成");
    }

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