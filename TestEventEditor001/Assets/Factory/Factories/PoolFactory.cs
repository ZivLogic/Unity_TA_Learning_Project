using System;
using System.Collections.Generic;
using UnityEngine;

public class PoolFactory : IFactoryStep
{
    #region 对象池管理
    private readonly Dictionary<string, ObjectPool> _pools = new();
    private readonly Dictionary<string, string> _entityPoolMap = new();
    private bool _isPrewarming = false;
    #endregion

    #region 统计
    public int TotalPooledCount
    {
        get
        {
            int total = 0;
            foreach (var pool in _pools.Values) total += pool.Count;
            return total;
        }
    }
    #endregion

    #region 流水线处理
    public void Process(Entity entity)
    {
        if (entity?.Blueprint == null) return;
        if (entity.Blueprint is not EntityBlueprintsConfig.EntityBlueprint blueprint) return;
        if (string.IsNullOrEmpty(blueprint.poolId)) return;

        // 无论是否入池，先记录归属池
        _entityPoolMap[entity.UUID] = blueprint.poolId;

        if (_isPrewarming)
        {
            Debug.Log($"[PoolFactory] 预热模式，跳过入池: {entity.UUID}");
            return;
        }

        var poolConfig = ConfigManager.Instance.FindObjectPool(blueprint.poolId);
        if (poolConfig == null)
        {
            Debug.LogError($"[PoolFactory] 找不到对象池配置: {blueprint.poolId}");
            return;
        }

        var pool = GetOrCreatePool(poolConfig);
        pool.Push(entity);

        Debug.Log($"[PoolFactory] 实体放入对象池: {blueprint.blueprintId} → {poolConfig.poolId} (池内: {pool.Count})");
    }
    #endregion

    #region 统一销毁同步入口

    /// <summary>
    /// 总表销毁实体时，同步清理池表（唯一同步入口）
    /// </summary>
    public void OnEntityDestroyed(Entity entity)
    {
        if (entity == null) return;

        if (_entityPoolMap.TryGetValue(entity.UUID, out var poolId))
        {
            if (_pools.TryGetValue(poolId, out var pool))
                pool.Remove(entity);

            _entityPoolMap.Remove(entity.UUID);
        }
    }
    #endregion

    #region 对象池操作
    private ObjectPool GetOrCreatePool(ObjectPoolsConfig.PoolData poolConfig)
    {
        if (_pools.TryGetValue(poolConfig.poolId, out var existingPool))
            return existingPool;

        var newPool = new ObjectPool(poolConfig.poolId, poolConfig.preloadCount, poolConfig.maxSize, poolConfig.autoExpand);
        _pools[poolConfig.poolId] = newPool;

        Debug.Log($"[PoolFactory] 创建对象池: {poolConfig.poolId}");
        return newPool;
    }

    public Entity GetFromPool(string poolId)
    {
        if (!_pools.TryGetValue(poolId, out var pool))
        {
            Debug.LogWarning($"[PoolFactory] 对象池不存在: {poolId}");
            return null;
        }

        var entity = pool.Pop();
        if (entity != null)
            Debug.Log($"[PoolFactory] 从对象池取出: {entity.Blueprint.blueprintId} (剩余: {pool.Count})");
        return entity;
    }

    public void ReturnToPool(Entity entity)
    {
        if (entity == null) return;

        if (entity.IsPooled)
        {
            Debug.LogWarning($"[PoolFactory] 实体已在池中，跳过: {entity.EntityId}");
            return;
        }

        string poolId = GetPoolId(entity);
        if (string.IsNullOrEmpty(poolId))
        {
            Debug.LogWarning($"[PoolFactory] 实体没有对象池配置: {entity.EntityId}");
            return;
        }

        var poolConfig = ConfigManager.Instance.FindObjectPool(poolId);
        if (poolConfig == null)
        {
            Debug.LogError($"[PoolFactory] 找不到对象池配置: {poolId}");
            return;
        }

        var pool = GetOrCreatePool(poolConfig);
        pool.Push(entity);

        Debug.Log($"[PoolFactory] 实体放回对象池: {entity.EntityId} → {poolId} (池内: {pool.Count})");
    }

    private string GetPoolId(Entity entity)
    {
        if (_entityPoolMap.TryGetValue(entity.UUID, out var poolId))
            return poolId;

        if (entity.Blueprint is EntityBlueprintsConfig.EntityBlueprint blueprint)
        {
            poolId = blueprint.poolId;
            if (!string.IsNullOrEmpty(poolId))
                _entityPoolMap[entity.UUID] = poolId;
            return poolId;
        }

        return null;
    }
    #endregion

    #region 预热
    public void PrewarmPool(string blueprintId, int count)
    {
        var blueprint = MasterFactory.Instance.GetBlueprint(blueprintId);
        if (blueprint == null)
        {
            Debug.LogError($"[PoolFactory] 找不到蓝图: {blueprintId}");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            var entity = MasterFactory.Instance.CreateEntity(blueprint);
            if (entity == null) break;
        }
    }

    public void BeginPrewarm()
    {
        _isPrewarming = true;
        Debug.Log("[PoolFactory] 开始预热模式");
    }

    public void EndPrewarm()
    {
        _isPrewarming = false;
        Debug.Log("[PoolFactory] 结束预热模式");
    }
    #endregion

    #region 清理检查
    public void CheckPoolOnExit()
    {
        Debug.Log("[PoolFactory] 对象池退出检查...");

        foreach (var poolPair in _pools)
            Debug.Log($"  对象池 {poolPair.Key}: {poolPair.Value.Count} 个实体");

        Debug.Log($"[PoolFactory] 总池化实体: {TotalPooledCount} 个");
        ClearAllPools();
    }

    public void ClearAllPools()
    {
        foreach (var pool in _pools.Values)
            pool.Clear();

        _pools.Clear();
        _entityPoolMap.Clear();

        Debug.Log("[PoolFactory] 所有对象池已清空");
    }
    #endregion

    #region 批量操作

    /// <summary>
    /// 批量放回对象池（通过 UUID 列表）
    /// </summary>
    public void BatchReturnToPool(List<string> uuids)
    {
        int success = 0;
        foreach (var uuid in uuids)
        {
            var entity = MasterFactory.Instance.FindByUUID(uuid);
            if (entity != null)
            {
                ReturnToPool(entity);
                success++;
            }
        }
        Debug.Log($"[PoolFactory] 批量放回: {success}/{uuids.Count}");
    }

    /// <summary>
    /// 批量取出对象池（通过 UUID 列表）
    /// </summary>
    public List<Entity> BatchGetFromPool(List<string> uuids)
    {
        var result = new List<Entity>();
        foreach (var uuid in uuids)
        {
            var entity = MasterFactory.Instance.FindByUUID(uuid);
            if (entity != null && entity.IsPooled)
            {
                if (_entityPoolMap.TryGetValue(entity.UUID, out var poolId)
                    && _pools.TryGetValue(poolId, out var pool))
                {
                    var popped = pool.Pop();
                    if (popped != null) result.Add(popped);
                }
            }
        }
        Debug.Log($"[PoolFactory] 批量取出: {result.Count}/{uuids.Count}");
        return result;
    }

    /// <summary>
    /// 按 Tag 批量放回对象池
    /// </summary>
    public void BatchReturnByTag(string tag)
    {
        var entities = MasterFactory.Instance.FindAllByTag(tag);
        foreach (var e in entities) ReturnToPool(e);
        Debug.Log($"[PoolFactory] 按Tag批量放回: {tag} × {entities.Count}");
    }

    /// <summary>
    /// 按蓝图批量放回对象池
    /// </summary>
    public void BatchReturnByBlueprint(string blueprintId)
    {
        var entities = MasterFactory.Instance.FindByBlueprint(blueprintId);
        foreach (var e in entities) ReturnToPool(e);
        Debug.Log($"[PoolFactory] 按蓝图批量放回: {blueprintId} × {entities.Count}");
    }

    /// <summary>
    /// 按 Tag 批量取出（基于总表过滤）
    /// </summary>
    public List<Entity> BatchGetByTag(string tag, int count)
    {
        var result = new List<Entity>();
        var entities = MasterFactory.Instance.FindAllByTag(tag);

        foreach (var entity in entities)
        {
            if (result.Count >= count) break;
            if (!entity.IsPooled) continue;

            if (_entityPoolMap.TryGetValue(entity.UUID, out var poolId)
                && _pools.TryGetValue(poolId, out var pool))
            {
                var popped = pool.Pop();
                if (popped != null) result.Add(popped);
            }
        }
        Debug.Log($"[PoolFactory] 按Tag批量取出: {tag} × {result.Count}");
        return result;
    }
    #endregion
}

public class ObjectPool
{
    private readonly string _poolId;
    private readonly int _maxSize;
    private readonly bool _autoExpand;
    private readonly Queue<Entity> _entities;

    public int Count => _entities.Count;

    public ObjectPool(string poolId, int preloadCount, int maxSize, bool autoExpand)
    {
        _poolId = poolId;
        _maxSize = maxSize;
        _autoExpand = autoExpand;
        _entities = new Queue<Entity>(Mathf.Max(preloadCount, 10));
    }

    public void Push(Entity entity)
    {
        if (entity == null) return;

        if (entity.IsPooled)
        {
            Debug.LogWarning($"[ObjectPool] 实体已在池中: {entity.UUID}");
            return;
        }

        if (_entities.Count >= _maxSize && !_autoExpand)
        {
            Debug.LogWarning($"[ObjectPool] 对象池已满: {_poolId}");
            return;
        }

        entity.IsPooled = true;
        if (entity.Root != null) entity.Root.SetActive(false);
        _entities.Enqueue(entity);
    }

    public Entity Pop()
    {
        if (_entities.Count == 0)
        {
            Debug.LogWarning($"[ObjectPool] 对象池为空: {_poolId}");
            return null;
        }

        var entity = _entities.Dequeue();
        entity.IsPooled = false;
        if (entity.Root != null) entity.Root.SetActive(true);
        return entity;
    }

    /// <summary>
    /// 从池中移除指定实体（总表销毁时同步）
    /// </summary>
    public void Remove(Entity entity)
    {
        if (entity == null) return;

        var temp = new Queue<Entity>(_entities.Count);
        while (_entities.Count > 0)
        {
            var e = _entities.Dequeue();
            if (e != entity) temp.Enqueue(e);
        }
        while (temp.Count > 0) _entities.Enqueue(temp.Dequeue());
    }

    public void Clear()
    {
        while (_entities.Count > 0)
        {
            var entity = _entities.Dequeue();
            if (entity?.Root != null) GameObject.Destroy(entity.Root);
        }
    }
}