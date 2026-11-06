using System;
using System.Collections.Generic;
using UnityEngine;

public class MasterFactory : MonoBehaviour
{
    #region 单例

    private static MasterFactory _instance;
    private static bool _isDestroyed = false;

    public static MasterFactory Instance
    {
        get
        {
            // 退出阶段或已销毁：不重建
            if (AppLifecycle.IsQuitting || _isDestroyed) return _instance;

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
    private readonly Dictionary<string, List<Entity>> _entitiesByTag = new();
    private readonly Dictionary<string, Entity> _entitiesByUUID = new();
    private readonly Dictionary<string, List<Entity>> _entitiesByBlueprint = new();
    private readonly Dictionary<EntityType, List<Entity>> _entitiesByType = new();
    #endregion

    #region 统计
    public int TotalEntityCount { get; private set; }
    public int TotalPooledCount => PoolFactory != null ? PoolFactory.TotalPooledCount : 0;
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
        _isDestroyed = false;
        DontDestroyOnLoad(gameObject);

        RegisterFactories();
        UIFactory.Initialize();

        Debug.Log("[MasterFactory] 总工厂初始化完成");
    }

    private void RegisterFactories()
    {
        EntityFactory = new EntityFactory();
        PhysicsFactory = new PhysicsFactory();
        LogicFactory = new LogicFactory();
        RenderFactory = new RenderFactory();
        PoolFactory = new PoolFactory();
        UIFactory = new UIFactory();
        MapFactory = new MapFactory();
        AudioFactory = new AudioFactory();

        _factorySteps.Clear();
        _factorySteps.Add(EntityFactory);
        _factorySteps.Add(PhysicsFactory);
        _factorySteps.Add(LogicFactory);
        _factorySteps.Add(RenderFactory);
        _factorySteps.Add(AudioFactory);
        _factorySteps.Add(PoolFactory);
        _factorySteps.Add(UIFactory);
        _factorySteps.Add(MapFactory);
    }

    #endregion

    #region 实体创建

    public Entity CreateEntity(EntityBlueprintBase blueprint)
    {
        if (blueprint == null) return null;

        var entity = CreateEntitySkeleton(blueprint);

        foreach (var factory in _factorySteps)
            factory.Process(entity);

        RegisterEntity(entity);
        TotalEntityCount++;

        return entity;
    }

    private readonly Dictionary<string, int> _entityCounters = new();

    private string GenerateUUID(string tag)
    {
        if (!_entityCounters.ContainsKey(tag))
            _entityCounters[tag] = 0;

        _entityCounters[tag]++;
        return $"{tag}_{_entityCounters[tag]:D4}";
    }

    private Entity CreateEntitySkeleton(EntityBlueprintBase blueprint)
    {
        string uuid = GenerateUUID(blueprint.tag);

        var entity = new Entity
        {
            EntityId = blueprint.blueprintId,
            UUID = uuid,
            Tag = blueprint.tag,
            Blueprint = blueprint,
            EntityType = DetermineEntityType(blueprint)
        };

        var root = new GameObject(uuid);
        root.tag = blueprint.tag;
        entity.Root = root;

        var logicObj = new GameObject("Logic");
        logicObj.transform.SetParent(root.transform);
        entity.LogicObject = logicObj;

        var viewObj = new GameObject("View");
        viewObj.transform.SetParent(logicObj.transform);
        entity.ViewObject = viewObj;

        var audioObj = new GameObject("Audio");
        audioObj.transform.SetParent(logicObj.transform);
        entity.AudioObject = audioObj;

        root.transform.position = new Vector3(0, blueprint.defaultHeight, 0);

        return entity;
    }

    private EntityType DetermineEntityType(EntityBlueprintBase blueprint)
    {
        if (blueprint is UIBlueprintsConfig.UIBlueprint) return EntityType.UI;
        if (blueprint is EntityBlueprintsConfig.EntityBlueprint) return EntityType.Normal;
        return EntityType.Normal;
    }

    #endregion

    #region 快速匹配

    private void RegisterEntity(Entity entity)
    {
        _entitiesByUUID[entity.UUID] = entity;

        if (!_entitiesByTag.ContainsKey(entity.Tag))
            _entitiesByTag[entity.Tag] = new List<Entity>();
        _entitiesByTag[entity.Tag].Add(entity);

        if (!_entitiesByBlueprint.ContainsKey(entity.Blueprint.blueprintId))
            _entitiesByBlueprint[entity.Blueprint.blueprintId] = new List<Entity>();
        _entitiesByBlueprint[entity.Blueprint.blueprintId].Add(entity);

        if (!_entitiesByType.ContainsKey(entity.EntityType))
            _entitiesByType[entity.EntityType] = new List<Entity>();
        _entitiesByType[entity.EntityType].Add(entity);
    }

    public Entity FindByUUID(string uuid)
        => _entitiesByUUID.TryGetValue(uuid, out var e) ? e : null;

    public Entity FindFirstByTag(string tag)
        => _entitiesByTag.TryGetValue(tag, out var list) && list.Count > 0 ? list[0] : null;

    public List<Entity> FindAllByTag(string tag)
        => _entitiesByTag.TryGetValue(tag, out var list) ? new List<Entity>(list) : new List<Entity>();

    public List<Entity> FindByBlueprint(string blueprintId)
        => _entitiesByBlueprint.TryGetValue(blueprintId, out var list) ? new List<Entity>(list) : new List<Entity>();

    public List<Entity> FindByType(EntityType type)
        => _entitiesByType.TryGetValue(type, out var list) ? new List<Entity>(list) : new List<Entity>();

    public List<Entity> GetAllEntities()
        => new List<Entity>(_entitiesByUUID.Values);

    #endregion

    #region 蓝图获取

    public EntityBlueprintBase GetBlueprint(string blueprintId)
    {
        var entityBlueprints = ConfigAPI.Get<EntityBlueprintsConfig>();
        if (entityBlueprints?.blueprints != null)
        {
            foreach (var bp in entityBlueprints.blueprints)
                if (bp.blueprintId == blueprintId) return bp;
        }

        var uiBlueprints = ConfigAPI.Get<UIBlueprintsConfig>();
        if (uiBlueprints?.blueprints != null)
        {
            foreach (var bp in uiBlueprints.blueprints)
                if (bp.blueprintId == blueprintId) return bp;
        }

        Debug.LogError($"[MasterFactory] 找不到蓝图: {blueprintId}");
        return null;
    }

    #endregion

    #region 销毁管理（唯一权威入口）

    /// <summary>
    /// 统一销毁入口：唯一权威
    /// </summary>
    public void DestroyEntity(string uuid)
    {
        var entity = FindByUUID(uuid);
        if (entity == null) return;

        // 1. 池表同步
        PoolFactory?.OnEntityDestroyed(entity);

        // 2. 总表移除
        RemoveFromAllTables(entity);

        // 3. GameObject 销毁
        if (entity.Root != null)
            Destroy(entity.Root);

        TotalEntityCount--;

        Debug.Log($"[MasterFactory] 实体已销毁: {entity.EntityId} (UUID: {uuid})");
    }

    /// <summary>
    /// 保底：GameObject 被外部直接销毁时清理总表
    /// 优先判断退出状态，退出阶段直接跳过
    /// </summary>
    public void OnEntityMonoDestroyed(Entity entity)
    {
        // 保底：退出阶段不处理
        if (AppLifecycle.IsQuitting || _isDestroyed) return;
        if (entity == null) return;
        if (!_entitiesByUUID.ContainsKey(entity.UUID)) return;

        PoolFactory?.OnEntityDestroyed(entity);
        RemoveFromAllTables(entity);
        TotalEntityCount--;

        Debug.Log($"[MasterFactory] 保底清理: {entity.UUID}");
    }

    private void RemoveFromAllTables(Entity entity)
    {
        _entitiesByUUID.Remove(entity.UUID);
        _entitiesByTag[entity.Tag]?.Remove(entity);
        _entitiesByBlueprint[entity.Blueprint.blueprintId]?.Remove(entity);
        _entitiesByType[entity.EntityType]?.Remove(entity);
    }

    public void DestroyAllByTag(string tag)
    {
        var list = FindAllByTag(tag);
        foreach (var e in list) DestroyEntity(e.UUID);
    }

    public void DestroyAllByBlueprint(string blueprintId)
    {
        var list = FindByBlueprint(blueprintId);
        foreach (var e in list) DestroyEntity(e.UUID);
    }

    public void DestroyAllEntities()
    {
        var allUUIDs = new List<string>(_entitiesByUUID.Keys);
        foreach (var uuid in allUUIDs) DestroyEntity(uuid);
        Debug.Log("[MasterFactory] 所有实体已清空");
    }

    private void OnApplicationQuit()
    {
        // 主动标记退出，优先于任何 OnDestroy
        AppLifecycle.MarkQuitting();

        Debug.Log("[MasterFactory] 开始清理检查...");

        if (LogicStateManager.Instance != null)
            LogicStateManager.Instance.SaveAllStates();

        PoolFactory?.CheckPoolOnExit();

        if (_entitiesByUUID.Count > 0)
        {
            Debug.LogWarning($"[MasterFactory] 退出时仍有 {_entitiesByUUID.Count} 个实体未销毁");
            DestroyAllEntities();
        }

        Debug.Log($"[MasterFactory] 最终统计: 总创建={TotalEntityCount}, 池化={TotalPooledCount}, 活跃={TotalActiveCount}");
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _isDestroyed = true;
            _instance = null;
        }
    }

    #endregion
}