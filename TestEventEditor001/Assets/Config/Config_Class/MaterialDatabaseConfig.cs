// Configs/ConfigClasses/MaterialDatabaseConfig.cs
using System;
using UnityEngine;

[Serializable]
[Config(
    configId: "material_database",
    priority: 100
)]
public class MaterialDatabaseConfig : ConfigDefinition
{
    public MaterialData[] materials;

    [Serializable]
    public class MaterialData
    {
        public string materialId;           // 字典键
        public string path;                 // 材质路径
        public string shaderName;           // Shader名称
        public Color mainColor;             // 主颜色
        public float metallic;              // 金属度
        public float smoothness;            // 光滑度

        // 新增：渲染设置
        public bool castShadows;            // 是否投射阴影
        public bool receiveShadows;         // 是否接收阴影
        public string renderLayer;          // 渲染层级
        public int lodLevel;                // LOD等级
    }
}