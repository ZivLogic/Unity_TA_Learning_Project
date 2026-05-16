using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public abstract class BasePublishSystem
{
    //发布端系统ID
    public abstract string SystemID { get; }

    //系统自动化注册实例
    protected BasePublishSystem()
    {
        EventRouteRegistrar.RegisterSystemInstancePublish(SystemID, this);
    }
    private List<EventRouteCacheUnit> _publishRouteCache => EventRouteRegistrar._publishRouteCache;
    protected void AutoPublish(PackageEvent e)
    {
        if (e == null)
        {
            Debug.LogError($"[{SystemID}]事件为空：{e}");
            return;
        }
        Type evtType = e.GetType();
        //全局动态开关拦截（配置初始化 + 运行可改）
        if (EventRouteRegistrar.PublishEventDictIsEnable.TryGetValue(evtType, out var isEnable))
        {
            if (!isEnable)return;            
        }
        else
        {
            //没配置默认禁用
            return;
        }
        var cache = _publishRouteCache.FirstOrDefault(x => x.eventType == evtType);
        if (cache == null)
        {
            Debug.LogWarning($"[{SystemID}]未找到事件路由配置：{evtType.FullName}");
            return;
        }
        //父类统一发射 自动调用对应泛型Emit
        DoEmitByQueue(cache.queueType, e);
    }
    private void DoEmitByQueue(EventQueueType queueType, PackageEvent e)
    {
        var evtType = e.GetType();
        var em = EventManager.Instance;
        if (em == null)return;
        switch (queueType)
        {
            case EventQueueType.Logic:
                em.EmitLogic(e); break;
            case EventQueueType.Render:
                em.EmitRender(e); break;
            case EventQueueType.Physics:
                em.EmitPhysics(e); break;
            case EventQueueType.Audio:
                em.EmitAudio(e); break;
        }
    }
    //反射调用闭合泛型 调用EmitXXX<T>
    //private void EmitInvoke(EventManager em, string methodName, Type evtType, PackageEvent e)
    //{
    //    MethodInfo mi = typeof(EventManager).GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
    //    if (mi == null) return;
    //    var genericMi = mi.MakeGenericMethod(evtType);
    //    genericMi.Invoke(e, new object[] { e });
    //}
    #region 封装注销
    //注销本系统 单个指定事件
    public void UnEmitSingleEvent<T>() where T : PackageEvent
    {
        EventRouteRegistrar.UnEmitSystemSingleEvent(SystemID, typeof(T));
    }
    //注销本系统 所有事件发布
    public void UnEmitAllEvent()
    {
        EventRouteRegistrar.UnEmitSystemAll(SystemID);
    }
    #endregion
}
