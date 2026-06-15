using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;
using EventSystemV2;

public static class Binding_EventBoolTest002
{
     private static bool _initialized = false;

    public static void Initialize()
     {
         if (_initialized)return;
             _initialized = true;

        //绑定监听：TestListen.TestEvent002
        var listener_0 = BusinessInstanceManager.Get("TestListen");
        if (listener_0 == null)
        {
            Debug.LogWarning($"监听实例TestListen未注册");
            return;
        }

        Action<E_EventBoolTest002> handler_0 = (evt) => 
         {
             try
             {
                bool p_Bool;
                if ( ! evt.Package.Get<bool>("TestBool", out p_Bool))
                {
                    Debug.LogError($"获取参数Bool失败，KEY:TestBool");
                    return;
                }

                var method = listener_0.GetType().GetMethod("TestEvent002");
                method?.Invoke(listener_0, new object[] {p_Bool});
             }
             catch (Exception e)
             {
                Debug.LogError($"执行监听TestEvent002失败：{e.Message}");
             }
         };
        EventManager.Instance.Listen<E_EventBoolTest002>(handler_0);

     }
}
