// Assets/Scripts/LogicScripts/FollowTarget.cs
using UnityEngine;

public class FollowTarget : MonoBehaviour, IEntityAware
{
    public Entity Entity { get; set; }

    public string targetTag = "Pawn";
    private Transform _target;

    //void Start()
    //{
    //    // 第二阶段绑定：查找目标
    //    var targetEntity = MasterFactory.Instance.FindFirstByTag(targetTag);
    //    if (targetEntity != null)
    //    {
    //        _target = targetEntity.Root.transform;
    //        Debug.Log($"[FollowTarget] 绑定目标: {targetTag}");
    //    }
    //}

    //void Update()
    //{
    //    if (_target == null) return;

    //    // 跟随目标（屏幕空间UI）
    //    Vector3 screenPos = Camera.main.WorldToScreenPoint(_target.position);
    //    transform.position = screenPos + new Vector3(0, 50, 0); // 偏移50像素
    //}
}