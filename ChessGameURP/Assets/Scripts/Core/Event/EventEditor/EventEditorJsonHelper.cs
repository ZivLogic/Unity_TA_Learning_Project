using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using UnityEditor;

public static class EventEditorJsonHelper
{
    //配置存储路径
    public const string JsonSavePath = "Assets/Resources/Config/Json/Event/EventConfig.json";
    //传入内存三份列表，自动打包成标准结构
    public static void SaveConfig(List<EventDefine> evtDefList, List<EventFieldMapping> mapList, List <EventOperateLog> logList, List<SystemIdItem> sysList)
    {
        //拼装成标准ROOT模型
        EventRootCfg root = new EventRootCfg
        {
            EventDefine = new EventDefineWrap() { Items = evtDefList },
            FieldMapping = new FieldMappingWrap() { Items = mapList },
            OperateLog = new LogWrap() { Items = logList },
            SystemIdList = new SystemIdWrap() { Items = sysList }
        };
        //序列化缩进JSON
        string json = JsonConvert.SerializeObject(root, Formatting.Indented);
        File.WriteAllText(JsonSavePath, json);
        AssetDatabase.Refresh();
    }
    //从本地JSON读会内存（编辑器打开窗口加载）
    public static EventRootCfg LoadConfig()
    {
        if (!File.Exists(JsonSavePath)) return new EventRootCfg
        {
            EventDefine = new EventDefineWrap(),
            FieldMapping = new FieldMappingWrap(),
            OperateLog = new LogWrap(),
            SystemIdList = new SystemIdWrap()
        };
        string json = File.ReadAllText(JsonSavePath);
        return JsonConvert.DeserializeObject<EventRootCfg>(json);
    }
}