// Configs/ConfigClasses/PhysicsConfigs.cs
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