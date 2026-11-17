using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 通用配置热更服务：下载 → 校验 → 原子替换 → 备份回退。
/// 只依赖 IConfigSource，不依赖任何具体配置系统。
///
/// 相比旧 ConfigUpdater 的改进（顺带修掉旧隐患）：
///   1. 文件名白名单 + 路径穿越校验（IsSafeFileName），杜绝远程恶意文件名越界写；
///   2. 下载按字节写盘，md5 按字节计算（避免文本编码/BOM 导致 md5 对不上）；
///   3. 校验失败不替换，替换失败自动回滚；
///   4. 校验"每个文件"由 source.ValidateFile 负责（各系统知道自己文件的正确类型），
///      不再用 JsonUtility.FromJson<object>（那对 object 根本无效）。
/// </summary>
public class ConfigHotUpdater : MonoBehaviour
{
    public static ConfigHotUpdater Instance { get; private set; }

    private bool _isBusy;
    public bool IsBusy => _isBusy;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ========== 版本检查 ==========

    /// <summary>拉远程 version.json 与本地比较，onResult(needUpdate, remote)。</summary>
    public IEnumerator CheckRemoteVersion(IConfigSource source, string remoteVersionUrl, Action<bool, ConfigVersionInfo> onResult)
    {
        using var req = UnityWebRequest.Get(remoteVersionUrl);
        req.timeout = 10;
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[{source.SourceId}] 版本检查失败: {req.error}, 配置地址：{remoteVersionUrl}");
            onResult?.Invoke(false, null);
            yield break;
        }

        ConfigVersionInfo remote = null;
        try { remote = source.Serializer.Deserialize<ConfigVersionInfo>(req.downloadHandler.text); }
        catch (Exception e) { Debug.LogWarning($"[{source.SourceId}] 远程版本解析失败: {e.Message}"); }

        if (remote == null || string.IsNullOrEmpty(remote.version))
        {
            onResult?.Invoke(false, null);
            yield break;
        }

        string localVer = source.GetCurrentVersion();
        bool needUpdate = VersionUtil.Compare(remote.version, localVer) > 0;
        Debug.Log($"[{source.SourceId}] 本地 {localVer} / 远程 {remote.version} → {(needUpdate ? "需更新" : "已最新")}");
        onResult?.Invoke(needUpdate, remote);
    }

    // ========== 下载应用 ==========

    /// <summary>下载整包到 TmpDir → 校验 → 原子替换到 HotDir，旧 HotDir 备份到 BackupDir。</summary>
    public IEnumerator DownloadAndApply(IConfigSource source, ConfigVersionInfo remote, Func<string, string> getFileUrl, Action<bool, string> done)
    {
        if (_isBusy)
        {
            done?.Invoke(false, "正在更新中");
            yield break;
        }
        _isBusy = true;

        // 1. 准备临时目录
        if (!PrepareTmpDir(source, out var err))
        {
            done?.Invoke(false, err);
            _isBusy = false;
            yield break;
        }

        // 2. 逐个下载（文件名安全校验 + md5 校验）
        var files = remote.files ?? Array.Empty<ConfigFileEntry>();
        foreach (var fileEntry in files)
        {
            string fileName = fileEntry?.name;
            if (!IsSafeFileName(fileName))
            {
                Debug.LogError($"[{source.SourceId}] 非法文件名: {fileName}");
                done?.Invoke(false, "非法文件名");
                _isBusy = false;
                yield break;
            }

            string url = getFileUrl(fileName);
            string savePath = Path.Combine(source.TmpDir, fileName);

            using var req = UnityWebRequest.Get(url);
            req.timeout = 30;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[{source.SourceId}] 下载失败: {fileName} → {req.error}");
                done?.Invoke(false, $"下载失败: {fileName}");
                _isBusy = false;
                yield break;
            }

            byte[] bytes = req.downloadHandler.data;
            File.WriteAllBytes(savePath, bytes);

            // md5 校验（version.json 提供了才校验）
            if (!string.IsNullOrEmpty(fileEntry.md5))
            {
                string actual = ComputeMd5(bytes);
                if (!string.Equals(actual, fileEntry.md5, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogError($"[{source.SourceId}] md5 校验失败: {fileName}");
                    done?.Invoke(false, $"校验失败: {fileName}");
                    _isBusy = false;
                    yield break;
                }
            }
            Debug.Log($"[{source.SourceId}] 下载完成: {fileName}");
        }

        // 3. 写 version.json 进 TmpDir（通用协议，热更服务自己负责）
        string tmpVerPath = Path.Combine(source.TmpDir, source.VersionFile);
        File.WriteAllText(tmpVerPath, source.Serializer.Serialize(remote, true));

        // 4. 校验 version.json 能正常反序列化
        try
        {
            var parsed = source.Serializer.Deserialize<ConfigVersionInfo>(File.ReadAllText(tmpVerPath));
            if (parsed == null || string.IsNullOrEmpty(parsed.version))
            {
                done?.Invoke(false, "version.json 校验失败");
                _isBusy = false;
                yield break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[{source.SourceId}] version.json 解析失败: {e.Message}");
            done?.Invoke(false, "version.json 校验失败");
            _isBusy = false;
            yield break;
        }

        // 5. 校验业务文件（每个 source 自己知道文件的正确类型，故交给它）
        foreach (var fileName in source.GetFileNames())
        {
            string path = Path.Combine(source.TmpDir, fileName);
            if (!File.Exists(path))
            {
                Debug.LogError($"[{source.SourceId}] 缺少文件: {fileName}");
                done?.Invoke(false, $"缺少文件: {fileName}");
                _isBusy = false;
                yield break;
            }
            string content = File.ReadAllText(path);
            if (!source.ValidateFile(fileName, content))
            {
                Debug.LogError($"[{source.SourceId}] 校验失败: {fileName}");
                done?.Invoke(false, $"校验失败: {fileName}");
                _isBusy = false;
                yield break;
            }
        }

        // 6. 原子替换：HotDir → BackupDir，TmpDir → HotDir
        if (!SwapDirectories(source, out err))
        {
            done?.Invoke(false, err);
            _isBusy = false;
            yield break;
        }

        Debug.Log($"[{source.SourceId}] 更新完成，版本 {remote.version}");
        _isBusy = false;

        // 7. 通知重载（在释放 busy 之后，重载逻辑可能较慢）
        source.OnHotUpdated();
        done?.Invoke(true, remote.version);
    }

    // ========== 回退 / 重置 ==========

    /// <summary>回退到上一版（BackupDir → HotDir，当前版先挪到 TmpDir 便于排查）。</summary>
    public bool Rollback(IConfigSource source)
    {
        if (!Directory.Exists(source.BackupDir))
        {
            Debug.LogWarning($"[{source.SourceId}] 没有可回退的备份");
            return false;
        }
        try
        {
            if (Directory.Exists(source.TmpDir)) Directory.Delete(source.TmpDir, true);
            if (Directory.Exists(source.HotDir)) Directory.Move(source.HotDir, source.TmpDir);
            Directory.Move(source.BackupDir, source.HotDir);
            Debug.Log($"[{source.SourceId}] 已回退到上一版");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[{source.SourceId}] 回退失败: {e.Message}");
            return false;
        }
    }

    /// <summary>重置为出厂配置（删掉 HotDir，下次读取回退到 StreamingAssets 保底）。</summary>
    public bool ResetToDefault(IConfigSource source)
    {
        try
        {
            if (Directory.Exists(source.HotDir)) Directory.Delete(source.HotDir, true);
            Debug.Log($"[{source.SourceId}] 已重置为出厂配置");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[{source.SourceId}] 重置失败: {e.Message}");
            return false;
        }
    }

    // ========== 私有辅助 ==========

    private static bool PrepareTmpDir(IConfigSource source, out string error)
    {
        error = null;
        try
        {
            // TmpDir 被同名文件占用时先删文件
            if (File.Exists(source.TmpDir)) File.Delete(source.TmpDir);
            Directory.CreateDirectory(source.TmpDir);
            // 只清空目录内文件，不删目录本身（避免目录句柄被占用导致删除失败）
            foreach (var f in Directory.GetFiles(source.TmpDir)) File.Delete(f);
            return true;
        }
        catch (Exception e)
        {
            error = "清理临时目录失败";
            Debug.LogError($"[{source.SourceId}] {error}: {e.Message}");
            return false;
        }
    }

    private static bool SwapDirectories(IConfigSource source, out string error)
    {
        error = null;
        try
        {
            if (Directory.Exists(source.BackupDir)) Directory.Delete(source.BackupDir, true);
            if (Directory.Exists(source.HotDir)) Directory.Move(source.HotDir, source.BackupDir);
            Directory.Move(source.TmpDir, source.HotDir);
            return true;
        }
        catch (Exception e)
        {
            error = "替换失败";
            Debug.LogError($"[{source.SourceId}] {error}: {e.Message}");
            // 尝试恢复：若 HotDir 已被挪到 BackupDir，挪回来，保证旧配置可用
            try
            {
                if (!Directory.Exists(source.HotDir) && Directory.Exists(source.BackupDir))
                    Directory.Move(source.BackupDir, source.HotDir);
            }
            catch { /* 恢复失败也只能放弃 */ }
            return false;
        }
    }

    /// <summary>文件名安全校验：拒绝路径分隔符、..、非法字符，防止越界写。</summary>
    private static bool IsSafeFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return false;
        if (fileName.Contains("..")) return false;
        if (fileName.IndexOf('/') >= 0 || fileName.IndexOf('\\') >= 0) return false;
        if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return false;
        return true;
    }

    /// <summary>计算字节 md5（与 ConfigVersionGenerator 生成 md5 的格式一致）。</summary>
    private static string ComputeMd5(byte[] data)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(data);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}
