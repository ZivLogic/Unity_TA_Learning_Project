using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

//输入系统通用工具
public static class InputHelper
{
    //极简测试选中方法
    public static GameObject RaycastSelectSingle()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray,out RaycastHit hit, 1000f))
        {
            return hit.collider.gameObject;
        }
        return null;
    }

    #region  3D选中 | 射线中心点 + 范围球形 + 权重筛选

    //现在配置问题比较大


    public static GameObject GetPrioritySelectObject(float selectRadius, LayerMask selectLayer)   //获取鼠标区域内预设体
    {
        if (!RaycastMouseHit(out RaycastHit hit, 1000f, selectLayer)) 
            return null;
        Collider[] hitCols = Physics.OverlapSphere(hit.point, selectRadius, selectLayer);  //对象获取列表
        GameObject bestObj = null;                 //初始值为空
        int maxWeght = int.MinValue;               //初始权重值为负无穷
        foreach (var col in hitCols)               //遍历所有物体
        {
            GameObject target = col.gameObject;    //获取对象
            if (!InputConfigCache.ObjectSelectDict.TryGetValue(target.name, out var cfg))   //如果不在缓存里，跳过
                continue;
            if (!cfg.CanSelect)       //如果不可选择，跳过
                continue;
            if (cfg.SelectWeight > maxWeght)    //选取优先级最大的物体
            {
                maxWeght = cfg.SelectWeight;
                bestObj = target;
            }
        }
        return bestObj;
    }
    #endregion

    #region  按键字符串互转
    //字符串转按键枚举
    public static KeyCode StringToKeyCode(string keyStr)
    {
        if (string.IsNullOrEmpty(keyStr) || keyStr == "None")    //如果为空或为“None”，返回空
            return KeyCode.None;
        return (KeyCode)Enum.Parse(typeof(KeyCode), keyStr);     //如果不为，返回对应Enum值
    }
    //枚举转字符串
    public static string KeyCodeToString(KeyCode key)
    {
        return key.ToString();
    }
    #endregion

    #region  基础射线 & 坐标
    //鼠标发射3D射线
    public static Ray GetMouseRay()
    {
        return Camera.main.ScreenPointToRay(Input.mousePosition);
    }
    //获取鼠标射线首个3D碰撞体
    public static bool RaycastMouseHit(out RaycastHit hit, float maxDistance = 1000f, LayerMask mask = default)
    {
        Ray ray = GetMouseRay();
        if (mask == default)
        {
            return Physics.Raycast(ray, out hit, maxDistance);
        }
        return Physics.Raycast(ray, out hit, maxDistance, mask);
    }
    //获取鼠标转换成地面的真实世界坐标
    public static Vector3 GetMouseGroundPos(float groundY = 0)
    {
        Ray ray = GetMouseRay();
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0,groundY,0));
        if (groundPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }
        return Vector3.zero;
    }
    #endregion

    #region  UI 检测 | 常驻UI/弹窗通用
    public static bool IsMouseOverUI()
    {
        if (EventSystem.current == null)     //检测当前场景是否有EventSystem实例，如果不存在，返回false
            return false;
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData,results);
        foreach (var res in results)
        {
            if (res.gameObject.layer == LayerMask.NameToLayer("UI"))
                return true;
        }
        return false;
    }
    #endregion

    #region  镜头 | 鼠标偏移 | 光标锁定 | 灵敏度
    //设置锁定
    public static void SetCursorLock(bool isLock)
    {
        Cursor.lockState = isLock ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !isLock;
    }
    //鼠标偏移（用轴）
    public static Vector2 GetMouseDelta(float sensitivity)
    {
        float x = Input.GetAxisRaw("Mouse X") * sensitivity;
        float y = Input.GetAxisRaw("Mouse Y") * sensitivity;
        return new Vector2(x, y);
    }
    //限制范围
    public static Vector2 ClampCameraAngle(Vector2 curAngle, float min, float max)
    {
        curAngle.y = Mathf.Clamp(curAngle.y, min, max);
        return curAngle;
    }
    #endregion

    #region  改建冲突检测工具
    public static bool CheckKeyConflict(KeyCode checkKey, InputAction excludeAction, InputContext currentCtx)
    {
        foreach (var kv in InputConfigCache.InputBindDict)
        {
            string actionKey = kv.Key;
            var cfg = kv.Value;

            KeyCode existKey = StringToKeyCode(cfg.KeyBindCode);
            if (existKey != checkKey || existKey == KeyCode.None) 
                continue;
            if (!Enum.TryParse<InputAction>(actionKey, out InputAction existAction))
                continue;
            if (existAction == excludeAction)
                continue;
            //同上下文 判定冲突
            if (cfg.AllowContext.Contains(currentCtx))
                return true;
        }
        return false;
    }
    #endregion


















    //public static GameObject GetPrioritySelectObject(Vector3 worldPos, float totalCheckRadius)   //获取鼠标区域内预设体
    //{
    //    Collider[] hitCols = Physics.OverlapSphere(worldPos, totalCheckRadius);         //预设体获取列表
    //    GameObject bestObj = null;                 //初始值为空
    //    int maxWeght = int.MinValue;               //初始权重值为负无穷
    //    foreach (var col in hitCols)               //遍历所有物体
    //    {
    //        GameObject target = col.gameObject;    //获取对象
    //        if (!InputConfigCache.ObjectSelectDict.TryGetValue(target.name, out var cfg))   //如果不在缓存里，跳过
    //            continue;
    //        if (!cfg.CanSelect)       //如果不可选择，跳过
    //            continue;
    //        if (cfg.SelectWeight > maxWeght)    //选取优先级最大的物体
    //        {
    //            maxWeght = cfg.SelectWeight;
    //            bestObj = target;
    //        }
    //    }
    //    return bestObj;
    //}
    //    //屏幕世界转世界坐标 //错误代码，世界坐标不准
    //    public static Vector3 ScreenToWorld(Vector2 screenPos)
    //    {
    //        return Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Camera.main.nearClipPlane)); 
    //    }
}
