 # 基础 容器类别定义

## 1.实体蓝图

### 定义总结

实体蓝图主要负责定义一个物体**长什么样，有哪些属性，需要哪一些配置，就是根据需要的各部分配置集合在一起，组合成一份“物体配置”。**实体蓝图里包含的基础信息有蓝图ID，物理ID，逻辑脚本列表等等。同时有实体蓝图基类和UI蓝图基类的区别。

### 代码示例

```c#
/// <summary>
/// 所有蓝图的基类
/// </summary>
[Serializable]
public abstract class EntityBlueprintBase
{
    public string blueprintId;                //蓝图ID
    public string tag;                        //蓝图标签
    public float defaultHeight = 1.0f;        //初始高度
    public string[] logicScriptIds;           //逻辑脚本列表

    //三体配置
    public string physicsConfigId;            //物理配置ID
    public string materialId;                 //材质配置ID
    public string[] audioIds;                 //音频配置ID

    //对象池ID
    public string poolId;                     //对象池配置
}

// 实体蓝图
[Serializable]
[Config(
    configId: "entity_blueprints",
    priority: 200,
    dependsOn: new[] {                         //依赖关系
        "logic_scripts",
        "object_pools",
        "physics_configs",
        "material_database"
    }
)]
public class EntityBlueprintsConfig : ConfigDefinition
{
    public EntityBlueprint[] blueprints;

    [Serializable]
    public class EntityBlueprint : EntityBlueprintBase
    {
        public string modelPath;                //模型/预制体路径
    }
}
```

## 2.UI蓝图

### 定义总结

UI蓝图定义一个UI长什么样，有哪些音效，哪些逻辑。**UI蓝图现在没有渲染配置和物理配置。**

### 代码示例

```c#
[Serializable]
[Config(
    configId: "ui_blueprints",
    priority: 200,
    dependsOn: new[] {
        "ui_prefabs",         //暂时没依赖
        "logic_scripts",
        "canvas_registry",    // 依赖画布注册表
        "camera_registry"     // 依赖摄像机注册表
    }
)]
public class UIBlueprintsConfig : ConfigDefinition
{
    public UIBlueprint[] blueprints;

    [Serializable]
    public class UIBlueprint : EntityBlueprintBase
    {
        public string prefabPath;        // UI预制体路径
        public string canvasName;        // 所属画布ID（引用canvas_registry）
        public int sortingOrder;         // 渲染顺序

        // 新增字段
        public string uiLayer;           // UI层级：background, normal, popup, tooltip, overlay
        public bool raycastTarget;       // 是否可穿透
        public bool alwaysOnTop;         // 是否始终顶层
    }
}
```

## 3.物理配置

### 定义总结

定义了单个物体需要的物理配置，如刚体、碰撞体等

### 代码示例

```c#
using System;
using UnityEngine;

[Serializable]
[Config(
    configId: "physics_configs",
    priority: 100
)]
public class PhysicsConfigs : ConfigDefinition
{
    public PhysicsData[] physicsList;

    [Serializable]
    public class PhysicsData
    {
        public string configId;              // 字典键
        public float mass;                   // 质量
        public bool isKinematic;             // 是否运动学
        public bool useGravity;              // 是否使用重力
        public bool interpolate;             // 是否插值
        public bool continuousCollision;     // 连续碰撞检测
        public ColliderData[] colliders;     // 碰撞体列表
    }

    [Serializable]
    public class ColliderData
    {
        public string type;                  // capsule / sphere / box / mesh
        public Vector3 center;               // 中心点
        public float radius;                 // 半径（球体/胶囊）
        public float height;                 // 高度（胶囊）
        public Vector3 size;                 // 尺寸（盒体）
        public int direction;                // 方向（胶囊：0=X,1=Y,2=Z）
        public bool convex;                  // 凸包（网格碰撞体）
        public string physicsMaterial;       // 物理材质路径
    }
}
```

## 4.渲染配置

### 定义总结

定义了单个物体的渲染材质，渲染层级等

### 代码示例

```c#
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
```

## 5.音频配置

### 定义总结

定义了音频路径，音频设置属性等

### 代码示例

```c#
[Serializable]
[Config(
    configId: "audio_configs",
    priority: 100
)]
public class AudioConfigs : ConfigDefinition
{
    public AudioData[] audioList;

    [Serializable]
    public class AudioData
    {
        public string audioId;           // 字典键
        public string clipPath;          // 音频片段路径
        public float volume;             // 音量（0-1）
        public float pitch;              // 音调（0.5-2）
        public bool loop;                // 是否循环
        public float spatialBlend;       // 空间混合（0=2D，1=3D）
        public float minDistance;        // 最小距离
        public float maxDistance;        // 最大距离
        public string playMode;          // 播放模式：onAwake, loop, manual, trigger
    }
}
```

## 6.对象池配置

### 定义总结

定义了实体归属的对象池

### 代码示例

```c#
using System;

[Serializable]
[Config(
    configId: "object_pools",
    priority: 100
)]
public class ObjectPoolsConfig : ConfigDefinition
{
    public PoolData[] pools;

    [Serializable]
    public class PoolData
    {
        public string poolId;         // 字典键
        public int preloadCount;      // 预加载数量
        public int maxSize;           // 最大容量
        public bool autoExpand;       // 自动扩容
    }
}
```

## 7. 摄像机配置

### 定义总结

定义摄像机的初始信息

### 代码示例

```c#
[Serializable]
[Config(
    configId: "camera_registry",
    priority: 50  // 最高优先级，最先加载
)]
public class CameraRegistryConfig : ConfigDefinition
{
    public CameraEntry[] cameras;

    [Serializable]
    public class CameraEntry
    {
        public string cameraId;          // 字典键
        public string cameraTag;         // Tag（用于查找）
        public string scenePath;         // 场景中的路径（用于查找）
        public string cameraType;        // 类型：main, weapon, vehicle
        public int priority;             // 优先级
    }
}
```

