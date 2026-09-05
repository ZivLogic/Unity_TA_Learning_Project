using System;
using UnityEngine;

//实体工厂：流水线第一步
//职责：根据蓝图类型创建对应的实体基础结构
//—普通实体：创建模型到视觉体
//—UI实体：加载UI预制体
//—地图实体：创建地形

public class EntityFactory : IFactoryStep
{
    //处理实体
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

    //处理普通实体蓝图
    private void ProcessEntityBlueprint(Entity entity, EntityBlueprintsConfig.EntityBlueprint blueprint)
    {
        Debug.Log($"[EntityFactory] 处理普通实体：{blueprint.blueprintId}");

        // 1.加载模型到视觉体
        LoadModel(entity, blueprint);

        // 2.设置默认位置
        SetDefaultPostion(entity, blueprint);

        // 3.添加实体标识组件
        AddEntityComponent(entity);
    }

    //加载模型到视觉体
    private void LoadModel(Entity entity, EntityBlueprintsConfig.EntityBlueprint blueprint)
    {
        //如果没有模型路径，跳过
        if (string.IsNullOrEmpty(blueprint.modelPath))
        {
            Debug.LogWarning($"[EntityFactory] {blueprint.blueprintId}没有模型路径");
            return;
        }

        //加载模型预制体
        var modelPrefab = Resources.Load<GameObject>(blueprint.modelPath);
        if ( modelPrefab == null)
        {
            Debug.LogError($"[EntityFactory] 模型加载失败：{blueprint.modelPath}");
            return;
        }

        //实例化模型到视觉体
        var modelInstance = GameObject.Instantiate(modelPrefab, entity.ViewObject.transform);
        modelInstance.name = "Model";

        //获取渲染器引用（供后续渲染工厂使用）
        entity.Renderer = modelInstance.GetComponentInChildren<Renderer>();

        if (entity.Renderer != null)
        {
            Debug.Log($"[Entityfactory] 模型加载成功：{blueprint.modelPath}(渲染器：{entity.Renderer.GetType().Name})");
        }
        else
        {
            Debug.LogWarning($"[EntityFactory] 模型没有渲染器组件：{blueprint.modelPath}");
        }

        //记录模型引用
        entity.ModelInstance = modelInstance;
    }

    //设置默认位置
    private void SetDefaultPostion(Entity entity, EntityBlueprintsConfig.EntityBlueprint blueprint)
    {
        //默认放在世界原点上方一点（防止物理掉落）
        entity.Root.transform.position = new Vector3(
                0,
                blueprint.defaultHeight,
                0
            );

        Debug.Log($"[EntityFactory] 默认位置：（0，{blueprint.defaultHeight}, 0");
    }

    //添加实体标识组件
    private void AddEntityComponent(Entity entity)
    {
        //在跟节点添加EntityMono组件
        var entityMono = entity.Root.AddComponent<EntityMono>();
        entityMono.Entity = entity;

        Debug.Log($"[EntityFactory] 添加实体标识组件：{entity.EntityId}");
    }
    #endregion

    #region UI实体处理
    //处理UI实体蓝图
    private void ProcessUIBlueprint(Entity entity, UIBlueprintsConfig.UIBlueprint blueprint)
    {
        if (string.IsNullOrEmpty(blueprint.prefabPath))
        {
            Debug.LogWarning($"[EntiyFactory]{blueprint.blueprintId} 没有UI预制体路径");
            return;
        }

        //加载UI预制体
        var prefab = Resources.Load<GameObject>(blueprint.prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[EntityFactory] UI预制体加载失败：{blueprint.prefabPath}");
            return;
        }

        //实例化到视觉体
        var uiInstance = GameObject.Instantiate(prefab, entity.ViewObject.transform);
        uiInstance.name = "UI_View";

        //获取渲染器（UI用 CanvasRenderer）
        entity.Renderer = uiInstance.GetComponentInChildren<Renderer>();

        Debug.Log($"[EntityFactory] UI预制体加载成功：{blueprint.prefabPath}");
    }

    //挂载到Canvas
    private void AttachToCanvas(Entity entity, UIBlueprintsConfig.UIBlueprint blueprint)
    {
        if (string.IsNullOrEmpty(blueprint.canvasName))
            return;

        //查找Canvas
        var canvasObj = GameObject.Find(blueprint.canvasName);
        if (canvasObj == null)
        {
            Debug.LogWarning($"[EntityFactory] 找不到Canvas:{blueprint.canvasName}");
            return;
        }

        //挂载到Canvas
        entity.Root.transform.SetParent(canvasObj.transform, false);

        //设置渲染顺序
        if (blueprint.sortingOrder > 0)
        {
            var canvas = canvasObj.GetComponentInChildren<Canvas>();
            if (canvas != null)
            {
                canvas.sortingOrder = blueprint.sortingOrder;
            }

            Debug.Log($"[EntityFactory] UI挂载到Canvas:{blueprint.canvasName}");
        }
    }
    #endregion
}