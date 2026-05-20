using System.Collections.Generic;
using UnityEngine;


/// 实体生成静态工具层
/// 只负责：逻辑空根创建、父子层级绑定、身份组件赋值
/// 逻辑体与视图体完全分离，不处理任何模型偏移

public static class EntitySpawnUtil
{
    #region 创建【逻辑空实体根】适配 位置+旋转配置    
    /// 根据世界位置、欧拉旋转 创建标准化逻辑空根
    public static GameObject CreateEmptyEntityRoot(string rootName, Vector3 worldPos, Vector3 eulerRotation)
    {
        GameObject entityRoot = new GameObject(rootName);
        entityRoot.transform.position = worldPos;
        entityRoot.transform.rotation = Quaternion.Euler(eulerRotation);
        return entityRoot;
    }
    /// 重载：无旋转 默认0
    public static GameObject CreateEmptyEntityRoot(string rootName, Vector3 worldPos)
    {
        return CreateEmptyEntityRoot(rootName, worldPos, Vector3.zero);
    }
    #endregion

    #region 父子层级绑定（纯逻辑空物体挂载）
    /// 单个逻辑实体绑定到父级，保持局部不变
    public static void BindEntityToParent(GameObject childRoot, GameObject parentRoot)
    {
        if (childRoot == null || parentRoot == null) return;
        childRoot.transform.SetParent(parentRoot.transform, false);
    }
    /// 批量逻辑实体统一挂到同一个父级
    public static void BindEntityListToParent(List<GameObject> childRootList, GameObject parentRoot)
    {
        if (childRootList == null || childRootList.Count == 0 || parentRoot == null) return;
        foreach (var child in childRootList)
        {
            BindEntityToParent(child, parentRoot);
        }
    }
    #endregion

    #region 身份组件赋值 大类/中类 通用封装
    /// 单独赋值大类身份
    public static void SetEntityMajorIdentity(GameObject entity, EntityIdentityType majorType)
    {
        if (entity == null) return;
        EntityIdentityTag tag = entity.GetComponent<EntityIdentityTag>() ?? entity.AddComponent<EntityIdentityTag>();
        tag.IdentityType = majorType;
    }
    /// 单独赋值中类特殊身份
    public static void SetEntityMinorIdentity(GameObject entity, EntitySpecialIdentityType minorType)
    {
        if (entity == null) return;
        EntitySpecialIdentityTag tag = entity.GetComponent<EntitySpecialIdentityTag>() ?? entity.AddComponent<EntitySpecialIdentityTag>();
        tag.SpecialType = minorType;
    }
    /// 一次性设置 大类+中类 全套身份
    public static void SetEntityFullIdentity(GameObject entity, EntityIdentityType majorType, EntitySpecialIdentityType minorType)
    {
        SetEntityMajorIdentity(entity, majorType);
        SetEntityMinorIdentity(entity, minorType);
    }
    #endregion
}