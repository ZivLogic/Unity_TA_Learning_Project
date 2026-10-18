using System.Collections.Generic;
using UnityEngine;

//实体系统
//只负责输入分发，不存数据
//数据源：MasterFactory
public class EntitySystem : MonoBehaviour
{
    public static EntitySystem Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    //注入输入事件
    public void InjectInput(EntityInputEvent evt)
    {
        if (evt == null || evt.TargetUuids == null)return;
        foreach (var uuid in evt.TargetUuids)
        {
            //从工厂查实体
            var entity = MasterFactory.Instance.FindByUUID(uuid);
            if (entity == null)
            {
                Debug.LogWarning($"[EntitySystem] 工厂未找到：{uuid}");
                continue;
            }

            //从 LogicScripts 找输入入口
            var entry = FindInputEntry(entity);
            if (entry == null)
            {
                Debug.LogWarning($"[EntitySystem] {uuid} 无输入入口");
                continue;
            }

            Debug.Log($"[EntitySystem] 注入 -> {uuid} Action = {evt.Action}");
            entry.ReceiveInput( evt );
        }
    }

    //从LogicScripts找输入入口
    private EntityInputEntry FindInputEntry(Entity entity)
    {
        if (entity?.LogicScripts == null) return null;
        foreach (var script in entity.LogicScripts)
        {
            if (script is EntityInputEntry entry)
                return entry;
        }
        return null;
    }
}