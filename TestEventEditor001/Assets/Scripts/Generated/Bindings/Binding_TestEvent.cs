using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;
using EventSystemV2;

public static class Binding_TestEvent
{
     private static bool _initialized = false;

    public static void Initialize()
     {
         if (_initialized)return;
             _initialized = true;

        //绑定监听：TestListen.TestEventListen
        var listener_0 = BusinessInstanceManager.Get("TestListen");
        if (listener_0 == null)
        {
            Debug.LogWarning($"监听实例TestListen未注册");
            return;
        }

        Action<E_TestEvent> handler_0 = (evt) => 
         {
             try
             {
                int p_value;
                if ( ! evt.Package.Get<int>("Value", out p_value))
                {
                    Debug.LogError($"获取参数value失败，KEY:Value");
                    return;
                }
                string p_massage;
                if ( ! evt.Package.Get<string>("Message", out p_massage))
                {
                    Debug.LogError($"获取参数massage失败，KEY:Message");
                    return;
                }

                var method = listener_0.GetType().GetMethod("TestEventListen");
                method?.Invoke(listener_0, new object[] {p_value,p_massage});
             }
             catch (Exception e)
             {
                Debug.LogError($"执行监听TestEventListen失败：{e.Message}");
             }
         };
        EventManager.Instance.Listen<E_TestEvent>(handler_0);

     }
}
