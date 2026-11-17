using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public static class ConfigVersionGenerator
{
    private const string VersionFile = "version.json";

    [MenuItem("Tools/Config/Generate Version File")]
    public static void Generate()
    {
        // 【修正点】：加上 Config 子目录
        string fullDir = Path.Combine(Application.streamingAssetsPath, "Config");

        if (!Directory.Exists(fullDir))
        {
            Debug.LogError($"[ConfigVersion] 配置目录不存在: {fullDir}");
            return;
        }

        string root = fullDir + Path.DirectorySeparatorChar;

        // 获取 Config 目录下的所有 json 文件名
        var fileNames = Directory.GetFiles(fullDir, "*.json", SearchOption.TopDirectoryOnly)
            .Select(p => p.Substring(root.Length).Replace('\\', '/'))
            .Where(f => !string.Equals(f, VersionFile, StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToArray();

        if (fileNames.Length == 0)
        {
            Debug.LogWarning($"[ConfigVersion] 在 {fullDir} 没有找到任何配置 json，请检查文件是否放对位置。");
            return;
        }

        // 构造对象数组
        var files = new List<ConfigFileEntry>();
        foreach (var fileName in fileNames)
        {
            files.Add(new ConfigFileEntry
            {
                name = fileName,
                md5 = GetMD5(Path.Combine(fullDir, fileName))
            });
        }

        // 读取旧版本号
        string version = "0.0.1";
        string versionPath = Path.Combine(fullDir, VersionFile);
        if (File.Exists(versionPath))
        {
            try
            {
                var old = JsonUtility.FromJson<ConfigVersionInfo>(
                    File.ReadAllText(versionPath));
                if (old != null && !string.IsNullOrEmpty(old.version))
                    version = old.version;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ConfigVersion] 读取旧 version.json 失败，使用默认版本: {e.Message}");
            }
        }

        var info = new ConfigVersionInfo
        {
            version = version, // 注意：这里只是读取旧版本号，不会自动给你 +1
            updatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            note = "自动生成",
            files = files.ToArray()
        };

        // 写入 version.json
        File.WriteAllText(versionPath, JsonUtility.ToJson(info, true));
        AssetDatabase.Refresh();

        Debug.Log($"[ConfigVersion] 成功！已生成 {versionPath}，版本 {version}，包含 {files.Count} 个文件");
    }

    private static string GetMD5(string filePath)
    {
        using (var md5 = System.Security.Cryptography.MD5.Create())
        {
            using (var stream = File.OpenRead(filePath))
            {
                var hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}