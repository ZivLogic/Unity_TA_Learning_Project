using System;
using UnityEngine;

[Serializable]
[Config(
    configId: "map_placements",
    priority: 1000,  // 依赖地图总览
    dependsOn: new[] { "map_overview", "entity_blueprints" }
)]
public class MapPlacementsConfig : ConfigDefinition
{
    public MapPlacement[] placements;

    [Serializable]
    public class MapPlacement
    {
        public string placementId;   // 放置ID
        public string zoneId;        // 所属区域
        public Vector3 position;     // 坐标
        public Vector3 rotation;     // 旋转
        public Vector3 scale;        // 缩放
        public string blueprintId;   // 引用蓝图ID
        public string tag;           // 生成的Tag（可选，默认用蓝图的tag）
        public string spawnCondition; // 生成条件：always, random, trigger
    }
}