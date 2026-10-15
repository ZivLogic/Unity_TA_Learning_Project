using System;
using System.Collections.Generic;
using UnityEngine;

//批量生产启动器：读取生产配置，批量创建实体
public class ProductionLauncher :  MonoBehaviour
{
    private static ProductionLauncher _instance;
    public static ProductionLauncher Instance => _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log("[ProductionLauncher] 工厂蓝图启动器初始化完成");
    }


    //启动批量生产
    public void Launch()
    {
        Debug.Log("[ProductionLauncher] =========== 开始批量生产 ===========");

        //1.读取生产配置
        var config = ConfigAPI.Get<ProductionConfigs>();

        if (config?.entries == null)
        {
            Debug.LogWarning("[ProductionLauncher] 生产配置为空");
            return;
        }

        //2.遍历每个条目
        foreach (var entry in config.entries )
        {
            ProduceEntry(entry);
        }

        Debug.Log("[ProductionLauncher] ========== 批量生产完成 ===========");
    }


    //生产单个条目
    private void ProduceEntry(ProductionConfigs.ProductionEntry entry)
    {
        Debug.Log($"[ProdctionLauncher] 生产：{entry.blueprintId} + 总数：{entry.count}, 激活：{entry.spawnCount}, 预热：{entry.prewarm}");

        //1.获取蓝图
        var blueprint = MasterFactory.Instance.GetBlueprint(entry.blueprintId);
        if (blueprint == null)
        {
            Debug.LogError($"[ProductionLauncher] 找不到蓝图：{entry.blueprintId}");
            return;
        }

        //2.根据预热开关决定策略
        if (entry.prewarm)
        {
            ProduceWithPrewarm(entry, blueprint);
        }
        else
        {
            ProduceDirectly(entry, blueprint);
        }

    }

    //预热模式：创建 count 个， 取出 spawnCount 个
    private void ProduceWithPrewarm(ProductionConfigs.ProductionEntry entry, EntityBlueprintBase blueprint)
    {
        //1.开启预热模式
        MasterFactory.Instance.PoolFactory.BeginPrewarm();

        //2.预热：创建 count 个实体
        var prewarmedEntities = new List<Entity>();

        for (int i = 0; i < entry.count; i++)
        {
            var entity = MasterFactory.Instance.CreateEntity(blueprint);
            if (entity == null)break;
            prewarmedEntities.Add(entity);
        }

        //3.关闭预热模式
        MasterFactory.Instance.PoolFactory.EndPrewarm();

        //算出实际要用的数量，只对多余的入池
        int spawnCount = Mathf.Min(entry.spawnCount, prewarmedEntities.Count);
        int prewarmOnlyCount = prewarmedEntities.Count - spawnCount;  //纯预热、不激活的部分

        //4.差量入池：只把“多余”的部分放回池
        //约定：前 spawnCount 个保持 active 直接使用，后 prewarmOnlyCount 个入池
        for (int i = spawnCount; i < prewarmedEntities.Count; i++)
        {
            MasterFactory.Instance.PoolFactory.ReturnToPool(prewarmedEntities[i]);
        }
        Debug.Log($"[ProductionLauncher] 预热完成：{prewarmedEntities.Count} 个，其中激活 {spawnCount} 个，池化 {prewarmOnlyCount}个");

        //5.激活：直接使用前 spawnCount 个， 不再走 GetFromPool
        for (int i = 0; i < spawnCount;  i++)
        {
            var entity = prewarmedEntities[i];

            Vector3 position = entry.basePosition + entry.spacing * i;
            entity.Root.transform.position = position;

            if (!string.IsNullOrEmpty(entry.tagOverride))
            {
                entity.Root.tag = entry.tagOverride;
                entity.Tag = entry.tagOverride;
            }

            Debug.Log($"[ProdctionLauncher] 激活{entity.UUID} at {position}");
        }
    }

    //直接创建模式：不预热，直接创建 spawnCount个
    private void ProduceDirectly(ProductionConfigs.ProductionEntry entry, EntityBlueprintBase blueprint)
    {
        int spawnCount = entry.spawnCount;
        Debug.Log($"[ProductionLauncher] 直接创建 {spawnCount} 个");

        for (int i = 0; i < spawnCount; i++)
        {
            var entity = MasterFactory.Instance.CreateEntity(blueprint);
            if (entity == null) break;

            // 计算位置
            Vector3 position = entry.basePosition + entry.spacing * i;
            entity.Root.transform.position = position;

            // 覆盖Tag
            if (!string.IsNullOrEmpty(entry.tagOverride))
            {
                entity.Root.tag = entry.tagOverride;
                entity.Tag = entry.tagOverride;
            }

            Debug.Log($"[ProductionLauncher] 创建: {entity.UUID} at {position}");
        }
    }

    //重置并重新生产
    public void ResetAndRelaunch()
    {
        Debug.Log("[ProductionLauncher] 重置并重新生产...");

        //1.销毁所有实体
        MasterFactory.Instance.DestroyAllEntities();

        //2.清空对象池
        MasterFactory.Instance.PoolFactory.ClearAllPools();

        //3.重新生产
        Launch();
    }
}