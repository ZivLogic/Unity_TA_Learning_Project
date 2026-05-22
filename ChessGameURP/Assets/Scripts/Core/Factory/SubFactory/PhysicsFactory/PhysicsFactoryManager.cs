using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PhysicsFactoryManager : MonoBehaviour, IFactory
{
    //物理优先级，比如先加载地板，再加载人
    private Dictionary<string, PhysicsValueConfig> _physicsValueCache = new Dictionary<string, PhysicsValueConfig>();
    //物理组件添加表,指定谁添加物理组件
    private Dictionary<string, PhysicsComponentConfig> _physicsComponentCache = new Dictionary<string, PhysicsComponentConfig>();
    //缓存场景所有实体
    private List<EntityIdentityTag> _globalEntityList = new();
    private List<EntitySpecialIdentityTag> _specialEntityList = new();
    
    public string FactoryName => "Physics";

    //初始化注册自己
    private void Awake()
    {
        FactoryManager.Instance.RegisterFactory(this);
    }

    public void Initialize()
    {
        _physicsComponentCache.Clear();
        _physicsValueCache.Clear();
        _globalEntityList.Clear();
        _specialEntityList.Clear();

    }

    //获取配置
    private void GetPhysicsID()
    {
        _physicsComponentCache = EntityFactoryManager.PhysicsComponentCache;
        _physicsValueCache = EntityFactoryManager.PhysicsValueCache;
        if (_physicsComponentCache == null || _physicsValueCache == null)
        {
            Debug.LogError("[PhysicsFactoryManager]获取配置失败");
            return;
        }
        
    }
    //只查找一次 缓存所有全局/特殊实体
    private void CacheAllEntityTags()
    {
        _globalEntityList = FindObjectsOfType<EntityIdentityTag>().ToList();
        _specialEntityList = FindObjectsOfType<EntitySpecialIdentityTag>().ToList();
    }

    public void AutoAttachAllPhysics()
    {
        GetPhysicsID();
        if (_physicsValueCache == null)
            Debug.LogError("优先级配置为空");
        CacheAllEntityTags();
        if (_globalEntityList == null || _specialEntityList == null)
            Debug.LogError("[PhysicsFactoryManager]实体列表为空");
        Debug.Log($"[PhysicsFactoryManager]全局缓存{_globalEntityList.Count},特殊缓存{_specialEntityList.Count}");
        //过滤启用的配置组 并按优先级排序
        //先筛选配置启用的组
        //var tempList = new List<KeyValuePair<string, PhysicsValueConfig>>();
        //foreach (var kv in _physicsValueCache)
        //{
        //    if (kv.Value.IsEnable)
        //    {

        //        tempList.Add(kv);
        //        Debug.Log($"[PhysicsFactoryManager]启用分组{kv.Key},优先级={kv.Value.Priority}");
        //    }
        //    if (tempList.Count == 0 || tempList == null)
        //    {
        //        Debug.LogError("[PhysicsFactoryManager]没有任何ture的分组");
        //    }
        //}       
        //var enableGroups = tempList.OrderBy(kv => kv.Value.Priority).ToList();
        var enableGroups = _physicsValueCache.Where(kv => kv.Value.IsEnable).OrderBy(kv => kv.Value.Priority).ToList();
        if (enableGroups == null || enableGroups.Count == 0)
            Debug.LogError("[PhysicsFactoryManager]没有启用的配置");
        //按优先级从小到大逐组执行
        foreach (var groupKv in enableGroups)
        {
            ProcessSinglePriorityGroup(groupKv.Value);
        }
        Debug.Log("[PhysicsFactoryManager]挂载物理组件完成");
    }
    //处理单个优先级分组下的所有ID
    private void ProcessSinglePriorityGroup(PhysicsValueConfig groupCfg)
    {
        foreach (string idStr in groupCfg.IDTable)
        {
            ProcessSingleEntityID(idStr);
        }
        //Debug.Log("[PhysicsFactoryManager]第一步完成");
    }
    //处理单个ID：尝试转枚举
    private void ProcessSingleEntityID(string idStr)
    {
        if ( ! _physicsComponentCache.TryGetValue(idStr, out var componentCfg))
        {
            Debug.LogError($"[PhysicsFactoryManager]未找到ID:{idStr}的物理组件配置");
            return;
        }
        //优先匹配基础枚举
        if (PhysicsComponentUtil.TryParseEnum<EntityIdentityType>(idStr, out var globalID))
        {
            MatchGlobalEntityAndAttach(globalID, componentCfg);
            //Debug.Log("[PhysicsFactoryManager]第二步完成");
        }
        else if (PhysicsComponentUtil.TryParseEnum<EntitySpecialIdentityType>(idStr, out var sepcialID))
        {
            MatchSpecialEntityAndAttach(sepcialID, componentCfg);
            //Debug.Log("[PhysicsFactoryManager]第二步完成");
        }
        else 
        {
            Debug.LogError($"[PhysicsFactoryManager]字符串:{idStr}无法转换枚举");
        }
    }
    //匹配全局枚举实体 挂载物理脚本
    private void MatchGlobalEntityAndAttach(EntityIdentityType globalID, PhysicsComponentConfig componentCfg)
    {
        foreach (var entityTag in _globalEntityList)
        {
            if (entityTag.IdentityType != globalID)
                continue;
            //挂载实例
            AttachPhysicsToRoot(entityTag.gameObject, componentCfg);
        }
        //Debug.Log("[PhysicsFactoryManager]第三步完成");

    }
    //匹配特殊枚举
    private void MatchSpecialEntityAndAttach(EntitySpecialIdentityType speecialID, PhysicsComponentConfig componentCfg)
    {
        foreach (var entityTag in _specialEntityList)
        {
            if (entityTag.SpecialType != speecialID)
                continue;
            //挂载实例
            AttachPhysicsToRoot(entityTag.gameObject, componentCfg);
        }
        //Debug.Log("[PhysicsFactoryManager]第三步完成");
    }
    //给根据实体跟物体 按配置自动加载碰撞体/刚体 + 赋值所有属性
    private void AttachPhysicsToRoot(GameObject root, PhysicsComponentConfig componentCfg)
    {
        var compTable = componentCfg.ComponentTable;
        foreach (var compKv in compTable)
        {
            string compName = compKv.Key;
            var paramDict = compKv.Value;
            switch (compName)
            {
                case "Box":
                    DealBoxCollider(root, paramDict);
                    break;
                case "Capsule":
                    DealCapsuleCollider(root, paramDict);
                    break;
                case "Sphere":
                    DealSphereCollider(root, paramDict);
                    break;
                case "Mesh": 
                    DealMeshCollider(root, paramDict);
                    break;
                case "Rigidbody":
                    DealRigidbody(root, paramDict);
                    break;
            }
        }
        //Debug.Log("[PhysicsFactoryManager]第四步完成");
        //Debug.Log($"[PhysicsFactoryManager]加载{root.name}");
    }
    //Box碰撞体
    private void DealBoxCollider(GameObject root, Dictionary<string, object> paramDict)
    {
        var col = PhysicsComponentUtil.AddCollider(root, "Box") as BoxCollider;
        if (col == null)
        {
            Debug.LogError($"[PhysicsFactoryManager]Box添加失败,失败者：{root}");
            return;
        }

        col.center = PhysicsComponentUtil.GetVector3Param(paramDict, "Center");
        col.size = PhysicsComponentUtil.GetVector3Param(paramDict, "Size");
        col.isTrigger = PhysicsComponentUtil.GetParam<bool>(paramDict, "IsTrigger");
    }
    //胶囊体
    private void DealCapsuleCollider(GameObject root, Dictionary<string, object> paramDict)
    {
        var col = PhysicsComponentUtil.AddCollider(root, "Capsule") as CapsuleCollider;
        if (col == null)
        {
            Debug.LogError($"[PhysicsFactoryManager]Capsule添加失败,失败者：{root}");
            return;
        }

        col.center = PhysicsComponentUtil.GetVector3Param(paramDict, "Center");
        col.radius = PhysicsComponentUtil.GetParam<float>(paramDict, "Radius");
        col.height = PhysicsComponentUtil.GetParam<float>(paramDict, "Height");
        col.isTrigger = PhysicsComponentUtil.GetParam<bool>(paramDict, "IsTrigger");
    }
    //Sphere碰撞体
    private void DealSphereCollider(GameObject root, Dictionary<string, object> paramDict)
    {
        var col = PhysicsComponentUtil.AddCollider(root, "Sphere") as SphereCollider;
        if (col == null)
        {
            Debug.LogError($"[PhysicsFactoryManager]Sphere添加失败,失败者：{root}");
            return;
        }

        col.center = PhysicsComponentUtil.GetVector3Param(paramDict, "Center");
        col.radius = PhysicsComponentUtil.GetParam<float>(paramDict, "Radius");
        col.isTrigger = PhysicsComponentUtil.GetParam<bool>(paramDict, "IsTrigger");
    }
    //网格碰撞体
    private void DealMeshCollider(GameObject root, Dictionary<string, object> paramDict)
    {
        var col = PhysicsComponentUtil.AddCollider(root, "Mesh") as MeshCollider;
        if (col == null)
        {
            Debug.LogError($"[PhysicsFactoryManager]Mash添加失败,失败者：{root}");
            return;
        }

        col.convex = PhysicsComponentUtil.GetParam<bool>(paramDict, "Convex");
    }
    //刚体
    private void DealRigidbody(GameObject root, Dictionary<string, object> paramDict)
    {
        float mass = PhysicsComponentUtil.GetParam<float>(paramDict, "Mass", 1f);
        float drag = PhysicsComponentUtil.GetParam<float>(paramDict, "Drag", 0.1f);
        bool gravity = PhysicsComponentUtil.GetParam<bool>(paramDict, "UseGravity");
        bool kinematic = PhysicsComponentUtil.GetParam<bool>(paramDict, "IsKinematic");

        PhysicsComponentUtil.AddRigidbody(root, mass, drag, gravity, kinematic);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
