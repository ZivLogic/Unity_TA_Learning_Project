using UnityEngine;
using UnityEditor;

public class FloorGenerator
{
    [MenuItem("Tools/Generate Floor Prefab")]
    public static void GenerateFloorPrefab()
    {
        // 1. 创建平面
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor_10x10";

        // 2. 设置位置
        floor.transform.position = Vector3.zero;

        // 3. 移除碰撞体（你要求不带碰撞体）
        var collider = floor.GetComponent<Collider>();
        if (collider != null)
        {
            GameObject.DestroyImmediate(collider);
        }

        // 4. 设置缩放（10x10米）
        floor.transform.localScale = new Vector3(1, 1, 1); // Plane默认是10x10

        // 5. 保存为预制体
        string prefabPath = "Assets/Resources/Map/floor_10x10.prefab";

        // 确保目录存在
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Map"))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "Map");
        }

        PrefabUtility.SaveAsPrefabAsset(floor, prefabPath);

        // 6. 删除场景中的临时对象
        GameObject.DestroyImmediate(floor);

        AssetDatabase.Refresh();
        Debug.Log($"地板预制体已创建: {prefabPath}");
    }
}