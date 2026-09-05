using System;
using UnityEngine;

/// <summary>
/// 物理工厂：流水线第二步
/// 职责：根据蓝图配置，给逻辑体添加刚体和碰撞体
/// </summary>
public class PhysicsFactory : IFactoryStep
{
    /// <summary>
    /// 处理实体
    /// </summary>
    public void Process(Entity entity)
    {
        if (entity?.Blueprint == null) return;

        // 只有普通实体蓝图才有物理配置
        if (entity.Blueprint is not EntityBlueprintsConfig.EntityBlueprint blueprint)
            return;

        // 如果没有物理配置ID，跳过
        if (string.IsNullOrEmpty(blueprint.physicsConfigId))
        {
            Debug.Log($"[PhysicsFactory] {blueprint.blueprintId} 没有物理配置，跳过");
            return;
        }

        // 1. 获取物理配置
        var physicsConfig = ConfigManager.Instance.FindPhysicsConfig(blueprint.physicsConfigId);
        if (physicsConfig == null)
        {
            Debug.LogError($"[PhysicsFactory] 找不到物理配置: {blueprint.physicsConfigId}");
            return;
        }

        Debug.Log($"[PhysicsFactory] 添加物理组件: {physicsConfig.configId} (质量: {physicsConfig.mass})");

        // 2. 添加刚体
        AddRigidbody(entity.LogicObject, physicsConfig);

        // 3. 添加碰撞体
        AddColliders(entity.LogicObject, physicsConfig);
    }

    #region 刚体

    /// <summary>
    /// 添加刚体组件
    /// </summary>
    private void AddRigidbody(GameObject logicObj, PhysicsConfigs.PhysicsData physicsConfig)
    {
        var rb = logicObj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = logicObj.AddComponent<Rigidbody>();
        }

        // 设置刚体属性
        rb.mass = physicsConfig.mass;
        rb.isKinematic = physicsConfig.isKinematic;
        rb.useGravity = physicsConfig.useGravity;
        rb.interpolation = physicsConfig.interpolate ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
        rb.collisionDetectionMode = physicsConfig.continuousCollision
            ? CollisionDetectionMode.Continuous
            : CollisionDetectionMode.Discrete;

        Debug.Log($"[PhysicsFactory] 刚体配置: 质量={rb.mass}, 运动学={rb.isKinematic}, 重力={rb.useGravity}");
    }

    #endregion

    #region 碰撞体

    /// <summary>
    /// 添加碰撞体
    /// </summary>
    private void AddColliders(GameObject logicObj, PhysicsConfigs.PhysicsData physicsConfig)
    {
        if (physicsConfig.colliders == null || physicsConfig.colliders.Length == 0)
        {
            Debug.LogWarning($"[PhysicsFactory] 没有碰撞体配置");
            return;
        }

        foreach (var colliderConfig in physicsConfig.colliders)
        {
            AddCollider(logicObj, colliderConfig);
        }
    }

    /// <summary>
    /// 根据配置添加单个碰撞体
    /// </summary>
    private void AddCollider(GameObject obj, PhysicsConfigs.ColliderData config)
    {
        switch (config.type.ToLower())
        {
            case "capsule":
                AddCapsuleCollider(obj, config);
                break;

            case "sphere":
                AddSphereCollider(obj, config);
                break;

            case "box":
                AddBoxCollider(obj, config);
                break;

            case "mesh":
                AddMeshCollider(obj, config);
                break;

            default:
                Debug.LogWarning($"[PhysicsFactory] 未知碰撞体类型: {config.type}");
                break;
        }
    }

    /// <summary>
    /// 添加胶囊碰撞体
    /// </summary>
    private void AddCapsuleCollider(GameObject obj, PhysicsConfigs.ColliderData config)
    {
        var collider = obj.AddComponent<CapsuleCollider>();

        collider.center = config.center;
        collider.radius = config.radius > 0 ? config.radius : 0.5f;
        collider.height = config.height > 0 ? config.height : 2.0f;
        collider.direction = config.direction; // 0=X, 1=Y, 2=Z

        // 设置物理材质
        if (!string.IsNullOrEmpty(config.physicsMaterial))
        {
            collider.material = Resources.Load<PhysicMaterial>(config.physicsMaterial);
        }

        Debug.Log($"[PhysicsFactory] 添加胶囊碰撞体: 半径={collider.radius}, 高度={collider.height}");
    }

    /// <summary>
    /// 添加球体碰撞体
    /// </summary>
    private void AddSphereCollider(GameObject obj, PhysicsConfigs.ColliderData config)
    {
        var collider = obj.AddComponent<SphereCollider>();

        collider.center = config.center;
        collider.radius = config.radius > 0 ? config.radius : 0.5f;

        if (!string.IsNullOrEmpty(config.physicsMaterial))
        {
            collider.material = Resources.Load<PhysicMaterial>(config.physicsMaterial);
        }

        Debug.Log($"[PhysicsFactory] 添加球体碰撞体: 半径={collider.radius}");
    }

    /// <summary>
    /// 添加盒体碰撞体
    /// </summary>
    private void AddBoxCollider(GameObject obj, PhysicsConfigs.ColliderData config)
    {
        var collider = obj.AddComponent<BoxCollider>();

        collider.center = config.center;
        collider.size = config.size != Vector3.zero ? config.size : Vector3.one;

        if (!string.IsNullOrEmpty(config.physicsMaterial))
        {
            collider.material = Resources.Load<PhysicMaterial>(config.physicsMaterial);
        }

        Debug.Log($"[PhysicsFactory] 添加盒体碰撞体: 尺寸={collider.size}");
    }

    /// <summary>
    /// 添加网格碰撞体
    /// </summary>
    private void AddMeshCollider(GameObject obj, PhysicsConfigs.ColliderData config)
    {
        var collider = obj.AddComponent<MeshCollider>();

        // 尝试从视觉体获取网格
        var meshFilter = obj.GetComponentInParent<Entity>()?.ViewObject?.GetComponentInChildren<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            collider.sharedMesh = meshFilter.sharedMesh;
        }

        collider.convex = config.convex;

        Debug.Log($"[PhysicsFactory] 添加网格碰撞体: 凸包={collider.convex}");
    }

    #endregion
}