// Framework/Factories/RenderFactory.cs
using System;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 渲染工厂：流水线第四步
/// 职责：根据蓝图配置，给视觉体设置材质、阴影、LOD等渲染相关组件
/// </summary>
public class RenderFactory : IFactoryStep
{
    /// <summary>
    /// 处理实体
    /// </summary>
    public void Process(Entity entity)
    {
        if (entity?.Blueprint == null) return;

        // 只有普通实体蓝图才有渲染配置
        if (entity.Blueprint is not EntityBlueprintsConfig.EntityBlueprint blueprint)
            return;

        // 如果没有材质配置ID，跳过
        if (string.IsNullOrEmpty(blueprint.materialId))
        {
            Debug.Log($"[RenderFactory] {blueprint.blueprintId} 没有材质配置，跳过");
            return;
        }

        // 1. 获取材质配置
        var materialConfig = ConfigManager.Instance.FindMaterialConfig(blueprint.materialId);
        if (materialConfig == null)
        {
            Debug.LogError($"[RenderFactory] 找不到材质配置: {blueprint.materialId}");
            return;
        }

        Debug.Log($"[RenderFactory] 设置渲染: {materialConfig.materialId} (路径: {materialConfig.path})");

        // 2. 设置材质
        ApplyMaterial(entity, materialConfig);

        // 3. 设置阴影
        ApplyShadowSettings(entity, materialConfig);

        // 4. 设置渲染层级
        ApplyRenderLayer(entity, materialConfig);
    }

    #region 材质设置

    /// <summary>
    /// 应用材质
    /// </summary>
    private void ApplyMaterial(Entity entity, MaterialDatabaseConfig.MaterialData materialConfig)
    {
        // 检查渲染器是否存在
        if (entity.Renderer == null)
        {
            // 尝试从视觉体获取
            entity.Renderer = entity.ViewObject?.GetComponentInChildren<Renderer>();

            if (entity.Renderer == null)
            {
                Debug.LogWarning($"[RenderFactory] 视觉体没有渲染器组件，无法设置材质");
                return;
            }
        }

        // 加载材质
        Material material = LoadMaterial(materialConfig);

        if (material != null)
        {
            entity.Renderer.material = material;
            Debug.Log($"[RenderFactory] 材质应用成功: {material.name}");
        }
        else
        {
            Debug.LogWarning($"[RenderFactory] 材质加载失败，使用默认材质");
            entity.Renderer.material = GetDefaultMaterial();
        }
    }

    /// <summary>
    /// 加载材质
    /// </summary>
    private Material LoadMaterial(MaterialDatabaseConfig.MaterialData materialConfig)
    {
        // 1. 尝试从 Resources 加载
        if (!string.IsNullOrEmpty(materialConfig.path))
        {
            var material = Resources.Load<Material>(materialConfig.path);
            if (material != null)
            {
                return CreateMaterialInstance(material, materialConfig);
            }
        }

        // 2. 尝试创建新材质
        if (!string.IsNullOrEmpty(materialConfig.shaderName))
        {
            var shader = Shader.Find(materialConfig.shaderName);
            if (shader != null)
            {
                var newMaterial = new Material(shader);
                ApplyMaterialProperties(newMaterial, materialConfig);
                return newMaterial;
            }
        }

        return null;
    }

    /// <summary>
    /// 创建材质实例（避免修改原始材质）
    /// </summary>
    private Material CreateMaterialInstance(Material originalMaterial, MaterialDatabaseConfig.MaterialData materialConfig)
    {
        // 创建实例，避免影响其他使用同一材质的物体
        var materialInstance = new Material(originalMaterial);

        // 应用配置中的属性
        ApplyMaterialProperties(materialInstance, materialConfig);

        return materialInstance;
    }

    /// <summary>
    /// 应用材质属性
    /// </summary>
    private void ApplyMaterialProperties(Material material, MaterialDatabaseConfig.MaterialData materialConfig)
    {
        // 设置主颜色（兼容两种Shader）
        if (materialConfig.mainColor != default)
        {
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", materialConfig.mainColor);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", materialConfig.mainColor);
            }
            else
            {
                material.color = materialConfig.mainColor;
            }
        }

        // 设置金属度
        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", materialConfig.metallic);
        }

        // 设置光滑度（兼容两种Shader）
        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", materialConfig.smoothness);
        }
        else if (material.HasProperty("_Glossiness"))
        {
            material.SetFloat("_Glossiness", materialConfig.smoothness);
        }

        Debug.Log($"[RenderFactory] 材质属性: 颜色={material.color}, 金属度={materialConfig.metallic}, 光滑度={materialConfig.smoothness}");
    }

    /// <summary>
    /// 获取默认材质
    /// </summary>
    private Material GetDefaultMaterial()
    {
        // Unity 内置默认材质
        var shader = Shader.Find("Standard");
        return new Material(shader);
    }

    #endregion

    #region 阴影设置

    /// <summary>
    /// 应用阴影设置
    /// </summary>
    private void ApplyShadowSettings(Entity entity, MaterialDatabaseConfig.MaterialData materialConfig)
    {
        if (entity.Renderer == null) return;

        // 设置阴影投射
        entity.Renderer.shadowCastingMode = materialConfig.castShadows
            ? ShadowCastingMode.On
            : ShadowCastingMode.Off;

        // 设置阴影接收
        entity.Renderer.receiveShadows = materialConfig.receiveShadows;

        Debug.Log($"[RenderFactory] 阴影设置: 投射={entity.Renderer.shadowCastingMode}, 接收={entity.Renderer.receiveShadows}");
    }

    #endregion

    #region 渲染层级

    /// <summary>
    /// 应用渲染层级
    /// </summary>
    private void ApplyRenderLayer(Entity entity, MaterialDatabaseConfig.MaterialData materialConfig)
    {
        if (entity.Root == null) return;

        // 设置渲染层级（用于摄像机裁剪）
        if (!string.IsNullOrEmpty(materialConfig.renderLayer))
        {
            int layer = LayerMask.NameToLayer(materialConfig.renderLayer);
            if (layer != -1)
            {
                SetLayerRecursively(entity.Root, layer);
                Debug.Log($"[RenderFactory] 渲染层级: {materialConfig.renderLayer} ({layer})");
            }
        }
    }

    /// <summary>
    /// 递归设置层级
    /// </summary>
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    #endregion
}