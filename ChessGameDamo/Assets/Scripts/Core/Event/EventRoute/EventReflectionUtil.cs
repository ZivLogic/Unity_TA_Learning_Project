using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class EventReflectionUtil
{
    #region  订阅 注销
    //反射调用EventManager.Listen<T>(callback)
    public static void CallListen(Type eventType, Action<PackageEvent> callback)
    {
        MethodInfo listenMethod = typeof(EventManager)
            .GetMethod("Listen", BindingFlags.Public | BindingFlags.Instance);
        //构造泛型方法
        MethodInfo genericMethod = listenMethod.MakeGenericMethod(eventType);
        //执行
        genericMethod.Invoke(EventManager.Instance, new object[] { callback });
    }
    //反射调用EventManager.UnListen<T>(callback)
    public static void CallUnListen(Type eventType, Delegate callback)
    {
        //关键保护：编辑器退出/对象已销毁时直接返回，不执行
        if (EventManager.Instance == null || callback == null) 
            return;
        MethodInfo listenMethod = typeof(EventManager)
            .GetMethod("UnListen", BindingFlags.Public | BindingFlags.Instance);
        //构造泛型方法
        MethodInfo genericMethod = listenMethod.MakeGenericMethod(eventType);
        //执行
        genericMethod.Invoke(EventManager.Instance, new object[] { callback });
    }
    #endregion
    #region  发射重载：传Type自动创建实例 + 自动泛型调用
    //反射调用EventManager.EmitLogic(callback)
    public static void CallEmitLogic(Type eventType)
    {
        if (!typeof(PackageEvent).IsAssignableFrom(eventType))
        {
            Debug.LogError($"[EventReflectionUtil]非法事件类型：{eventType.FullName}");
            return;
        }
        //自动创建空实例
        PackageEvent evt = Activator.CreateInstance(eventType) as PackageEvent;
        if (evt == null) return;
        //拿到泛型EmitLogic<T>
        MethodInfo method = typeof(EventManager)
            .GetMethod("EmitLogic", BindingFlags.Public | BindingFlags.Instance);
        if (method == null)
        { Debug.LogWarning($"[EventReflectionUtil]找不到对应方法：{method}"); return; }
        //Debug.Log($"[EventReflectionUtil]找到对应方法：{listenMethod}");
        //用真实事件类型闭合泛型
        MethodInfo genericMethod = method.MakeGenericMethod(eventType);
        //执行
        Debug.Log($"[EventReflectionUtil]evt是否为空；{evt == null}, 类型：{evt?.GetType()}");
        //Debug.Log($"[EventReflectionUtil]EventManager是否为空：{EventManager.Instance == null}");
        genericMethod.Invoke(EventManager.Instance, new object[] { evt });
    }
    //反射调用EventManager.EmitRender(callback)
    public static void CallEmitRender(Type eventType)
    {
        if (!typeof(PackageEvent).IsAssignableFrom(eventType))
        {
            Debug.LogError($"[EventReflectionUtil]非法事件类型：{eventType.FullName}");
            return;
        }
        //自动创建空实例
        PackageEvent evt = Activator.CreateInstance(eventType) as PackageEvent;
        if (evt == null) return;
        MethodInfo method = typeof(EventManager)
            .GetMethod("EmitRender", BindingFlags.Public | BindingFlags.Instance);
        if (method == null)
        { Debug.LogWarning($"[EventReflectionUtil]找不到对应方法：{method}"); return; }
        MethodInfo genericMethod = method.MakeGenericMethod(eventType);
        //执行
        genericMethod.Invoke(EventManager.Instance, new object[] { evt });
    }
    //反射调用EventManager.EmitPhysics(callback)
    public static void CallEmitPhysics(Type eventType)
    {
        if (!typeof(PackageEvent).IsAssignableFrom(eventType))
        {
            Debug.LogError($"[EventReflectionUtil]非法事件类型：{eventType.FullName}");
            return;
        }
        //自动创建空实例
        PackageEvent evt = Activator.CreateInstance(eventType) as PackageEvent;
        if (evt == null) return;
        MethodInfo method = typeof(EventManager)
            .GetMethod("EmitPhysics", BindingFlags.Public | BindingFlags.Instance);
        if (method == null)
        { Debug.LogWarning($"[EventReflectionUtil]找不到对应方法：{method}"); return; }
        MethodInfo genericMethod = method.MakeGenericMethod(eventType);
        //执行
        genericMethod.Invoke(EventManager.Instance, new object[] { evt });
    }
    //反射调用EventManager.EmitAudio(callback)
    public static void CallEmitAudio(Type eventType)
    {
        if (!typeof(PackageEvent).IsAssignableFrom(eventType))
        {
            Debug.LogError($"[EventReflectionUtil]非法事件类型：{eventType.FullName}");
            return;
        }
        //自动创建空实例
        PackageEvent evt = Activator.CreateInstance(eventType) as PackageEvent;
        if (evt == null) return;
        MethodInfo method = typeof(EventManager)
            .GetMethod("EmitAudio", BindingFlags.Public | BindingFlags.Instance);
        if (method == null)
        { Debug.LogWarning($"[EventReflectionUtil]找不到对应方法：{method}"); return; }
        MethodInfo genericMethod = method.MakeGenericMethod(eventType);
        //执行
        genericMethod.Invoke(EventManager.Instance, new object[] { evt });
    }
    #endregion
}