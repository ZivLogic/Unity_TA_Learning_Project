using System;
using UnityEngine;

//实体工厂：流水线第一步
//职责：根据蓝图类型创建对应的实体基础结构
// - 普通实体：创建模型到视觉体
// - UI实体：创建UI骨架（实际挂载由UIFactory处理）
// - 所有实体：添加 EntiityMono 标识组件

public class EntityFactory : IFactoryStep
{
    public void Process(Entity entity)
    {
        if (entity?.Blueprint == null)return;

        //根据蓝图类型分发
        switch (entity.Blueprint)
        {
            case EntityBlueprintsConfig.EntityBlueprint entityBlueprint:
                ProcessEntityBlueprint(entity, entityBlueprint);
                break;
            case UIBlueprintsConfig.UIBlueprint uiBlueprint:
                ProcessUIBlueprint(entity, uiBlueprint);
                break;
            default:
                Debug.LogWarning($"[EntityFactory] 未知蓝图类型：{entity.Blueprint.GetType().Name}");
                break;
        }
    }

    #region 普通实体处理

    private void ProcessEntityBlueprint(Entity entity, EntityBlueprintsConfig.EntityBlueprint blueprint)
    {
        Debug.Log($"[EntityFactory] 处理普通实体：{blueprint.blueprintId}");

        //1.加载模型到视觉体
        LoadModel(entity, blueprint);

        //2.设置默认位置
        SetDefaultPosition(entity, blueprint);

        //3.添加实体标识组件
        AddEntityComponent(entity);
    }

    private void LoadModel(Entity entity, EntityBlueprintsConfig.EntityBlueprint blueprint)
    {
        if (string.IsNullOrEmpty(blueprint.modelPath))
        {
            Debug.LogWarning($"[EntityFactory] {blueprint.blueprintId} 没有模型路径");
            return;
        }

        var modelPrefab = Resources.Load<GameObject>(blueprint.modelPath );
        if ( modelPrefab == null)
        {
            Debug.LogError($"[EntityFactory] 模型加载失败：{blueprint.modelPath}");
            return;
        }

        var modelInstance = GameObject.Instantiate( modelPrefab, entity.ViewObject.transform );
        modelInstance.name = "Model";

        entity.Renderer = modelInstance.GetComponentInChildren<Renderer>();
        entity.ModelInstance = modelInstance;

        Debug.Log($"[EntityFactory] 模型加载成功：{blueprint.modelPath}");
    }

    private void SetDefaultPosition(Entity entity, EntityBlueprintsConfig.EntityBlueprint blueprint)
    {
        entity.Root.transform.position = new Vector3(0, blueprint.defaultHeight, 0);
        Debug.Log($"[EntityFactory] 默认位置：（0, {blueprint.defaultHeight}, 0）");
    }
    #endregion

    #region UI实体处理

    //处理UI实体（只做骨架和标识，实际挂载由UIFactory处理）
    private void ProcessUIBlueprint(Entity entity, UIBlueprintsConfig.UIBlueprint blueprint)
    {
        Debug.Log($"[EntityFactory] 处理UI实体：{blueprint.blueprintId}");

        //1.设置默认位置（UI不需要物理，直接归零）
        entity.Root.transform.position = Vector3.zero;

        //2.添加实体标识组件
        AddEntityComponent(entity);

        //3.UI的实际挂载由 UIFactory 处理
    }
    #endregion

    #region 通用处理

    //添加实体标识组件（所有实体通用）
    private void AddEntityComponent(Entity entity)
    {
        var entityMono = entity.Root.AddComponent<EntityMono>();
        entityMono.Entity = entity;

        Debug.Log($"[EntityFactory] 添加EntityMono: {entityMono.EntityId}, UUID: {entityMono.UUID}, Tag: {entityMono.Tag}");
    }
    #endregion
}