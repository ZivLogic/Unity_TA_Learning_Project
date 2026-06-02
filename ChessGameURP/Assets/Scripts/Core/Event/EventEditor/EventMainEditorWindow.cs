using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class EventMainEditorWindow : EditorWindow
{
    private Vector2 _scrollPos;
    private int _tabIndex;

    //全局内存配置
    private List<EventDefine> _evtData;
    private List<EventFieldMapping> _mapData;
    private List<EventOperateLog> _logData;
    private List<SystemIdItem> _sysIdList = new List<SystemIdItem>();
    private string _newSysId = "";
    private string _newSysRemark = "";
    private int _selectSysIdx = 0;

    //下拉缓存
    private List<MethodInfo> _allPubMethods;
    private List<MethodInfo> _allLstMethods;
    private int _selectPubIdx;
    private int _selectLstIdx;

    //新建事件临时缓存
    private string _curEventName = "";
    private EventQueueType _curQueue = EventQueueType.Logic;
    private string _publishSysId = "DefaultPublish";
    private string _listenSysId = "DefaultListen";

    //字段绑定面板缓存
    private int _selectEvtIndex = 0;
    private int _selectKeyIndex = 0;
    private int _selectParamIndex = 0;
    private EventDefine _selectedEvt;
    private List<string> _pkgKeyList = new List<string>();
    private List<string> _canInjectParamList = new List<string>();

    [MenuItem("EventTool/事件可视化编辑器")]
    public static void Open()
    {
        var win = GetWindow<EventMainEditorWindow>("事件配置编辑器");
        win.minSize = new Vector2(1020, 650);
        win.Show();
    }

    private void OnEnable()
    {
        EventRootCfg root = EventEditorJsonHelper.LoadConfig();
        _evtData = root.EventDefine.Items;
        _mapData = root.FieldMapping.Items;
        _logData = root.OperateLog.Items;
        //加载系统ID
        if (root.SystemIdList == null) root.SystemIdList = new SystemIdWrap();
        _sysIdList = root.SystemIdList.Items;

        _allPubMethods = EventEditorUtil.GetAllPublishMethods();
        _allLstMethods = EventEditorUtil.GetAllListenMethods();
    }

    private void OnGUI()
    {
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
        string[] tabs = { "事件总览", "新建/编辑事件", "字段映射绑定", "操作日志", "系统ID管理" };
        _tabIndex = GUILayout.Toolbar(_tabIndex, tabs);
        EditorGUILayout.Space();
        switch (_tabIndex)
        {
            case 0: DrawOverview(); break;
            case 1: DrawCreateEvent(); break;
            case 2: DrawFieldMapPanel(); break;
            case 3: DrawLogPanel(); break;
            case 4: DrawSysIdManager(); break;
        }
        EditorGUILayout.EndScrollView();
    }

    #region 面板绘制
    private void DrawOverview()
    {
        EditorGUILayout.LabelField("全局事件一览表", EditorStyles.boldLabel);
        for (int i = 0; i < _evtData.Count; i++)
        {
            var evt = _evtData[i];
            EditorGUILayout.BeginHorizontal("Box");
            EditorGUILayout.LabelField(evt.eventName, GUILayout.Width(130));
            EditorGUILayout.LabelField(evt.queueType.ToString(), GUILayout.Width(100));
            //【修复：补上勾选绘制，之前缺失】
            evt.isGlobalEnable = EditorGUILayout.Toggle("启用", evt.isGlobalEnable, GUILayout.Width(60));
            if (GUI.changed)
            {
                SaveAllCfg();
                AddLog(evt.eventName, "修改启用状态", $"{!evt.isGlobalEnable}", $"{evt.isGlobalEnable}");
            }
            if (GUILayout.Button("编辑", GUILayout.Width(70))) _tabIndex = 1;
            //新增删除按钮
            if (GUILayout.Button("删除", GUILayout.Width(70)))
            {
                bool delOk = EditorUtility.DisplayDialog("确认删除", $"确定要删除事件【{evt.eventName}】？\n自动删除对应生成代码+配置", "删除", "取消");
                if (delOk)
                {
                    DeleteEventFileAndConfig(evt);
                    AddLog(evt.eventName, "删除事件", evt.eventName, "已移除");
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.Space();
        if (GUILayout.Button("保存配置到JSON", GUILayout.Height(30)))
        {
            SaveAllCfg();
            AddLog("全局", "批量修改事件启用状态", "", "开关已同步");
            EditorUtility.DisplayDialog("完成", "EventConfig.json已更新", "OK");
        }
    }

    private void DrawCreateEvent()
    {
        EditorGUILayout.LabelField("新建/编辑事件", EditorStyles.boldLabel);
        _curEventName = EditorGUILayout.TextField("事件名称", _curEventName);
        _curQueue = (EventQueueType)EditorGUILayout.EnumPopup("目标队列", _curQueue);
        EditorGUILayout.Space();

        //发布配置
        EditorGUILayout.LabelField("【发布端配置】", EditorStyles.boldLabel);
        string[] sysArr = new string[_sysIdList.Count];
        for (int i = 0; i < _sysIdList.Count; i++) sysArr[i] = _sysIdList[i].sysId;
        if (sysArr.Length > 0)
        {
            int pubSysIdx = Array.FindIndex(sysArr, s => s == _publishSysId);
            if (pubSysIdx < 0) pubSysIdx = 0;
            pubSysIdx = EditorGUILayout.Popup("发布所属SystemID(下拉选择)", pubSysIdx, sysArr);
            _publishSysId = sysArr[pubSysIdx];
        }
        else
        {
            EditorGUILayout.LabelField("暂无系统ID，请前往【系统ID管理】面板新增");
        }

        //监听SystemID
        if (sysArr.Length > 0)
        {
            int lstSysIdx = Array.FindIndex(sysArr, s => s == _listenSysId);
            if (lstSysIdx < 0) lstSysIdx = 0;
            lstSysIdx = EditorGUILayout.Popup("监听所属SystemID(下拉选择)", lstSysIdx, sysArr);
            _listenSysId = sysArr[lstSysIdx];
        }
        else
        {
            EditorGUILayout.LabelField("暂无系统ID，请前往【系统ID管理】面板新增");
        }

        string[] pubOpts = new string[_allPubMethods.Count];
        for (int i = 0; i < _allPubMethods.Count; i++)
            pubOpts[i] = $"{_allPubMethods[i].DeclaringType.Name}.{_allPubMethods[i].Name}";

        if (_selectPubIdx < 0 || _selectPubIdx >= pubOpts.Length) _selectPubIdx = 0;
        _selectPubIdx = EditorGUILayout.Popup("选择发布业务方法", _selectPubIdx, pubOpts);

        MethodInfo selectPub = null;
        EventEditorUtil.PublishParamSplitResult pubSplit = null;
        if (_allPubMethods.Count > 0)
        {
            selectPub = _allPubMethods[_selectPubIdx];
            pubSplit = EventEditorUtil.SplitPublishParam(selectPub);
            EditorGUILayout.LabelField($"该方法:外部入参{pubSplit.InArgs.Count}个 | 打包out{pubSplit.OutArgs.Count}个");
        }
        else
        {
            EditorGUILayout.LabelField("未扫描到带[EventPublishMethodAttribute]的业务方法");
        }
        EditorGUILayout.Space();

        //监听配置
        EditorGUILayout.LabelField("【监听端配置】", EditorStyles.boldLabel);
        string[] lstOpts = new string[_allLstMethods.Count];
        for (int i = 0; i < _allLstMethods.Count; i++)
            lstOpts[i] = $"{_allLstMethods[i].DeclaringType.Name}.{_allLstMethods[i].Name}";

        if (_selectLstIdx < 0 || _selectLstIdx >= lstOpts.Length) _selectLstIdx = 0;
        _selectLstIdx = EditorGUILayout.Popup("选择监听业务方法", _selectLstIdx, lstOpts);

        MethodInfo selectLst = null;
        EventEditorUtil.ListenParamSplitResult lstSplit = null;
        if (_allLstMethods.Count > 0)
        {
            selectLst = _allLstMethods[_selectLstIdx];
            lstSplit = EventEditorUtil.SplitListenParam(selectLst);
            EditorGUILayout.LabelField($"该方法:可注入参数{lstSplit.CanInjectArgs.Count}个 | 输出out{lstSplit.OutReturnArgs.Count}个");
        }
        else
        {
            EditorGUILayout.LabelField("未扫描到带[EventListenMethodAttribute]的业务方法");
        }
        EditorGUILayout.Space();

        if (GUILayout.Button("一键生成代码+注册配置", GUILayout.Height(32)))
        {
            if (string.IsNullOrWhiteSpace(_curEventName))
            {
                EditorUtility.DisplayDialog("错误", "事件名称不能为空", "OK");
                return;
            }
            if (selectPub == null || selectLst == null || pubSplit == null || lstSplit == null)
            {
                EditorUtility.DisplayDialog("错误", "请先选择有效的发布/监听方法", "OK");
                return;
            }
            List<string> pkgKeys = new List<string>();
            foreach (var p in pubSplit.OutArgs) pkgKeys.Add(p.Name);
            EventDefine newEvt = new EventDefine()
            {
                eventName = _curEventName,
                eventClassName = $"{_curEventName}Event",
                eventTypeFullName = $"AutoGen.Event.{_curEventName}Event",
                queueType = _curQueue,
                isGlobalEnable = true,
                packageKeys = pkgKeys
            };
            List<FieldMapItem> emptyMap = new List<FieldMapItem>();
            EventEditorUtil.AutoGenAllCode(newEvt, selectPub, pubSplit, _publishSysId, selectLst, lstSplit, _listenSysId, emptyMap);
            _evtData.Add(newEvt);
            SaveAllCfg();
            AddLog(_curEventName, "新建事件", "", $"队列:{_curQueue},发布系统:{_publishSysId},监听系统:{_listenSysId}");
            EditorUtility.DisplayDialog("成功", "事件代码&基础配置生成完成，请前往字段绑定面板配置参数映射", "OK");
        }
    }

    private void DrawFieldMapPanel()
    {
        EditorGUILayout.LabelField("数据包Key ↔ 监听入参 绑定配置", EditorStyles.boldLabel);
        string[] evtNames = new string[_evtData.Count];
        for (int i = 0; i < _evtData.Count; i++) evtNames[i] = _evtData[i].eventName;

        if (_selectEvtIndex >= evtNames.Length) _selectEvtIndex = 0;
        _selectEvtIndex = EditorGUILayout.Popup("选择目标事件", _selectEvtIndex, evtNames);

        _selectedEvt = null;
        if (_evtData.Count > 0)
        {
            _selectedEvt = _evtData[_selectEvtIndex];
            _pkgKeyList = _selectedEvt.packageKeys;
        }
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("当前事件已有绑定关系：", EditorStyles.boldLabel);
        EventFieldMapping targetMap = null;
        if (_selectedEvt != null)
        {
            targetMap = _mapData.Find(x => x.eventTypeFullName == _selectedEvt.eventTypeFullName);
            //无映射自动创建空配置，彻底杜绝空指针
            if (targetMap == null)
            {
                targetMap = new EventFieldMapping();
                targetMap.eventTypeFullName = _selectedEvt.eventTypeFullName;
                targetMap.fieldMaps = new FieldMapItem[0];
                _mapData.Add(targetMap);
                SaveAllCfg();
            }
        }

        //展示绑定列表+删除单条
        if (targetMap != null && targetMap.fieldMaps != null && targetMap.fieldMaps.Length > 0)
        {
            for (int i = 0; i < targetMap.fieldMaps.Length; i++)
            {
                var item = targetMap.fieldMaps[i];
                EditorGUILayout.BeginHorizontal("Box");
                EditorGUILayout.LabelField($"Key:{item.packageKey} → 参数:{item.paramName}", GUILayout.Width(320));
                if (GUILayout.Button("删除本条绑定", GUILayout.Width(100)))
                {
                    List<FieldMapItem> temp = new List<FieldMapItem>(targetMap.fieldMaps);
                    temp.RemoveAt(i);
                    targetMap.fieldMaps = temp.ToArray();
                    SaveAllCfg();
                    AddLog(_selectedEvt.eventName, "删除单条绑定", $"{item.packageKey}→{item.paramName}", "已解绑");
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        else
        {
            EditorGUILayout.LabelField("暂无任何字段绑定");
        }
        EditorGUILayout.Space();

        if (_selectedEvt != null)
        {
            EditorGUILayout.LabelField("数据包现有Key:" + string.Join(",", _pkgKeyList));
            EditorGUILayout.Space();

            string selKey = "";
            if (_pkgKeyList.Count > 0)
            {
                if (_selectKeyIndex >= _pkgKeyList.Count) _selectKeyIndex = 0;
                _selectKeyIndex = EditorGUILayout.Popup("选择数据包Key", _selectKeyIndex, _pkgKeyList.ToArray());
                _selectKeyIndex = Mathf.Min(_selectKeyIndex, _pkgKeyList.Count - 1);
                selKey = _pkgKeyList[_selectKeyIndex];
            }

            MethodInfo lstMth = null;
            if (_allLstMethods.Count > 0 && _selectLstIdx < _allLstMethods.Count)
                lstMth = _allLstMethods[_selectLstIdx];

            if (lstMth != null && !string.IsNullOrEmpty(selKey))
            {
                var split = EventEditorUtil.SplitListenParam(lstMth);
                _canInjectParamList.Clear();
                foreach (var p in split.CanInjectArgs) _canInjectParamList.Add(p.Name);

                if (_canInjectParamList.Count > 0 && _selectParamIndex >= _canInjectParamList.Count)
                    _selectParamIndex = 0;
                _selectParamIndex = EditorGUILayout.Popup("绑定到监听参数", _selectParamIndex, _canInjectParamList.ToArray());
                string selParam = _canInjectParamList[_selectParamIndex];

                MethodInfo pubMth = null;
                if (_allPubMethods.Count > 0 && _selectPubIdx < _allPubMethods.Count)
                    pubMth = _allPubMethods[_selectPubIdx];
                if (pubMth == null) return;

                var pubSplit = EventEditorUtil.SplitPublishParam(pubMth);
                ParameterInfo pkgParamInfo = pubSplit.OutArgs.Find(p => p.Name == selKey);
                ParameterInfo argParamInfo = split.CanInjectArgs.Find(p => p.Name == selParam);
                if (pkgParamInfo == null || argParamInfo == null) return;

                FieldMapItem newMapItem = new FieldMapItem()
                {
                    packageKey = selKey,
                    paramName = selParam,
                    isRequired = true
                };

                if (GUILayout.Button("添加绑定映射", GUILayout.Height(26)))
                {
                    //重复绑定拦截
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
                        EditorUtility.DisplayDialog("绑定失败", $"数据包Key【{newMapItem.packageKey}】已绑定参数，一个Key仅允许绑定单个入参", "确定");
                        return;
                    }
                    bool pass = EventEditorUtil.CheckMapTypeSafe(newMapItem, pkgParamInfo.ParameterType, argParamInfo.ParameterType);
                    if (!pass) return;

                    List<FieldMapItem> temp = new List<FieldMapItem>(targetMap.fieldMaps ?? new FieldMapItem[0]);
                    temp.Add(newMapItem);
                    targetMap.fieldMaps = temp.ToArray();
                    SaveAllCfg();
                    AddLog(_selectedEvt.eventName, $"新增绑定:{selKey}→{selParam}", "", "字段绑定成功");
                }
            }
        }
        EditorGUILayout.Space();

        //保存并刷新监听代码
        if (_selectedEvt != null && GUILayout.Button("保存全部映射+刷新监听代码", GUILayout.Height(32)))
        {
            var evt = _selectedEvt;
            var mapCfg = _mapData.Find(d => d.eventTypeFullName == evt.eventTypeFullName);
            List<FieldMapItem> bindList = mapCfg == null ? new List<FieldMapItem>() : new List<FieldMapItem>(mapCfg.fieldMaps);

            MethodInfo lstMth = null;
            if (_allLstMethods.Count > 0 && _selectLstIdx < _allLstMethods.Count)
                lstMth = _allLstMethods[_selectLstIdx];
            MethodInfo pubMth = null;
            if (_allPubMethods.Count > 0 && _selectPubIdx < _allPubMethods.Count)
                pubMth = _allPubMethods[_selectPubIdx];

            if (lstMth != null && pubMth != null)
            {
                var lstSplit = EventEditorUtil.SplitListenParam(lstMth);
                EventEditorUtil.GenListenSystemCode(evt, lstMth, lstSplit, _listenSysId, bindList);
            }
            SaveAllCfg();
            AddLog("字段配置", "批量保存映射+刷新监听代码", "", "已落地Json，监听代码已重新生成");
            EditorUtility.DisplayDialog("提示", "字段映射&监听代码刷新完成", "OK");
        }
    }

    private void DrawLogPanel()
    {
        foreach (var log in _logData)
        {
            EditorGUILayout.BeginHorizontal("Box");
            EditorGUILayout.LabelField(log.operateTime, GUILayout.Width(135));
            EditorGUILayout.LabelField(log.eventName, GUILayout.Width(150));
            EditorGUILayout.LabelField(log.operateContent);
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawSysIdManager()
    {
        EditorGUILayout.LabelField("全局SystemID注册表（新增/删除/备注，新建事件下拉可选ID，不用手写）", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        _newSysId = EditorGUILayout.TextField("新增ID", _newSysId);
        _newSysRemark = EditorGUILayout.TextField("备注(模块名)", _newSysRemark);
        if (GUILayout.Button("添加ID", GUILayout.Width(80)))
        {
            if (string.IsNullOrWhiteSpace(_newSysId)) return;
            bool exist = _sysIdList.Exists(s => s.sysId == _newSysId);
            if (exist) { EditorUtility.DisplayDialog("提示", "ID已存在", "OK"); return; }
            _sysIdList.Add(new SystemIdItem() { sysId = _newSysId, remark = _newSysRemark });
            _newSysId = ""; _newSysRemark = "";
            SaveAllCfg();
            AddLog("系统ID", "新增ID", _newSysId, "添加成功");
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        string[] sysOpts = new string[_sysIdList.Count];
        for (int i = 0; i < _sysIdList.Count; i++) sysOpts[i] = $"{_sysIdList[i].sysId} | {_sysIdList[i].remark}";
        if (_selectSysIdx >= sysOpts.Length) _selectSysIdx = 0;
        _selectSysIdx = EditorGUILayout.Popup("已注册系统ID列表", _selectSysIdx, sysOpts);

        if (_sysIdList.Count > 0)
        {
            var selItem = _sysIdList[_selectSysIdx];
            EditorGUILayout.LabelField($"选中ID:{selItem.sysId} 备注:{selItem.remark}");
            if (GUILayout.Button("删除选中ID", GUILayout.Height(26)))
            {
                bool ok = EditorUtility.DisplayDialog("删除ID", "删除后新建事件无法再选择此ID，已绑定旧事件不受影响", "删除", "取消");
                if (ok)
                {
                    _sysIdList.RemoveAt(_selectSysIdx);
                    SaveAllCfg();
                    AddLog("系统ID", "删除ID", selItem.sysId, "已移除");
                }
            }
        }
    }
    #endregion

    #region 内部工具
    private void SaveAllCfg()
    {
        EventEditorJsonHelper.SaveConfig(_evtData, _mapData, _logData, _sysIdList);
    }
    private void AddLog(string evtName, string content, string oldVal, string newVal)
    {
        var log = new EventOperateLog()
        {
            operateTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            eventName = evtName,
            operateContent = content,
            oldValue = oldVal,
            newValue = newVal
        };
        _logData.Add(log);
        SaveAllCfg();
    }
    #endregion

    #region 删除事件文件
    private void DeleteEventFileAndConfig(EventDefine evt)
    {
        string autoPath = EventEditorUtil.AutoGenPath;
        string evtCs = $"{autoPath}{evt.eventClassName}.cs";
        string keyCs = $"{autoPath}EventKey_{evt.eventClassName}.cs";
        string pubCs = $"{autoPath}{_publishSysId}_PublishSys_Publish_{evt.eventName}.cs";
        string lstCs = $"{autoPath}{_listenSysId}_ListenSys_Listen_{evt.eventName}.cs";

        DeleteFileIfExist(evtCs);
        DeleteFileIfExist(keyCs);
        DeleteFileIfExist(pubCs);
        DeleteFileIfExist(lstCs);

        _evtData.Remove(evt);
        _mapData.RemoveAll(m => m.eventTypeFullName == evt.eventTypeFullName);
        SaveAllCfg();
        AssetDatabase.Refresh();
    }
    private void DeleteFileIfExist(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }
    #endregion
}