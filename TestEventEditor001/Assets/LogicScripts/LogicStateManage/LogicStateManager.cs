using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;


//逻辑状态管理器：统一保存/加载所有逻辑状态

//保存流程：
//1.读取顶级注册表
//2.遍历每个父容器
//3.收集所有脚本实例的数据
//4.写入对应的父容器JSON

//加载流程：
//1.读取顶级注册表
//2.遍历每个父容器
//3.遍历父容器种的子容器
//4.通过UUID查找实体
//5.调用脚本的LoadFromEntry

public class LogicStateManager : MonoBehaviour
{
    private static LogicStateManager _instance;
    public static LogicStateManager Instance => _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        Debug.Log("[LogicStateManager] 初始化完成");
    }

    #region 保存
    public void SaveAllStates()
    {
        Debug.Log("[LogicStateManager] ========= 开始保存所有逻辑状态 =========");

        //1.读取顶级注册表
        var registry = ConfigAPI.Get<LogicStateRegistry>();
        if (registry?.entries == null)
        {
            Debug.LogWarning("[LogicStateManager]注册表为空");
            return;
        }

        //遍历每个父容器
        foreach (var entry in registry.entries)
        {
            SaveContainer(entry);
        }

        Debug.Log("[LogicStateManager] ======== 保存完成 =========");
    }

    //保存单个父容器
    private void SaveContainer(LogicStateRegistry.LogicStateEntry entry)
    {
        //1.获取脚本类型
        var scriptType = FindScriptType(entry.scriptName);
        if (scriptType == null)
        {
            Debug.LogError($"[LogicStateManager] 找不到脚本类型：{entry.scriptName}");
            return;
        }

        //2.查找所有该类型的脚本实例
        var scripts = FindAllScripts(scriptType);
        if (scripts.Count == 0)
        {
            Debug.Log($"[LogicStateManager]没有找到{entry.scriptName} 实例");
            return;
        }

        //3，收集数据
        var entries = new List<object>();
        foreach (var script in scripts)
        {
            if (script is LogicScriptBase logicBase)
            {
                var data = logicBase.SaveToEntry();
                if (data != null)
                {
                    entries.Add(data);
                }
            }
        }

        //写入JSON
        WriteContainerToJson(entry, entries);

        Debug.Log($"[LogicStateManager] 保存{entry.scriptName}: {entries.Count}条数据");
    }


    //对象转字典（通用方法）
    private Dictionary<string, object> ConvertToDict(object obj)
    {
        var dict = new Dictionary<string, object>();
        var type = obj.GetType();

        foreach (var field in type.GetFields())
        {
            var value = field.GetValue(obj);
            if (value == null)continue;

            //处理 Vector2Int / Vector3 等
            if (value is Vector2Int v2i)
            {
                dict[field.Name] = new Dictionary<string, object>
                {
                    {"x", v2i.x},
                    {"y", v2i.y}
                };
            }
            else if (value is Vector3 v3i)
            {
                dict[field.Name] = new Dictionary<string, object>
                {
                    {"x", v3i.x},
                    {"y", v3i.y},
                    {"z", v3i.z}
                };
            }
            else if (value is Vector2 v2)
            {
                dict[field.Name] = new Dictionary<string, object>
                {
                    {"x", v2.x},
                    {"y", v2.y}
                };
            }
            else
            {
                dict[field.Name] = value;
            }
        }

        return dict;
    }

    private void WriteContainerToJson(LogicStateRegistry.LogicStateEntry entry, List<object> entries)
    {
        var builder = new ConfigBuilder();

        var entriesJson = new List<object>();
        foreach (var data in entries)
        {
            entriesJson.Add(ConvertToDict(data));
        }

        builder.Add("entries", entriesJson);
        builder.ExportToFileFormat(entry.savePath);
    }
    #endregion

    #region 加载
    //加载所有逻辑状态
    public void LoadAllStates()
    {
        Debug.Log("[LogicStateManager] ========== 开始加载所有逻辑状态 ===========");

        //1.读取顶级注册表
        var registry = ConfigAPI.Get<LogicStateRegistry>();
        if (registry?.entries == null)
        {
            Debug.LogWarning("[LogicStateManager 注册表为空]");
            return;
        }

        //遍历每个父容器
        foreach (var entry in registry.entries)
        {
            LoadContainer(entry);
        }

        Debug.Log("[LogicStateManager] =========== 加载完成 ==========");
    }

    //加载单个父容器
    private void LoadContainer(LogicStateRegistry.LogicStateEntry entry)
    {
        //1.读取父容器配置
        var containerConfig = ConfigAPI.Get<ConfigDefinition>(entry.containerId);
        if (containerConfig == null)
        {
            Debug.LogWarning($"[LogicStateManager] 父容器不存在：{entry.containerId}");
            return;
        }

        //2.获取子容器列表
        var entriesField = containerConfig.GetType().GetField("entries");
        if (entriesField == null)
        {
            Debug.LogWarning("[LogicStateManager]子容器列表为空");
            return;
        }

        var entriesArray = entriesField.GetValue(containerConfig) as Array;
        if (entriesArray == null)return ;

        //遍历子容器
        int LoadedCount = 0;
        foreach (var childEntry in entriesArray)
        {
            if (childEntry is not ILogicStateEntry stateEntry)continue;

            //4.通过UUID查找实体
            var entity = MasterFactory.Instance.FindByUUID(stateEntry.GetUUID());

            //如果找不到，通过Tag查找实体
            if (entity == null && !string.IsNullOrEmpty(stateEntry.GetTag()))
            {
                entity = MasterFactory.Instance.FindFirstByTag(stateEntry.GetTag());
            }

            if (entity == null)
            {
                Debug.LogWarning($"[LogicStateManager] 找不到实体：{stateEntry.GetUUID()}");
                continue;
            }

            //6.查找对应的逻辑脚本
            var script = FindScriptOnEntity(entity, entry.scriptName);
            if (script == null)
            {
                Debug.LogWarning($"[LogicStateManager] 实体上没脚本：{entry.scriptName}");
                continue;
            }

            //7.调用脚本的加载方法
            script.LoadFromEntry(stateEntry);
            LoadedCount++;
        }
        Debug.Log($"[LogicStateManager] 加载{entry.scriptName} : {LoadedCount} 条数据");
    }
    #endregion

    #region 工具方法 

    //查找脚本类型
    private Type FindScriptType(string scriptName)
    {
        var type = Type.GetType(scriptName);
        if (type != null)return type;
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(scriptName);
            if ( type != null)
            {
                return type;
            }
        }
        return null;
    }

    //查找场景中所有该类型的脚本
    private List<LogicScriptBase> FindAllScripts(Type scriptType)
    {
        var result = new List<LogicScriptBase>();
        var allScripts = FindObjectsOfType(scriptType);
        foreach (var script in allScripts)
        {
            if (script is LogicScriptBase logicBase)
            {
                result.Add(logicBase);
            }
        }

        return result;
    }

    //在实体上查找指定脚本
    private LogicScriptBase FindScriptOnEntity(Entity entity, string scriptName)
    {
        foreach (var script in entity.LogicScripts)
        {
            if (script.GetType().Name == scriptName && script is LogicScriptBase logicBase)
            {
                return logicBase;
            }
        }
        return null;
    }
    #endregion
}