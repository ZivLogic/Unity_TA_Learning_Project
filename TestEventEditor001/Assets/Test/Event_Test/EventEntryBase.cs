using System;
using System.Collections.Generic;
using UnityEngine;
using EventSystemV2;

/// <summary>
/// 事件入口基类
/// 继承 LogicScriptBase，可被 LogicStateManager 管理
/// 职责：
/// 1. 维护事件处理方法表（key = 类名.方法名）
/// 2. 收到事件 → 查表 → 命中才调
/// 3. 提供 Emit 发布入口
/// 接收方只写业务方法，注册由生成代码（partial）代劳
/// </summary>
public abstract class EventEntryBase : LogicScriptBase
{
    // key = "类名.方法名"，value = 处理方法
    private readonly Dictionary<string, Action<PackageEvent>> _table
        = new Dictionary<string, Action<PackageEvent>>();

    public override string ContainerId => "event";

    protected virtual void Awake()
    {
        RegisterEvents();
    }

    /// <summary>
    /// 由生成代码（partial）实现，把接收方法注册进表
    /// 手写场景也可以子类自己重写
    /// </summary>
    protected abstract void RegisterEvents();

    /// <summary>
    /// 注册一个处理方法（key = 类名.方法名）
    /// </summary>
    protected void RegisterMethod(string methodName, Action<PackageEvent> handler)
    {
        if (string.IsNullOrEmpty(methodName) || handler == null) return;
        _table[$"{GetType().Name}.{methodName}"] = handler;
    }

    /// <summary>
    /// 统一接收入口：链路代码按配置传 key
    /// </summary>
    public void Receive(string key, PackageEvent evt)
    {
        if (string.IsNullOrEmpty(key) || evt == null) return;
        if (_table.TryGetValue(key, out var handler))
        {
            try { handler.Invoke(evt); }
            catch (Exception e)
            {
                Debug.LogError($"[EventEntryBase] {GetType().Name} 处理 {key} 异常：{e}");
            }
        }
    }

    /// <summary>
    /// 兼容：按事件类型查（弱类型通道用）
    /// </summary>
    public void Receive(PackageEvent evt)
    {
        // 弱类型通道：遍历表找能处理该事件类型的（暂时用全局 typedListeners 兜底）
        //EventSystemV2.EventManager.Instance?.DispatchTypedToEntry(evt);
    }

    /// <summary>
    /// 发布入口（子类只调 Emit，不碰事件系统内部）
    /// </summary>
    protected void Emit(PackageEvent evt)
    {
        EventSystemV2.EventUtil.EmitEvent(evt);
    }

    // ===== LogicScriptBase 实现（事件入口默认无状态）=====
    public override void LoadFromEntry(ILogicStateEntry entry) { }
    public override ILogicStateEntry SaveToEntry() => null;
}