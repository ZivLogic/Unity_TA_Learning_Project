using System;
using System.Collections.Generic;
using System.Diagnostics;

// 全局通用·万能数据包
[Serializable]
// 万能资源包
public class Package
{
    // 内部直接存一个字典
    public Dictionary<string, object> data = new();

    // 按名字存
    public void Put(string key, object content)
    {
        data[key] = content;
    }

    // 按名字取
    public bool Get<T>(string key, out T result)
    {
        if (data.TryGetValue(key, out var obj) && obj is T t)
        {
            result = t;
            return true;
        }
        result = default;
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
    public bool ValidsteAll()
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
                return false;
            }
        }
        return true;
    }
}
