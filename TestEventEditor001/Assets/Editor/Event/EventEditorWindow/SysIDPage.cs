//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEditor;
//using System.Linq;

//public static class SysIDPage
//{
//    private static Vector2 _scrollPos;
//    private static string _newSysId = "";
//    private static string _newRemark = "";
//    private static string _selectedSysId = "";

//    private static SysIdConfigRoot SysRoot = new();

//    public static void InitCofig(SysIDCache cache, EventEditorWindow window)
//    {
//        SysRoot = EventConfigHelper.DeepCopy(window.GlobalSysConfig);
//        cache.InitCache(SysRoot);
//    }

//    public static void Draw(SysIDCache cache, EventEditorWindow window)
//    {
//        GUILayout.BeginHorizontal();
//        if (GUILayout.Button("刷新", GUILayout.Width(80)))
//        {
//            cache.Refresh();
//            _newSysId = "";
//            _newRemark = "";
//            _selectedSysId = "";
//        }
//        GUILayout.EndHorizontal();

//        //新建系统ID
//        GUILayout.Label("新建系统ID", EditorStyles.boldLabel);
//        GUILayout.BeginHorizontal();
//        _newSysId = EditorGUILayout.TextField("系统ID", _newSysId);
//        _newRemark = EditorGUILayout.TextField("备注", _newRemark);
//        if (GUILayout.Button("添加",GUILayout.Width(60)))
//        {
//            if (!string.IsNullOrEmpty(_newSysId))
//            {
//                cache.AddNewSysId(_newSysId, _newRemark);
//                _newSysId = "";
//                _newRemark = "";
//            }
//        }
//        GUILayout.EndHorizontal();

//        //现有ID列表
//        GUILayout.Label("现有系统ID", EditorStyles.boldLabel);
//        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

//        foreach (var kv in cache.IsEnableIDList)
//        {
//            string sysId = kv.Key;
//            SysIdItem item = kv.Value;

//            GUILayout.BeginHorizontal();
//            if (GUILayout.Toggle(_selectedSysId == sysId, "", GUILayout.Width(20)))
//            {
//                _selectedSysId = sysId;
//            }
//            GUILayout.Label(sysId, GUILayout.Width(150));
//            GUILayout.Label(item.Remark, GUILayout.Width(200));

//            if (GUILayout.Button("删除", GUILayout.Width(60)))
//            {
//                cache.MarkForDeletion(sysId);
//            }
//            GUILayout.EndHorizontal();
//        }

//        GUILayout.EndScrollView();

//        //方法绑定区域
//        if (!string.IsNullOrEmpty(_selectedSysId))
//        {
//            GUILayout.Label($"方法绑定——{_selectedSysId}", EditorStyles.boldLabel);

//            //发布方法绑定
//            GUILayout.Label("发布方法", EditorStyles.boldLabel);
//            DrawMethodList(cache, _selectedSysId, cache.GetPublishMethods(_selectedSysId), true);

//            //监听方法绑定
//            GUILayout.Label("监听方法", EditorStyles.boldLabel);
//            DrawMethodList(cache, _selectedSysId, cache.GetListenMethods(_selectedSysId), false);
//        }

//        //保存按钮
//        if (GUILayout.Button("保存到全局缓存", GUILayout.Height(30)))
//        {
//            cache.CommitToGlobal();
//            Debug.Log("系统ID已保存到全局缓存，请点击总览页的一键保存配置落地");
//        }
//    }

//    private static void DrawMethodList(SysIDCache cache, string sysId, List<string> boundMethods, bool isPublish)
//    {
//        //显示已绑定方法
//        foreach (var method in boundMethods)
//        {
//            GUILayout.BeginHorizontal();
//            GUILayout.Label(method, GUILayout.Width(300));
//            if (GUILayout.Button("移除", GUILayout.Width(60)))
//            {
//                if (isPublish)
//                    cache.PubSystemRemoveMethod(sysId, method);
//                else
//                    cache.LstSystemRemoveMethod(sysId, method);
//            }
//            GUILayout.EndHorizontal();
//        }

//        //添加新方法
//        GUILayout.BeginHorizontal();

//        var allMethods = isPublish ? cache.PubMethodList : cache.LstMethodList;
//        List<string> methodNames = allMethods.Select(m => $"{m.DeclaringType.Name}.{m.Name}").ToList();

//        //添加新方法
//        if (methodNames.Count > 0)
//        {
//            int selectedIndex = isPublish ? cache.SelectedPubMethodIndex : cache.SelectedLstMethodIndex;
//            selectedIndex = EditorGUILayout.Popup(selectedIndex, methodNames.ToArray());

//            if (isPublish)
//                cache.SelectedPubMethodIndex = selectedIndex;
//            else
//                cache.SelectedLstMethodIndex = selectedIndex;

//            if (GUILayout.Button("添加", GUILayout.Width(60)))
//            {
//                string selectedMethod = methodNames[selectedIndex];
//                if (isPublish)
//                    cache.PubSystemAddMethod(sysId, selectedMethod);
//                else
//                    cache.LstSystemAddMethod(sysId, selectedMethod);
//            }

            
//        }

//        GUILayout.EndHorizontal();
//    }
//}
