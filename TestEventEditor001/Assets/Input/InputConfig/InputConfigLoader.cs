using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 输入配置加载器（最终版）
/// 保底配置：编辑器走工程目录，打包走 Resources
/// 玩家存档：始终 persistentDataPath
/// </summary>
public static class InputConfigLoader
{
    // ============================================================
    // 路径
    // ============================================================

    /// <summary>
    /// 编辑器下的保底配置根目录（绝对路径）
    /// </summary>
    private static string EditorDefaultRootFolder =>
        Path.Combine(
            Directory.GetCurrentDirectory(),
            InputConfigConst.EditorDefaultRootFolder);

    /// <summary>
    /// 玩家存档根目录
    /// </summary>
    private static string PlayerRootFolder =>
        Path.Combine(
            Application.persistentDataPath,
            InputConfigConst.PlayerRootFolderName);

    /// <summary>
    /// 玩家存档索引文件
    /// </summary>
    private static string PlayerIndexPath =>
        Path.Combine(PlayerRootFolder, InputConfigConst.PlayerIndexFileName);

    // ============================================================
    // 目录
    // ============================================================

    private static void EnsurePlayerFolder()
    {
        if (!Directory.Exists(PlayerRootFolder))
            Directory.CreateDirectory(PlayerRootFolder);
    }

    // ============================================================
    // 文件读写
    // ============================================================

    /// <summary>
    /// 原子写盘：先写 tmp，再替换
    /// </summary>
    private static void WriteAllTextAtomic(string path, string content)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string tmp = path + ".tmp";
        File.WriteAllText(tmp, content);
        if (File.Exists(path)) File.Delete(path);
        File.Move(tmp, path);
    }

    private static string ReadAllTextSafe(string path)
    {
        if (!File.Exists(path)) return null;
        try { return File.ReadAllText(path); }
        catch (Exception e)
        {
            Debug.LogError($"[InputConfigLoader]读取失败：{path}\n{e}");
            return null;
        }
    }

    // ============================================================
    // 保底配置读取（编辑器优先工程目录，回退 Resources）
    // ============================================================

    /// <summary>
    /// 读取一个保底配置文件的文本
    /// 编辑器：先查工程目录，查不到再查 Resources
    /// 打包：只查 Resources
    /// </summary>
    private static string ReadDefaultJson(string jsonFile)
    {
#if UNITY_EDITOR
        // 1. 编辑器优先工程目录
        string editorPath = Path.Combine(EditorDefaultRootFolder, jsonFile);
        string editorJson = ReadAllTextSafe(editorPath);
        if (!string.IsNullOrEmpty(editorJson))
            return editorJson;
#endif

        // 2. Resources 回退（打包后唯一路径）
        string resourcePath = $"{InputConfigConst.ResourceDefaultRootFolder}/{StripJsonExt(jsonFile)}";
        var asset = Resources.Load<TextAsset>(resourcePath);
        if (asset != null) return asset.text;

        return null;
    }

    /// <summary>
    /// 去掉 .json 后缀，Resources 路径不需要后缀
    /// </summary>
    private static string StripJsonExt(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return fileName;
        if (fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            return fileName.Substring(0, fileName.Length - 5);
        return fileName;
    }

    // ============================================================
    // 深拷贝
    // ============================================================

    public static InputConfigRoot CloneRoot(InputConfigRoot src)
    {
        if (src == null) return new InputConfigRoot();
        var json = JsonUtility.ToJson(src);
        var clone = JsonUtility.FromJson<InputConfigRoot>(json);
        clone?.EnsureNotNull();
        return clone ?? new InputConfigRoot();
    }

    // ============================================================
    // 保底配置加载
    // ============================================================

    /// <summary>
    /// 加载全部保底配置
    /// 扫描所有 InputConfigDefinition 子类，按 ConfigId 找文件
    /// </summary>
    public static InputConfigRoot LoadDefaultRoot()
    {
        var root = new InputConfigRoot();
        var types = InputConfigAutoLoader.ScanInputConfigTypes();

        foreach (var type in types)
        {
            var attr = InputConfigAutoLoader.GetAttr(type);
            if (attr == null) continue;

            string json = ReadDefaultJson(attr.JsonFile);
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning($"[InputConfigLoader]保底配置缺失：{attr.JsonFile}（ConfigId={attr.ConfigId}）");
                continue;
            }

            try
            {
                var obj = (InputConfigDefinition)JsonUtility.FromJson(json, type);
                InputConfigAutoLoader.AssignToRoot(root, obj);
            }
            catch (Exception e)
            {
                Debug.LogError($"[InputConfigLoader]保底配置解析失败：{attr.JsonFile}\n{e}");
            }
        }

        root.EnsureNotNull();
        return root;
    }

    /// <summary>
    /// 加载保底存档
    /// </summary>
    public static InputConfigArchive LoadDefaultArchive()
    {
        var archive = new InputConfigArchive
        {
            ArchiveId = "default",
            IsDefault = true,
            Root = LoadDefaultRoot()
        };
        archive.SetCreateTime(DateTime.MinValue);
        return archive;
    }

    // ============================================================
    // 玩家存档加载
    // ============================================================

    public static List<InputConfigArchive> LoadPlayerArchives()
    {
        var result = new List<InputConfigArchive>();
        var indexJson = ReadAllTextSafe(PlayerIndexPath);
        if (string.IsNullOrEmpty(indexJson)) return result;

        InputArchiveIndex index = null;
        try { index = JsonUtility.FromJson<InputArchiveIndex>(indexJson); }
        catch (Exception e) { Debug.LogError($"[InputConfigLoader]索引解析失败：{e}"); }

        if (index?.Items == null) return result;

        foreach (var item in index.Items)
        {
            if (item == null || string.IsNullOrEmpty(item.FileName)) continue;

            string path = Path.Combine(PlayerRootFolder, item.FileName);
            var json = ReadAllTextSafe(path);
            if (string.IsNullOrEmpty(json)) continue;

            InputConfigArchive archive = null;
            try { archive = JsonUtility.FromJson<InputConfigArchive>(json); }
            catch (Exception e) { Debug.LogError($"[InputConfigLoader]存档解析失败：{path}\n{e}"); }

            if (archive != null)
            {
                archive.Root?.EnsureNotNull();
                result.Add(archive);
            }
        }
        return result;
    }

    // ============================================================
    // 玩家存档保存
    // ============================================================

    public static void SavePlayerArchives(List<InputConfigArchive> archives)
    {
        EnsurePlayerFolder();
        var index = new InputArchiveIndex();

        foreach (var archive in archives)
        {
            if (archive == null || archive.IsDefault) continue;
            if (string.IsNullOrEmpty(archive.ArchiveId))
                archive.ArchiveId = Guid.NewGuid().ToString("N");

            string fileName = InputConfigConst.PlayerArchivePrefix + archive.ArchiveId + ".json";
            string path = Path.Combine(PlayerRootFolder, fileName);
            var json = JsonUtility.ToJson(archive, true);
            WriteAllTextAtomic(path, json);

            index.Items.Add(new InputArchiveIndexItem
            {
                ArchiveId = archive.ArchiveId,
                CreateTime = archive.CreateTime,
                FileName = fileName
            });
        }

        WriteAllTextAtomic(PlayerIndexPath, JsonUtility.ToJson(index, true));
    }

    // ============================================================
    // 删除单个存档文件
    // ============================================================

    public static void DeleteArchiveFile(string archiveId)
    {
        if (string.IsNullOrEmpty(archiveId)) return;
        string fileName = InputConfigConst.PlayerArchivePrefix + archiveId + ".json";
        string path = Path.Combine(PlayerRootFolder, fileName);
        if (File.Exists(path)) File.Delete(path);
    }

    // ============================================================
    // 编辑器工具：导出 Resources 保底配置
    // ============================================================

#if UNITY_EDITOR
    /// <summary>
    /// 把工程目录下的保底配置复制到 Resources 下
    /// 方便打包前同步
    /// </summary>
    public static void ExportDefaultToResources()
    {
        string src = EditorDefaultRootFolder;
        if (!Directory.Exists(src))
        {
            Debug.LogError($"[InputConfigLoader]工程目录不存在：{src}");
            return;
        }

        string dst = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/Input/ConfigData/Resources",
            InputConfigConst.ResourceDefaultRootFolder);

        if (!Directory.Exists(dst))
            Directory.CreateDirectory(dst);

        foreach (var file in Directory.GetFiles(src, "*.json"))
        {
            string fileName = Path.GetFileName(file);
            string dstFile = Path.Combine(dst, fileName);
            File.Copy(file, dstFile, true);
        }

        Debug.Log($"[InputConfigLoader]已导出保底配置到：{dst}");
    }
#endif
}