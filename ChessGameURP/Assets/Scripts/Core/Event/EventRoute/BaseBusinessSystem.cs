using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseBusinessSystem
{
    //监听方系统标识
    //唯一系统标识，和路由表systemID完全一致
    public abstract string SystemID {  get; }
    //构造自动注册实例
    protected BaseBusinessSystem()
    {
        EventRouteRegistrar.RegisterSystemInstance(SystemID, this);
    }
    #region 封装注销
    //注销本系统 单个指定事件
    public void UnlistenSingleEvent<T>() where T : PackageEvent
    {
        EventRouteRegistrar.UnlistenSystemSingleEvent(SystemID, typeof(T));
    }
    //注销本系统 所有事件订阅
    public void UnlistenAllEvent()
    {
        EventRouteRegistrar.UnlistenSystemAll(SystemID);
    }
    #endregion
}
