using System;
using UnityEngine;

[Serializable]
[Config(
    configId: "ui_prefabs",
    priority: 100
)]
public class UIPrefabsConfig : ConfigDefinition
{
    public UIPrefab[] prefabs;

    [Serializable]
    public class UIPrefab
    {
        public string prefabId;          // 预制体ID（字典键）
        public string path;              // 预制体路径
        public string type;              // 类型：healthbar、dialog、inventory
        public Vector2 defaultSize;      // 默认尺寸
    }
}