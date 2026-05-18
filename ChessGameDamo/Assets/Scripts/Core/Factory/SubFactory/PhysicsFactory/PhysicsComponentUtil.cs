using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json.Linq;

public static class PhysicsComponentUtil
{
    #region 碰撞体封装
    public static Collider AddCollider(GameObject obj, string colliderType)
    {
        Collider col = null;
        switch (colliderType)
        {
            case "Box":
                col = obj.AddComponent<BoxCollider>();
                break;
            case "Sphere":
                col = obj.AddComponent<SphereCollider>();
                break;
            case "Capsule":
                col = obj.AddComponent<CapsuleCollider>();
                break;
            case "Mesh":
                col = obj.AddComponent<MeshCollider>();
                break;
        }
        return col;
    }
    #endregion
    #region 刚体封装
    public static Rigidbody AddRigidbody(GameObject obj, float mass, float drag, bool useGravity, bool isKinematic)
    {
        Rigidbody rb = obj.AddComponent <Rigidbody>();
        rb.mass = mass;
        rb.drag = drag;
        rb.useGravity = useGravity;
        rb.isKinematic = isKinematic;
        return rb;
    }
    #endregion
    #region 字符串转枚举
    public static bool TryParseEnum<T>(string str, out T enumValue) where T : Enum
    {
        enumValue = default;
        if (string.IsNullOrEmpty(str))
            return false;
        foreach (var item in Enum.GetValues(typeof(T)))
        {
            if (item.ToString().Equals(str, StringComparison.OrdinalIgnoreCase))
            {
                enumValue = (T)item;
                return true;
            }
        }
        return false;
    }
    #endregion
    #region 从字典取普通类型值 带默认值
    public static T GetParam<T>(Dictionary<string, object> dict, string key, T defaultValue = default)
    {
        if ( ! dict.ContainsKey(key))return defaultValue;
        try
        {
            return (T)Convert.ChangeType(dict[key], typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }
    #endregion
    #region 从字典取X Y Z 转 Veector3
    public static Vector3 GetVector3Param(Dictionary<string, object> dict, string key)
    {
        if (!dict.ContainsKey(key))
        {
            Debug.LogError($"[PhysicsComponentUtil] 找不到Key：{key}");
            return Vector3.zero;
        }

        object data = dict[key];
        JArray jArr = data as JArray;
        if (jArr == null || jArr.Count < 3)
        {
            Debug.LogError($"[PhysicsComponentUtil] JArray解析失败，值：{data}");
            return Vector3.zero;
        }

        float x = (float)jArr[0];
        float y = (float)jArr[1];
        float z = (float)jArr[2];
        return new Vector3(x, y, z);
    }
    #endregion
}