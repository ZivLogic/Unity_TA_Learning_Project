using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using static EventEditorUtil;

public class EventMainEditorWindow : EditorWindow
{
    // ========== UI 状态（只与显示相关） ==========
    private Vector2 _scrollPos;
    private int _tabIndex;
    private string _statusMessage = "";
    private bool _hasError = false;

    // ========== 缓存管理器 ==========
    private EventEditorCache _cache;

    // ========== 新建/编辑事件面板 UI 数据 ==========
    private string _curEventName = "";
    private EventQueueType _curQueue = EventQueueType.Logic;
    private string _publishSysId = "";
    private string _listenSysId = "";
    private int _selectPubIdx = 0;
    private int _selectLstIdx = 0;
    private EventDefine _editingEvt = null;

    // ========== 字段映射面板 UI 数据 ==========
    private int _selectEvtIndex = 0;
    private int _selectKeyIndex = 0;
    private int _selectParamIndex = 0;
    private EventDefine _selectedEvt;
    private List<string> _pkgKeyList = new List<string>();
    private List<string> _canInjectParamList = new List<string>();

    // ========== 系统ID管理面板 UI 数据 ==========
    private string _newSysId = "";
    private string _newSysRemark = "";
    private int _selectSysIdx = 0;

    // ========== 菜单入口 ==========
    [MenuItem("EventTool/事件可视化编辑器")]
    public static void Open()
    {
        var win = GetWindow<EventMainEditorWindow>("事件配置编辑器");
        win.minSize = new Vector2(1020, 650);
        win.Show();
    }

    // ========== 窗口生命周期 ==========
    private void OnEnable()
    {
        // 创建缓存
        _cache = new EventEditorCache();

        // 加载配置
        if (!_cache.ReloadConfig())
        {
            _hasError = true;
            _statusMessage = "配置加载失败，请检查 JSON 文件格式";
        }
        else
        {
            _hasError = false;
            _statusMessage = "就绪";

            // 初始化 UI 数据
            InitUIData();
        }

        // 订阅更新事件
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        // 取消订阅
        EditorApplication.update -= OnEditorUpdate;

        // 如果有未保存修改，执行回退
        if (_cache != null && _cache.IsDirtyModify)
        {
            try
            {
                RollbackToSnapshot();
                Debug.LogWarning("[事件编辑器]窗口关闭：存在未保存修改，已回退");
            }
            catch (Exception e)
            {
                Debug.LogError($"回退失败：{e.Message}");
            }
        }

        // 释放缓存
        _cache?.Dispose();
        _cache = null;
    }

    /// <summary>
    /// 初始化 UI 数据（从缓存中读取默认值）
    /// </summary>
    private void InitUIData()
    {
        _editingEvt = null;
        _curEventName = "";
        _curQueue = EventQueueType.Logic;

        if (_cache.SysIdList.Count > 0)
        {
            _publishSysId = _cache.SysIdList[0].sysId;
            _listenSysId = _cache.SysIdList[0].sysId;
        }
        else
        {
            _publishSysId = "";
            _listenSysId = "";
        }

        _selectPubIdx = 0;
        _selectLstIdx = 0;
        _selectEvtIndex = 0;
        _selectKeyIndex = 0;
        _selectParamIndex = 0;
        _selectSysIdx = 0;
        _selectedEvt = null;
        _pkgKeyList.Clear();
        _canInjectParamList.Clear();
        _newSysId = "";
        _newSysRemark = "";
    }

    // ========== 编译完成检测 ==========
    private void OnEditorUpdate()
    {
        if (_cache == null) return;

        if (_cache.IsWaitingForCompile && !EditorApplication.isCompiling)
        {
            _cache.IsWaitingForCompile = false;
            _cache.IsGeneratingCode = false;
            _cache.AfterCompileAction?.Invoke();
            _cache.AfterCompileAction = null;
            Repaint();
        }
    }

    // ========== 主绘制 ==========
    private void OnGUI()
    {
        // 安全检查
        if (_cache == null)
        {
            EditorGUILayout.HelpBox("缓存未初始化，请重新打开窗口", MessageType.Error);
            return;
        }

        // 代码生成期间显示等待
        if (_cache.IsGeneratingCode)
        {
            EditorGUILayout.LabelField("正在生成代码中，请稍后...", EditorStyles.boldLabel);
            return;
        }

        // 错误状态显示
        if (_hasError)
        {
            EditorGUILayout.HelpBox(_statusMessage, MessageType.Error);
            if (GUILayout.Button("重新加载配置"))
            {
                if (_cache.ReloadConfig())
                {
                    _hasError = false;
                    _statusMessage = "就绪";
                    InitUIData();
                }
                else
                {
                    _statusMessage = "重新加载失败，请检查 JSON 文件";
                }
            }
            return;
        }

        // 数据就绪检查
        if (_cache.EvtData == null || _cache.AllPubMethods == null)
        {
            EditorGUILayout.HelpBox("数据未就绪，请稍后...", MessageType.Warning);
            return;
        }

        // 正常绘制
        using (var scrollScope = new EditorGUILayout.ScrollViewScope(_scrollPos))
        {
            _scrollPos = scrollScope.scrollPosition;

            string[] tabs = { "事件总览", "新建/编辑事件", "字段映射绑定", "操作日志", "系统ID管理" };
            _tabIndex = GUILayout.Toolbar(_tabIndex, tabs);
            EditorGUILayout.Space();

            // 每个面板独立 try-catch
            try
            {
                switch (_tabIndex)
                {
                    case 0: DrawOverviewPanel(); break;
                    case 1: DrawCreateEventPanel(); break;
                    case 2: DrawFieldMapPanel(); break;
                    case 3: DrawLogPanel(); break;
                    case 4: DrawSysIdManagerPanel(); break;
                }
            }
            catch (Exception ex)
            {
                EditorGUILayout.HelpBox($"绘制错误：{ex.Message}", MessageType.Error);
                Debug.LogError($"UI绘制异常：{ex}");
            }
        }
    }

    // ========== 面板1：事件总览 ==========
    private void DrawOverviewPanel()
    {
        try
        {
            EditorGUILayout.LabelField("全局事件一览表", EditorStyles.boldLabel);

            if (_cache.EvtData.Count == 0)
            {
                EditorGUILayout.LabelField("暂无事件配置");
            }

            for (int i = 0; i < _cache.EvtData.Count; i++)
            {
                var evt = _cache.EvtData[i];
                if (evt == null) continue;

                EditorGUILayout.BeginHorizontal("Box");
                EditorGUILayout.LabelField(evt.eventName ?? "未知", GUILayout.Width(130));
                EditorGUILayout.LabelField(evt.queueType.ToString(), GUILayout.Width(100));
                EditorGUILayout.LabelField(evt.isGlobalEnable ? "【启用】" : "【禁用】", GUILayout.Width(80));
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("编辑", GUILayout.Width(70)))
                {
                    _editingEvt = evt;
                    _curEventName = evt.eventName;
                    _curQueue = evt.queueType;
                    _publishSysId = evt.PublishSysId;
                    _listenSysId = evt.ListenSysId;
                    _tabIndex = 1;
                }

                if (GUILayout.Button("删除", GUILayout.Width(70)))
                {
                    bool delOk = EditorUtility.DisplayDialog("确认删除", $"确定要删除事件【{evt.eventName}】？", "删除", "取消");
                    if (delOk)
                    {
                        DeleteEventFileAndConfig(evt);
                        AddLog(evt.eventName, "删除事件", evt.eventName, "已移除");
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("新建空白事件", GUILayout.Height(30)))
            {
                _editingEvt = null;
                _curEventName = "";
                _curQueue = EventQueueType.Logic;
                _publishSysId = _cache.SysIdList.Count > 0 ? _cache.SysIdList[0].sysId : "";
                _listenSysId = _cache.SysIdList.Count > 0 ? _cache.SysIdList[0].sysId : "";
                _tabIndex = 1;
            }

            if (GUILayout.Button("保存配置到JSON", GUILayout.Height(30)))
            {
                SaveAllCfg();
                AddLog("全局", "手动全量保存配置", "", "已落地JSON");
                EditorUtility.DisplayDialog("完成", "EventConfig.json已更新", "OK");
            }

            if (GUILayout.Button("放弃全部修改，一键回退", GUILayout.Height(30)))
            {
                bool sure = EditorUtility.DisplayDialog("确认回退", "所有未保存改动全部丢弃", "确认回退", "取消");
                if (sure)
                {
                    RollbackToSnapshot();
                    EditorUtility.DisplayDialog("回退成功", "配置+临时生成代码全部恢复初始状态", "OK");
                }
            }

            EditorGUILayout.EndHorizontal();
        }
        catch (Exception ex)
        {
            EditorGUILayout.HelpBox($"绘制事件总览失败：{ex.Message}", MessageType.Error);
        }
    }

    // ========== 面板2：新建/编辑事件 ==========
    private void DrawCreateEventPanel()
    {
        try
        {
            if (_editingEvt != null)
                EditorGUILayout.LabelField("【编辑已有事件】", EditorStyles.boldLabel);
            else
                EditorGUILayout.LabelField("【新建事件】", EditorStyles.boldLabel);

            _curEventName = EditorGUILayout.TextField("事件名称", _curEventName);
            _curQueue = (EventQueueType)EditorGUILayout.EnumPopup("目标队列", _curQueue);

            // 启用开关
            bool editEnable = _editingEvt != null ? _editingEvt.isGlobalEnable : true;
            bool newEnable = EditorGUILayout.Toggle("全局启用事件", editEnable);
            if (_editingEvt != null && newEnable != editEnable)
            {
                _editingEvt.isGlobalEnable = newEnable;
                _cache.IsDirtyModify = true;
            }
            EditorGUILayout.Space();

            // 系统ID选择
            DrawSystemIdSelection();

            // 发布方法选择
            MethodInfo selectPub = DrawPublishMethodSelection();

            EditorGUILayout.Space();

            // 监听方法选择
            MethodInfo selectLst = DrawListenMethodSelection();

            EditorGUILayout.Space();

            // 保存按钮
            if (GUILayout.Button("保存配置+刷新代码", GUILayout.Height(32)))
            {
                HandleSaveAndGenerate(newEnable, selectPub, selectLst);
            }
        }
        catch (Exception ex)
        {
            EditorGUILayout.HelpBox($"绘制新建事件面板失败：{ex.Message}", MessageType.Error);
        }
    }

    private void DrawSystemIdSelection()
    {
        EditorGUILayout.LabelField("【发布端配置】", EditorStyles.boldLabel);
        string[] sysArr = _cache.SysIdList.Select(s => s.sysId).ToArray();

        if (sysArr.Length > 0)
        {
            int pubSysIdx = Array.FindIndex(sysArr, s => s == _publishSysId);
            if (pubSysIdx < 0) pubSysIdx = 0;
            pubSysIdx = EditorGUILayout.Popup("发布所属SystemID", pubSysIdx, sysArr);
            _publishSysId = sysArr[pubSysIdx];
        }
        else
        {
            EditorGUILayout.LabelField("无可用SystemID，请去系统ID管理新增");
        }

        EditorGUILayout.LabelField("【监听端配置】", EditorStyles.boldLabel);
        if (sysArr.Length > 0)
        {
            int lstSysIdx = Array.FindIndex(sysArr, s => s == _listenSysId);
            if (lstSysIdx < 0) lstSysIdx = 0;
            lstSysIdx = EditorGUILayout.Popup("监听所属SystemID", lstSysIdx, sysArr);
            _listenSysId = sysArr[lstSysIdx];
        }
    }

    private MethodInfo DrawPublishMethodSelection()
    {
        string[] pubOpts = _cache.AllPubMethods.Select(m => $"{m.DeclaringType.Name}.{m.Name}").ToArray();

        if (_cache.AllPubMethods.Count == 0)
        {
            EditorGUILayout.HelpBox("未找到标记 [EventPublishMethod] 的方法", MessageType.Warning);
            return null;
        }

        if (_selectPubIdx < 0 || _selectPubIdx >= pubOpts.Length) _selectPubIdx = 0;
        int newPubIdx = EditorGUILayout.Popup("选择发布业务方法", _selectPubIdx, pubOpts);
        _selectPubIdx = Mathf.Clamp(newPubIdx, 0, pubOpts.Length - 1);

        MethodInfo selectPub = _cache.AllPubMethods[_selectPubIdx];
        var pubSplit = EventEditorUtil.SplitPublishParam(selectPub);
        if (pubSplit != null)
        {
            EditorGUILayout.LabelField($"入参:{pubSplit.InArgs.Count} | Out打包:{pubSplit.OutArgs.Count}");
        }

        return selectPub;
    }

    private MethodInfo DrawListenMethodSelection()
    {
        string[] lstOpts = _cache.AllLstMethods.Select(m => $"{m.DeclaringType.Name}.{m.Name}").ToArray();

        if (_cache.AllLstMethods.Count == 0)
        {
            EditorGUILayout.HelpBox("未找到标记 [EventListenMethod] 的方法", MessageType.Warning);
            return null;
        }

        if (_selectLstIdx < 0 || _selectLstIdx >= lstOpts.Length) _selectLstIdx = 0;
        int newLstIdx = EditorGUILayout.Popup("选择监听业务方法", _selectLstIdx, lstOpts);
        _selectLstIdx = Mathf.Clamp(newLstIdx, 0, lstOpts.Length - 1);

        MethodInfo selectLst = _cache.AllLstMethods[_selectLstIdx];
        var lstSplit = EventEditorUtil.SplitListenParam(selectLst);
        if (lstSplit != null)
        {
            EditorGUILayout.LabelField($"可注入参数:{lstSplit.CanInjectArgs.Count} | Out输出:{lstSplit.OutReturnArgs.Count}");
        }

        return selectLst;
    }

    private void HandleSaveAndGenerate(bool newEnable, MethodInfo selectPub, MethodInfo selectLst)
    {
        if (string.IsNullOrWhiteSpace(_curEventName))
        {
            EditorUtility.DisplayDialog("错误", "事件名不能为空", "OK");
            return;
        }

        if (selectPub == null || selectLst == null)
        {
            EditorUtility.DisplayDialog("错误", "请选择发布方法和监听方法", "OK");
            return;
        }

        var pubSplit = EventEditorUtil.SplitPublishParam(selectPub);
        var lstSplit = EventEditorUtil.SplitListenParam(selectLst);

        if (pubSplit == null || lstSplit == null)
        {
            EditorUtility.DisplayDialog("错误", "方法参数解析失败", "OK");
            return;
        }

        // 捕获数据
        string captureEventName = _curEventName;
        EventQueueType captureQueue = _curQueue;
        string capturePublishSysId = _publishSysId;
        string captureListenSysId = _listenSysId;
        bool captureNewEnable = newEnable;
        MethodInfo captureSelectPub = selectPub;
        MethodInfo captureSelectLst = selectLst;
        var capturePubSplit = pubSplit;
        var captureLstSplit = lstSplit;
        EventDefine captureEditingEvt = _editingEvt;

        _cache.IsGeneratingCode = true;

        // 延迟两帧执行
        EditorApplication.delayCall += () =>
        {
            EditorApplication.delayCall += () =>
            {
                DoSaveAndGenerate(
                    captureEventName, captureQueue, capturePublishSysId, captureListenSysId,
                    captureNewEnable, captureSelectPub, captureSelectLst,
                    capturePubSplit, captureLstSplit, captureEditingEvt
                );
            };
        };
    }

    // ========== 面板3：字段映射绑定 ==========
    private void DrawFieldMapPanel()
    {
        try
        {
            EditorGUILayout.LabelField("数据包Key ↔ 监听入参 绑定配置", EditorStyles.boldLabel);

            // 选择事件
            string[] evtNames = _cache.EvtData.Select(e => e.eventName).ToArray();
            if (evtNames.Length > 0)
            {
                if (_selectEvtIndex >= evtNames.Length) _selectEvtIndex = 0;
                _selectEvtIndex = EditorGUILayout.Popup("选择目标事件", _selectEvtIndex, evtNames);
                _selectedEvt = _cache.EvtData[_selectEvtIndex];
                _pkgKeyList = _selectedEvt?.packageKeys ?? new List<string>();
            }
            else
            {
                EditorGUILayout.LabelField("暂无事件，请先创建事件");
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("当前事件已有绑定关系：", EditorStyles.boldLabel);

            // 获取映射配置
            EventFieldMapping targetMap = GetOrCreateFieldMapping(_selectedEvt);

            // 显示已有绑定
            DisplayExistingBindings(targetMap, _selectedEvt);

            EditorGUILayout.Space();

            // 添加新绑定
            DrawAddBindingUI(targetMap, _selectedEvt);

            EditorGUILayout.Space();

            // 保存按钮
            if (GUILayout.Button("保存全部映射+刷新监听代码", GUILayout.Height(32)))
            {
                HandleSaveFieldMapping(_selectedEvt);
            }
        }
        catch (Exception ex)
        {
            EditorGUILayout.HelpBox($"绘制字段映射面板失败：{ex.Message}", MessageType.Error);
        }
    }

    private EventFieldMapping GetOrCreateFieldMapping(EventDefine evt)
    {
        if (evt == null) return null;

        var targetMap = _cache.MapData.Find(x => x.eventTypeFullName == evt.eventTypeFullName);
        if (targetMap == null)
        {
            targetMap = new EventFieldMapping
            {
                eventTypeFullName = evt.eventTypeFullName,
                fieldMaps = new FieldMapItem[0]
            };
            _cache.MapData.Add(targetMap);
            _cache.IsDirtyModify = true;
        }
        return targetMap;
    }

    private void DisplayExistingBindings(EventFieldMapping targetMap, EventDefine evt)
    {
        if (targetMap == null || targetMap.fieldMaps == null || targetMap.fieldMaps.Length == 0)
        {
            EditorGUILayout.LabelField("暂无任何字段绑定");
            return;
        }

        for (int i = 0; i < targetMap.fieldMaps.Length; i++)
        {
            var item = targetMap.fieldMaps[i];
            EditorGUILayout.BeginHorizontal("Box");
            EditorGUILayout.LabelField($"Key:{item.packageKey} → 参数:{item.paramName}", GUILayout.Width(320));

            if (GUILayout.Button("删除本条绑定", GUILayout.Width(100)))
            {
                if (EditorApplication.isCompiling)
                {
                    EditorUtility.DisplayDialog("提示", "正在编译中,请稍后再试", "OK");
                    return;
                }

                var temp = new List<FieldMapItem>(targetMap.fieldMaps);
                temp.RemoveAt(i);
                targetMap.fieldMaps = temp.ToArray();
                _cache.IsDirtyModify = true;
                AddLog(evt.eventName, "删除单条绑定", $"{item.packageKey}→{item.paramName}", "已解绑");
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawAddBindingUI(EventFieldMapping targetMap, EventDefine evt)
    {
        if (evt == null) return;

        EditorGUILayout.LabelField("数据包现有Key:" + string.Join(",", _pkgKeyList));
        EditorGUILayout.Space();

        // 选择 Key
        string selKey = "";
        if (_pkgKeyList.Count > 0)
        {
            if (_selectKeyIndex >= _pkgKeyList.Count) _selectKeyIndex = 0;
            _selectKeyIndex = EditorGUILayout.Popup("选择数据包Key", _selectKeyIndex, _pkgKeyList.ToArray());
            _selectKeyIndex = Mathf.Min(_selectKeyIndex, _pkgKeyList.Count - 1);
            selKey = _pkgKeyList[_selectKeyIndex];
        }
        else
        {
            EditorGUILayout.LabelField("该事件没有数据包Key，请先保存事件生成代码");
            return;
        }

        // 选择监听方法
        if (_cache.AllLstMethods.Count == 0 || _selectLstIdx >= _cache.AllLstMethods.Count)
        {
            EditorGUILayout.LabelField("请先在【新建/编辑事件】页选择监听方法");
            return;
        }

        MethodInfo lstMth = _cache.AllLstMethods[_selectLstIdx];
        var split = EventEditorUtil.SplitListenParam(lstMth);

        _canInjectParamList.Clear();
        foreach (var p in split.CanInjectArgs) _canInjectParamList.Add(p.Name);

        if (_canInjectParamList.Count == 0)
        {
            EditorGUILayout.LabelField("该监听方法没有可注入的参数");
            return;
        }

        if (_selectParamIndex >= _canInjectParamList.Count) _selectParamIndex = 0;
        _selectParamIndex = EditorGUILayout.Popup("绑定到监听参数", _selectParamIndex, _canInjectParamList.ToArray());
        string selParam = _canInjectParamList[_selectParamIndex];

        // 获取发布方法
        if (_cache.AllPubMethods.Count == 0 || _selectPubIdx >= _cache.AllPubMethods.Count)
        {
            EditorGUILayout.LabelField("请先在【新建/编辑事件】页选择发布方法");
            return;
        }

        MethodInfo pubMth = _cache.AllPubMethods[_selectPubIdx];
        var pubSplit = EventEditorUtil.SplitPublishParam(pubMth);

        ParameterInfo pkgParamInfo = pubSplit.OutArgs.Find(p => p.Name == selKey);
        ParameterInfo argParamInfo = split.CanInjectArgs.Find(p => p.Name == selParam);

        if (pkgParamInfo == null || argParamInfo == null)
        {
            EditorGUILayout.LabelField("类型不匹配或参数不存在");
            return;
        }

        FieldMapItem newMapItem = new FieldMapItem
        {
            packageKey = selKey,
            paramName = selParam,
            isRequired = true
        };

        if (GUILayout.Button("添加绑定映射", GUILayout.Height(26)))
        {
            if (EditorApplication.isCompiling)
            {
                EditorUtility.DisplayDialog("提示", "正在编译中,请稍后再试", "OK");
                return;
            }

            // 检查是否已绑定
            bool keyAlreadyBind = false;
            if (targetMap.fieldMaps != null)
            {
                foreach (var oldMap in targetMap.fieldMaps)
                {
                    if (oldMap.packageKey == newMapItem.packageKey)
                    {
                        keyAlreadyBind = true;
                        break;
                    }
                }
            }

            if (keyAlreadyBind)
            {
                EditorUtility.DisplayDialog("绑定失败", $"数据包Key【{newMapItem.packageKey}】已绑定参数", "确定");
                return;
            }

            bool pass = EventEditorUtil.CheckMapTypeSafe(newMapItem, pkgParamInfo.ParameterType, argParamInfo.ParameterType);
            if (!pass) return;

            var temp = new List<FieldMapItem>(targetMap.fieldMaps ?? new FieldMapItem[0]);
            temp.Add(newMapItem);
            targetMap.fieldMaps = temp.ToArray();
            _cache.IsDirtyModify = true;
            AddLog(evt.eventName, $"新增绑定:{selKey}→{selParam}", "", "字段绑定成功");
        }
    }

    private void HandleSaveFieldMapping(EventDefine evt)
    {
        if (evt == null) return;

        EventDefine captureEvt = evt;
        string captureListenSysId = _listenSysId;
        int captureSelectLstIdx = _selectLstIdx;
        int captureSelectPubIdx = _selectPubIdx;

        _cache.IsGeneratingCode = true;

        EditorApplication.delayCall += () =>
        {
            DoSaveFieldMapping(captureEvt, captureListenSysId, captureSelectLstIdx, captureSelectPubIdx);
        };
    }

    // ========== 面板4：操作日志 ==========
    private void DrawLogPanel()
    {
        try
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("操作日志列表", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("一键清空全部日志", GUILayout.Width(120)))
            {
                bool clean = EditorUtility.DisplayDialog("清空确认", "确定清空所有操作日志？", "确认清空", "取消");
                if (clean)
                {
                    _cache.LogData.Clear();
                    _cache.IsDirtyModify = true;
                    AddLog("系统", "批量清空全部操作日志", "", "日志已清空");
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            foreach (var log in _cache.LogData)
            {
                EditorGUILayout.BeginHorizontal("Box");
                EditorGUILayout.LabelField(log.operateTime, GUILayout.Width(135));
                EditorGUILayout.LabelField(log.eventName, GUILayout.Width(150));
                EditorGUILayout.LabelField(log.operateContent);
                EditorGUILayout.EndHorizontal();
            }
        }
        catch (Exception ex)
        {
            EditorGUILayout.HelpBox($"绘制日志面板失败：{ex.Message}", MessageType.Error);
        }
    }

    // ========== 面板5：系统ID管理 ==========
    private void DrawSysIdManagerPanel()
    {
        try
        {
            EditorGUILayout.LabelField("全局SystemID注册表", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // 新增
            EditorGUILayout.BeginHorizontal();
            _newSysId = EditorGUILayout.TextField("新增ID", _newSysId);
            _newSysRemark = EditorGUILayout.TextField("备注(模块名)", _newSysRemark);

            if (GUILayout.Button("添加ID", GUILayout.Width(80)))
            {
                if (string.IsNullOrWhiteSpace(_newSysId))
                {
                    EditorUtility.DisplayDialog("提示", "ID不能为空", "OK");
                    return;
                }

                bool exist = _cache.SysIdList.Exists(s => s.sysId == _newSysId);
                if (exist)
                {
                    EditorUtility.DisplayDialog("提示", "ID已存在", "OK");
                    return;
                }

                _cache.SysIdList.Add(new SystemIdItem { sysId = _newSysId, remark = _newSysRemark });
                _newSysId = "";
                _newSysRemark = "";
                _cache.IsDirtyModify = true;
                AddLog("系统ID", "新增ID", _newSysId, "添加成功");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 列表显示
            string[] sysOpts = _cache.SysIdList.Select(s => $"{s.sysId} | {s.remark}").ToArray();
            if (_selectSysIdx >= sysOpts.Length) _selectSysIdx = 0;
            _selectSysIdx = EditorGUILayout.Popup("已注册系统ID列表", _selectSysIdx, sysOpts);

            if (_cache.SysIdList.Count > 0 && _selectSysIdx < _cache.SysIdList.Count)
            {
                var selItem = _cache.SysIdList[_selectSysIdx];
                EditorGUILayout.LabelField($"选中ID:{selItem.sysId} 备注:{selItem.remark}");

                if (GUILayout.Button("删除选中ID", GUILayout.Height(26)))
                {
                    bool ok = EditorUtility.DisplayDialog("删除ID", $"确定删除 {selItem.sysId}？", "删除", "取消");
                    if (ok)
                    {
                        _cache.SysIdList.RemoveAt(_selectSysIdx);
                        _cache.IsDirtyModify = true;
                        AddLog("系统ID", "删除ID", selItem.sysId, "已移除");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            EditorGUILayout.HelpBox($"绘制系统ID管理面板失败：{ex.Message}", MessageType.Error);
        }
    }

    // ========== 核心业务方法 ==========

    private void DoSaveAndGenerate(
    string eventName, EventQueueType queue, string pubSysId, string lstSysId,
    bool newEnable, MethodInfo selectPub, MethodInfo selectLst,
    PublishParamSplitResult pubSplit, ListenParamSplitResult lstSplit,
    EventDefine editingEvt)
    {
        try
        {
            string autoPath = EventEditorUtil.AutoGenPath;

            // 确保目录存在
            if (!Directory.Exists(autoPath))
            {
                Directory.CreateDirectory(autoPath);
            }

            // ★ 第一步：删除旧文件（但不刷新 AssetDatabase）
            string oldEventFile = $"{autoPath}{eventName}Event.cs";
            string oldKeyFile = $"{autoPath}EventKey_{eventName}Event.cs";
            string oldPubFile = $"{autoPath}{pubSysId}_PublishSys_Publish_{eventName}.cs";
            string oldLstFile = $"{autoPath}{lstSysId}_ListenSys_Listen_{eventName}.cs";

            DeleteFileAndMeta(oldEventFile);
            DeleteFileAndMeta(oldKeyFile);
            DeleteFileAndMeta(oldPubFile);
            DeleteFileAndMeta(oldLstFile);

            // 生成新代码
            List<string> pkgKeys = new List<string>();
            foreach (var p in pubSplit.OutArgs) pkgKeys.Add(p.Name);

            EventDefine saveEvt = new EventDefine
            {
                eventName = eventName,
                eventClassName = $"{eventName}Event",
                eventTypeFullName = $"AutoGen.Event.{eventName}Event",
                queueType = queue,
                isGlobalEnable = newEnable,
                packageKeys = pkgKeys,
                PublishSysId = pubSysId,
                ListenSysId = lstSysId
            };

            // ★ 生成所有代码文件
            List<string> genFilePaths = EventEditorUtil.AutoGenAllCode(
                saveEvt, selectPub, pubSplit, pubSysId,
                selectLst, lstSplit, lstSysId, new List<FieldMapItem>());

            // ★ 验证所有文件都生成成功
            foreach (var path in genFilePaths)
            {
                // 等待文件出现（最多等待1秒）
                bool fileExists = false;
                for (int wait = 0; wait < 20; wait++)
                {
                    if (File.Exists(path))
                    {
                        fileExists = true;
                        break;
                    }
                    System.Threading.Thread.Sleep(50);
                }

                if (!fileExists)
                {
                    throw new Exception($"文件生成失败：{path}");
                }

                // 记录到临时列表（用于回退时删除）
                if (!_cache.TempCreateCodePath.Contains(path))
                {
                    _cache.TempCreateCodePath.Add(path);
                }
            }

            // ★ 更新内存数据
            if (editingEvt != null)
            {
                bool keyInfoChanged = editingEvt.eventName != eventName ||
                                      editingEvt.PublishSysId != pubSysId ||
                                      editingEvt.ListenSysId != lstSysId;

                if (keyInfoChanged)
                {
                    _cache.EvtData.Add(saveEvt);
                    AddLog(eventName, "编辑关键信息变动→生成新事件", editingEvt.eventName, saveEvt.eventName);
                }
                else
                {
                    int idx = _cache.EvtData.IndexOf(editingEvt);
                    saveEvt.packageKeys = editingEvt.packageKeys;
                    _cache.EvtData[idx] = saveEvt;
                    AddLog(eventName, "修改事件配置", $"{editingEvt.isGlobalEnable}", $"{newEnable}");
                }
            }
            else
            {
                _cache.EvtData.Add(saveEvt);
                AddLog(eventName, "新建事件", "", $"队列:{queue}");
            }

            // ★ 保存配置到 JSON
            EventEditorJsonHelper.SaveConfig(_cache.EvtData, _cache.MapData, _cache.LogData, _cache.SysIdList);

            // ★ 等待文件系统稳定（短暂等待即可）
            System.Threading.Thread.Sleep(100);

            // ★ 刷新 AssetDatabase（只调用一次）
            AssetDatabase.Refresh();

            // ★ 等待 Unity 完成文件导入
            System.Threading.Thread.Sleep(200);

            // ★ 更新快照（不清空 TempCreateCodePath！）
            var newRoot = EventEditorJsonHelper.LoadConfig();
            _cache.OriginSnapshot = DeepCopyRootCfg(newRoot);
            _cache.IsDirtyModify = false;
            // ❌ 不要清空 TempCreateCodePath！回退时需要它来删除文件
            // _cache.TempCreateCodePath.Clear();  // 删除这行

            // ★ 等待编译完成
            _cache.IsWaitingForCompile = true;
            _cache.AfterCompileAction = () =>
            {
                _cache.IsGeneratingCode = false;
                // ★ 编译成功后，可以安全清空临时文件记录了
                _cache.TempCreateCodePath.Clear();
                _editingEvt = null;
                _tabIndex = 0;
                EditorUtility.DisplayDialog("成功", "配置&代码已更新", "OK");
                Repaint();
            };
        }
        catch (Exception ex)
        {
            _cache.IsGeneratingCode = false;
            _cache.IsWaitingForCompile = false;
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("错误", $"生成代码失败：{ex.Message}", "OK");
            Debug.LogError($"生成代码异常：{ex}");
        }
    }

    private void DoSaveFieldMapping(EventDefine evt, string listenSysId, int selectLstIdx, int selectPubIdx)
    {
        try
        {
            MethodInfo lstMth = (_cache.AllLstMethods.Count > 0 && selectLstIdx < _cache.AllLstMethods.Count)
                ? _cache.AllLstMethods[selectLstIdx] : null;
            MethodInfo pubMth = (_cache.AllPubMethods.Count > 0 && selectPubIdx < _cache.AllPubMethods.Count)
                ? _cache.AllPubMethods[selectPubIdx] : null;

            if (lstMth == null || pubMth == null)
            {
                EditorUtility.DisplayDialog("错误", "未找到选中的发布或监听方法", "OK");
                _cache.IsGeneratingCode = false;
                return;
            }

            string autoPath = EventEditorUtil.AutoGenPath;
            string oldListenFile = $"{autoPath}{listenSysId}_ListenSys_Listen_{evt.eventName}.cs";
            DeleteFileAndMeta(oldListenFile);

            var mapCfg = _cache.MapData.Find(d => d.eventTypeFullName == evt.eventTypeFullName);
            List<FieldMapItem> bindList = mapCfg == null ? new List<FieldMapItem>() : new List<FieldMapItem>(mapCfg.fieldMaps);

            var lstSplit = EventEditorUtil.SplitListenParam(lstMth);
            var genPaths = EventEditorUtil.GenListenSystemCode(evt, lstMth, lstSplit, listenSysId, bindList);
            foreach (var p in genPaths)
            {
                _cache.TempCreateCodePath.Add(p);
            }

            SaveAllCfg();
            AddLog("字段配置", "批量保存映射+刷新监听代码", "", "已落地Json");

            AssetDatabase.Refresh();
            System.Threading.Thread.Sleep(50);

            _cache.IsWaitingForCompile = true;
            _cache.AfterCompileAction = () =>
            {
                _cache.IsGeneratingCode = false;
                EditorUtility.DisplayDialog("提示", "字段映射&监听代码刷新完成", "OK");
                Repaint();
            };
        }
        catch (Exception ex)
        {
            _cache.IsGeneratingCode = false;
            _cache.IsWaitingForCompile = false;
            EditorUtility.DisplayDialog("错误", $"保存字段映射失败：{ex.Message}", "OK");
            Debug.LogError($"保存字段映射异常：{ex}");
        }
    }

    // ========== 工具方法 ==========

    private void SaveAllCfg(bool needRefresh = true)
    {
        if (_cache == null) return;

        EventEditorJsonHelper.SaveConfig(_cache.EvtData, _cache.MapData, _cache.LogData, _cache.SysIdList);
        var newRoot = EventEditorJsonHelper.LoadConfig();
        _cache.OriginSnapshot = DeepCopyRootCfg(newRoot);
        _cache.IsDirtyModify = false;
        _cache.TempCreateCodePath.Clear();

        if (needRefresh)
        {
            AssetDatabase.Refresh();
        }
    }

    private void RollbackToSnapshot()
    {
        if (_cache?.OriginSnapshot == null) return;

        _cache.EvtData = _cache.OriginSnapshot.EventDefine?.Items ?? new List<EventDefine>();
        _cache.MapData = _cache.OriginSnapshot.FieldMapping?.Items ?? new List<EventFieldMapping>();
        _cache.LogData = _cache.OriginSnapshot.OperateLog?.Items ?? new List<EventOperateLog>();
        _cache.SysIdList = _cache.OriginSnapshot.SystemIdList?.Items ?? new List<SystemIdItem>();

        DeleteTempCreateCode();
        _cache.IsDirtyModify = false;
        AssetDatabase.Refresh();
        Repaint();
    }

    private void DeleteTempCreateCode()
    {
        if (_cache == null) return;

        foreach (var path in _cache.TempCreateCodePath)
        {
            DeleteFileAndMeta(path);
        }
        _cache.TempCreateCodePath.Clear();
        AssetDatabase.Refresh();
    }

    private void DeleteEventFileAndConfig(EventDefine evt)
    {
        string autoPath = EventEditorUtil.AutoGenPath;

        DeleteFileAndMeta($"{autoPath}{evt.eventClassName}.cs");
        DeleteFileAndMeta($"{autoPath}EventKey_{evt.eventClassName}.cs");
        DeleteFileAndMeta($"{autoPath}{evt.PublishSysId}_PublishSys_Publish_{evt.eventName}.cs");
        DeleteFileAndMeta($"{autoPath}{evt.ListenSysId}_ListenSys_Listen_{evt.eventName}.cs");

        _cache.EvtData.Remove(evt);
        _cache.MapData.RemoveAll(m => m.eventTypeFullName == evt.eventTypeFullName);
        _cache.IsDirtyModify = true;
        AssetDatabase.Refresh();
    }

    private void DeleteFileAndMeta(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return;

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            string metaPath = filePath + ".meta";
            if (File.Exists(metaPath)) File.Delete(metaPath);
        }
    }

    private void AddLog(string evtName, string content, string oldVal, string newVal)
    {
        if (_cache == null) return;

        var log = new EventOperateLog
        {
            operateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            eventName = evtName,
            operateContent = content,
            oldValue = oldVal,
            newValue = newVal
        };
        _cache.LogData.Add(log);
        _cache.IsDirtyModify = true;
    }

    private EventRootCfg DeepCopyRootCfg(EventRootCfg source)
    {
        try
        {
            if (source == null) return new EventRootCfg();
            string json = EventEditorJsonHelper.ToJson(source);
            return EventEditorJsonHelper.FromJson(json);
        }
        catch
        {
            return new EventRootCfg();
        }
    }
}