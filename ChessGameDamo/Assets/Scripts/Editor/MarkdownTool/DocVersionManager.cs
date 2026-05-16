using System;
using System.IO;

/// <summary>
/// 框架MD技术文档版本管理工具
/// 语义化版本：v主.次.修订 如 v1.0.0
/// </summary>
public static class DocVersionManager
{
    // ========== 自己改这三个路径就行 ==========
    private const string RootDocsPath = @"E:/JsonFile/Markdown";
    private const string CurrentDocPath = @"E:/JsonFile/Markdown/Current";
    private const string HistoryDocPath = @"E:/JsonFile/Markdown/History";

    /// <summary>
    /// 归档当前整套文档到历史版本
    /// </summary>
    /// <param name="version">版本号 格式：1.0.0</param>
    /// <param name="updateDesc">更新说明</param>
    public static void ArchiveCurrentToHistory(string version, string updateDesc)
    {
        // 格式化版本文件夹 v1.0.0
        string versionFolder = Path.Combine(HistoryDocPath, $"v{version}");

        if (Directory.Exists(versionFolder))
        {
            Console.WriteLine($"版本文件夹已存在：{versionFolder}，无需重复归档");
            return;
        }

        // 拷贝整个Current到历史版本目录
        CopyDirectory(CurrentDocPath, versionFolder);

        // 生成版本更新日志
        string logPath = Path.Combine(versionFolder, "版本更新说明.txt");
        string logContent = $"版本号：v{version}\r\n更新时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}\r\n更新说明：{updateDesc}";
        File.WriteAllText(logPath, logContent);

        Console.WriteLine($"✅ 文档归档成功：{versionFolder}");
    }

    /// <summary>
    /// 用新MD文件 替换 Current 里的旧MD
    /// </summary>
    /// <param name="newMdPath">新写好的MD完整路径</param>
    /// <param name="oldMdRelativePath">Current下旧文档相对路径 例如：前言.md</param>
    public static void ReplaceSingleMdFile(string newMdPath, string oldMdRelativePath)
    {
        if (!File.Exists(newMdPath))
        {
            Console.WriteLine($"❌ 新文件不存在：{newMdPath}");
            return;
        }

        string oldMdFullPath = Path.Combine(CurrentDocPath, oldMdRelativePath);

        // 目录不存在则创建
        string oldDir = Path.GetDirectoryName(oldMdFullPath);
        if (!Directory.Exists(oldDir))
        {
            Directory.CreateDirectory(oldDir);
        }

        // 覆盖替换
        File.Copy(newMdPath, oldMdFullPath, overwrite: true);
        Console.WriteLine($"✅ 文档替换完成：{oldMdFullPath}");
    }

    /// <summary>
    /// 递归拷贝整个文件夹
    /// </summary>
    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        if (!Directory.Exists(targetDir))
            Directory.CreateDirectory(targetDir);

        // 拷贝文件
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            string destFile = Path.Combine(targetDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }

        // 递归子文件夹
        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            string destSubDir = Path.Combine(targetDir, Path.GetFileName(dir));
            CopyDirectory(dir, destSubDir);
        }
    }
}