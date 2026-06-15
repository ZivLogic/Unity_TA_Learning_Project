using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;
using EventSystemV2;

public static class Binding_TestGameObj
{
     private static bool _initialized = false;

    public static void Initialize()
     {
         if (_initialized)return;
             _initialized = true;

        //绑定监听：TestListen.TestLisenGameObj
        var listener_0 = BusinessInstanceManager.Get("TestListen");
        if (listener_0 == null)
        {
            Debug.LogWarning($"监听实例TestListen未注册");
            return;
        }

        Action<E_TestGameObj> handler_0 = (evt) => 
         {
             try
             {
                UnityEngine.GameObject p_Obj;
                if ( ! evt.Package.Get<UnityEngine.GameObject>("Pawn", out p_Obj))
                {
                    Debug.LogError($"获取参数Obj失败，KEY:Pawn");
                    return;
                }

                var method = listener_0.GetType().GetMethod("TestLisenGameObj");
                method?.Invoke(listener_0, new object[] {p_Obj});
             }
             catch (Exception e)
             {
                Debug.LogError($"执行监听TestLisenGameObj失败：{e.Message}");
             }
         };
        EventManager.Instance.Listen<E_TestGameObj>(handler_0);

     }
}
