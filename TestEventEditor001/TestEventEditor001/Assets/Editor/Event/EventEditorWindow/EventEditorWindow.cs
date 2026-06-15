// EventEditorWindow.cs
using CodiceApp.EventTracking.Plastic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEditor;
using UnityEngine;

namespace EventSystemV2
{
    public class EventEditorWindow : EditorWindow
    {
        private int _currentTab = 0;
        private string[] _tabName = { "事件总览", "事件编辑", "系统ID管理", "操作日志" };

        // ========== 统一数据源（主窗口管理）==========
        public Dictionary<string, EventItem> AllEvents = new();
        public Dictionary<string, SysIdItem> AllSysIds = new();
        public Dictionary<string, SysIdItem> Is_NoEnableSysID = new();
        public Dictionary<string, List<string>> SysIDToPublishMethod = new();
        public Dictionary<string, List<string>> SysIDToListenMethod = new();
        public Dictionary<string, List<string>> MethodToKeyList = new();
        public Dictionary<string, MethodParamInfo> GlobalMethodParams = new();

        // 编辑页的临时编辑对象
        public EventItem EditingEvent = null;
        public string EditingEventName = "";
        public string SelectedListenSysId = "";
        public List<string> EditingListenSysIds = new();
        public string SelectedPublishSysId = "";

        // 系统ID页的临时数据
        public string NewSysIdInput = "";
        public string NewRemarkInput = "";
        public string SelectedSysId = "";
        public int SelectedPubMethodIndex = 0;
        public int SelectedLstMethodIndex = 0;

        [MenuItem("Tools/事件系统/编辑器窗口")]
        public static void ShowWindow()
        {
            GetWindow<EventEditorWindow>("事件系统配置");
        }

        // ========== 初始化 ==========
        private void OnEnable()
        {
            //先清空缓存
            EventGlobalCache.ClearAllMemorCache();

            //初始化全局缓存
            EventGlobalCache.ReloadAllFromDisk();

            LoadAllData();
        }

        private void LoadAllData()
        {
            // 从全局缓存加载
            AllEvents = EventConfigHelper.DeepCopy(EventGlobalCache.GlobalEventConfig?.Items) ?? new();
            AllSysIds = EventConfigHelper.DeepCopy(EventGlobalCache.SysId_IsEnable) ?? new();
            Is_NoEnableSysID = EventConfigHelper.DeepCopy(EventGlobalCache.SysId_NoEnable) ?? new();
            SysIDToPublishMethod = EventConfigHelper.DeepCopy(EventGlobalCache.SysIDToPubMethod) ?? new();
            SysIDToListenMethod = EventConfigHelper.DeepCopy(EventGlobalCache.SysIDToLstMethod) ?? new();
            MethodToKeyList = EventConfigHelper.DeepCopy(EventGlobalCache.MethodToKeyList) ?? new();
            GlobalMethodParams = EventConfigHelper.DeepCopy(EventGlobalCache.GlobalMethodParams) ?? new();

            Debug.Log($"加载数据: 事件数={AllEvents.Count}, 系统ID数={AllSysIds.Count}，监听系统检索表的数量={SysIDToListenMethod.Count}");
            foreach (var kv in SysIDToListenMethod)
            {
                foreach (var k in kv.Value)
                {
                    Debug.Log($"系统名称={kv.Key}，方法具体名称={k}");
                }
            }
        }

        // ========== 统一保存 ==========
        private void SaveAllToGlobal()
        {
            // 清空全局缓存
            EventGlobalCache.GlobalEventConfig.Items.Clear();
            EventGlobalCache.GlobalSysConfig.ItemsIsEnable.Clear();
            EventGlobalCache.GlobalSysConfig.SysIDToPublsihMethod.Clear();
            EventGlobalCache.GlobalSysConfig.SysIDToListenMethod.Clear();

            // 写入主窗口数据
            foreach (var kv in AllEvents)
            {
                EventGlobalCache.GlobalEventConfig.Items[kv.Key] = EventConfigHelper.DeepCopy(kv.Value);
            }
            foreach (var kv in AllSysIds)
            {
                EventGlobalCache.GlobalSysConfig.ItemsIsEnable[kv.Key] = EventConfigHelper.DeepCopy(kv.Value);
            }
            foreach (var kv in SysIDToPublishMethod)
            {
                EventGlobalCache.GlobalSysConfig.SysIDToPublsihMethod[kv.Key] = new List<string>(kv.Value);
            }
            foreach (var kv in SysIDToListenMethod)
            {
                EventGlobalCache.GlobalSysConfig.SysIDToListenMethod[kv.Key] = new List<string>(kv.Value);
            }

            // 刷新映射缓存
            EventGlobalCache.RefeshMethodInstancesToEvent();

            // 保存到磁盘
            EventGlobalCache.SaveAllToDisk();

            Debug.Log("已保存到全局缓存和磁盘");
        }

        // ========== 统一刷新 ==========
        public void RefreshAll()
        {
            LoadAllData();
            Repaint();
            Debug.Log("已刷新所有数据");
        }


        // ========== 绘制 ==========
        private void OnGUI()
        {
            DrawToolbar();

            switch (_currentTab)
            {
                case 0:
                    DrawOverviewPage();
                    break;
                case 1:
                    DrawEventEditorPage();
                    break;
                case 2:
                    DrawSysIDPage();
                    break;
                case 3:
                    DrawLogPage();
                    break;
            }
        }

        private void DrawToolbar()
        {
            GUILayout.BeginHorizontal();

            _currentTab = GUILayout.Toolbar(_currentTab, _tabName);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("刷新", GUILayout.Width(60)))
            {
                RefreshAll();
            }

            if (GUILayout.Button("保存", GUILayout.Width(60)))
            {
                SaveAllToGlobal();
            }

            GUILayout.EndHorizontal();
        }

        // ========== 事件总览页 ==========
        private void DrawOverviewPage()
        {
            foreach (var kv in AllEvents)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(kv.Key, GUILayout.Width(150));
                GUILayout.Label($"发布: {kv.Value.EventPublish}", GUILayout.Width(200));
                GUILayout.Label($"监听数: {kv.Value.EventListen.Count}", GUILayout.Width(80));

                if (GUILayout.Button("编辑", GUILayout.Width(60)))
                {
                    StartEditEvent(kv.Key);
                }

                if (GUILayout.Button("删除", GUILayout.Width(60)))
                {
                    DeleteEvent(kv.Key);
                }

                GUILayout.EndHorizontal();
            }

            if (GUILayout.Button("+ 新建事件"))
            {
                StartNewEvent();
            }
        }

        // ========== 事件编辑页 ==========
        private void StartEditEvent(string eventName)
        {
            if (!AllEvents.TryGetValue(eventName, out var eventItem))
            {
                Debug.LogError($"事件 {eventName} 不存在");
                return;
            }

            EditingEvent = EventConfigHelper.DeepCopy(eventItem);
            EditingEventName = eventName;

            // 恢复监听系统ID列表

            // 恢复发布系统ID
            SelectedPublishSysId = EditingEvent.PublishMethodToSystemID.GetValueOrDefault(EditingEvent.EventPublish, "");

            _currentTab = 1;
        }

        private void StartNewEvent()
        {
            EditingEvent = new EventItem
            {
                EventName = "",
                QueueType = EventQueueType.Logic,
                GlobalEnable = true,
                EventPublish = "",
                EventListen = new List<string>(),
                ListenMethodDict = new Dictionary<string, string>(),
                PublishSysId = new List<string>(),
                ListenSysId = new List<string>(),
                PublishClassList = new List<string>(),
                ListenClassList = new List<string>(),
                PublishClassName = new Dictionary<string, string>(),
                ListenClassName = new Dictionary<string, string>(),
                KeyConnection = new Dictionary<string, Dictionary<string, string>>(),
                ListenMethodToSystemID = new Dictionary<string, string>(),
                PublishMethodToSystemID = new Dictionary<string, string>(),
                PublishKey = new Dictionary<string, List<string>>(),
                ListenKey = new Dictionary<string, List<string>>()
            };
            EditingEventName = "";
            EditingListenSysIds.Clear();
            SelectedPublishSysId = "";
            _currentTab = 1;
        }

        private void DrawEventEditorPage()
        {
            if (EditingEvent == null)
            {
                GUILayout.Label("请从总览页新建或编辑事件");
                return;
            }

            // 编辑UI...
            EditingEvent.EventName = EditorGUILayout.TextField("事件名", EditingEvent.EventName);
            EditingEvent.QueueType = (EventQueueType)EditorGUILayout.EnumPopup("队列", EditingEvent.QueueType);
            EditingEvent.GlobalEnable = EditorGUILayout.Toggle("启用", EditingEvent.GlobalEnable);

            // 发布方选择
            DrawPublishSelector();

            if (!string.IsNullOrEmpty(EditingEvent.EventPublish))
            {
                // 监听方列表
                DarwListenList();

                // 字段映射
                if (!string.IsNullOrEmpty(EditingEvent.EventPublish) && EditingEvent.EventListen.Count > 0)
                {
                    DrawFieldMappings();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("请先选择发布方", MessageType.Info);
            }

            GUILayout.FlexibleSpace();

            GUILayout.Space(20);
            if (GUILayout.Button("保存事件", GUILayout.Height(30)))
            {
                if (string.IsNullOrEmpty(EditingEvent.EventName))
                {
                    Debug.LogError("事件名不能为空");
                    return;
                }

                var completeEvent = AllToConfig();
                if (completeEvent == null)
                {
                    Debug.LogError("生成事件配置失败");
                    return;
                }

                EditingEvent = completeEvent;

                if (!IsValid())
                {
                    Debug.Log("事件数据不完整，请重新编辑事件");
                    return;
                }


                // 更新主窗口数据
                AllEvents[EditingEvent.EventName] = EventConfigHelper.DeepCopy(EditingEvent);

                UpdateSysIdEventStatus(EditingEvent);

                // 如果是编辑旧事件且名字变了，删除旧条目
                if (!string.IsNullOrEmpty(EditingEventName) && EditingEventName != EditingEvent.EventName)
                {
                    AllEvents.Remove(EditingEventName);
                }

                Debug.Log($"事件 [{EditingEvent.EventName}] 已保存");
                //添加日志
                EventGlobalCache.AddLog($"选择的事件：{EditingEvent.EventName}", $"保存了事件：{EditingEvent.EventName}");
                EditingEvent = null;
                EditingEventName = "";
                SelectedPublishSysId = "";
                EditingListenSysIds.Clear();
                SelectedListenSysId = "";

                _currentTab = 0;
            }
        }

        // ========== 系统ID管理页 ==========
        private void DrawSysIDPage()
        {
            // 新建系统ID
            GUILayout.Label("新建系统ID", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            NewSysIdInput = EditorGUILayout.TextField("系统ID", NewSysIdInput);
            NewRemarkInput = EditorGUILayout.TextField("备注", NewRemarkInput);
            if (GUILayout.Button("添加", GUILayout.Width(60)))
            {
                if (!string.IsNullOrEmpty(NewSysIdInput) && !AllSysIds.ContainsKey(NewSysIdInput))
                {
                    var newItem = new SysIdItem
                    {
                        SysId = NewSysIdInput,
                        Remark = NewRemarkInput,
                        PublishEvent = new Dictionary<string, bool>(),
                        ListenEvent = new Dictionary<string, bool>()
                    };
                    AllSysIds[NewSysIdInput] = newItem;
                    SysIDToPublishMethod[NewSysIdInput] = new List<string>();
                    SysIDToListenMethod[NewSysIdInput] = new List<string>();

                    RefreshMethodCaches();

                    NewSysIdInput = "";
                    NewRemarkInput = "";
                }
            }
            GUILayout.EndHorizontal();

            // 现有系统ID列表
            GUILayout.Label("现有系统ID", EditorStyles.boldLabel);
            foreach (var kv in AllSysIds)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Toggle(SelectedSysId == kv.Key, "", GUILayout.Width(20)))
                {
                    SelectedSysId = kv.Key;
                }
                GUILayout.Label(kv.Key, GUILayout.Width(150));
                GUILayout.Label(kv.Value.Remark, GUILayout.Width(200));
                if (GUILayout.Button("删除", GUILayout.Width(60)))
                {
                    RemoveSysIdFromEvents(SelectedSysId);

                    AllSysIds.Remove(kv.Key);
                    SysIDToPublishMethod.Remove(kv.Key);
                    SysIDToListenMethod.Remove(kv.Key);
                    if (SelectedSysId == kv.Key) SelectedSysId = "";
                }
                GUILayout.EndHorizontal();
            }

            // 方法绑定区域
            if (!string.IsNullOrEmpty(SelectedSysId) && AllSysIds.ContainsKey(SelectedSysId))
            {
                DrawMethodBinding(SelectedSysId);
            }
        }

        private void DrawMethodBinding(string sysId)
        {
            GUILayout.Label($"方法绑定 - {sysId}", EditorStyles.boldLabel);

            // 获取所有可用方法
            var allPublishMethods = EventReflectTool.GetAllPublish()
                .Select(m => $"{m.DeclaringType.Name}.{m.Name}").ToList();
            var allListenMethods = EventReflectTool.GetAllListen()
                .Select(m => $"{m.DeclaringType.Name}.{m.Name}").ToList();

            // 发布方法
            GUILayout.Label("发布方法", EditorStyles.boldLabel);
            DrawMethodList(sysId, SysIDToPublishMethod, allPublishMethods, true);

            // 监听方法
            GUILayout.Label("监听方法", EditorStyles.boldLabel);
            DrawMethodList(sysId, SysIDToListenMethod, allListenMethods, false);
        }

        private void DrawMethodList(string sysId, Dictionary<string, List<string>> targetDict,
            List<string> allMethods, bool isPublish)
        {
            if (!targetDict.TryGetValue(sysId, out var boundMethods))
            {
                boundMethods = new List<string>();
                targetDict[sysId] = boundMethods;
            }

            for (int i = boundMethods.Count - 1; i >= 0; i--)
            {
                var method = boundMethods[i];
                GUILayout.BeginHorizontal();
                GUILayout.Label(method, GUILayout.Width(300));
                if (GUILayout.Button("移除", GUILayout.Width(60)))
                {
                    boundMethods.RemoveAt(i);
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            if (allMethods.Count > 0)
            {
                int selectedIndex = isPublish ? SelectedPubMethodIndex : SelectedPubMethodIndex;

                if (selectedIndex < 0 || selectedIndex >= allMethods.Count)
                    selectedIndex = 0;

                selectedIndex = EditorGUILayout.Popup(selectedIndex, allMethods.ToArray());

                if (isPublish)
                    SelectedPubMethodIndex = selectedIndex;
                else
                    SelectedLstMethodIndex = selectedIndex;

                if (GUILayout.Button("添加", GUILayout.Width(60)) && selectedIndex >= 0)
                {
                    string selectedMethod = allMethods[selectedIndex];
                    if (!boundMethods.Contains(selectedMethod))
                    {
                        boundMethods.Add(selectedMethod);
                    }
                }
            }
            GUILayout.EndHorizontal();
        }

        // ========== 日志页 ==========
        private void DrawLogPage()
        {
            GUILayout.Label("操作日志", EditorStyles.boldLabel);
            foreach (var log in EventGlobalCache.AllEventLog)
            {
                GUILayout.Label($"[{log.OperateTime}] {log.TargetEvent}: {log.Content}");
            }

            if (GUILayout.Button("清空日志"))
            {
                EventGlobalCache.AllEventLog.Clear();
            }
        }

        //获取发布方
        private void DrawPublishSelector()
        {
            if (EditingEvent == null) return;

            GUILayout.Label("发布方", EditorStyles.boldLabel);

            //系统ID下拉
            List<string> sysIds = AllSysIds.Keys.ToList();
            int selectedSysIndex = Math.Max(0, sysIds.IndexOf(SelectedPublishSysId));
            selectedSysIndex = EditorGUILayout.Popup("系统ID", selectedSysIndex, sysIds.ToArray());

            if (selectedSysIndex >= 0 && selectedSysIndex < sysIds.Count)
            {
                string selectedSysId = sysIds[selectedSysIndex];
                SelectedPublishSysId = selectedSysId;

                //获取该系统下的发布方法
                var methods = SysIDToPublishMethod.GetValueOrDefault(selectedSysId, new List<string>());
                int currentMethodIndex = Math.Max(0, methods.IndexOf(EditingEvent?.EventPublish ?? ""));

                int newMethodIndex = EditorGUILayout.Popup("发布方法", currentMethodIndex, methods.ToArray());

                if (newMethodIndex >= 0 && newMethodIndex < methods.Count)
                {
                    string newPublishMethod = methods[newMethodIndex];

                    if (EditingEvent.EventPublish != newPublishMethod)
                    {
                        foreach (var listenMethod in EditingEvent.EventListen)
                        {
                            string oldMappingKey = $"{EditingEvent.EventPublish}_{listenMethod}";
                            EditingEvent.KeyConnection.Remove(oldMappingKey);

                            string newMappingKey = $"{newPublishMethod}_{listenMethod}";
                            if (!EditingEvent.KeyConnection.ContainsKey(newMappingKey))
                            {
                                EditingEvent.KeyConnection[newPublishMethod] = new Dictionary<string, string>();
                            }
                        }

                        EditingEvent.EventPublish = newPublishMethod;
                    }

                }
            }
        }

        //获取监听方
        private void DarwListenList()
        {
            GUILayout.Label("监听方", EditorStyles.boldLabel);

            //显示已添加的监听
            for (int i = 0; i < EditingEvent.EventListen.Count; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(EditingEvent.EventListen[i]);
                if (GUILayout.Button("移除", GUILayout.Width(60)))
                {
                    string listenFullName = EditingEvent.EventListen[i];
                    EditingEvent.EventListen.RemoveAt(i);
                    EditingEvent.ListenClassName.Remove(listenFullName);

                    string mappingKey = $"{EditingEvent.EventPublish}_{listenFullName}";
                    EditingEvent.KeyConnection.Remove(mappingKey);

                    if (i < EditingListenSysIds.Count)
                    {
                        EditingEvent.ListenSysId.RemoveAt(i);
                    }
                    EditingEvent.ListenMethodToSystemID.Remove(listenFullName);

                    i--;
                }
                GUILayout.EndHorizontal();
            }

            //添加新监听
            DrawAddListenSelector();
        }

        private void DrawAddListenSelector()
        {
            if (EditingEvent == null || string.IsNullOrEmpty(EditingEvent.EventPublish))
            {
                EditorGUILayout.HelpBox("请先选择发布方", MessageType.Info);
                return;
            }


            GUILayout.BeginHorizontal();

            List<string> sysIds = AllSysIds.Keys.ToList();
            int selectedSysIndex = Math.Max(0, sysIds.IndexOf(SelectedListenSysId));
            selectedSysIndex = EditorGUILayout.Popup("系统ID", selectedSysIndex, sysIds.ToArray());

            if (selectedSysIndex >= 0 && selectedSysIndex < sysIds.Count)
            {
                SelectedListenSysId = sysIds[selectedSysIndex];

                var methods = SysIDToListenMethod.GetValueOrDefault(SelectedListenSysId, new List<string>());
                int selectedMethodIndex = EditorGUILayout.Popup("监听方法", SelectedLstMethodIndex, methods.ToArray());
                SelectedLstMethodIndex = selectedMethodIndex;

                if (GUILayout.Button("添加", GUILayout.Width(60)))
                {
                    if (selectedMethodIndex >= 0 && selectedMethodIndex < methods.Count)
                    {
                        string listenFullName = methods[selectedMethodIndex];
                        if (EditingEvent.EventListen.Contains(methods[selectedMethodIndex])) return;

                        string[] parts = listenFullName.Split('.');
                        if (parts.Length < 2)
                        {
                            Debug.LogError($"方法格式名错误，方法名：{listenFullName}");
                            return;
                        }

                        string listenClassName = parts[0];

                        EditingEvent.EventListen.Add(listenFullName);
                        EditingEvent.ListenClassName[listenFullName] = listenClassName;

                        EditingEvent.ListenSysId.Add(SelectedListenSysId);
                        EditingEvent.ListenMethodToSystemID[listenFullName] = SelectedListenSysId;

                        //初始化映射
                        string mappingKey = $"{EditingEvent.EventPublish}_{listenFullName}";
                        if (!EditingEvent.KeyConnection.ContainsKey(mappingKey))
                        {
                            EditingEvent.KeyConnection[mappingKey] = new Dictionary<string, string>();
                        }

                    }
                }
            }

            GUILayout.EndHorizontal();
        }

        //字段映射绘制
        private void DrawFieldMappings()
        {
            GUILayout.Label("字段映射", EditorStyles.boldLabel);

            foreach (var listenMethod in EditingEvent.EventListen)
            {
                string mappingKey = $"{EditingEvent.EventPublish}_{listenMethod}";
                if (!EditingEvent.KeyConnection.ContainsKey(mappingKey))
                {
                    EditingEvent.KeyConnection[mappingKey] = new Dictionary<string, string>();
                }

                var mapping = EditingEvent.KeyConnection[mappingKey];
                var listenKeys = MethodToKeyList.GetValueOrDefault(listenMethod, new List<string>());
                var publishKeys = MethodToKeyList.GetValueOrDefault(EditingEvent.EventPublish, new List<string>());

                GUILayout.Label($"监听：{listenMethod}", EditorStyles.miniBoldLabel);
                EditorGUI.indentLevel++;

                foreach (var listenKey in listenKeys)
                {
                    GUILayout.BeginHorizontal();
                    Type listenType = GetParamType(listenMethod, listenKey);
                    string listenTypeName = listenType?.Name ?? "object";

                    GUILayout.Label($"{listenKey}({listenTypeName}) ->", GUILayout.Width(100));

                    // 选项列表：["(不使用)", "发布参数A", "发布参数B"...]
                    var options = new List<string> { "(不使用)" };

                    foreach (var pk in publishKeys)
                    {
                        Type publishType = GetParamType(EditingEvent.EventPublish, pk);
                        string publishTypeName = publishType?.Name ?? "object";
                        options.Add(pk);
                    }

                    // 获取当前选中的值
                    string currentMapping = mapping.GetValueOrDefault(listenKey, "");
                    int selectedIndex = options.IndexOf(currentMapping);
                    if (selectedIndex < 0) selectedIndex = 0;

                    // 绘制下拉菜单
                    int newSelectedIndex = EditorGUILayout.Popup(selectedIndex, options.ToArray());

                    // 处理选择结果
                    if (newSelectedIndex == 0)
                    {
                        // 选择了"(不使用)"，移除映射
                        if (mapping.ContainsKey(listenKey))
                        {
                            mapping.Remove(listenKey);
                        }
                        EditorGUILayout.LabelField("跳过", GUILayout.Width(40));
                    }
                    else
                    {
                        string selectedOption = options[newSelectedIndex];
                        int parenIndex = selectedOption.IndexOf('(');
                        string newPublishKey = parenIndex > 0 ? selectedOption.Substring(0, parenIndex) : selectedOption;

                        mapping[listenKey] = newPublishKey;

                        // 类型校验
                        if (!CheckTypeMetch(EditingEvent.EventPublish, listenMethod, listenKey, newPublishKey))
                        {
                            EditorGUILayout.LabelField("类型错误", GUILayout.Width(40));
                        }
                        else
                        {
                            EditorGUILayout.LabelField("类型正确", GUILayout.Width(40));
                        }
                    }
                    GUILayout.EndHorizontal();
                }

                EditorGUI.indentLevel--;
            }
        }

        public bool CheckTypeMetch(string publishMethod, string listenMethod, string lsitenKeyName, string publishKeyName)
        {
            if (!EventGlobalCache.GlobalMethodParams.TryGetValue(publishMethod, out var publishParams))
            {
                Debug.LogError($"类型判断--未找到发布方法：{publishMethod}");
                return false;
            }
            if (!EventGlobalCache.GlobalMethodParams.TryGetValue(listenMethod, out var listenParams))
            {
                Debug.LogError($"类型判断--未找到监听方法：{listenMethod}");
                return false;
            }

            if (!listenParams.ParamNameToType.TryGetValue(lsitenKeyName, out var listenType))
            {
                Debug.LogError($"类型判断--监听方法中未找到参数：{lsitenKeyName}");
                return false;
            }
            if (!publishParams.ParamNameToType.TryGetValue(publishKeyName, out var publishType))
            {
                Debug.LogError($"类型判断--发布方法中未找到参数：{publishKeyName}");
                return false;
            }

            if (listenType != publishType)
            {
                if (!listenType.IsAssignableFrom(publishType) && !publishType.IsAssignableFrom(listenType))
                {
                    Debug.LogError($"类型不匹配:{lsitenKeyName}{listenType} vs {publishKeyName}{publishType}");
                    return false;
                }
            }
            return true;
        }

        public EventItem AllToConfig()
        {
            // 不传参数，直接从 EditTarget 和缓存中提取
            if (EditingEvent == null) return null;

            var item = new EventItem();

            // 1. 基础信息
            item.EventName = EditingEvent.EventName;
            item.EventClassFullName = GetEventClassName(EditingEvent.EventName);
            item.QueueType = EditingEvent.QueueType;
            item.GlobalEnable = EditingEvent.GlobalEnable;

            // 2. 发布方信息
            item.EventPublish = EditingEvent.EventPublish;
            string[] publishParts = EditingEvent.EventPublish.Split('.');
            string publishClassName = publishParts[0];
            string publishMethodName = publishParts[1];

            item.PublishClassList = new List<string> { publishClassName };
            item.PublishClassName = new Dictionary<string, string>
    {
        { EditingEvent.EventPublish, publishClassName }
    };

            // 发布方系统ID
            item.PublishSysId = new List<string> { SelectedPublishSysId };
            item.PublishMethodToSystemID = new Dictionary<string, string>
    {
        { EditingEvent.EventPublish, SelectedPublishSysId }
    };

            // 3. 监听方信息
            item.EventListen = new List<string>(EditingEvent.EventListen);
            item.ListenMethodDict = new Dictionary<string, string>();
            item.ListenClassList = new List<string>();
            item.ListenClassName = new Dictionary<string, string>(EditingEvent.ListenClassName);
            item.ListenSysId = new List<string>(EditingEvent.ListenSysId);
            item.ListenMethodToSystemID = new Dictionary<string, string>(EditingEvent.ListenMethodToSystemID);

            foreach (var listenFullName in EditingEvent.EventListen)
            {
                string[] listenParts = listenFullName.Split(".");
                string listenClassName = listenParts[0];
                string listenMethodName = listenParts[1];

                item.ListenMethodDict[listenFullName] = listenMethodName;
                item.ListenClassList.Add(listenClassName);
            }

            // 4. 包KEY信息
            item.PublishKey = new Dictionary<string, List<string>>();
            if (MethodToKeyList.TryGetValue(EditingEvent.EventPublish, out var publishKeys))
            {
                item.PublishKey[EditingEvent.EventPublish] = new List<string>(publishKeys);
            }

            item.ListenKey = new Dictionary<string, List<string>>();
            foreach (var listenFullName in EditingEvent.EventListen)
            {
                if (MethodToKeyList.TryGetValue(listenFullName, out var listenKeys))
                {
                    item.ListenKey[listenFullName] = new List<string>(listenKeys);
                }
            }

            // 5. 字段映射
            item.KeyConnection = new Dictionary<string, Dictionary<string, string>>(EditingEvent.KeyConnection);

            // 6. 代码路径
            item.CodePath = new Dictionary<string, string>
    {
        { "EventClass", $"{EVENT_PATH}{item.EventClassFullName}.cs" },
        { "Binding", $"{BINDING_PATH}Binding_{item.EventClassFullName}.cs" }
    };

            item.IsOldEvent = false;

            return item;
        }

        //定义规范事件名称，与工具层的规范代码格式统一
        private string GetEventClassName(string eventName)
        {
            return $"E_{eventName}";
        }

        //代码生成路径定义(这里要求与实际代码生成路径相同)

        private const string GENERATED_PATH = "Assets/Scripts/Generated/";

        private const string EVENT_PATH = GENERATED_PATH + "Events/";

        private const string BINDING_PATH = GENERATED_PATH + "Bindings/";

        //验证事件是否完整
        public bool IsValid()
        {
            if (EditingEvent == null) return false;
            if (string.IsNullOrEmpty(EditingEvent.EventName)) return false;
            if (string.IsNullOrEmpty(EditingEvent.EventPublish)) return false;
            if (EditingEvent.EventListen == null || EditingEvent.EventListen.Count == 0) return false;

            //验证是否有同名事件
            if (string.IsNullOrEmpty(EditingEventName) || EditingEventName != EditingEvent.EventName)
            {
                if (AllEvents.ContainsKey(EditingEvent.EventName))
                {
                    Debug.LogWarning($"已有同名事件，事件名：{EditingEvent.EventName}，不可重复添加事件");
                    return false;
                }
            }

            //检查字段映射是否完整
            foreach (var listenMethod in EditingEvent.EventListen)
            {
                string mappingKey = $"{EditingEvent.EventPublish}_{listenMethod}";

                // 允许没有映射字典（所有参数都选择"不使用"）
                if (!EditingEvent.KeyConnection.ContainsKey(mappingKey))
                {
                    continue;  // 跳过，允许空映射
                }

                var mapping = EditingEvent.KeyConnection[mappingKey];
                var listenKeys = MethodToKeyList.GetValueOrDefault(listenMethod, new List<string>());

                // 只检查有映射的参数，不要求全部
                foreach (var listenKey in listenKeys)
                {
                    if (mapping.ContainsKey(listenKey))
                    {
                        // 有映射的才需要校验
                        string publishKey = mapping[listenKey];
                        if (!CheckTypeMetch(EditingEvent.EventPublish, listenMethod, listenKey, publishKey))
                        {
                            return false;
                        }
                    }
                    // 没有映射的参数可以跳过
                }
            }
            return true;
        }


        private void UpdateSysIdEventStatus(EventItem eventItem)
        {
            string eventName = EditingEvent.EventClassFullName;

            //发布方
            string publishSysId = eventItem.PublishMethodToSystemID?.GetValueOrDefault(eventItem.EventPublish, "");
            if (!string.IsNullOrEmpty(publishSysId) && AllSysIds.ContainsKey(publishSysId))
            {
                if (AllSysIds[publishSysId].PublishEvent == null)
                    AllSysIds[publishSysId].PublishEvent = new();
                AllSysIds[publishSysId].PublishEvent[eventName] = true;
            }

            //监听方
            foreach (var listenMethod in eventItem.EventListen)
            {
                string listenSysId = eventItem.ListenMethodToSystemID?.GetValueOrDefault(listenMethod, "");
                if (!string.IsNullOrEmpty(listenSysId) && AllSysIds.ContainsKey(listenSysId))
                {
                    if (AllSysIds[listenSysId].ListenEvent == null)
                        AllSysIds[listenSysId].ListenEvent = new();
                    AllSysIds[listenSysId].ListenEvent[eventName] = true;
                }
            }
        }

        private void DeleteEvent(string eventName)
        {
            if (!AllEvents.TryGetValue(eventName, out var eventItem)) return;

            //从系统ID中移除事件状态
            string publishSysId = eventItem.PublishMethodToSystemID?.GetValueOrDefault(eventItem.EventPublish, "");
            if (!string.IsNullOrEmpty(publishSysId) && AllSysIds.ContainsKey(publishSysId))
            {
                AllSysIds[publishSysId].PublishEvent?.Remove(eventName);
            }

            foreach (var listenMethod in eventItem.EventListen)
            {
                string listenSysId = eventItem.ListenMethodToSystemID?.GetValueOrDefault(listenMethod, "");
                if (!string.IsNullOrEmpty(listenSysId) && AllSysIds.ContainsKey(listenSysId))
                {
                    AllSysIds[listenSysId].ListenEvent?.Remove(eventName);
                }
            }

            AllEvents.Remove(eventName);
        }

        // 系统ID刷新方法
        private void RefreshMethodCaches()
        {
            // 重新获取所有方法
            var allPublishMethods = EventReflectTool.GetAllPublish()
                .Select(m => $"{m.DeclaringType.Name}.{m.Name}").ToList();
            var allListenMethods = EventReflectTool.GetAllListen()
                .Select(m => $"{m.DeclaringType.Name}.{m.Name}").ToList();

            // 重新构建 MethodToKeyList
            MethodToKeyList.Clear();
            GlobalMethodParams.Clear();

            foreach (var method in EventReflectTool.GetAllPublish())
            {
                string fullName = $"{method.DeclaringType.Name}.{method.Name}";
                MethodToKeyList[fullName] = method.GetParameters().Select(p => p.Name).ToList();

                var paramInfo = new MethodParamInfo { MethodFullName = fullName };
                foreach (var param in method.GetParameters())
                {
                    paramInfo.Parameters.Add(new ParamInfo
                    {
                        Name = param.Name,
                        Type = param.ParameterType,
                        TypeName = param.ParameterType.ToString()
                    });
                    paramInfo.ParamNameToType[param.Name] = param.ParameterType;
                }
                GlobalMethodParams[fullName] = paramInfo;
            }

            foreach (var method in EventReflectTool.GetAllListen())
            {
                string fullName = $"{method.DeclaringType.Name}.{method.Name}";
                MethodToKeyList[fullName] = method.GetParameters().Select(p => p.Name).ToList();

                var paramInfo = new MethodParamInfo { MethodFullName = fullName };
                foreach (var param in method.GetParameters())
                {
                    paramInfo.Parameters.Add(new ParamInfo
                    {
                        Name = param.Name,
                        Type = param.ParameterType,
                        TypeName = param.ParameterType.ToString()
                    });
                    paramInfo.ParamNameToType[param.Name] = param.ParameterType;
                }
                GlobalMethodParams[fullName] = paramInfo;
            }
        }

        //删除系统ID的关联数据
        private void RemoveSysIdFromEvents(string sysId)
        {
            List<string> eventsToRemove = new List<string>();

            foreach (var kv in AllEvents)
            {
                string eventName = kv.Key;
                EventItem eventItem = kv.Value;
                bool needSave = false;

                // 检查发布方
                if (eventItem.PublishMethodToSystemID?.ContainsValue(sysId) == true)
                {
                    // 清除发布方信息
                    eventItem.EventPublish = "";
                    eventItem.PublishMethodToSystemID.Clear();
                    eventItem.PublishSysId?.Clear();
                    needSave = true;
                }

                // 检查监听方
                List<string> toRemove = new List<string>();
                foreach (var listenMethod in eventItem.EventListen)
                {
                    string listenSysId = eventItem.ListenMethodToSystemID?.GetValueOrDefault(listenMethod, "");
                    if (listenSysId == sysId)
                    {
                        toRemove.Add(listenMethod);
                    }
                }

                foreach (var listenMethod in toRemove)
                {
                    int index = eventItem.EventListen.IndexOf(listenMethod);
                    eventItem.EventListen.Remove(listenMethod);
                    eventItem.ListenClassName?.Remove(listenMethod);
                    if (index < eventItem.ListenSysId?.Count)
                        eventItem.ListenSysId?.RemoveAt(index);
                    eventItem.ListenMethodToSystemID?.Remove(listenMethod);

                    string mappingKey = $"{eventItem.EventPublish}_{listenMethod}";
                    eventItem.KeyConnection?.Remove(mappingKey);
                    needSave = true;
                }

                // 如果事件变成空，标记待删除
                if (string.IsNullOrEmpty(eventItem.EventPublish) &&
                    (eventItem.EventListen == null || eventItem.EventListen.Count == 0))
                {
                    eventsToRemove.Add(eventName);
                }
            }

            // 删除空事件
            foreach (var eventName in eventsToRemove)
            {
                AllEvents.Remove(eventName);
            }
        }

        private Type GetParamType(string methodFullName, string paramName)
        {
            if (GlobalMethodParams.TryGetValue(methodFullName, out var paramInfo))
            {
                return paramInfo.ParamNameToType.GetValueOrDefault(paramName);
            }
            return null;
        }
    }
}
