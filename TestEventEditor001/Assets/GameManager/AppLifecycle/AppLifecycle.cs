// Framework/AppLifecycle.cs
using UnityEngine;

/// <summary>
/// 应用生命周期：全局退出状态
/// 所有系统统一查询，避免退出阶段误重建/误回调
/// </summary>
public static class AppLifecycle
{
    private static bool _isQuitting = false;

    public static bool IsQuitting => _isQuitting;

    /// <summary>标记进入退出阶段</summary>
    public static void MarkQuitting() => _isQuitting = true;

    /// <summary>重置（重新进入播放模式）</summary>
    public static void Reset() => _isQuitting = false;
}

/// <summary>
/// 生命周期初始化器：场景里挂一个即可
/// 负责在每次进入播放模式时重置标记
/// </summary>
public class AppLifecycleInitializer : MonoBehaviour
{
    private void Awake()
    {
        AppLifecycle.Reset();
    }

    private void OnApplicationQuit()
    {
        AppLifecycle.MarkQuitting();
    }
}