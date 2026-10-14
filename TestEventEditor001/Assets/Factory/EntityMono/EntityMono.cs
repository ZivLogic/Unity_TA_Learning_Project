using UnityEngine;

// 实体运行时标识组件
// 挂载在根节点，用于快速识别实体
public class EntityMono : MonoBehaviour
{
    public Entity Entity;
    public string UUID => Entity?.UUID;
    public string EntityId => Entity?.EntityId;
    public string Tag => Entity?.Tag;

}