using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class EventEditorJsonHelper
{
    //配置存储路径
    public const string JsonSavePath = "Assets/Resources/Config/Json/Event/EventConfig.json";

    //传入内存四份列表，自动打包成标准结构落地JSON
    public static void SaveConfig(List<EventDefine> evtDefList, List<EventFieldMapping> mapList, List<EventOperateLog> logList, List<SystemIdItem> sysList)
    {
        //拼装成标准ROOT模型
        EventRootCfg root = new EventRootCfg
        {
            EventDefine = new EventDefineWrap() { Items = evtDefList },
            FieldMapping = new FieldMappingWrap() { Items = mapList },
            OperateLog = new LogWrap() { Items = logList },
            SystemIdList = new SystemIdWrap() { Items = sysList }
        };
        string json = ToJson(root);
        File.WriteAllText(JsonSavePath, json);
        AssetDatabase.Refresh();
    }

    //从本地JSON读回内存（编辑器打开窗口加载）
    public static EventRootCfg LoadConfig()
    {
        try
        {
            if (!File.Exists(JsonSavePath))
            {
                return new EventRootCfg
                {
                    EventDefine = new EventDefineWrap(),
                    FieldMapping = new FieldMappingWrap(),
                    OperateLog = new LogWrap(),
                    SystemIdList = new SystemIdWrap()
                };
            }
            string json = File.ReadAllText(JsonSavePath);
            return FromJson(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"【事件配置读取异常】{ex.Message}，自动初始化空配置");
            return new EventRootCfg
            {
                EventDefine = new EventDefineWrap(),
                FieldMapping = new FieldMappingWrap(),
                OperateLog = new LogWrap(),
                SystemIdList = new SystemIdWrap()
            };
        }
    }

    //【快照深拷贝专用：对象转Json字符串】
    public static string ToJson(EventRootCfg root)
    {
        return JsonConvert.SerializeObject(root, Formatting.Indented);
    }

    //【快照深拷贝专用：Json字符串反序列全新对象】
    public static EventRootCfg FromJson(string jsonStr)
    {
        return JsonConvert.DeserializeObject<EventRootCfg>(jsonStr);
    }
}