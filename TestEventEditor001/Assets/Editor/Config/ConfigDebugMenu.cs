using UnityEditor;
using UnityEngine;

public static class ConfigDebugMenu
{
    private static readonly MainConfigSource _mainSource = new MainConfigSource();

    [MenuItem("Tools/Config/Rollback to Previous")]
    public static void Rollback()
    {
        if (ConfigHotUpdater.Instance == null)
        {
            Debug.LogError("请先运行游戏，ConfigHotUpdater 还没初始化！");
            return;
        }

        if (ConfigHotUpdater.Instance.Rollback(_mainSource))
        {
            ConfigManager.Instance.ReloadAll();
            Debug.Log("回退成功，已重新加载配置");
        }
    }

    [MenuItem("Tools/Config/Reset to Default")]
    public static void ResetToDefault()
    {
        if (ConfigHotUpdater.Instance == null)
        {
            Debug.LogError("请先运行游戏，ConfigHotUpdater 还没初始化！");
            return;
        }

        if (ConfigHotUpdater.Instance.ResetToDefault(_mainSource))
        {
            ConfigManager.Instance.ReloadAll();
            Debug.Log("已重置为出厂配置，并重新加载");
        }
    }
}
