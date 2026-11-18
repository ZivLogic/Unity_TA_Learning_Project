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
    // 保底目录（出厂只读）
    private static string DefaultDir =>
        Path.Combine(ConfigPaths.DefaultRoot, ConfigDirs.InputConfig);

    // 热更目录（运行时读优先）
    private static string HotDir =>
        Path.Combine(ConfigPaths.HotRoot, ConfigDirs.InputConfig);

    // 玩家改键存档目录（独立，不参与热更）
    private static string PlayerRootFolder =>
        Path.Combine(ConfigPaths.SaveRoot, ConfigDirs.InputPlayer);

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
        // 热更优先
        string hot = ReadAllTextSafe(Path.Combine(HotDir, jsonFile));
        if (!string.IsNullOrEmpty(hot)) return hot;

        // 回退保底（StreamingAssets）
        string def = ReadAllTextSafe(Path.Combine(DefaultDir, jsonFile));
        if (!string.IsNullOrEmpty(def)) return def;

        return null;
    }

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
}