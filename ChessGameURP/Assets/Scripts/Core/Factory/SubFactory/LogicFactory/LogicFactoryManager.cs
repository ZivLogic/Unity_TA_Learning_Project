using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogicFactoryManager : MonoBehaviour, IFactory
{
    public string FactoryName => "Logic";

    //泛用身份类，如人，狗这种大类别
    private Dictionary<EntityIdentityType, EntityIDConfig> _enableConfigCache = new Dictionary<EntityIdentityType, EntityIDConfig>();
    //精确身份类，如主角，NPC1这种区别
    private Dictionary<EntitySpecialIdentityType, EntityIDConfig_Special> _specialConfigCache = new Dictionary<EntitySpecialIdentityType, EntityIDConfig_Special>();
    
    //初始化注册自己
    private void Awake()
    {
        FactoryManager.Instance.RegisterFactory(this);
    }

    //初始化
    public void Initialize()
    {
        _enableConfigCache.Clear();
        _specialConfigCache.Clear();
        GetEntityConfigClassID();
    }

    //读取实体身份配置路由
    private void GetEntityConfigClassID()
    {
        _enableConfigCache = EntityFactoryManager._EnableConfigCache;
        _specialConfigCache = EntityFactoryManager._SpecialConfigCache;
        if (_enableConfigCache == null || _specialConfigCache == null)
        { 
            Debug.LogError("[LogicFactoryManager]获取实体配置失败");
            return;
        }
    }

    
    //匹配实体，自动挂载逻辑脚本
    public void AutoAttachAllLogic()
    {
        EntityIdentityTag[] allEntities = GameObject.FindObjectsOfType<EntityIdentityTag>();
        EntitySpecialIdentityTag[] allSpecialEntities = GameObject.FindObjectsOfType<EntitySpecialIdentityTag>();
        foreach (var tag in allEntities)
        {
            var entityType = tag.IdentityType;
            GameObject go = tag.gameObject;
            //不在启用缓存里，跳过
            if ( ! _enableConfigCache.TryGetValue(entityType, out var config) )
                continue;
            //挂载配置里的所有脚本
            AttachLogicScripts(go, config.ClassIDTable);
            //校验：找出生成了但没配置/没挂载的实体
            CheckUnMatchedEntity(allEntities);
        }
        foreach (var tag in allSpecialEntities)
        {
            var entityType = tag.SpecialType;
            GameObject go = tag.gameObject;
            //不在启用缓存里，跳过
            if (! _specialConfigCache.TryGetValue(entityType, out var config))
                continue;
            AttachLogicScripts(go, config.ClassIDTable);
            CheckUnMatchedEntity(allEntities);
        }
        Debug.Log("[LogicFactoryManager]挂载脚本完成");
    }
    //根据类名字符串动态加载脚本
    private void AttachLogicScripts(GameObject entity, List<string> scriptTypeNames)
    {
        foreach (var typeName in scriptTypeNames)
        {
            Type logicType = Type.GetType(typeName);
            if (logicType == null)
            {
                Debug.LogError($"[LogicFactoryManager]找不到脚本类：{typeName},挂载失败");
                continue;
            }
            //避免重复挂载
            if (entity.GetComponent(logicType) != null ) 
                continue;
            entity.AddComponent(logicType);
            //Debug.Log($"[LogicFactoryManager]实体[{entity.name}]挂载脚本[{typeName}]成功");
        }
        
    }
    //双向校验：漏配置，漏挂载报错
    private void CheckUnMatchedEntity(EntityIdentityTag[] allEntities)
    {
        List<EntityIdentityTag> unMatched = new List<EntityIdentityTag>();
        foreach (var tag in allEntities)
        {
            if (!_enableConfigCache.ContainsKey(tag.IdentityType))
            {
                unMatched.Add(tag);
            }
        }
        if (unMatched.Count > 0)
        {
            Debug.LogError($"[LogicFactoryManager]发现{unMatched.Count}个实体无匹配启用配置");
            foreach (var t in unMatched)
            {
                Debug.LogError($"[LogicFactoryManager]具体实体：{t.gameObject.name} | 身份：{t.IdentityType}未配置逻辑脚本");
                //后续拓展
                //1.自动销毁   Destroy(t.gameobject);
                //2.回对象池
                //3.隐藏禁用
            }
        }
        else
        {
            //Debug.Log($"[LogicFactoryManager]实体完成逻辑脚本挂载，校验通过");
        }
        
    }



    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
