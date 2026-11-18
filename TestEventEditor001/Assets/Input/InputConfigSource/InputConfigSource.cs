using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 输入配置源：把输入配置系统适配到统一热更接口。
/// 序列化用 JsonUtility（输入配置是强类型 List 结构）。
/// 依赖方向：InputConfigSource → InputManager（单向，重载时触发）。
/// </summary>
public class InputConfigSource : ConfigSourceBase
{
    public override string SourceId => "Input";
    public override string DefaultSubDir => ConfigDirs.InputConfig;
    public override IConfigSerializer Serializer => ConfigSerializers.JsonUtility;

    public override string[] GetFileNames()
    {
        // 从扫描到的输入配置类型取 JsonFile（与 InputConfigLoader.LoadDefaultRoot 同一来源）
        var names = new List<string>();
        foreach (var type in InputConfigAutoLoader.ScanInputConfigTypes())
        {
            var attr = InputConfigAutoLoader.GetAttr(type);
            if (attr != null && !string.IsNullOrEmpty(attr.JsonFile))
                names.Add(attr.JsonFile);
        }
        return names.ToArray();
    }

    public override bool ValidateFile(string fileName, string content)
    {
        // 按文件名反查输入配置类型，用 JsonUtility 反序列化到该类型做真校验。
        foreach (var type in InputConfigAutoLoader.ScanInputConfigTypes())
        {
            var attr = InputConfigAutoLoader.GetAttr(type);
            if (attr == null || attr.JsonFile != fileName) continue;

            try
            {
                return JsonUtility.FromJson(content, type) != null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{SourceId}] {fileName} 校验失败: {e.Message}");
                return false;
            }
        }

        Debug.LogWarning($"[{SourceId}] 未找到配置类型: {fileName}");
        return false;
    }

    public override void OnHotUpdated()
    {
        // 触发输入系统重载：重读配置 + 重建绑定 + 重建拦截管道。
        if (InputManager.Instance != null)
            InputManager.Instance.ReloadConfig();
    }
}
