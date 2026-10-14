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
/// 
/// 工作流程：
/// 读配置 → 创建实体 → 摆放
/// （缓存功能留待后续地图编辑器完成后再实现）
/// </summary>
public class MapFactory : IFactoryStep
{
    #region 数据

    // 当前地图ID
    private string _currentMapId;

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
    /// 加载地图（纯按需加载）
    /// </summary>
    public void LoadMap(string mapId)
    {
        Debug.Log($"[MapFactory] ========== 开始加载地图: {mapId} ==========");

        _currentMapId = mapId;

        // 清空之前的地图
        ClearMap();

        // 按需加载
        LoadFromConfig(mapId);

        Debug.Log($"[MapFactory] ✅ 地图加载完成: {mapId}");
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

        Debug.Log($"[MapFactory] 按需加载完成，共摆放 {placedCount} 个实体");
    }

    /// <summary>
    /// 放置单个实体
    /// </summary>
    private Entity PlaceEntity(MapPlacementsConfig.MapPlacement placement)
    {
        // 1. 获取蓝图
        var blueprint = MasterFactory.Instance.GetBlueprint(placement.blueprintId);
        if (blueprint == null)
        {
            Debug.LogError($"[MapFactory] 找不到蓝图: {placement.blueprintId}");
            return null;
        }

        // 2. 创建实体（直接创建，不走对象池）
        var entity = MasterFactory.Instance.CreateEntity(blueprint);
        if (entity == null) return null;

        // 3. 设置位置
        entity.Root.transform.position = placement.position;
        entity.Root.transform.rotation = Quaternion.Euler(placement.rotation);
        entity.Root.transform.localScale = placement.scale;

        // 4. 覆盖Tag（如果需要）(恢复成和地形放置配置一样)
        if (!string.IsNullOrEmpty(placement.tag))
        {
            entity.Root.tag = placement.tag;
            entity.Tag = placement.tag;
        }

        // 5. 更新实体ID（使用 placementId）
        entity.EntityId = placement.placementId;

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

    #region 清理

    /// <summary>
    /// 清空地图
    /// </summary>
    public void ClearMap()
    {
        Debug.Log("[MapFactory] 清空地图...");

        // 销毁所有已摆放的实体
        foreach (var entity in _placedEntities)
        {
            if (entity?.Root != null)
            {
                GameObject.Destroy(entity.Root);
            }
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