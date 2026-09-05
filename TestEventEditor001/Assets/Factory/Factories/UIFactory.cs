using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI工厂：流水线第七步
/// 职责：根据UI蓝图配置，创建UI实体并挂载到对应的画布
/// 
/// 前置依赖（必须在场景中提前创建）：
/// 1. 摄像机（Camera）- 编辑器手动创建
///    - 主摄像机（Tag: Camera_Main）
///    - 武器摄像机（Tag: Camera_WeaponScope）
///    - 载具摄像机（Tag: Camera_Vehicle）
/// 
/// 2. 画布（Canvas）- 编辑器手动创建
///    - 主画布（MainCanvas）- 绑定主摄像机
///    - 世界空间画布（WorldSpaceCanvas）- 绑定主摄像机
///    - 瞄准镜画布（ScopeCanvas）- 绑定武器摄像机
///    - 载具画布（VehicleCanvas）- 绑定载具摄像机
/// 
/// 3. 配置注册表
///    - canvas_registry.json - 记录画布信息
///    - camera_registry.json - 记录摄像机信息
/// </summary>
public class UIFactory : IFactoryStep
{
    #region 缓存

    // 画布缓存：画布ID → Canvas组件
    private readonly Dictionary<string, Canvas> _canvasCache = new();

    // 摄像机缓存：摄像机ID → Camera组件
    private readonly Dictionary<string, Camera> _cameraCache = new();

    #endregion

    #region 初始化

    /// <summary>
    /// 初始化UI工厂（在总工厂中调用）
    /// </summary>
    public void Initialize()
    {
        Debug.Log("[UIFactory] 初始化UI工厂...");

        // 1. 缓存所有画布
        CacheAllCanvases();

        // 2. 缓存所有摄像机
        CacheAllCameras();

        Debug.Log($"[UIFactory] 缓存完成: {_canvasCache.Count} 个画布, {_cameraCache.Count} 个摄像机");
    }

    /// <summary>
    /// 缓存场景中所有画布
    /// </summary>
    private void CacheAllCanvases()
    {
        _canvasCache.Clear();

        // 1. 从配置读取画布注册表
        var canvasRegistry = ConfigAPI.Get<CanvasRegistryConfig>();
        if (canvasRegistry?.canvases == null) return;

        foreach (var canvasEntry in canvasRegistry.canvases)
        {
            // 通过Tag或名称查找场景中的画布
            var canvas = FindCanvasInScene(canvasEntry);

            if (canvas != null)
            {
                _canvasCache[canvasEntry.canvasId] = canvas;
                Debug.Log($"[UIFactory] 缓存画布: {canvasEntry.canvasId} → {canvas.gameObject.name}");
            }
            else
            {
                Debug.LogError($"[UIFactory] 场景中找不到画布: {canvasEntry.canvasId} (Tag: {canvasEntry.canvasTag})");
            }
        }
    }

    /// <summary>
    /// 缓存场景中所有摄像机
    /// </summary>
    private void CacheAllCameras()
    {
        _cameraCache.Clear();

        // 从配置读取摄像机注册表
        var cameraRegistry = ConfigAPI.Get<CameraRegistryConfig>();
        if (cameraRegistry?.cameras == null) return;

        foreach (var cameraEntry in cameraRegistry.cameras)
        {
            // 通过Tag或名称查找场景中的摄像机
            var camera = FindCameraInScene(cameraEntry);

            if (camera != null)
            {
                _cameraCache[cameraEntry.cameraId] = camera;
                Debug.Log($"[UIFactory] 缓存摄像机: {cameraEntry.cameraId} → {camera.gameObject.name}");
            }
            else
            {
                Debug.LogError($"[UIFactory] 场景中找不到摄像机: {cameraEntry.cameraId} (Tag: {cameraEntry.cameraTag})");
            }
        }
    }

    /// <summary>
    /// 在场景中查找画布
    /// </summary>
    private Canvas FindCanvasInScene(CanvasRegistryConfig.CanvasEntry canvasEntry)
    {
        // 1. 优先通过Tag查找
        if (!string.IsNullOrEmpty(canvasEntry.canvasTag))
        {
            var canvasByTag = GameObject.FindGameObjectWithTag(canvasEntry.canvasTag);
            if (canvasByTag != null)
            {
                return canvasByTag.GetComponent<Canvas>();
            }
        }

        // 2. 通过场景路径查找
        if (!string.IsNullOrEmpty(canvasEntry.scenePath))
        {
            var canvasByPath = GameObject.Find(canvasEntry.scenePath);
            if (canvasByPath != null)
            {
                return canvasByPath.GetComponent<Canvas>();
            }
        }

        return null;
    }

    /// <summary>
    /// 在场景中查找摄像机
    /// </summary>
    private Camera FindCameraInScene(CameraRegistryConfig.CameraEntry cameraEntry)
    {
        // 1. 优先通过Tag查找
        if (!string.IsNullOrEmpty(cameraEntry.cameraTag))
        {
            var cameraByTag = GameObject.FindGameObjectWithTag(cameraEntry.cameraTag);
            if (cameraByTag != null)
            {
                return cameraByTag.GetComponent<Camera>();
            }
        }

        // 2. 通过场景路径查找
        if (!string.IsNullOrEmpty(cameraEntry.scenePath))
        {
            var cameraByPath = GameObject.Find(cameraEntry.scenePath);
            if (cameraByPath != null)
            {
                return cameraByPath.GetComponent<Camera>();
            }
        }

        return null;
    }

    #endregion

    #region 流水线处理

    /// <summary>
    /// 处理实体（创建UI并挂载到画布）
    /// </summary>
    public void Process(Entity entity)
    {
        if (entity?.Blueprint == null) return;

        // 只有UI蓝图才处理
        if (entity.Blueprint is not UIBlueprintsConfig.UIBlueprint blueprint)
            return;

        Debug.Log($"[UIFactory] 处理UI实体: {blueprint.blueprintId}");

        // 1. 查找画布
        var canvas = GetCanvas(blueprint.canvasName);
        if (canvas == null)
        {
            Debug.LogError($"[UIFactory] 找不到画布: {blueprint.canvasName}");
            return;
        }

        // 2. 加载UI预制体
        var prefab = Resources.Load<GameObject>(blueprint.prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[UIFactory] UI预制体加载失败: {blueprint.prefabPath}");
            return;
        }

        // 3. 实例化到画布
        var uiInstance = GameObject.Instantiate(prefab, canvas.transform);
        uiInstance.name = blueprint.blueprintId;

        // 4. 设置UI层级
        SetUILayer(uiInstance, blueprint.uiLayer);

        // 5. 设置渲染顺序
        SetSortingOrder(uiInstance, blueprint.sortingOrder);

        // 6. 设置穿透
        SetRaycastTarget(uiInstance, blueprint.raycastTarget);

        // 7. 设置始终顶层
        if (blueprint.alwaysOnTop)
        {
            uiInstance.transform.SetAsLastSibling();
        }

        // 8. 记录到实体
        entity.ViewObject = uiInstance;

        Debug.Log($"[UIFactory] UI创建成功: {blueprint.blueprintId} → 画布: {blueprint.canvasName}");
    }

    #endregion

    #region UI设置

    /// <summary>
    /// 获取画布
    /// </summary>
    private Canvas GetCanvas(string canvasId)
    {
        // 1. 从缓存获取
        if (_canvasCache.TryGetValue(canvasId, out var canvas))
        {
            return canvas;
        }

        // 2. 尝试按名称查找
        var canvasObj = GameObject.Find(canvasId);
        if (canvasObj != null)
        {
            var foundCanvas = canvasObj.GetComponent<Canvas>();
            if (foundCanvas != null)
            {
                _canvasCache[canvasId] = foundCanvas;
                return foundCanvas;
            }
        }

        return null;
    }

    /// <summary>
    /// 设置UI层级
    /// </summary>
    private void SetUILayer(GameObject uiObj, string uiLayer)
    {
        if (string.IsNullOrEmpty(uiLayer)) return;

        // 尝试获取CanvasGroup组件
        var canvasGroup = uiObj.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = uiObj.AddComponent<CanvasGroup>();
        }

        // 设置层级（通过排序值）
        int sortingValue = uiLayer.ToLower() switch
        {
            "background" => 0,
            "normal" => 100,
            "popup" => 200,
            "tooltip" => 300,
            "overlay" => 400,
            _ => 100
        };

        // 存储到CanvasGroup的alpha值作为层级标识（临时方案）
        // 实际项目中应该用专门的UI层级管理器
        Debug.Log($"[UIFactory] UI层级: {uiLayer} (排序值: {sortingValue})");
    }

    /// <summary>
    /// 设置渲染顺序
    /// </summary>
    private void SetSortingOrder(GameObject uiObj, int sortingOrder)
    {
        if (sortingOrder <= 0) return;

        // 设置SiblingIndex控制渲染顺序
        uiObj.transform.SetSiblingIndex(sortingOrder);

        Debug.Log($"[UIFactory] 渲染顺序: {sortingOrder}");
    }

    /// <summary>
    /// 设置穿透
    /// </summary>
    private void SetRaycastTarget(GameObject uiObj, bool raycastTarget)
    {
        // 获取所有Graphic组件
        var graphics = uiObj.GetComponentsInChildren<Graphic>();

        foreach (var graphic in graphics)
        {
            graphic.raycastTarget = raycastTarget;
        }

        Debug.Log($"[UIFactory] 穿透设置: {raycastTarget}");
    }

    #endregion
}