using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 摄像机测试脚本
/// 数据源改为 MasterFactory
/// </summary>
public class CameraTestScript : MonoBehaviour
{
    public Camera TargetCamera;      //拖 Camera_Main 进来

    public float RayDistance = 100f;
    public LayerMask SelectLayerMask = ~0;

    public bool DebugLog = true;

    public bool IsSelecting { get; private set; }
    public GameObject SelectedEntity { get; private set; }

    // ========== Scope 解析 ==========
    public List<GameObject> ResolveScope(SelectScope scope, InputOutputPayload output)
    {
        var result = new List<GameObject>();
        if (scope == null) return result;

        var scopeType = scope.GetScopeType();

        switch (scopeType)
        {
            case SelectScopeType.Self:
                Log("Self 由中转层处理，摄像机不处理");
                break;
            case SelectScopeType.Single: ResolveSingle(scope, result); break;
            case SelectScopeType.Category: ResolveCategory(scope, result); break;
            case SelectScopeType.ValueRange: ResolveValueRange(scope, result); break;
            case SelectScopeType.Area: ResolveArea(scope, result); break;
            case SelectScopeType.Global:
                Log("Global 由中转层处理，摄像机不处理");
                break;
        }

        Log($"ResolveScope({scope.ScopeType}) → {result.Count} 个");
        return result;
    }

    // ========== Single ==========
    private void ResolveSingle(SelectScope scope, List<GameObject> result)
    {
        var hit = RaycastEntity();
        if (hit == null) { Log("Single：射线未命中"); return; }

        var mono = EntityMono.FindInParents(hit);
        if (mono == null) { Log("Single：找不到 EntityMono"); return; }

        if (!string.IsNullOrEmpty(scope.SingleEntityId)
            && mono.UUID != scope.SingleEntityId)
        {
            Log("Single：UUID 不匹配");
            return;
        }

        // 返回总根，不是命中的子节点
        result.Add(mono.gameObject);
    }

    // ========== Category ==========
    private void ResolveCategory(SelectScope scope, List<GameObject> result)
    {
        var anchor = RaycastEntity();
        if (anchor == null) { Log("Category：射线未命中"); return; }

        var anchorMono = EntityMono.FindInParents(anchor);
        if (anchorMono?.Entity == null)
        {
            Log("Category：锚点找不到 EntityMono");
            return;
        }

        string anchorTag = anchorMono.Entity.Tag;
        if (!string.IsNullOrEmpty(scope.CategoryTag)
            && anchorTag != scope.CategoryTag)
        {
            Log($"Category：Tag={anchorTag} 不匹配 {scope.CategoryTag}");
            return;
        }

        var entities = MasterFactory.Instance.FindAllByTag(anchorTag);
        foreach (var e in entities)
        {
            if (e?.Root != null)
                result.Add(e.Root);
        }
    }

    // ========== ValueRange ==========
    private void ResolveValueRange(SelectScope scope, List<GameObject> result)
    {
        var anchor = RaycastEntity();
        if (anchor == null) { Log("ValueRange：射线未命中"); return; }

        var anchorMono = EntityMono.FindInParents(anchor);
        if (anchorMono?.Entity == null) return;

        float anchorValue = GetBlueprintValue(anchorMono.Entity, scope.ValueKey);
        Log($"ValueRange：锚点 {scope.ValueKey}={anchorValue}");

        var all = MasterFactory.Instance.GetAllEntities();
        foreach (var e in all)
        {
            float v = GetBlueprintValue(e, scope.ValueKey);
            if (v >= scope.MinValue && v <= scope.MaxValue)
                result.Add(e.Root);
        }
    }

    // ========== Area ==========
    private void ResolveArea(SelectScope scope, List<GameObject> result)
    {
        Vector3 center;
        bool hasCenter = scope.AreaCenter.x != 0
                         || scope.AreaCenter.y != 0
                         || scope.AreaCenter.z != 0;

        if (hasCenter)
        {
            center = scope.AreaCenter.ToVector3();
        }
        else
        {
            var hit = RaycastEntity();
            if (hit == null) { Log("Area：射线未命中"); return; }

            var mono = EntityMono.FindInParents(hit);
            center = mono != null ? mono.transform.position : hit.transform.position;
            Log($"Area：锚点 {center}");
        }

        var all = MasterFactory.Instance.GetAllEntities();
        foreach (var e in all)
        {
            if (e?.Root == null) continue;
            if (IsInArea(scope, center, e.Root.transform.position))
                result.Add(e.Root);
        }
    }

    private bool IsInArea(SelectScope scope, Vector3 center, Vector3 pos)
    {
        switch (scope.AreaModelId)
        {
            case "Sphere":
                return Vector3.Distance(center, pos) <= scope.Radius;

            case "Box":
                var half = new Vector3(scope.Length * 0.5f, scope.Height * 0.5f, scope.Width * 0.5f);
                var diff = pos - center;
                return Mathf.Abs(diff.x) <= half.x
                    && Mathf.Abs(diff.y) <= half.y
                    && Mathf.Abs(diff.z) <= half.z;

            case "Cylinder":
                var flat = new Vector3(pos.x - center.x, 0, pos.z - center.z);
                return flat.magnitude <= scope.Radius
                    && Mathf.Abs(pos.y - center.y) <= scope.Height * 0.5f;

            case "Cone":
                var toPos = pos - center;
                float angle = Vector3.Angle(Vector3.forward, toPos);
                return angle <= scope.Angle * 0.5f
                    && toPos.magnitude <= scope.Radius;

            default:
                return Vector3.Distance(center, pos) <= scope.Radius;
        }
    }

    // ========== Global ==========
    private void ResolveGlobal(SelectScope scope, List<GameObject> result)
    {
        var all = FindAllEntities();
        foreach (var e in all)
        {
            if (e?.Root != null) result.Add(e.Root);
        }
        Log($"Global：全选 {result.Count} 个");
    }

    // ========== 从工厂取全部实体 ==========
    private List<Entity> FindAllEntities()
    {
        return MasterFactory.Instance.GetAllEntities();
    }

    // ========== 射线 ==========
    private GameObject RaycastEntity()
    {
        var cam = TargetCamera != null ? TargetCamera : Camera.main;

        if (cam == null) { Log("摄像机 为空"); return null; }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit, RayDistance, SelectLayerMask))
        {
            Log($"射线命中：{hit.collider.gameObject.name}");
            return hit.collider.gameObject;
        }
        Log("射线未命中");
        return null;
    }

    // ========== Blueprint 取值 ==========
    private float GetBlueprintValue(Entity entity, string key)
    {
        if (entity?.Blueprint == null) return 0f;

        switch (key)
        {
            case "defaultHeight": return entity.Blueprint.defaultHeight;
        }

        var field = entity.Blueprint.GetType().GetField(key,
            BindingFlags.Public | BindingFlags.Instance);
        if (field != null && field.FieldType == typeof(float))
            return (float)field.GetValue(entity.Blueprint);

        return 0f;
    }

    // ========== 旧选择流程 ==========
    public void BeginSelect()
    {
        if (IsSelecting) return;
        IsSelecting = true;
        SelectedEntity = null;
        Log("=== 进入选择状态 ===");
    }

    public void ConfirmSelect()
    {
        if (!IsSelecting) return;
        Log("=== 确认选择 ===");
        var hit = RaycastEntity();
        EndSelect(hit);
    }

    public void EndSelect(GameObject entity)
    {
        IsSelecting = false;
        SelectedEntity = entity;

        if (entity != null)
        {
            var mono = EntityMono.FindInParents(entity);
            if (mono != null)
            {
                Log($"=== 选择结束：{mono.UUID} ===");

                // 只通知中转层
                var mediator = FindObjectOfType<InputMediator>();
                mediator?.SetMainCharacter(mono.UUID);
            }
        }
        else
        {
            Log("=== 选择结束：无目标 ===");
            FindObjectOfType<InputMediator>()?.SetMainCharacter(null);
        }
    }


    private void Log(string msg)
    {
        if (DebugLog) Debug.Log($"[Camera] {msg}");
    }



    private void Update()
    {
        // 选择模式下左键确认
        if (IsSelecting && Input.GetMouseButtonDown(0))
        {
            ConfirmSelect();
        }

        // Esc 取消
        if (IsSelecting && Input.GetKeyDown(KeyCode.Escape))
        {
            IsSelecting = false;
            Log("取消选择");
        }
    }
}