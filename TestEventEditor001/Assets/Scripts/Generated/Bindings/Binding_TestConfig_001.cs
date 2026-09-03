using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;
using EventSystemV2;

public static class Binding_TestConfig_001
{
     private static bool _initialized = false;

    public static void Initialize()
     {
         if (_initialized)return;
             _initialized = true;

        //绑定监听：TestListen.TestConfigData
        var listener_0 = BusinessInstanceManager.Get("TestListen");
        if (listener_0 == null)
        {
            Debug.LogWarning($"监听实例TestListen未注册");
            return;
        }

        Action<E_TestConfig_001> handler_0 = (evt) => 
         {
             try
             {
                var method = listener_0.GetType().GetMethod("TestConfigData");
                method?.Invoke(listener_0, null);
             }
             catch (Exception e)
             {
                Debug.LogError($"执行监听TestConfigData失败：{e.Message}");
             }
         };
        EventManager.Instance.Listen<E_TestConfig_001>(handler_0);

     }
}
