using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EventSystemV2
{
    //实例初始化注册器
    public static class BusinessInstanceManager
    {
        private static Dictionary<string, object> _instances = new();
        private static Dictionary<Type, object> _typeInstances = new();

        //注册业务实例（通过类名）
        public static void Register(string className, object instance)
        {
            if (!_instances.ContainsKey(className))
                _instances[className] = instance;
            if (!_typeInstances.ContainsKey(instance.GetType()))
                _typeInstances[instance.GetType()] = instance;
        }
        //获取业务实例
        public static object Get(string className)
        {
            //确保注册先正确初始化，避免没注册就记录事件
            //EventRuntimeInitializer.Initialize();
            _instances.TryGetValue(className, out var instance);
            return instance;
        }


        public static T Get<T>() where T : class
        {
            _typeInstances.TryGetValue(typeof(T), out var instance);
            return instance as T;
        }

        //检查是否已被注册
        public static bool HasInstance(string className)
        {
            return _instances.ContainsKey(className);
        }

        //获取所有实例（用于初始化绑定）
        public static IEnumerable<object> GetAllInstances()
        {
            return _typeInstances.Values;
        }
    }
}

