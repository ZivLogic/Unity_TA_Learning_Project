using System;
using System.Collections.Generic;
using UnityEngine;


namespace EventSystemV2
{
    // 全局通用·万能数据包
    [Serializable]
    // 万能资源包
    public class Package
    {
        // 内部直接存一个字典
        public Dictionary<string, object> data = new();

        //字段映射列表
        public List<string> KeyList = new();

        // 按名字存
        public void Put(string key, object content)
        {
            data[key] = content;
            KeyList.Add(key);
        }

        // 按名字取
        public bool Get<T>(string key, out T result)
        {
            result = default;
            if (!data.TryGetValue(key, out var obj))
                return false;
            if (obj is T)
            {
                result = (T)obj;
                return true;
            }
            if (obj is UnityEngine.Object unityObj && typeof(T).IsAssignableFrom(unityObj.GetType()))
            {
                result = (T)(object)unityObj;
                return true;
            }
            return false;
        }

        //不抛异常版本
        public T GetSafe<T>(string key, T defaultValue = default)
        {
            return Get<T>(key, out var value) ? value : defaultValue;
        }

        // 快速判断有没有
        public bool Has(string key) => data.ContainsKey(key);

        //验证整个包，如果任何一个值为null，返回false
        public bool ValidateAll()
        {
            foreach (var kv in data)
            {
                if (kv.Value == null)
                {
                    return false;
                }
            }
            return true;
        }

        //验证指定字段列表是否都存在且不为null
        public bool ValidateFields(params string[] keys)
        {
            foreach (var key in keys)
            {
                if (!data.TryGetValue(key, out var value) || value == null)
                {
                    Debug.LogError($"打包检验：{key}为空！");
                    return false;
                }
            }
            return true;
        }
    }
}
