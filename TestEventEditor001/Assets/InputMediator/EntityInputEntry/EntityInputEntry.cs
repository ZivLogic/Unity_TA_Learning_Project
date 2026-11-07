using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 实体输入入口基类
/// 继承 LogicScriptBase，可被逻辑脚本系统管理
/// 
/// 职责：
/// 1. 维护输入行为表
/// 2. 收到输入事件 → 查表 → 命中才执行
/// 3. 实现 LoadFromEntry / SaveToEntry
/// 4. 提供事件系统桥接（通过 Entity 查找 EntityEventEntry）
/// </summary>
public abstract class EntityInputEntry : LogicScriptBase
{
    // ============ 输入行为表 ============
    private Dictionary<string, Action<EntityInputEvent>> _inputTable = new Dictionary<string, Action<EntityInputEvent>>();

    public override string ContainerId => "input";

    protected virtual void Awake()
    {
        RegisterInputs();
    }

    /// <summary>
    /// 子类注册输入行为
    /// </summary>
    protected abstract void RegisterInputs();

    protected void RegisterInput(string action, Action<EntityInputEvent> handler)
    {
        if (string.IsNullOrEmpty(action) || handler == null) return;
        _inputTable[action] = handler;
    }

    /// <summary>
    /// 统一入口：查表，命中才执行
    /// </summary>
    public void ReceiveInput(EntityInputEvent evt)
    {
        if (evt == null) return;

        if (_inputTable.TryGetValue(evt.Action, out var handler))
        {
            try { handler.Invoke(evt); }
            catch (Exception e) { Debug.LogError($"[Entry] {name} handler 异常：{e}"); }
        }
    }

    public override void LoadFromEntry(ILogicStateEntry entry)
    {
        // 默认空实现
    }

    public override ILogicStateEntry SaveToEntry()
    {
        return null;
    }
}