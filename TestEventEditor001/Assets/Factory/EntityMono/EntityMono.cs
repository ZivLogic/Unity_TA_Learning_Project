using UnityEngine;

// 实体运行时标识组件
// 挂载在根节点，用于快速识别实体
public class EntityMono : MonoBehaviour
{
    public Entity Entity;
    public string UUID => Entity?.UUID;
    public string EntityId => Entity?.EntityId;
    public string Tag => Entity?.Tag;


    //从任意子节点向上找 EntityMono
    //视觉艮、逻辑根、模型都能找到总根上的标记
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
}