using System;
using System.Collections.Generic;
using UnityEngine;
using EventSystemV2;

public abstract class EventEntryBase : LogicScriptBase
{
    private readonly Dictionary<string, Action<PackageEvent>> _table = new Dictionary<string, Action<PackageEvent>>();
    public override string ContainerId => "event";

    // 由工厂在 Entity 注入后调用
    public void Initialize()
    {
        RegisterEvents(); // 触发 partial 生成的注册
        if (Entity != null && !string.IsNullOrEmpty(Entity.UUID))
            EventManager.Instance?.RegisterTarget(Entity.UUID, this);
        //Debug.Log($"注册事件入口成功！UUID = {Entity.UUID}");
    }

    // 由工厂在销毁前调用
    public void Shutdown()
    {
        if (Entity != null && !string.IsNullOrEmpty(Entity.UUID))
            EventManager.Instance?.UnregisterTarget(Entity.UUID);
    }

    // 【关键】改成 virtual 空实现，让 partial 生成代码 override
    protected virtual void RegisterEvents() { }

    protected void RegisterMethod(string methodName, Action<PackageEvent> handler)
    {
        if (string.IsNullOrEmpty(methodName) || handler == null) return;
        _table[$"{GetType().Name}.{methodName}"] = handler;
    }

    public void Receive(string key, PackageEvent evt)
    {
        if (string.IsNullOrEmpty(key) || evt == null)
        {
            Debug.LogWarning("接收方法名为空或事件不存在！");
            return;
        }
        if (_table.TryGetValue(key, out var handler))
        {
            //Debug.Log("尝试触发接收逻辑");
            try { handler.Invoke(evt); }
            catch (Exception e) { Debug.LogError($"[EventEntryBase] {GetType().Name} 处理 {key} 异常：{e}"); }
        }
    }

    protected void Emit(PackageEvent evt) => EventUtil.EmitEvent(evt);

    public override void LoadFromEntry(ILogicStateEntry entry) { }
    public override ILogicStateEntry SaveToEntry() => null;
}