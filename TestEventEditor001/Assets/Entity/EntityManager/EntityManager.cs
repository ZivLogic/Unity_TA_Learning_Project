using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//实体管理器：批量操作的信息中转层
public class EntityManager : MonoBehaviour
{
    private static EntityManager _instance;
    public static EntityManager Instance => _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log($"[EntityManager]初始化完成");
    }

    #region 批量查询
    //通过UUID列表批量查询实体
    public List<Entity> GetEntitiesByUUIDs(List<string> uuids)
    {
        var result = new List<Entity>();
        foreach (var uuid in uuids)
        {
            var entity = MasterFactory.Instance.FindByUUID(uuid);
            if (entity != null) result.Add(entity);
        }
        return result;
    }

    //通过Tag批量查询实体
    public List<Entity> GetEntitiesByTag(string tag)
    {
        return MasterFactory.Instance.FindAllByTag(tag);
    }

    //通过蓝图ID批量查询实体
    public List<Entity> GetEntitiesByBlueprint(string blueprintId)
    {
        return MasterFactory.Instance.FindByBlueprint(blueprintId);
    }
    #endregion

    #region 批量操作

    //批量隐藏实体（通过 UUID 列表）
    public void BatchHideByUUIDs(List<string> uuids)
    {
        Debug.Log($"[EntityManager] 批量隐藏: {uuids.Count} 个实体");

        foreach (var uuid in uuids)
        {
            var entity = MasterFactory.Instance.FindByUUID(uuid);
            if (entity?.Root != null)
            {
                entity.Root.SetActive(false);
            }
        }
    }

    // 批量显示实体（通过 UUID 列表）
    public void BatchShowByUUIDs(List<string> uuids)
    {
        Debug.Log($"[EntityManager] 批量显示: {uuids.Count} 个实体");

        foreach (var uuid in uuids)
        {
            var entity = MasterFactory.Instance.FindByUUID(uuid);
            if (entity?.Root != null)
            {
                entity.Root.SetActive(true);
            }
        }
    }

    // 批量隐藏（通过 Tag）
    public void BatchHideByTag(string tag)
    {
        var entities = GetEntitiesByTag(tag);
        Debug.Log($"[EntityManager] 按Tag批量隐藏: {tag} x {entities.Count}");

        foreach (var entity in entities)
        {
            if (entity?.Root != null)
            {
                entity.Root.SetActive(false);
            }
        }
    }

    // 批量显示（通过 Tag）
    public void BatchShowByTag(string tag)
    {
        var entities = GetEntitiesByTag(tag);
        Debug.Log($"[EntityManager] 按Tag批量显示: {tag} x {entities.Count}");

        foreach (var entity in entities)
        {
            if (entity?.Root != null)
            {
                entity.Root.SetActive(true);
            }
        }
    }

    // 批量放入对象池
    public void BatchPoolByUUIDs(List<string> uuids)
    {
        MasterFactory.Instance.PoolFactory.BatchReturnToPool(uuids);
    }

    // 按 Tag 批量放入对象池
    public void BatchPoolByTag(string tag)
    {
        MasterFactory.Instance.PoolFactory.BatchReturnByTag(tag);
    }

    // 批量销毁（通过 UUID 列表）
    public void BatchDestroyByUUIDs(List<string> uuids)
    {
        Debug.Log($"[EntityManager] 批量销毁: {uuids.Count} 个实体");

        foreach (var uuid in uuids)
        {
            MasterFactory.Instance.DestroyEntity(uuid);
        }
    }

    // 批量销毁（通过 Tag）
    public void BatchDestroyByTag(string tag)
    {
        var entities = GetEntitiesByTag(tag);
        Debug.Log($"[EntityManager] 按Tag批量销毁: {tag} x {entities.Count}");

        foreach (var entity in entities)
        {
            MasterFactory.Instance.DestroyEntity(entity.UUID);
        }
    }
    #endregion
}
