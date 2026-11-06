using UnityEngine;

public class EntityMono : MonoBehaviour
{
    public Entity Entity;
    public string UUID => Entity?.UUID;
    public string EntityId => Entity?.EntityId;
    public string Tag => Entity?.Tag;

    /// <summary>
    /// 从任意子节点向上找 EntityMono
    /// </summary>
    public static EntityMono FindInParents(GameObject go)
    {
        if (go == null) return null;

        var t = go.transform;
        while (t != null)
        {
            var mono = t.GetComponent<EntityMono>();
            if (mono != null) return mono;
            t = t.parent;
        }
        return null;
    }

    /// <summary>
    /// 保底：GameObject 被外部直接 Destroy 时通知总表
    /// 优先判断退出状态；总工厂已销毁则跳过
    /// </summary>
    private void OnDestroy()
    {
        // 退出阶段直接跳过
        if (AppLifecycle.IsQuitting) return;

        // 总工厂已销毁则跳过（不触发重建）
        if (MasterFactory.Instance == null) return;

        if (Entity != null)
            MasterFactory.Instance.OnEntityMonoDestroyed(Entity);
    }
}