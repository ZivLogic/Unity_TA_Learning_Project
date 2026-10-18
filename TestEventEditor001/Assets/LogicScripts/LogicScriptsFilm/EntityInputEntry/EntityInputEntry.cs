using System;
using System.Collections.Generic;
using UnityEngine;

//实体输入入口基类
//继承logicScriptBase，可被逻辑脚本系统管理
//职责：
//1.维护输入行为表
//2.收到事件 -》 查表 -》 只调用命中的方法
//3.实现LoadFromEntry / SaveToEntry（输入状态持久化）
public abstract class EntityInputEntry : LogicScriptBase
{
    //输入行为表：Action -》 处理方法
    private Dictionary<string, Action<EntityInputEvent>> _inputTable = new Dictionary<string, Action<EntityInputEvent>>();

    //父容器ID：固定为input
    public override string ContainerId => "input";

    protected virtual void Awake()
    {
        RegisterInputs();
    }


    //子类注册自己的输入行为
    protected abstract void RegisterInputs();
    
    protected void RegisterInput(string action, Action<EntityInputEvent> handler)
    {
        if (string.IsNullOrEmpty(action) || handler == null)return;
        _inputTable[action] = handler;
    }

    //统一入口：查表，命中才执行
    public void ReceiveInput(EntityInputEvent evt)
    {
        if (evt == null) return;

        Debug.Log($"[Entry] {name} 收到 {evt.Action}，表内 Action=[{string.Join(",", _inputTable.Keys)}]");

        if (_inputTable.TryGetValue(evt.Action, out var handler))
        {
            Debug.Log($"[Entry] {name} 命中 {evt.Action}");
            try
            {
                handler.Invoke(evt);
                Debug.Log($"[Entry] {name} handler 执行完成");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Entry] {name} handler 执行异常：{e}");
            }
        }
        else
        {
            Debug.LogWarning($"[Entry] {name} 未命中 {evt.Action}");
        }
    }

    // ============= logicScriptBase 实现 =============
    //输入逻辑默认无持久化状态，子类需要可重写
    public override void LoadFromEntry(ILogicStateEntry entry)
    {
        //默认空实现
    }

    public override ILogicStateEntry SaveToEntry()
    {
        //默认返回 null,子类需要可重写
        return null;
    }
}