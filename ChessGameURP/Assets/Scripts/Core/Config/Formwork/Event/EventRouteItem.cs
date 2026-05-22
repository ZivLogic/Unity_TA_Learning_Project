using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EventRouteItem
{
    //[Tooltip("业务系统标识名，和系统基类SystemID对应")]     //属性提示窗口
    public string SystemID;
    //[Tooltip("事件类完整命名空间 + 类名 例如：IsInputConfig")]
    public string EventTypeFullName;
    //[Tooltip("系统内回调方法名，方法签名固定：void 方法名(PackageEvent e)")]
    public string HandlerMethodName;
    //[Tooltip("事件派发队列")]
    public string QueueType;
    //是否启用这条链路
    public bool IsEnable;
    //是否管控发布方 （拓展）
    //public bool ControlPublisher;
}
//[CreateAssetMenu(fileName = "EventRouteTable", menuName = "Event/事件路由表")]   //自动创建配置文件
//public class EventRouteTable : ScriptableObject
//{
//    public List<EventRouteItem> routeList = new List<EventRouteItem>();
//}
