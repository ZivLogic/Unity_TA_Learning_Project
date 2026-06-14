//using System.Collections;
//using System.Collections.Generic;
//using UnityEditor;
//using UnityEngine;


//public static class EventOverviewPage
//{
//    private static Vector2 _scrollPos;

//    public static void Draw(EventOverviewCache cache, EventEditorWindow window)
//    {
//        GUILayout.BeginHorizontal();
//        if (GUILayout.Button("刷新", GUILayout.Width(80)))
//        {
//            //cache.RefreshFromGlobal();
//            EventGlobalCache.IsRefresh = true;
//        }
//        if (GUILayout.Button("一键保存配置", GUILayout.Width(100)))
//        {
//            EventGlobalCache.SaveAllToDisk();
//            Debug.Log("配置已缓存");
//        }
//        GUILayout.EndHorizontal();

//        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

//        foreach (var kv in EventGlobalCache.GlobalEventConfig.Items)
//        {
//            string eventName = kv.Key;
//            EventItem item = kv.Value;

//            //绘画事件表
//            DrawEventItem(cache, window, eventName, item);
//        }

//        EditorGUILayout.EndScrollView();

//        //新建事件按钮
//        if (GUILayout.Button("+ 新建事件"))
//        {
//            //新建事件
//            window.SetNewEvent();
//            window.SwitchToTab(1);
            
//        }
//    }

//    //绘画事件表
//    private static void DrawEventItem(EventOverviewCache cache, EventEditorWindow window, string eventName, EventItem item)
//    {
//        GUILayout.BeginHorizontal();
//        GUILayout.Label(eventName, GUILayout.Width(150));
//        GUILayout.Label($"队列：{item.QueueType}", GUILayout.Width(100));
//        GUILayout.Label($"启用：{(item.GlobalEnable ? "已启用" : "未启用")}", GUILayout.Width(60));
//        GUILayout.Label($"发布：{item.EventPublish}", GUILayout.Width(200));
//        GUILayout.Label($"监听数：{item.EventListen.Count}", GUILayout.Width(80));

//        if (GUILayout.Button("编辑", GUILayout.Width(60)))
//        {
//            //跳转编辑页，传递事件名
//            window.SetEditTarget(eventName);
//            window.SwitchToTab(1);
            
//        }
//        GUILayout.EndHorizontal();
//    }


//}