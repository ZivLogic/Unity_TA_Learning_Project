using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void InputBizCallBack(InputKeyConfig cfg, InputKeyState state);

public static class InputBizRouter
{
    private static readonly Dictionary<InputAction, InputBizCallBack> _routeMap = new Dictionary<InputAction, InputBizCallBack>();

    //注册 输入行为枚举 => 业务回调
    public static void Register(InputAction action,  InputBizCallBack callback)
    {
        if (_routeMap.ContainsKey(action))
            _routeMap[action] = callback;
        else
            _routeMap.Add(action, callback);
        //Debug.Log("[InputBizRouter]注册输入业务成功");
    }
    //注销对应行为监听
    public static void UnRegister(InputAction action)
    {
        _routeMap.Remove(action);
    }
    //统一分发：根据枚举自动执行对应的业务方法
    public static void Dispatch(InputAction action, InputKeyConfig cfg, InputKeyState state)
    {
        if (_routeMap.TryGetValue(action, out var cb))
        {
            cb?.Invoke(cfg, state);
        }
    }
    //清空所有注册
    public static void Clear()
    {
        _routeMap.Clear();
    }
}
