using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 总工厂：统一调度所有子工厂，负责实体创建、UUID分配、快速匹配、销毁管理
/// </summary>
public class MasterFactory : MonoBehaviour
{
    #region 单例

    private static MasterFactory _instance;
    public static MasterFactory Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<MasterFactory>();
                if (_instance == null)
                {
                    var go = new GameObject("MasterFactory");
                    _instance = go.AddComponent<MasterFactory>();
                }
            }
            return _instance;
        }
    }

    #endregion

    #region 工厂列表

    private readonly List<IFactoryStep> _factorySteps = new();

    // 各个子工厂的引用（方便外部访问）
    public EntityFactory EntityFactory { get; private set; }
    public PhysicsFactory PhysicsFactory { get; private set; }
    public LogicFactory LogicFactory { get; private set; }
    public RenderFactory RenderFactory { get; private set; }
    public PoolFactory PoolFactory { get; private set; }
    public UIFactory UIFactory { get; private set; }
    public MapFactory MapFactory { get; private set; }
    public AudioFactory AudioFactory { get; private set; }

    #endregion

    #region 快速匹配缓存

    // Tag → 实体列表（快速查找）
    private readonly Dictionary<string, List<Entity>> _entitiesByTag = new();

    // UUID → 实体（精确查找）
    private readonly Dictionary<string, Entity> _entitiesByUUID = new();

    // 蓝图ID → 实体列表（按蓝图类型统计）
    private readonly Dictionary<string, List<Entity>> _entitiesByBlueprint = new();

    // 实体类型 → 实体列表（逻辑体、视觉体、音频体）
    private readonly Dictionary<EntityType, List<Entity>> _entitiesByType = new();

    #endregion

    #region 统计信息

    public int TotalEntityCount { get; private set; }
    public int TotalPooledCount { get; private set; }
    public int TotalActiveCount => TotalEntityCount - TotalPooledCount;

    #endregion

    #region 初始化

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // 注册所有工厂
        RegisterFactories();

        Debug.Log("[MasterFactory] 总工厂初始化完成");
    }

    /// <summary>
    /// 注册所有子工厂
    /// </summary>
    private void RegisterFactories()
    {
        // 创建工厂实例
        EntityFactory = new EntityFactory();
        PhysicsFactory = new PhysicsFactory();
        LogicFactory = new LogicFactory();
        RenderFactory = new RenderFactory();
        PoolFactory = new PoolFactory();
        UIFactory = new UIFactory();
        MapFactory = new MapFactory();
        AudioFactory = new AudioFactory();

        // 按顺序添加到流水线
        _factorySteps.Clear();
        _factorySteps.Add(EntityFactory);
        _factorySteps.Add(PhysicsFactory);
        _factorySteps.Add(LogicFactory);
        _factorySteps.Add(RenderFactory);
        _factorySteps.Add(AudioFactory);
        _factorySteps.Add(PoolFactory);
        _factorySteps.Add(UIFactory);
        _factorySteps.Add(MapFactory);

        Debug.Log($"[MasterFactory] 注册 {_factorySteps.Count} 个工厂");
    }

    #endregion

    #region 实体创建

    /// <summary>
    /// 创建实体（接受基类蓝图）
    /// </summary>
    public Entity CreateEntity(EntityBlueprintBase blueprint)
    {
        if (blueprint == null) return null;

        // 1. 创建实体骨架
        var entity = CreateEntitySkeleton(blueprint);

        // 2. 流水线处理
        foreach (var factory in _factorySteps)
        {
            factory.Process(entity);
        }

        // 3. 注册
        RegisterEntity(entity);

        // 4. 更新统计
        TotalEntityCount++;

        return entity;
    }

    /// <summary>
    /// 创建实体骨架
    /// </summary>
    private Entity CreateEntitySkeleton(EntityBlueprintBase blueprint)
    {
        // 分配 UUID
        string uuid = Guid.NewGuid().ToString();

        // 创建实体对象
        var entity = new Entity
        {
            EntityId = blueprint.blueprintId,
            UUID = uuid,
            Tag = blueprint.tag,
            Blueprint = blueprint,
            EntityType = DetermineEntityType(blueprint)
        };

        // 创建根节点
        var root = new GameObject($"{blueprint.blueprintId}_{uuid.Substring(0, 8)}");
        root.tag = blueprint.tag;
        entity.Root = root;

        // 创建逻辑体
        var logicObj = new GameObject("Logic");
        logicObj.transform.SetParent(root.transform);
        entity.LogicObject = logicObj;

        // 创建视觉体
        var viewObj = new GameObject("View");
        viewObj.transform.SetParent(root.transform);
        entity.ViewObject = viewObj;

        // 创建音频体
        var audioObj = new GameObject("Audio");
        audioObj.transform.SetParent(root.transform);
        entity.AudioObject = audioObj;

        // 设置默认位置
        root.transform.position = new Vector3(0, blueprint.defaultHeight, 0);

        return entity;
    }

    /// <summary>
    /// 判断实体类型
    /// </summary>
    private EntityType DetermineEntityType(EntityBlueprintBase blueprint)
    {
        if (blueprint is UIBlueprintsConfig.UIBlueprint)
            return EntityType.UI;

        if (blueprint is EntityBlueprintsConfig.EntityBlueprint)
            return EntityType.Normal;

        return EntityType.Normal;
    }

    #endregion

    #region 快速匹配

    /// <summary>
    /// 注册实体到快速匹配缓存
    /// </summary>
    private void RegisterEntity(Entity entity)
    {
        // 1. 按 UUID 注册
        _entitiesByUUID[entity.UUID] = entity;

        // 2. 按 Tag 注册
        if (!_entitiesByTag.ContainsKey(entity.Tag))
        {
            _entitiesByTag[entity.Tag] = new List<Entity>();
        }
        _entitiesByTag[entity.Tag].Add(entity);

        // 3. 按蓝图ID注册
        if (!_entitiesByBlueprint.ContainsKey(entity.Blueprint.blueprintId))
        {
            _entitiesByBlueprint[entity.Blueprint.blueprintId] = new List<Entity>();
        }
        _entitiesByBlueprint[entity.Blueprint.blueprintId].Add(entity);

        // 4. 按实体类型注册
        if (!_entitiesByType.ContainsKey(entity.EntityType))
        {
            _entitiesByType[entity.EntityType] = new List<Entity>();
        }
        _entitiesByType[entity.EntityType].Add(entity);
    }

    /// <summary>
    /// 通过 UUID 查找实体
    /// </summary>
    public Entity FindByUUID(string uuid)
    {
        return _entitiesByUUID.TryGetValue(uuid, out var entity) ? entity : null;
    }

    /// <summary>
    /// 通过 Tag 查找第一个实体
    /// </summary>
    public Entity FindFirstByTag(string tag)
    {
        if (_entitiesByTag.TryGetValue(tag, out var entities) && entities.Count > 0)
        {
            return entities[0];
        }
        return null;
    }

    /// <summary>
    /// 通过 Tag 查找所有实体
    /// </summary>
    public List<Entity> FindAllByTag(string tag)
    {
        return _entitiesByTag.TryGetValue(tag, out var entities)
            ? new List<Entity>(entities)
            : new List<Entity>();
    }

    /// <summary>
    /// 通过蓝图ID查找所有实体
    /// </summary>
    public List<Entity> FindByBlueprint(string blueprintId)
    {
        return _entitiesByBlueprint.TryGetValue(blueprintId, out var entities)
            ? new List<Entity>(entities)
            : new List<Entity>();
    }

    /// <summary>
    /// 通过实体类型查找所有实体
    /// </summary>
    public List<Entity> FindByType(EntityType type)
    {
        return _entitiesByType.TryGetValue(type, out var entities)
            ? new List<Entity>(entities)
            : new List<Entity>();
    }

    #endregion

    #region 蓝图获取

    /// 获取蓝图（从配置系统）
    /// 返回基类，子工厂根据类型判断
    /// </summary>
    public EntityBlueprintBase GetBlueprint(string blueprintId)
    {
        // 1. 先尝试从实体蓝图配置获取
        var entityBlueprints = ConfigAPI.Get<EntityBlueprintsConfig>();
        if (entityBlueprints?.blueprints != null)
        {
            foreach (var bp in entityBlueprints.blueprints)
            {
                if (bp.blueprintId == blueprintId)
                    return bp;
            }
        }

        // 2. 尝试从UI蓝图配置获取
        var uiBlueprints = ConfigAPI.Get<UIBlueprintsConfig>();
        if (uiBlueprints?.blueprints != null)
        {
            foreach (var bp in uiBlueprints.blueprints)
            {
                if (bp.blueprintId == blueprintId)
                    return bp;
            }
        }

        Debug.LogError($"[MasterFactory] 找不到蓝图: {blueprintId}");
        return null;
    }

    #endregion

    #region 销毁管理

    /// <summary>
    /// 销毁实体
    /// </summary>
    public void DestroyEntity(string uuid)
    {
        var entity = FindByUUID(uuid);
        if (entity == null) return;

        // 从所有缓存中移除
        _entitiesByUUID.Remove(uuid);
        _entitiesByTag[entity.Tag]?.Remove(entity);
        _entitiesByBlueprint[entity.Blueprint.blueprintId]?.Remove(entity);
        _entitiesByType[entity.EntityType]?.Remove(entity);

        // 销毁 GameObject
        if (entity.Root != null)
        {
            Destroy(entity.Root);
        }

        TotalEntityCount--;

        Debug.Log($"[MasterFactory] 实体已销毁: {entity.EntityId} (UUID: {uuid})");
    }

    /// <summary>
    /// 清空所有实体
    /// </summary>
    public void DestroyAllEntities()
    {
        var allUUIDs = new List<string>(_entitiesByUUID.Keys);
        foreach (var uuid in allUUIDs)
        {
            DestroyEntity(uuid);
        }

        Debug.Log("[MasterFactory] 所有实体已清空");
    }

    /// <summary>
    /// 程序退出前的清理检查
    /// </summary>
    private void OnApplicationQuit()
    {
        Debug.Log("[MasterFactory] 开始清理检查...");

        // 1. 检查对象池是否有残余
        if (PoolFactory != null)
        {
            PoolFactory.CheckPoolOnExit();
        }

        // 2. 检查是否有未销毁的实体
        if (_entitiesByUUID.Count > 0)
        {
            Debug.LogWarning($"[MasterFactory] 退出时仍有 {_entitiesByUUID.Count} 个实体未销毁");

            // 自动清理
            DestroyAllEntities();
        }
        else
        {
            Debug.Log("[MasterFactory] 所有实体已正确销毁");
        }

        // 3. 输出统计信息
        Debug.Log($"[MasterFactory] 最终统计:\n" +
                  $"  总创建: {TotalEntityCount}\n" +
                  $"  对象池: {TotalPooledCount}\n" +
                  $"  活跃: {TotalActiveCount}");
    }

    #endregion
}