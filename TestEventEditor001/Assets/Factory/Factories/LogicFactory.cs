using System;
using UnityEngine;

//逻辑工厂：流水线第三步
//职责：根据蓝图配置，给逻辑体挂载逻辑脚本
//逻辑工厂只负责挂载逻辑脚本，但不会给逻辑脚本注入数据，这是第一步。
//等所有实体被工厂创建完成后，会触发逻辑状态管理器事件，进行二次加载
//在二次加载的时候为各个实体的逻辑脚本注入对应的数据，这是第二步

public class LogicFactory : IFactoryStep
{
    public void Process(Entity entity)
    {
        if (entity?.Blueprint == null)return;

        var logicScriptIds = entity.Blueprint.logicScriptIds;

        if (logicScriptIds == null ||  logicScriptIds.Length == 0 )
        {
            Debug.Log($"[LogicFactory] {entity.Blueprint.blueprintId} 没有逻辑脚本，跳过");
            return;
        }

        Debug.Log($"[LogicFactory] 开始挂载逻辑脚本：{entity.Blueprint.blueprintId} (共 {logicScriptIds.Length} 个)");

        foreach (var scriptId in logicScriptIds )
        {
            //1.通过字典键查找脚本配置
            var scriptConfig = ConfigManager.Instance.FindLogicScript(scriptId);

            if (scriptConfig == null)
            {
                Debug.LogError($"[LogicFactory] 找不到逻辑脚本配置：{scriptId}");
                continue;
            }

            //2.挂载脚本
            var component = AttachScript(entity.LogicObject, scriptConfig);

            if (component != null)
            {
                //3.记录到实体
                entity.LogicScripts.Add(component);

                //4.注入实体引用
                if (component is IEntityAware entityAware)
                {
                    entityAware.Entity = entity;
                }
            }
        }
    }



    //挂载脚本到逻辑体
    private MonoBehaviour AttachScript(GameObject logicObj, LogicScriptsConfig.ScriptData scriptConfig)
    {
        Type scriptType = FindScriptType(scriptConfig.scriptName);

        if (scriptType == null)
        {
            Debug.LogError($"[LogicFactory] 找不到脚本类型： {scriptConfig.scriptName}");
            return null;
        }

        var existing = logicObj.GetComponent(scriptType);
        if (existing != null)
        {
            return existing as MonoBehaviour;
        }

        var component = logicObj.AddComponent(scriptType) as MonoBehaviour;

        if (component != null)
        {
            Debug.Log($"[LogicFactory] 挂载脚本成功：{scriptConfig.scriptName}");
        }

        return component;
    }


    //查找脚本类型
    private Type FindScriptType(string scriptName)
    {
        var type = Type.GetType(scriptName);
        if (type != null) return type;

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(scriptName);
            if (type != null) return type;
        }

        string[] comminNamespaces = { "LogicScripts.", "Scripts.Logic.", "GameLogic.", "" };
        foreach (var ns in comminNamespaces)
        {
            type = Type.GetType(ns + scriptName);
            if (type != null) return type;
        }

        return null;
    }
}

