using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 配置热更器：下载 → 校验 → 原子替换 → 保留备份
/// 回退器：backup → Config
/// </summary>
public class ConfigUpdater : MonoBehaviour
{
    private static string HotDir => Path.Combine(Application.persistentDataPath, "Config");
    private static string BackupDir => Path.Combine(Application.persistentDataPath, "Config_backup");
    private static string TmpDir => Path.Combine(Application.persistentDataPath, "Config_tmp");

    private bool _isDownloading = false;

    public static ConfigUpdater Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ========== 版本检查 ==========

    /// <summary>
    /// 从服务器拉 version.json，和本地比较。
    /// onResult(true) 表示有新版本需要更新。
    /// </summary>
    public IEnumerator CheckRemoteVersion(string remoteVersionUrl, Action<bool, ConfigManager.ConfigVersionInfo> onResult)
    {
        using var req = UnityWebRequest.Get(remoteVersionUrl);
        req.timeout = 10;
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[ConfigUpdater] 版本检查失败: {req.error}");
            onResult?.Invoke(false, null);
            yield break;
        }

        ConfigManager.ConfigVersionInfo remote = null;
        try { remote = JsonUtility.FromJson<ConfigManager.ConfigVersionInfo>(req.downloadHandler.text); }
        catch (Exception e) { Debug.LogWarning($"[ConfigUpdater] 远程版本解析失败: {e.Message}"); }

        if (remote == null || string.IsNullOrEmpty(remote.version))
        {
            onResult?.Invoke(false, null);
            yield break;
        }

        string localVer = ConfigManager.Instance.CurrentConfigVersion;
        bool needUpdate = CompareVersion(remote.version, localVer) > 0;
        Debug.Log($"[ConfigUpdater] 本地 {localVer} / 远程 {remote.version} → {(needUpdate ? "需更新" : "已最新")}");
        onResult?.Invoke(needUpdate, remote);
    }

    /// <summary>版本比较：a>b 返回正数</summary>
    private static int CompareVersion(string a, string b)
    {
        var pa = a.Split('.').Select(s => int.TryParse(s, out var n) ? n : 0).ToArray();
        var pb = b.Split('.').Select(s => int.TryParse(s, out var n) ? n : 0).ToArray();
        int len = Math.Max(pa.Length, pb.Length);
        for (int i = 0; i < len; i++)
        {
            int va = i < pa.Length ? pa[i] : 0;
            int vb = i < pb.Length ? pb[i] : 0;
            if (va != vb) return va - vb;
        }
        return 0;
    }

    // ========== 更新 ==========

    /// <summary>
    /// 下载整包配置到 TmpDir，校验后原子替换到 HotDir，并把旧 HotDir 备份到 BackupDir。
    /// fileUrls: 文件名 → 下载地址（应包含 version.json）
    /// </summary>
    public IEnumerator DownloadAndApply(
        ConfigManager.ConfigVersionInfo remote,
        Func<string, string> getFileUrl,   // 文件名 → URL
        Action<bool, string> onDone)
    {
        if (_isDownloading)
        {
            Debug.LogWarning("[ConfigUpdater] 正在下载中，忽略本次请求");
            onDone?.Invoke(false, "正在下载中");
            yield break;
        }

        _isDownloading = true;

        // 1. 确保临时目录存在且清空（避免被系统占用导致删除目录失败）
        try
        {
            if (File.Exists(TmpDir)) File.Delete(TmpDir);
            Directory.CreateDirectory(TmpDir); // 如果不存在会创建，存在则无影响

            // 清空里面的旧文件，而不是删除整个目录
            foreach (var file in Directory.GetFiles(TmpDir))
            {
                File.Delete(file);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[ConfigUpdater] 清理临时目录失败，可能文件被占用: {e.Message}");
            onDone?.Invoke(false, "清理临时目录失败");
            _isDownloading = false;
            yield break;
        }

        // 2. 逐个下载
        // 注意这里改成了 ConfigManager.ConfigFileEntry
        foreach (var fileEntry in remote.files ?? Array.Empty<ConfigManager.ConfigFileEntry>())
        {
            string fileName = fileEntry.name; // 取出具体的文件名

            if (string.IsNullOrEmpty(fileName))
            {
                Debug.LogError("[ConfigUpdater] 从 version.json 解析出的文件名为空，请检查 JSON 格式！");
                onDone?.Invoke(false, "文件名为空");
                _isDownloading = false;
                yield break;
            }

            string url = getFileUrl(fileName);
            string savePath = Path.Combine(TmpDir, fileName);

            using var req = UnityWebRequest.Get(url);
            req.timeout = 30;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[ConfigUpdater] 下载失败: {fileName} → {req.error}");
                onDone?.Invoke(false, $"下载失败: {fileName}");
                _isDownloading = false;
                yield break;
            }

            File.WriteAllText(savePath, req.downloadHandler.text);
            Debug.Log($"[ConfigUpdater] 下载完成: {fileName}");
        }

        // 3. 把 version.json 也写进 TmpDir（覆盖或补写）
        string tmpVer = Path.Combine(TmpDir, "version.json");
        File.WriteAllText(tmpVer, JsonUtility.ToJson(remote, true));

        // 4. 简单校验：每个文件能解析成 JSON
        foreach (var f in Directory.GetFiles(TmpDir, "*.json"))
        {
            try { JsonUtility.FromJson<object>(File.ReadAllText(f)); }
            catch { Debug.LogError($"[ConfigUpdater] JSON 校验失败: {f}"); onDone?.Invoke(false, "JSON 校验失败"); yield break; }
        }

        // 5. 原子替换：HotDir → BackupDir，TmpDir → HotDir
        try
        {
            if (Directory.Exists(BackupDir)) Directory.Delete(BackupDir, true);
            if (Directory.Exists(HotDir)) Directory.Move(HotDir, BackupDir);
            Directory.Move(TmpDir, HotDir);
        }
        catch (Exception e)
        {
            Debug.LogError($"[ConfigUpdater] 替换失败: {e}");
            // 尝试恢复
            if (!Directory.Exists(HotDir) && Directory.Exists(BackupDir))
                Directory.Move(BackupDir, HotDir);
            onDone?.Invoke(false, $"替换失败: {e.Message}");
            _isDownloading = false;
            yield break;
        }

        Debug.Log($"[ConfigUpdater] 更新完成，版本 {remote.version}");
        onDone?.Invoke(true, remote.version);

        // 无论成功失败，最后一定要释放锁
        _isDownloading = false;
    }

    // ========== 回退 ==========

    /// <summary>回退到上一版（BackupDir → HotDir）</summary>
    public bool Rollback()
    {
        if (!Directory.Exists(BackupDir))
        {
            Debug.LogWarning("[ConfigUpdater] 没有可回退的备份");
            return false;
        }

        try
        {
            // 当前版先挪到 tmp（不直接删，方便排查）
            if (Directory.Exists(TmpDir)) Directory.Delete(TmpDir, true);
            if (Directory.Exists(HotDir)) Directory.Move(HotDir, TmpDir);
            Directory.Move(BackupDir, HotDir);
            Debug.Log("[ConfigUpdater] 已回退到上一版");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[ConfigUpdater] 回退失败: {e}");
            return false;
        }
    }

    /// <summary>回退到出厂版本（删掉 HotDir，下次加载回退到 StreamingAssets）</summary>
    public bool ResetToDefault()
    {
        try
        {
            if (Directory.Exists(HotDir)) Directory.Delete(HotDir, true);
            Debug.Log("[ConfigUpdater] 已重置为出厂配置");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[ConfigUpdater] 重置失败: {e}");
            return false;
        }
    }
}