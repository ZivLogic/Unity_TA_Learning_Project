//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEditor;
//using System.Linq;
//using System;

//public static class EventEditorPage
//{
//    private static Vector2 _scrollPos;
//    private static string _editEventName = "";

//    private static EventConfigRoot EventRoot = new();
//    private static SysIdConfigRoot SysIDRoot = new();
//    private static Dictionary<string, List<string>> methodToKeyList = new();

//    //窗口将自己的缓存传回给缓存
//    public static void InitCache(EventEditorCache cache, EventEditorWindow window)
//    {
//        EventRoot = EventConfigHelper.DeepCopy(window.GlobalEventConfig);
//        SysIDRoot = EventConfigHelper.DeepCopy(window.GlobalSysConfig);
//        methodToKeyList = EventConfigHelper.DeepCopy(window.MethodToKeyList);
//        Debug.Log($"编辑事件页--自己的事件缓存数量：{EventRoot.Items.Count}");
//        cache.InitCache(EventRoot, SysIDRoot, methodToKeyList);
//    }


//    public static void Draw(EventEditorCache cache, EventEditorWindow window)
//    {

//        GUILayout.Label(_editEventName == "" ? "新建事件" : $"编辑事件：{_editEventName}", EditorStyles.boldLabel);

//        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

//        //事件基本信息
//        string newEventName = EditorGUILayout.TextField("事件名", cache.EditTarget?.EventName ?? "");
//        cache.EditTarget.QueueType = (EventQueueType)EditorGUILayout.EnumPopup("队列类型", cache.EditTarget.QueueType);
//        cache.EditTarget.GlobalEnable = EditorGUILayout.Toggle("全局启用", cache.EditTarget.GlobalEnable);

//        //发布方选择（依赖系统ID）
//        DrawPublishSelector(cache);

//        //监听方列表
//        DarwListenList(cache);

//        //字段映射
//        if (!string.IsNullOrEmpty(cache.EditTarget?.EventPublish) && cache.EditTarget.EventListen.Count > 0)
//        {
//            DrawFieldMappings(cache);
//        }

//        EditorGUILayout.EndScrollView();

//        GUILayout.FlexibleSpace();

//        //保存按钮
//        GUI.enabled = cache.IsValid();
//        if (GUILayout.Button("保存到全局缓存", GUILayout.Height(30)))
//        {
//            cache.CommitToGlobal();
//            _editEventName = "";
//            Debug.Log("事件已保存到全局缓存,请点击总览页的一键保存配置落地");
//        }
//        GUI.enabled = true;
//    }



//    //获取发布方
//    private static void DrawPublishSelector(EventEditorCache cache)
//    {
//        GUILayout.Label("发布方", EditorStyles.boldLabel);

//        //系统ID下拉
//        List<string> sysIds = cache.IsEnableIDList.Keys.ToList();
//        int selectedSysIndex = Math.Max(0, sysIds.IndexOf(cache.SelectedPublishSysId));
//        selectedSysIndex = EditorGUILayout.Popup("系统ID", selectedSysIndex, sysIds.ToArray());

//        if (selectedSysIndex >= 0 && selectedSysIndex < sysIds.Count)
//        {
//            string selectedSysId = sysIds[selectedSysIndex];
//            cache.SelectedPublishSysId = selectedSysId;

//            //获取该系统下的发布方法
//            var methods = cache.SysIDToLstenMethod.GetValueOrDefault(selectedSysId, new List<string>());
//            int selectedMethodIndex = Math.Max(0, methods.IndexOf(cache.EditTarget?.EventPublish ?? ""));
//            selectedMethodIndex = EditorGUILayout.Popup("发布方法", selectedMethodIndex, methods.ToArray());

//            if (selectedMethodIndex >= 0 && selectedMethodIndex < methods.Count)
//            {
//                cache.EditTarget.EventPublish = methods[selectedMethodIndex];
//            }
//        }
//    }

//    //获取监听方
//    private static void DarwListenList(EventEditorCache cache)
//    {
//        GUILayout.Label("监听方", EditorStyles.boldLabel);

//        //显示已添加的监听
//        for (int i = 0; i < cache.EditTarget.EventListen.Count; i++)
//        {
//            GUILayout.BeginHorizontal();
//            GUILayout.Label(cache.EditTarget.EventListen[i]);
//            if (GUILayout.Button("移除", GUILayout.Width(60)))
//            {
//                cache.RemoveListenAT(i);
//            }
//            GUILayout.EndHorizontal();
//        }

//        //添加新监听
//        DrawAddListenSelector(cache);
//    }

//    private static void DrawAddListenSelector(EventEditorCache cache)
//    {
//        GUILayout.BeginHorizontal();

//        List<string> sysIds = cache.IsEnableIDList.Keys.ToList();
//        int selectedSysIndex = Math.Max(0, sysIds.IndexOf(cache.SelectedListenSysId));
//        selectedSysIndex = EditorGUILayout.Popup("系统ID", selectedSysIndex, sysIds.ToArray());

//        if (selectedSysIndex >= 0 && selectedSysIndex < sysIds.Count)
//        {
//            string selectedSysId = sysIds[selectedSysIndex];
//            cache.SelectedListenSysId = selectedSysId;

//            var methods = cache.SysIDToLstenMethod.GetValueOrDefault(selectedSysId, new List<string>());
//            int selectedMethodIndex = EditorGUILayout.Popup("监听方法", cache.SelectedListenMethodIndex, methods.ToArray());
//            cache.SelectedListenMethodIndex = selectedMethodIndex;

//            if (GUILayout.Button("添加", GUILayout.Width(60)))
//            {
//                if (selectedMethodIndex >= 0 && selectedMethodIndex < methods.Count)
//                {
//                    cache.AddListenMethod(methods[selectedMethodIndex]);
//                    cache.SelectedLisSysIdList.Add(selectedSysId);
//                }
//            }
//        }

//        GUILayout.EndHorizontal();
//    }

//    //字段映射绘制
//    private static void DrawFieldMappings(EventEditorCache cache)
//    {
//        GUILayout.Label("字段映射", EditorStyles.boldLabel);

//        foreach (var listenMethod in cache.EditTarget.EventListen)
//        {
//            string mappingKey = $"{cache.EditTarget.EventPublish}_{listenMethod}";
//            if (!cache.EditTarget.KeyConnection.ContainsKey(mappingKey))
//            {
//                cache.EditTarget.KeyConnection[mappingKey] = new Dictionary<string, string>();
//            }

//            var mapping = cache.EditTarget.KeyConnection[mappingKey];
//            var listenKeys = cache.MethodToKey.GetValueOrDefault(listenMethod, new List<string>());
//            var publishKeys = cache.MethodToKey.GetValueOrDefault(cache.EditTarget.EventPublish, new List<string>());

//            GUILayout.Label($"监听：{listenMethod}", EditorStyles.miniBoldLabel);
//            EditorGUI.indentLevel++;

//            foreach (var listenKey in listenKeys)
//            {
//                GUILayout.BeginHorizontal();
//                GUILayout.Label($"{listenKey} ->", GUILayout.Width(100));

//                // 选项列表：["(不使用)", "发布参数A", "发布参数B"...]
//                var options = new List<string> { "(不使用)" };
//                options.AddRange(publishKeys);

//                // 获取当前选中的值
//                string currentMapping = mapping.GetValueOrDefault(listenKey, "");
//                int selectedIndex = options.IndexOf(currentMapping);
//                if (selectedIndex < 0) selectedIndex = 0;

//                // 绘制下拉菜单
//                int newSelectedIndex = EditorGUILayout.Popup(selectedIndex, options.ToArray());

//                // 处理选择结果
//                if (newSelectedIndex == 0)
//                {
//                    // 选择了"(不使用)"，移除映射
//                    if (mapping.ContainsKey(listenKey))
//                    {
//                        mapping.Remove(listenKey);
//                    }
//                    EditorGUILayout.LabelField("跳过", GUILayout.Width(40));
//                }
//                else
//                {
//                    string newPublishKey = options[newSelectedIndex];
//                    mapping[listenKey] = newPublishKey;

//                    // 类型校验
//                    if (!cache.CheckTypeMetch(cache.EditTarget.EventPublish, listenMethod, listenKey, newPublishKey))
//                    {
//                        EditorGUILayout.LabelField("类型错误", GUILayout.Width(40));
//                    }
//                    else
//                    {
//                        EditorGUILayout.LabelField("类型正确", GUILayout.Width(40));
//                    }
//                }

//                GUILayout.EndHorizontal();
//            }

//            EditorGUI.indentLevel--;
//        }
//    }
//}