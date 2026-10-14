using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 对象池工厂：流水线第六步
/// 职责：根据蓝图配置，将实体放入对应的对象池
/// </summary>
public class PoolFactory : IFactoryStep
{
    #region 对象池管理

    // 对象池字典：池名 → 对象池
    private readonly Dictionary<string, ObjectPool> _pools = new();

    // 实体UUID → 所属对象池
    private readonly Dictionary<string, string> _entityPoolMap = new();

    #endregion

    #region 流水线处理

    /// <summary>
    /// 处理实体（放入对象池）
    /// </summary>
    public void Process(Entity entity)
    {
        if (entity?.Blueprint == null) return;

        // 只有普通实体蓝图才有对象池配置
        if (entity.Blueprint is not EntityBlueprintsConfig.EntityBlueprint blueprint)
            return;

        // 如果没有对象池ID，跳过
        if (string.IsNullOrEmpty(blueprint.poolId))
        {
            Debug.Log($"[PoolFactory] {blueprint.blueprintId} 没有对象池配置，跳过");
            return;
        }

        // 1. 获取对象池配置
        var poolConfig = ConfigManager.Instance.FindObjectPool(blueprint.poolId);
        if (poolConfig == null)
        {
            Debug.LogError($"[PoolFactory] 找不到对象池配置: {blueprint.poolId}");
            return;
        }

        // 2. 获取或创建对象池
        var pool = GetOrCreatePool(poolConfig);

        // 3. 将实体放入对象池
        pool.Push(entity);

        // 4. 记录实体所属对象池
        _entityPoolMap[entity.UUID] = poolConfig.poolId;

        Debug.Log($"[PoolFactory] 实体放入对象池: {blueprint.blueprintId} → {poolConfig.poolId} (池内数量: {pool.Count})");
    }

    #endregion

    #region 对象池操作

    /// <summary>
    /// 获取或创建对象池
    /// </summary>
    private ObjectPool GetOrCreatePool(ObjectPoolsConfig.PoolData poolConfig)
    {
        if (_pools.TryGetValue(poolConfig.poolId, out var existingPool))
        {
            return existingPool;
        }

        // 创建新对象池
        var newPool = new ObjectPool(
            poolConfig.poolId,
            poolConfig.preloadCount,
            poolConfig.maxSize,
            poolConfig.autoExpand
        );

        _pools[poolConfig.poolId] = newPool;

        Debug.Log($"[PoolFactory] 创建对象池: {poolConfig.poolId} (预加载: {poolConfig.preloadCount}, 最大: {poolConfig.maxSize})");

        return newPool;
    }

    /// <summary>
    /// 从对象池获取实体
    /// </summary>
    public Entity GetFromPool(string poolId)
    {
        if (!_pools.TryGetValue(poolId, out var pool))
        {
            Debug.LogWarning($"[PoolFactory] 对象池不存在: {poolId}");
            return null;
        }

        var entity = pool.Pop();

        if (entity != null)
        {
            Debug.Log($"[PoolFactory] 从对象池取出: {entity.Blueprint.blueprintId} (池内剩余: {pool.Count})");
        }

        return entity;
    }

    /// <summary>
    /// 将实体放回对象池
    /// </summary>
    public void ReturnToPool(Entity entity)
    {
        if (entity == null) return;

        if (!_entityPoolMap.TryGetValue(entity.UUID, out var poolId))
        {
            Debug.LogWarning($"[PoolFactory] 实体不在任何对象池中: {entity.EntityId}");
            return;
        }

        if (_pools.TryGetValue(poolId, out var pool))
        {
            pool.Push(entity);
            Debug.Log($"[PoolFactory] 实体放回对象池: {entity.EntityId} → {poolId}");
        }
    }

    /// <summary>
    /// 预热对象池（游戏启动时调用）
    /// </summary>
    public void PrewarmPool(string blueprintId, int count)
    {
        Debug.Log($"[PoolFactory] 预热对象池: {blueprintId} x {count}");

        // 1. 先通过蓝图ID获取蓝图对象
        var blueprint = MasterFactory.Instance.GetBlueprint(blueprintId);

        if (blueprint == null)
        {
            Debug.LogError($"[PoolFactory] 找不到蓝图: {blueprintId}");
            return;
        }

        // 2. 批量创建实体
        for (int i = 0; i < count; i++)
        {
            var entity = MasterFactory.Instance.CreateEntity(blueprint);
            if (entity == null) break;
        }
    }

    #endregion

    #region 清理检查

    /// <summary>
    /// 检查对象池（程序退出时调用）
    /// </summary>
    public void CheckPoolOnExit()
    {
        Debug.Log("[PoolFactory] 对象池退出检查...");

        int totalPooled = 0;

        foreach (var poolPair in _pools)
        {
            var pool = poolPair.Value;
            totalPooled += pool.Count;

            Debug.Log($"  对象池 {poolPair.Key}: {pool.Count} 个实体");
        }

        // 更新统计
        if (MasterFactory.Instance != null)
        {
            // 通过反射或其他方式更新 TotalPooledCount
        }

        Debug.Log($"[PoolFactory] 总池化实体: {totalPooled} 个");

        // 清空所有对象池
        ClearAllPools();
    }

    /// <summary>
    /// 清空所有对象池
    /// </summary>
    public void ClearAllPools()
    {
        foreach (var pool in _pools.Values)
        {
            pool.Clear();
        }

        _pools.Clear();
        _entityPoolMap.Clear();

        Debug.Log("[PoolFactory] 所有对象池已清空");
    }

    #endregion
}

/// <summary>
/// 对象池类
/// </summary>
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

    /// <summary>
    /// 将实体放入对象池
    /// </summary>
    public void Push(Entity entity)
    {
        if (entity == null) return;

        //防止重复入池
        if (entity.IsPooled)
        {
            Debug.LogWarning($"[ObjectPool]实体已经在池中，重复入池{entity.UUID}");
            return;
        }


        // 检查容量
        if (_entities.Count >= _maxSize && !_autoExpand)
        {
            Debug.LogWarning($"[ObjectPool] 对象池已满: {_poolId} (容量: {_maxSize})");
            return;
        }

        // 标记为池化状态
        entity.IsPooled = true;

        // 禁用实体
        if (entity.Root != null)
        {
            entity.Root.SetActive(false);
        }

        // 加入队列
        _entities.Enqueue(entity);
    }

    /// <summary>
    /// 从对象池取出实体
    /// </summary>
    public Entity Pop()
    {
        if (_entities.Count == 0)
        {
            Debug.LogWarning($"[ObjectPool] 对象池为空: {_poolId}");
            return null;
        }

        var entity = _entities.Dequeue();

        // 标记为激活状态
        entity.IsPooled = false;

        // 启用实体
        if (entity.Root != null)
        {
            entity.Root.SetActive(true);
        }

        return entity;
    }

    /// <summary>
    /// 清空对象池
    /// </summary>
    public void Clear()
    {
        while (_entities.Count > 0)
        {
            var entity = _entities.Dequeue();
            if (entity?.Root != null)
            {
                GameObject.Destroy(entity.Root);
            }
        }
    }
}