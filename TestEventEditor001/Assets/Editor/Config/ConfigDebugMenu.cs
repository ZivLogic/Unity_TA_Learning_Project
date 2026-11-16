using UnityEditor;
using UnityEngine;

public static class ConfigDebugMenu
{
    [MenuItem("Tools/Config/Rollback to Previous")]
    public static void Rollback()
    {
        if (ConfigUpdater.Instance == null)
        {
            Debug.LogError("请先运行游戏，ConfigUpdater 还没初始化！");
            return;
        }

        if (ConfigUpdater.Instance.Rollback())
        {
            ConfigManager.Instance.ReloadAll();
            Debug.Log("回退成功，已重新加载配置");
        }
    }

    [MenuItem("Tools/Config/Reset to Default")]
    public static void ResetToDefault()
    {
        if (ConfigUpdater.Instance == null)
        {
            Debug.LogError("请先运行游戏，ConfigUpdater 还没初始化！");
            return;
        }

        if (ConfigUpdater.Instance.ResetToDefault())
        {
            ConfigManager.Instance.ReloadAll();
            Debug.Log("已重置为出厂配置，并重新加载");
        }
    }
}