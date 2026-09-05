// Framework/Factories/MapFactory.cs
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 地图工厂：流水线第八步
/// 职责：根据地图配置，批量摆放实体到地图坐标
/// 
/// 前置依赖：
/// 1. 地图总览配置（map_overview）- 地图基本信息
/// 2. 地形配置（terrain_configs）- 地形数据
/// 3. 物体放置配置（map_placements）- 实体摆放位置
/// 4. 地图缓存配置（map_cache）- 缓存数据（可选）
/// 
/// 工作流程：
/// 第一次加载（冷启动）：
///   读配置 → 创建实体 → 摆放 → 生成缓存
/// 
/// 第二次加载（热启动）：
///   读缓存 → 从对象池取实体 → 快速摆放
/// 
/// 缓存失效：
///   回退到按需加载 → 重新生成缓存
/// </summary>
public class MapFactory : IFactoryStep
{
    #region 缓存数据

    // 当前地图ID
    private string _currentMapId;

    // 地图缓存
    private MapCacheConfig _mapCache;

    // 已摆放的实体列表
    private readonly List<Entity> _placedEntities = new();

    #endregion

    #region 流水线处理

    /// <summary>
    /// 处理单个实体（地图工厂主要负责批量摆放）
    /// </summary>
    public void Process(Entity entity)
    {
        if (entity?.Blueprint == null) return;

        // 地图工厂在流水线中对单个实体的处理：
        // 默认位置已在 EntityFactory 中设置
        // 地图工厂主要负责批量摆放，单个实体处理留空
    }

    #endregion

    #region 地图加载

    /// <summary>
    /// 加载地图（带缓存机制）
    /// </summary>
    public void LoadMap(string mapId)
    {
        Debug.Log($"[MapFactory] ========== 开始加载地图: {mapId} ==========");

        _currentMapId = mapId;

        // 1. 尝试从缓存加载
        if (TryLoadFromCache(mapId))
        {
            Debug.Log($"[MapFactory] ✅ 从缓存加载地图成功: {mapId}");
            return;
        }

        // 2. 缓存失败，按需加载
        Debug.Log($"[MapFactory] 缓存失效，按需加载地图: {mapId}");
        LoadFromConfig(mapId);

        // 3. 生成新缓存
        GenerateCache(mapId);

        Debug.Log($"[MapFactory] ✅ 地图加载完成: {mapId}");
    }

    /// <summary>
    /// 尝试从缓存加载
    /// </summary>
    private bool TryLoadFromCache(string mapId)
    {
        // 1. 读取缓存配置
        _mapCache = ConfigAPI.Get<MapCacheConfig>();

        if (_mapCache?.entries == null || _mapCache.entries.Length == 0)
        {
            Debug.Log("[MapFactory] 没有缓存数据");
            return false;
        }

        // 2. 验证缓存是否有效
        if (!ValidateCache(mapId))
        {
            Debug.LogWarning("[MapFactory] 缓存验证失败");
            return false;
        }

        // 3. 从缓存快速加载
        int loadedCount = 0;

        foreach (var entry in _mapCache.entries)
        {
            // 从对象池获取实体
            var entity = MasterFactory.Instance.PoolFactory.GetFromPool(entry.blueprintId);

            if (entity == null)
            {
                // 对象池没有，创建新实体
                var blueprint = MasterFactory.Instance.GetBlueprint(entry.blueprintId);
                if (blueprint == null) continue;

                entity = MasterFactory.Instance.CreateEntity(blueprint);
            }

            if (entity == null) continue;

            // 恢复位置
            entity.Root.transform.position = entry.position;
            entity.Root.transform.rotation = Quaternion.Euler(entry.rotation);
            entity.Root.transform.localScale = entry.scale;

            // 恢复Tag
            entity.Root.tag = entry.tag;
            entity.Tag = entry.tag;

            // 记录已摆放的实体
            _placedEntities.Add(entity);
            loadedCount++;
        }

        Debug.Log($"[MapFactory] 从缓存加载 {loadedCount} 个实体");
        return loadedCount > 0;
    }

    /// <summary>
    /// 按需加载（从配置）
    /// </summary>
    private void LoadFromConfig(string mapId)
    {
        // 1. 读取地图总览
        var mapOverview = ConfigAPI.Get<MapOverviewConfig>();
        if (mapOverview == null)
        {
            Debug.LogError("[MapFactory] 地图总览配置不存在");
            return;
        }

        // 2. 加载地形
        LoadTerrain(mapOverview);

        // 3. 读取物体放置配置
        var placements = ConfigAPI.Get<MapPlacementsConfig>();
        if (placements?.placements == null)
        {
            Debug.LogWarning("[MapFactory] 没有物体放置配置");
            return;
        }

        // 4. 批量摆放实体
        int placedCount = 0;

        foreach (var placement in placements.placements)
        {
            // 检查生成条件
            if (!ShouldSpawn(placement.spawnCondition))
            {
                continue;
            }

            // 放置实体
            var entity = PlaceEntity(placement);

            if (entity != null)
            {
                _placedEntities.Add(entity);
                placedCount++;
            }
        }

        Debug.Log($"[MapFactory] 按需加载 {placedCount} 个实体");
    }

    /// <summary>
    /// 放置单个实体
    /// </summary>
    private Entity PlaceEntity(MapPlacementsConfig.MapPlacement placement)
    {
        // 1. 从对象池获取实体
        var entity = MasterFactory.Instance.PoolFactory.GetFromPool(placement.blueprintId);

        // 2. 如果对象池没有，创建新实体
        if (entity == null)
        {
            var blueprint = MasterFactory.Instance.GetBlueprint(placement.blueprintId);
            if (blueprint == null)
            {
                Debug.LogError($"[MapFactory] 找不到蓝图: {placement.blueprintId}");
                return null;
            }

            entity = MasterFactory.Instance.CreateEntity(blueprint);
        }

        if (entity == null) return null;

        // 3. 设置位置
        entity.Root.transform.position = placement.position;
        entity.Root.transform.rotation = Quaternion.Euler(placement.rotation);
        entity.Root.transform.localScale = placement.scale;

        // 4. 覆盖Tag（如果需要）
        if (!string.IsNullOrEmpty(placement.tag))
        {
            entity.Root.tag = placement.tag;
            entity.Tag = placement.tag;
        }

        Debug.Log($"[MapFactory] 放置实体: {placement.placementId} → ({placement.position.x}, {placement.position.y}, {placement.position.z})");

        return entity;
    }

    #endregion

    #region 地形加载

    /// <summary>
    /// 加载地形
    /// </summary>
    private void LoadTerrain(MapOverviewConfig mapOverview)
    {
        if (mapOverview.zones == null) return;

        foreach (var zone in mapOverview.zones)
        {
            // 获取地形配置
            var terrainConfig = ConfigManager.Instance.FindTerrainConfig(zone.terrainConfigId);
            if (terrainConfig == null) continue;

            // 创建地形
            CreateTerrain(zone, terrainConfig);
        }
    }

    /// <summary>
    /// 创建地形
    /// </summary>
    private void CreateTerrain(MapOverviewConfig.MapZone zone, TerrainConfigs.TerrainData terrainConfig)
    {
        var terrainObj = new GameObject($"Terrain_{zone.zoneId}");
        terrainObj.transform.position = new Vector3(
            zone.zoneCenter.x,
            terrainConfig.height,
            zone.zoneCenter.y
        );

        // 这里可以添加Terrain组件或加载地形预制体
        // 简化：创建一个平面代表地形
        var terrain = terrainObj.AddComponent<Terrain>();

        // 设置地形尺寸
        terrain.terrainData = new TerrainData();
        terrain.terrainData.size = new Vector3(
            zone.zoneSize.x,
            terrainConfig.height * 2,
            zone.zoneSize.y
        );

        Debug.Log($"[MapFactory] 创建地形: {zone.zoneId} (类型: {terrainConfig.terrainType})");
    }

    #endregion

    #region 缓存管理

    /// <summary>
    /// 生成缓存
    /// </summary>
    private void GenerateCache(string mapId)
    {
        var entries = new List<MapCacheConfig.MapCacheEntry>();

        // 遍历所有已摆放的实体
        foreach (var entity in _placedEntities)
        {
            if (entity?.Root == null) continue;

            var entry = new MapCacheConfig.MapCacheEntry
            {
                uuid = entity.UUID,
                tag = entity.Tag,
                blueprintId = entity.Blueprint.blueprintId,
                position = entity.Root.transform.position,
                rotation = entity.Root.transform.rotation.eulerAngles,
                scale = entity.Root.transform.localScale
            };

            entries.Add(entry);
        }

        // 保存到配置
        _mapCache = new MapCacheConfig
        {
            mapId = mapId,
            cacheVersion = "1.0.0",
            entries = entries.ToArray()
        };

        // 写入JSON
        var builder = new ConfigBuilder();
        builder.Add("mapId", mapId);
        builder.Add("cacheVersion", "1.0.0");
        builder.Add("entries", entries);
        builder.ExportToFileFormat("Assets/Configs/Data/map_cache.json");

        Debug.Log($"[MapFactory] 缓存生成完成: {entries.Count} 个实体");
    }

    /// <summary>
    /// 验证缓存是否有效
    /// </summary>
    private bool ValidateCache(string mapId)
    {
        // 1. 检查地图ID是否匹配
        if (_mapCache.mapId != mapId)
        {
            Debug.LogWarning($"[MapFactory] 缓存地图ID不匹配: {_mapCache.mapId} vs {mapId}");
            return false;
        }

        // 2. 检查缓存是否为空
        if (_mapCache.entries == null || _mapCache.entries.Length == 0)
        {
            return false;
        }

        // 3. 检查实体数量是否与地图配置一致
        var placements = ConfigAPI.Get<MapPlacementsConfig>();
        if (placements?.placements != null)
        {
            if (_mapCache.entries.Length != placements.placements.Length)
            {
                Debug.LogWarning($"[MapFactory] 缓存数量不匹配: {_mapCache.entries.Length} vs {placements.placements.Length}");
                return false;
            }
        }

        return true;
    }

    #endregion

    #region 清理

    /// <summary>
    /// 清空地图
    /// </summary>
    public void ClearMap()
    {
        Debug.Log("[MapFactory] 清空地图...");

        // 将所有实体放回对象池
        foreach (var entity in _placedEntities)
        {
            if (entity == null) continue;

            MasterFactory.Instance.PoolFactory.ReturnToPool(entity);
        }

        _placedEntities.Clear();

        Debug.Log("[MapFactory] 地图已清空");
    }

    #endregion

    #region 生成条件检查

    /// <summary>
    /// 检查是否应该生成
    /// </summary>
    private bool ShouldSpawn(string condition)
    {
        return condition.ToLower() switch
        {
            "always" => true,
            "random" => UnityEngine.Random.value > 0.5f,
            "trigger" => false,  // 需要触发器
            _ => true
        };
    }

    #endregion
}