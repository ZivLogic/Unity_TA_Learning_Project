using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;
using EventSystemV2;

public static class Binding_EventClass002
{
     private static bool _initialized = false;

    public static void Initialize()
     {
         if (_initialized)return;
             _initialized = true;

        //绑定监听：TestListen.TestEventlISTEN_CLASS_003
        var listener_0 = BusinessInstanceManager.Get("TestListen");
        if (listener_0 == null)
        {
            Debug.LogWarning($"监听实例TestListen未注册");
            return;
        }

        Action<E_EventClass002> handler_0 = (evt) => 
         {
             try
             {
                EventSystemV2.Test_CONFIG p_CFG;
                if ( ! evt.Package.Get<EventSystemV2.Test_CONFIG>("Test_cfg", out p_CFG))
                {
                    Debug.LogError($"获取参数CFG失败，KEY:Test_cfg");
                    return;
                }

                var method = listener_0.GetType().GetMethod("TestEventlISTEN_CLASS_003");
                method?.Invoke(listener_0, new object[] {p_CFG});
             }
             catch (Exception e)
             {
                Debug.LogError($"执行监听TestEventlISTEN_CLASS_003失败：{e.Message}");
             }
         };
        EventManager.Instance.Listen<E_EventClass002>(handler_0);

     }
}
