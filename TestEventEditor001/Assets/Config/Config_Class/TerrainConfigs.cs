using System;
using UnityEngine;

[Serializable]
[Config(
    configId: "terrain_configs",
    priority: 100
)]
public class TerrainConfigs : ConfigDefinition
{
    public TerrainData[] terrains;

    [Serializable]
    public class TerrainData
    {
        public string terrainId;         // 字典键
        public string terrainType;       // 类型：grass, sand, rock
        public float height;             // 基础高度
        public float noiseScale;         // 噪声缩放
        public float noiseAmplitude;     // 噪声幅度
        public string groundTexture;     // 地面贴图
        public string heightMapPath;     // 高度图路径
        public string diffuseTexture;    // 漫反射贴图
        public string normalTexture;     // 法线贴图路径
        public Vector2 tileSize = new Vector2(10, 10);  // 纹理平铺
        public Color groundColor = Color.gray;  // 备用颜色
    }
}