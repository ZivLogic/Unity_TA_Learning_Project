using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 事件编辑器缓存管理器
/// 负责管理所有数据状态，独立于窗口生命周期
/// </summary>
public class EventEditorCache : IDisposable
{
    // ========== 配置数据 ==========
    public List<EventDefine> EvtData { get; set; }
    public List<EventFieldMapping> MapData { get; set; }
    public List<EventOperateLog> LogData { get; set; }
    public List<SystemIdItem> SysIdList { get; set; }

    // ========== 反射缓存 ==========
    public List<MethodInfo> AllPubMethods { get; set; }
    public List<MethodInfo> AllLstMethods { get; set; }

    // ========== 快照 ==========
    public EventRootCfg OriginSnapshot { get; set; }

    // ========== 临时文件记录 ==========
    public List<string> TempCreateCodePath { get; } = new List<string>();

    // ========== 状态标志 ==========
    public bool IsDirtyModify { get; set; }      // 是否有未保存修改
    public bool IsGeneratingCode { get; set; }    // 是否正在生成代码
    public bool IsWaitingForCompile { get; set; } // 是否等待编译完成

    // ========== 回调 ==========
    public Action AfterCompileAction { get; set; }

    /// <summary>
    /// 构造函数 - 初始化空数据
    /// </summary>
    public EventEditorCache()
    {
        EvtData = new List<EventDefine>();
        MapData = new List<EventFieldMapping>();
        LogData = new List<EventOperateLog>();
        SysIdList = new List<SystemIdItem>();
        AllPubMethods = new List<MethodInfo>();
        AllLstMethods = new List<MethodInfo>();
        OriginSnapshot = new EventRootCfg();
        TempCreateCodePath.Clear();

        IsDirtyModify = false;
        IsGeneratingCode = false;
        IsWaitingForCompile = false;
        AfterCompileAction = null;
    }

    /// <summary>
    /// 加载配置（带错误处理）
    /// </summary>
    public bool ReloadConfig()
    {
        try
        {
            EventRootCfg root = EventEditorJsonHelper.LoadConfig();

            EvtData = root.EventDefine?.Items ?? new List<EventDefine>();
            MapData = root.FieldMapping?.Items ?? new List<EventFieldMapping>();
            LogData = root.OperateLog?.Items ?? new List<EventOperateLog>();
            SysIdList = root.SystemIdList?.Items ?? new List<SystemIdItem>();

            // 深拷贝快照
            OriginSnapshot = DeepCopyRootCfg(root);

            // 刷新反射方法
            AllPubMethods = EventEditorUtil.GetAllPublishMethods() ?? new List<MethodInfo>();
            AllLstMethods = EventEditorUtil.GetAllListenMethods() ?? new List<MethodInfo>();

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"加载配置失败：{ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 强制清除所有状态（用于错误恢复）
    /// </summary>
    public void ForceClear()
    {
        IsGeneratingCode = false;
        IsWaitingForCompile = false;
        IsDirtyModify = false;
        AfterCompileAction = null;
        TempCreateCodePath.Clear();
    }

    /// <summary>
    /// 深拷贝
    /// </summary>
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

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        ForceClear();
    }
}