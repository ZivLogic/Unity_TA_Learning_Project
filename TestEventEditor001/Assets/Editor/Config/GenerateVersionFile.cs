using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic; // 新增

public static class ConfigVersionGenerator
{
    private const string ConfigDir = "Assets/StreamingAssets/Config";
    private const string VersionFile = "version.json";

    [MenuItem("Tools/Config/Generate Version File")]
    public static void Generate()
    {
        string fullDir = Path.GetFullPath(Application.streamingAssetsPath);
        if (!Directory.Exists(fullDir))
        {
            Debug.LogError($"[ConfigVersion] 配置目录不存在: {fullDir}");
            return;
        }

        string root = fullDir + Path.DirectorySeparatorChar;

        // 获取所有 json 文件名
        var fileNames = Directory.GetFiles(fullDir, "*.json", SearchOption.TopDirectoryOnly)
            .Select(p => p.Substring(root.Length).Replace('\\', '/'))
            .Where(f => !string.Equals(f, VersionFile, StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToArray();

        if (fileNames.Length == 0)
        {
            Debug.LogWarning("[ConfigVersion] 没有找到任何配置 json");
            return;
        }

        // 构造对象数组
        var files = new List<ConfigManager.ConfigFileEntry>();
        foreach (var fileName in fileNames)
        {
            // 在构造 files 的循环里：
            files.Add(new ConfigManager.ConfigFileEntry
            {
                name = fileName,
                md5 = GetMD5(Path.Combine(fullDir, fileName))
            });
        }

        string version = "0.0.1";
        string versionPath = Path.Combine(fullDir, VersionFile);
        if (File.Exists(versionPath))
        {
            try
            {
                var old = JsonUtility.FromJson<ConfigManager.ConfigVersionInfo>(
                    File.ReadAllText(versionPath));
                if (old != null && !string.IsNullOrEmpty(old.version))
                    version = old.version;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ConfigVersion] 读取旧 version.json 失败，使用默认版本: {e.Message}");
            }
        }

        var info = new ConfigManager.ConfigVersionInfo
        {
            version = version,
            updatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            note = "自动生成",
            files = files.ToArray()
        };

        File.WriteAllText(versionPath, JsonUtility.ToJson(info, true));
        AssetDatabase.Refresh();

        Debug.Log($"[ConfigVersion] 已生成 {versionPath}，版本 {version}，文件 {files.Count} 个");
    }

    // 在 ConfigVersionGenerator 里加个辅助方法
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