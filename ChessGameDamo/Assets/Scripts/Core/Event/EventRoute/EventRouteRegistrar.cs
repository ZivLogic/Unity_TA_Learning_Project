using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

public class EventRouteRegistrar
{
    //全局路由缓存【监听方】
    public static readonly List<EventRouteCacheUnit> _globalRouteCache = new List<EventRouteCacheUnit>();
    //全局路由缓存【发布方】
    public static readonly List<EventRouteCacheUnit> _publishRouteCache = new List<EventRouteCacheUnit>();
    //系统实例缓存【监听方】
    private static readonly Dictionary<string, object> _systemInstanceDict = new Dictionary<string, object>();
    //系统实例缓存【发布方】
    private static readonly Dictionary<string, object> _systemInstancePublishDict = new Dictionary<string, object>();
    //全局事件发布字典启用表
    public static Dictionary<Type, bool> PublishEventDictIsEnable = new Dictionary<Type, bool>();
    
    #region  初始化注册
    //初始化注册，直接调用ConfigManager加载JSON事件路由表
    public static void InitFromConfigManager()
    {
        //加载整张字典表
        //监听方
        var routeDict = ConfigManager.Instance.GetAllConfigsInTable<EventRouteItem>("EventRoute");
        //发布方
        //var routePublishDict = ConfigManager.Instance.GetAllConfigsInTable<EventTriggerRouteItem>("EventTriggerRoute");
        if (routeDict == null || routeDict.Count == 0)
        {
            Debug.LogError("[EventRouteRegistrar]从配置系统加载路由表失败或为空,监听方配置失效");
            return;
        }
        //if (routePublishDict == null || routePublishDict.Count == 0)
        //{
        //    Debug.LogError("[EventRouteRegistrar]从配置系统加载路由表失败或为空,发布方配置失效");
        //    return;
        //}
        //遍历每一条路由配置，逐条注册
        //监听方
        foreach (var kv in routeDict)
        {
            //配置不启用时，直接不加载
            if (kv.Value.IsEnable == false)
            {
                Debug.Log($"配置{kv.Value}不启用");
                continue;
            }
            var item = kv.Value;
            RegisterSingleRoute(item);
        }
        //发布方
        //foreach (var kv in routePublishDict)
        //{
        //    //配置不启用时，直接不加载
        //    //if (kv.Value.IsEnable == false)
        //    //{ continue; }
        //    var item = kv.Value;
        //    RegisterSingleRoutePublish(item);
        //}
        Debug.Log("[EventRouteRegistrar]监听方从JSON配置自动注册完成");
    }
    public static void InitFromConfigManagerPublish()
    {
        //发布方
        var routePublishDict = ConfigManager.Instance.GetAllConfigsInTable<EventTriggerRouteItem>("EventTriggerRoute");
        if (routePublishDict == null || routePublishDict.Count == 0)
        {
            Debug.LogError("[EventRouteRegistrar]从配置系统加载路由表失败或为空,发布方配置失效");
            return;
        }
        //发布方
        foreach (var kv in routePublishDict)
        {
            //配置不启用时，直接不加载
            if (kv.Value.IsEnable == false)
            { continue; }
            var item = kv.Value;
            RegisterSingleRoutePublish(item);
        }
        Debug.Log("[EventRouteRegistrar]发布方从JSON配置自动注册完成");
    }
    //单条注册逻辑
    private static void RegisterSingleRoute(EventRouteItem item)
    {
        //获取系统实例
        if (!_systemInstanceDict.TryGetValue(item.SystemID, out var systemObj))
        {
            Debug.LogError($"[EventRouteRegistrar]未注册系统实例：{item.SystemID}");
            return;
        }
        //解析事件Type
        Type eventType = Type.GetType(item.EventTypeFullName);
        if (eventType == null || ! typeof(PackageEvent).IsAssignableFrom(eventType))
        {
            Debug.LogError($"[EventRouteRegistrar]事件类型解析失败：{item.EventTypeFullName}");
            return;
        }
        //找回调方法
        MethodInfo method = systemObj.GetType().GetMethod(item.HandlerMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (method == null )
        {
            Debug.LogError($"[EventRouteRegistrar]系统{item.SystemID}无方法：{item.HandlerMethodName}");
            return;
        }
        //封装委托
        Action<PackageEvent> handler = (e) => method.Invoke(systemObj, new object[] { e });
        //动态创建匹配事件类型的委托，解决签名不匹配
        //Type delegateType = typeof(Action<>).MakeGenericType(eventType);
        //Delegate handler = Delegate.CreateDelegate(delegateType, systemObj, method);

        //反射调用Listen注册
        EventReflectionUtil.CallListen(eventType, handler);
        //解析队列枚举
        if (Enum.TryParse<EventQueueType>(item.QueueType, out var queueType) == false )
            queueType = EventQueueType.Logic;
        //存入缓存用于注销
        _globalRouteCache.Add(new EventRouteCacheUnit
        {
            systemID = item.SystemID,
            systemInstance = systemObj,
            eventType = eventType,
            handlerDelegate = handler,
            queueType = queueType,
            publishMethod = method
        });
    }
    //发布方事件路由表加载
    private static void RegisterSingleRoutePublish(EventTriggerRouteItem item)
    {
        //获取系统实例
        if (!_systemInstancePublishDict.TryGetValue(item.SystemID, out var systemObj))
        {
            Debug.LogError($"[EventRouteRegistrar]未注册系统实例：{item.SystemID}");
            return;
        }
        //解析事件Type
        Type eventType = Type.GetType(item.EventTypeFullName);
        if (eventType == null || !typeof(PackageEvent).IsAssignableFrom(eventType))
        {
            Debug.LogError($"[EventRouteRegistrar]事件类型解析失败：{item.EventTypeFullName}");
            return;
        }

        //找回调方法
        MethodInfo method = systemObj.GetType().GetMethod(item.HandlerMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (method == null)
        {
            Debug.LogError($"[EventRouteRegistrar]系统{item.SystemID}无方法：{item.HandlerMethodName}");
            return;
        }
        //封装委托
        Action<PackageEvent> handler = (e) => method.Invoke(systemObj, new object[] { e });
        
        //解析队列枚举
        if (Enum.TryParse<EventQueueType>(item.QueueType, out var queueType) == false)
            queueType = EventQueueType.Logic;
        PublishEventDictIsEnable[eventType] = item.IsEnable;
        //反射调用
        ////逻辑事件
        //if (queueType == EventQueueType.Logic)
        //    EventReflectionUtil.CallEmitLogic(eventType);
        ////渲染事件
        //else if (queueType == EventQueueType.Render)
        //    EventReflectionUtil.CallEmitRender(eventType);
        ////物理事件
        //else if (queueType == EventQueueType.Physics)
        //    EventReflectionUtil.CallEmitPhysics(eventType);
        ////音频事件
        //else if (queueType == EventQueueType.Audio)
        //    EventReflectionUtil.CallEmitAudio(eventType);
        //else
        //{ Debug.LogError($"[EventRouteRegistrar]不是规定的系统队列！队列类型：{queueType}"); return; }

        //存入缓存用于注销
        _publishRouteCache.Add(new EventRouteCacheUnit
        {
            systemID = item.SystemID,
            systemInstance = systemObj,
            eventType = eventType,
            handlerDelegate = handler,
            queueType = queueType,
            publishMethod = method
        });
        //发布方事件启用注销表
        PublishEventDictIsEnable[eventType] = item.IsEnable;
    }
    #endregion
    #region  注册系统实例（业务系统启动时调用）
    //注册监听方事件关系
    public static void RegisterSystemInstance(string systemID, object systemObj)
    {
        if (! _systemInstanceDict.ContainsKey(systemID))
            _systemInstanceDict.Add(systemID, systemObj);
    }
    //注册发布方订阅方式
    public static void RegisterSystemInstancePublish(string systemID, object systemObj)
    {
        if ( ! _systemInstancePublishDict.ContainsKey(systemID))
            _systemInstancePublishDict.Add(systemID, systemObj);
    }
    #endregion
    #region  三级注销方法
    //注销某个系统的某个订阅事件
    public static void UnlistenSystemSingleEvent(string systemID, Type eventType)
    {
        var list = _globalRouteCache
            .Where(c => c.systemID == systemID && c.eventType == eventType)
            .ToList();
        foreach (var cache in list)
        {
            // 已经被销毁就跳过
            if (cache?.handlerDelegate == null || cache.systemInstance == null)
                continue;
            EventReflectionUtil.CallUnListen(cache.eventType, cache.handlerDelegate);
            _globalRouteCache.Remove(cache);
        }
    }
    //注销某个系统的所有订阅事件
    public static void UnlistenSystemAll(string systemID)
    {
        var list = _globalRouteCache
            .Where(c => c.systemID == systemID)
            .ToList();
        foreach(var cache in list)
        {
            // 已经被销毁就跳过
            if (cache?.handlerDelegate == null || cache.systemInstance == null)
                continue;
            EventReflectionUtil.CallUnListen(cache.eventType, cache.handlerDelegate);
            _globalRouteCache.Remove(cache);
        }
    }
    //注销一个全局监听事件
    public static void GlobalUnlistenEvent(Type eventType)
    {
        var list = _globalRouteCache
            .Where(c => c.eventType == eventType)
            .ToList();
        foreach (var cache in list)
        {
            // 已经被销毁就跳过
            if (cache?.handlerDelegate == null || cache.systemInstance == null)
                continue;
            EventReflectionUtil.CallUnListen(cache.eventType, cache.handlerDelegate);
            _globalRouteCache.Remove(cache);
        }
    }
    //注销所有监听事件
    public static void UnlistenAll()
    {
        foreach (var cache in _globalRouteCache)
        {
            // 已经被销毁就跳过
            if (cache?.handlerDelegate == null || cache.systemInstance == null)
                continue;
            EventReflectionUtil.CallUnListen(cache.eventType, cache.handlerDelegate);
            
        }
        _globalRouteCache.Clear();
        _systemInstanceDict.Clear();
    }
    //注销指定系统的指定发布事件
    public static void UnEmitSystemSingleEvent(string systemID, Type eventType)
    {
        var list = _publishRouteCache
            .Where(c => c.systemID == systemID && c.eventType == eventType)
            .ToList();
        foreach (var cache in list)
        {
            // 已经被销毁就跳过
            if (cache?.handlerDelegate == null || cache.systemInstance == null)
                continue;
            PublishEventDictIsEnable[cache.eventType] = false;
            _publishRouteCache.Remove(cache);
        }
    }
    //注销指定系统所有发布事件
    public static void UnEmitSystemAll(string systemID)
    {
        var list = _publishRouteCache
            .Where(c => c.systemID == systemID)
            .ToList();
        foreach (var cache in list)
        {
            // 已经被销毁就跳过
            if (cache?.handlerDelegate == null || cache.systemInstance == null)
                continue;
            PublishEventDictIsEnable[cache.eventType] = false;
            _publishRouteCache.Remove(cache);
        }
    }
    //注销一个全局发布事件
    public static void GlobalUnEmitEvent(Type eventType)
    {
        var list = _publishRouteCache
            .Where(c => c.eventType == eventType)
            .ToList();
        foreach (var cache in list)
        {
            // 已经被销毁就跳过
            if (cache?.handlerDelegate == null || cache.systemInstance == null)
                continue;
            PublishEventDictIsEnable[cache.eventType] = false;
            _publishRouteCache.Remove(cache);
        }
    }
    //注销所有发布事件
    public static void UnEmitAll()
    {
        foreach (var cache in _publishRouteCache)
        {
            // 已经被销毁就跳过
            if (cache?.handlerDelegate == null || cache.systemInstance == null)
                continue;
            PublishEventDictIsEnable[cache.eventType] = false;
            
        }
        _publishRouteCache.Clear();
        _systemInstancePublishDict.Clear();
    }

    #endregion
    //发布字典安全查询
    public static bool IsEventCanPublish(PackageEvent e)
    {
        if (e == null) return false;
        Type evt = e.GetType();
        if (PublishEventDictIsEnable.TryGetValue(evt, out var enable))
            return enable;
        //没配置过默认允许发布
        return true;
    }
}
