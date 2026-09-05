using System;

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
    }
}