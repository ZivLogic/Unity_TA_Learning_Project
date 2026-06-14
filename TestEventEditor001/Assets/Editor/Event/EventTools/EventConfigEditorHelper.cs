using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace EventSystemV2
{
    [InitializeOnLoad]
    public class EventConfigEditorHelper
    {
        private static double _lastRefreshTime;
        private const double REFRESH_INTERVAL = 0.5; //节流间隔


        static EventConfigEditorHelper()
        {
            //确保配置路径存在并刷新
            if (!System.IO.Directory.Exists(EventConfigHelper.BaseConfigDir))
            {
                System.IO.Directory.CreateDirectory(EventConfigHelper.BaseConfigDir);
                AssetDatabase.Refresh();
            }

            //监听刷新
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            //检查是否需要刷新
            if (!EventGlobalCache.IsRefresh) return;

            //节流：距离上次刷新不到0.5秒就跳过
            double now = EditorApplication.timeSinceStartup;
            if (now - _lastRefreshTime < REFRESH_INTERVAL) return;

            //执行刷新
            _lastRefreshTime = now;
            EventGlobalCache.IsRefresh = false;
            AssetDatabase.Refresh();
            Debug.Log("[EventConfigEditorHelper]资源数据库已刷新");
        }
    }
}
