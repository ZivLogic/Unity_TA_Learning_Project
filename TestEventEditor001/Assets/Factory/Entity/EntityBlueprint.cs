using System;

// 实体蓝图基类
[Serializable]
public class EntityBlueprint
{
    public string blueprintId;
    public string tag;
    public string modelPath;
    public float defaultHeight = 1.0f;

    // 三体配置
    public string physicsConfigId;      // 物理配置（逻辑体）
    public string materialId;           // 材质配置（视觉体）
    public string[] audioIds;           // 音频配置（音频体）

    // 逻辑脚本
    public string[] logicScriptIds;     // 逻辑脚本
    public string poolId;               // 对象池
}

// UI蓝图
[Serializable]
public class UIBlueprint : EntityBlueprint
{
    public string prefabId;             // UI预制体ID
    public string canvasName;           // 所属Canvas
    public int sortingOrder;            // 渲染顺序
}

// 地图蓝图
[Serializable]
public class MapBlueprint : EntityBlueprint
{
    public string terrainConfigId;      // 地形配置ID
    public string zoneId;               // 区域ID
}