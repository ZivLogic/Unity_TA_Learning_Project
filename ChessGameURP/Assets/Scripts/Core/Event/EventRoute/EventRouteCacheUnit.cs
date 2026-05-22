using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class EventRouteCacheUnit
{
    //自定义SystemID
    public string systemID;
    //系统实例
    public object systemInstance;
    //事件类型
    public Type eventType;
    //绑定的委托回调
    public Delegate handlerDelegate;
    //队列类型
    public EventQueueType queueType;
    //是否启用
    public bool isEnable;
    //对应方法
    public MethodInfo publishMethod;
}