using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;

public static class JsonUtils
{
    /// <summary>
    /// 从Resources加载字典结构的JSON，直接返回Dictionary<string, T>
    /// 完全适配你Python的 { "key":{...}, "key":{...} } 写法
    /// </summary>
    public static Dictionary<string, T> LoadDictFromResources<T>(string path)
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>(path);
        if (jsonAsset == null)
        {
            Debug.LogError($"[JsonUtils] 加载失败！找不到文件: {path}");
            return default;
        }
        return JsonConvert.DeserializeObject<Dictionary<string, T>>(jsonAsset.text);
    }
}