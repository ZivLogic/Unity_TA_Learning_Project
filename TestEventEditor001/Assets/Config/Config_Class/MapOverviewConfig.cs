using System;
using UnityEngine;

[Serializable]
[Config(
    configId: "map_overview",
    priority: 100  // 最先加载
)]
public class MapOverviewConfig : ConfigDefinition
{
    public string mapId;
    public string mapName;
    public Vector2 mapSize;          // 地图尺寸
    public string terrainPath;       // 地形预制体路径
    public MapZone[] zones;          // 区域列表

    [Serializable]
    public class MapZone
    {
        public string zoneId;        // 区域ID
        public Vector2 zoneCenter;   // 区域中心
        public Vector2 zoneSize;     // 区域尺寸
        public string zoneType;      // 区域类型：forest, desert, water
        public string terrainConfigId; // 地形配置ID（引用地形配置）
    }
}