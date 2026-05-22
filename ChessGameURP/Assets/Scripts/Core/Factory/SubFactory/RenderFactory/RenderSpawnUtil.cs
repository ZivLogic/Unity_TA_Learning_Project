using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//渲染工厂 静态工具层
//负责：模型实例化，绑定逻辑父，原点自适应，组件添加，基础渲染设置
public class RenderSpawnUtil
{
    #region 模型实例化 + 绑定到逻辑根父物体
    //模型实例化并绑定到指定逻辑根
    public static GameObject SpawnModelBindToParent(GameObject modelPrefab, GameObject logicParent)
    {
        if (modelPrefab == null || logicParent == null)
        {
            Debug.LogError($"[RenderSpawnUtil]模型{modelPrefab.name}为空或逻辑体{logicParent.name}为空");
            return null;
        }
        GameObject model = Object.Instantiate(modelPrefab, logicParent.transform);
        model.name = $"{logicParent.name}_Model";
        //暂时用不上偏移
        //model.transform.localPosition = localOffset;
        //model.transform.localRotation = Quaternion.Euler(localEuler);
        //自动适配模型原点高度
        //AdaptModelOriginHeight(model);
        return model;
    }
    #endregion
    #region 特效 实例化 + 绑定父级（独立方法，后续拓展用）
    public static GameObject SpawnEffectBindToParent(GameObject effectPrefab, GameObject parent,  Vector3 localPos, Vector3 localEuler)
    {
        if (effectPrefab == null || parent == null)
            return null;
        GameObject effect = Object.Instantiate(effectPrefab, parent.transform);
        effect.name = $"Effect_{effectPrefab.name}";
        effect.transform.localPosition = localPos;
        effect.transform.localRotation = Quaternion .Euler(localEuler);
        return effect;
    }
    #endregion
    #region 模型原点自动适配（底部/中心 自动补偿Y偏移）
    public static void AdaptModelOriginHeight(GameObject model)
    {
        if (model == null)
            return;
        Renderer rend = model.GetComponentInChildren<Renderer>();
        if (rend == null)return;
        //原点在底部，无需偏移
        if (Mathf.Abs(rend.bounds.center.y) < 0.05f)
            return;
        //原点在中心，向下补偿半高
        float halfHeight = rend.bounds.extents.y;
        model.transform.localPosition += new Vector3(0, halfHeight, 0);
    }
    #endregion
    #region 通用：添加泛型渲染组件
    public static T AddRenderComponent<T>(GameObject target) where T : Component
    {
        T comp = target.GetComponent<T>();
        if (comp == null)
            comp = target.AddComponent<T>();
        return comp;
    }
    #endregion
    #region 通用：根据组件名字符串挂载组件
    public static Component AddComponentByName(GameObject target, string componentName)
    {
        if (target == null || string.IsNullOrEmpty(componentName)) return null;

        System.Type compType = System.Type.GetType(componentName);
        if (compType == null)
        {
            Debug.LogError($"[RenderSpawnUtil] 找不到渲染组件类型：{componentName}");
            return null;
        }

        Component comp = target.GetComponent(compType);
        if (comp == null)
            comp = target.AddComponent(compType);

        return comp;
    }
    #endregion
    #region 通用：设置物体渲染Layer（含子物体）
    public static void SetRenderLayer(GameObject target, string layerName)
    {
        if (target == null) return;
        int layer = LayerMask.NameToLayer(layerName);
        target.layer = layer;
        foreach (Transform child in target.transform)
        {
            child.gameObject.layer = layer;
        }
    }
    #endregion
    #region 按 大类+中类 搜寻场景所有逻辑实体
    public static List<GameObject> FindLogicEntityByIdentity(EntityIdentityType major, EntitySpecialIdentityType minor)
    {
        List<GameObject> result = new List<GameObject>();
        EntityIdentityTag[] allMajorTags = Object.FindObjectsOfType<EntityIdentityTag>();

        foreach (var majorTag in allMajorTags)
        {
            if (majorTag.IdentityType != major) continue;

            EntitySpecialIdentityTag minorTag = majorTag.GetComponent<EntitySpecialIdentityTag>();
            // 无中类匹配None
            if (minorTag == null)
            {
                if (minor == EntitySpecialIdentityType.None)
                    result.Add(majorTag.gameObject);
                continue;
            }
            // 匹配指定中类
            if (minorTag.SpecialType == minor)
            {
                result.Add(majorTag.gameObject);
            }
        }
        return result;
    }
    #endregion
}