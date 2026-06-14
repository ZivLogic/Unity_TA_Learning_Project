using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using System;


namespace EventSystemV2
{
    //事件编辑器反射工具方法
    //遍历查找场景内所有带标记的方法

    //参数拆分结果-发布方法
    public sealed class PublishParamSplit
    {
        public List<ParameterInfo> InArgs = new();   //普通入参，外部传入
        public List<ParameterInfo> OutArgs = new();  //out入参, 不参与映射
    }

    //参数拆分结果-接收方法
    public sealed class ListenParamSplit
    {
        public List<ParameterInfo> InjectArgs = new();    //可从包注入参数
        public List<ParameterInfo> OutArgs = new();       //out输出参数，不参与映射
    }

    public static class EventReflectTool
    {
        //缓存全工程发布，监听方法，避免频繁反射
        private static List<MethodInfo> _cachePublish;
        private static List<MethodInfo> _cacheListen;
        //刷新全反射
        public static void RefreshAllMethod()
        {
            _cachePublish = new List<MethodInfo>();
            _cacheListen = new List<MethodInfo>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in assemblies)
            {
                foreach (Type type in asm.GetTypes())
                {
                    var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    foreach (var m in methods)
                    {
                        //获取发布标记业务方法
                        if (m.GetCustomAttribute<EventPublishAttr>() != null)
                            _cachePublish.Add(m);
                        //获取监听标记业务方法
                        if (m.GetCustomAttribute<EventListenAttr>() != null)
                            _cacheListen.Add(m);
                    }
                }
            }
        }
        public static List<MethodInfo> GetAllPublish()
        {
            if (_cachePublish == null)
                RefreshAllMethod();
            return _cachePublish;
        }
        public static List<MethodInfo> GetAllListen()
        {
            if (_cacheListen == null)
                RefreshAllMethod();
            return _cacheListen;
        }
        //拆分发布方法参数
        public static PublishParamSplit SplitPublishParam(MethodInfo method)
        {
            PublishParamSplit res = new();
            foreach (var p in method.GetParameters())
            {
                if (p.IsOut)
                    //是out
                    res.OutArgs.Add(p);
                else
                    //是in
                    res.InArgs.Add(p);
            }
            return res;
        }
        //拆分监听方法参数
        public static ListenParamSplit SplitListenParam(MethodInfo method)
        {
            ListenParamSplit res = new();
            foreach (var p in method.GetParameters())
            {
                if (p.IsOut || p.ParameterType.IsByRef)
                    res.OutArgs.Add(p);
                else
                    res.InjectArgs.Add(p);
            }
            return res;
        }
        //去除ref&out， 获取原生类型
        public static Type GetRawType(Type type)
        {
            if (type.IsByRef) return type.GetElementType();
            return type;
        }
        //匹配校验字段
        public static bool CheckTypeMatch(Type pkgType, Type paramType)
        {
            var t1 = GetRawType(pkgType);
            var t2 = GetRawType(paramType);
            return t1 == t2;
        }
    }
}