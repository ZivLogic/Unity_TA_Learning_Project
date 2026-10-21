using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 摄像机入口基类
/// 职责：
/// 1. 预识别输入意图 → 方法
/// 2. 通用选择工具（Self/Single/Category/ValueRange/Area/Global）
/// 3. 转 UUID + 预筛（活动/池化/标记）
/// 4. 输出到 OutputResultBase
/// 子类只写具体选择流程
/// </summary>
public abstract class CameraEntryBase : MonoBehaviour
{
    [Header("摄像机")]
    public Camera TargetCamera;

    [Header("射线")]
    public float RayDistance = 100f;
    public LayerMask SelectLayerMask = ~0;

    [Header("调试")]
    public bool DebugLog = true;

    protected Dictionary<string, Action<InputOutputPayload>> _intentTable
        = new Dictionary<string, Action<InputOutputPayload>>();

    protected OutputResultBase _result = new OutputResultBase();

    // 主角 UUID（中转层同步）
    protected string _mainCharacterUuid;

    protected virtual void Awake()
    {
        if (TargetCamera == null)
            TargetCamera = Camera.main;

        RegisterIntents();
    }

    protected abstract void RegisterIntents();

    protected void RegisterIntent(string action, Action<InputOutputPayload> handler)
    {
        if (string.IsNullOrEmpty(action) || handler == null) return;
        _intentTable[action] = handler;
    }

    // ========== 统一入口 ==========
    public void ReceiveInput(InputOutputPayload output)
    {
        if (output == null || !output.HasOutput) return;

        _result.Clear();

        if (_intentTable.TryGetValue(output.Action, out var handler))
        {
            Log($"命中意图：{output.Action}");
            handler.Invoke(output);
        }
        else
        {
            Log($"未注册意图：{output.Action}");
        }
    }

    // ========== 统一返回 ==========
    public OutputResultBase GetResult()
    {
        return _result;
    }

    // ========== 主角同步 ==========
    public virtual void SetMainCharacter(string uuid)
    {
        _mainCharacterUuid = uuid;
    }

    // ========== 切换清理 ==========
    /// <summary>
    /// 入口被切走时调用
    /// 子类重写清理自己的状态
    /// </summary>
    public virtual void OnDeactivated()
    {
        _result.Clear();
        Log("入口被切走");
    }

    /// <summary>
    /// 入口被切回时调用
    /// </summary>
    public virtual void OnActivated()
    {
        _result.Clear();
        Log("入口被激活");
    }

    // ========== 通用工具 ==========
    protected SelectScopeType GetScopeType(InputOutputPayload output)
    {
        if (output.Scope == null) return SelectScopeType.Single;
        return output.Scope.GetScopeType();
    }

    protected List<GameObject> ResolveByScope(SelectScope scope)
    {
        var result = new List<GameObject>();
        if (scope == null) return result;

        switch (scope.GetScopeType())
        {
            case SelectScopeType.Self:
                var self = ResolveSelf();
                if (self != null) result.Add(self);
                break;
            case SelectScopeType.Single:
                var single = ResolveSingle(scope);
                if (single != null) result.Add(single);
                break;
            case SelectScopeType.Category:
                result.AddRange(ResolveCategory(scope));
                break;
            case SelectScopeType.ValueRange:
                result.AddRange(ResolveValueRange(scope));
                break;
            case SelectScopeType.Area:
                result.AddRange(ResolveArea(scope));
                break;
            case SelectScopeType.Global:
                result.AddRange(ResolveGlobal());
                break;
        }
        return result;
    }

    protected GameObject ResolveSelf()
    {
        if (string.IsNullOrEmpty(_mainCharacterUuid)) return null;
        var entity = MasterFactory.Instance.FindByUUID(_mainCharacterUuid);
        if (entity?.Root == null) return null;
        if (!IsActive(entity.Root)) return null;
        return entity.Root;
    }

    protected GameObject ResolveSingle(SelectScope scope)
    {
        var hit = RaycastEntity();
        if (hit == null) return null;

        var mono = EntityMono.FindInParents(hit);
        if (mono?.Entity == null) return null;

        if (!string.IsNullOrEmpty(scope.SingleEntityId)
            && mono.UUID != scope.SingleEntityId)
            return null;

        if (!IsActive(mono.gameObject)) return null;
        return mono.gameObject;
    }

    protected List<GameObject> ResolveCategory(SelectScope scope)
    {
        var result = new List<GameObject>();
        var anchor = RaycastEntity();
        if (anchor == null) return result;

        var mono = EntityMono.FindInParents(anchor);
        if (mono?.Entity == null) return result;

        string tag = mono.Entity.Tag;
        if (!string.IsNullOrEmpty(scope.CategoryTag) && tag != scope.CategoryTag)
            return result;

        var entities = MasterFactory.Instance.FindAllByTag(tag);
        foreach (var e in entities)
        {
            if (e?.Root == null) continue;
            if (!IsActive(e.Root)) continue;
            result.Add(e.Root);
        }
        return result;
    }

    protected List<GameObject> ResolveValueRange(SelectScope scope)
    {
        var result = new List<GameObject>();
        var anchor = RaycastEntity();
        if (anchor == null) return result;

        var mono = EntityMono.FindInParents(anchor);
        if (mono?.Entity == null) return result;

        var all = MasterFactory.Instance.GetAllEntities();
        foreach (var e in all)
        {
            if (e?.Root == null) continue;
            if (!IsActive(e.Root)) continue;

            float v = GetBlueprintValue(e, scope.ValueKey);
            if (v >= scope.MinValue && v <= scope.MaxValue)
                result.Add(e.Root);
        }
        return result;
    }

    protected List<GameObject> ResolveArea(SelectScope scope)
    {
        var result = new List<GameObject>();
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
            if (hit == null) return result;
            var mono = EntityMono.FindInParents(hit);
            center = mono != null ? mono.transform.position : hit.transform.position;
        }

        var all = MasterFactory.Instance.GetAllEntities();
        foreach (var e in all)
        {
            if (e?.Root == null) continue;
            if (!IsActive(e.Root)) continue;
            if (IsInArea(scope, center, e.Root.transform.position))
                result.Add(e.Root);
        }
        return result;
    }

    protected List<GameObject> ResolveGlobal()
    {
        var result = new List<GameObject>();
        var all = MasterFactory.Instance.GetAllEntities();
        foreach (var e in all)
        {
            if (e?.Root == null) continue;
            if (!IsActive(e.Root)) continue;
            result.Add(e.Root);
        }
        return result;
    }

    protected bool IsActive(GameObject go)
    {
        if (go == null) return false;
        if (!go.activeInHierarchy) return false;

        var mono = EntityMono.FindInParents(go);
        if (mono?.Entity == null) return false;
        if (mono.Entity.IsPooled) return false;

        return true;
    }

    protected List<string> ToUuids(List<GameObject> entities)
    {
        var result = new List<string>();
        foreach (var go in entities)
        {
            if (go == null) continue;
            var mono = EntityMono.FindInParents(go);
            if (mono == null || string.IsNullOrEmpty(mono.UUID)) continue;
            if (MasterFactory.Instance.FindByUUID(mono.UUID) == null) continue;
            if (!result.Contains(mono.UUID)) result.Add(mono.UUID);
        }
        return result;
    }

    protected GameObject RaycastEntity()
    {
        var cam = TargetCamera != null ? TargetCamera : Camera.main;
        if (cam == null) return null;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit, RayDistance, SelectLayerMask))
            return hit.collider.gameObject;
        return null;
    }

    protected bool IsInArea(SelectScope scope, Vector3 center, Vector3 pos)
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

    protected float GetBlueprintValue(Entity entity, string key)
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

    protected void Log(string msg)
    {
        if (DebugLog) Debug.Log($"[CameraEntry] {msg}");
    }
}