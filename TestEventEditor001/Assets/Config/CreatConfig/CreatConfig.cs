using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatConfig : MonoBehaviour
{
    public void CreatConfigsData()
    {
        #region 蓝图总配置
        var builder_map = new ConfigBuilder();

        builder_map.Add("blueprints", new List<object>
        {
            new Dictionary<string, object>
            {
                { "blueprintId", "bread_tree" },
                { "tag", "Tree_Bread" },
                { "modelPath", "models/bread_tree.fbx" },
                { "defaultHeight", 1.0f },
            
                // 方式1：物理配置ID引用
                { "physicsConfigId", "tree_standard" },

                //// 方式2：@ref 引用材质路径（通过索引）
                //{ "materialPath", "@ref:material_database.materials[0].path" },

                {"materialId", "bread_tree_mat" },
            
                //// 方式3：@ref 引用完整材质数据
                //{ "materialData", "@ref:material_database.materials[0]" },



                { "logicScriptIds", new List<object> { "tree_growth", "fruit_bearing" } },
                { "poolId", "vegetation" }
            },
            new Dictionary<string, object>
            {
                { "blueprintId", "pine_tree" },
                { "tag", "Tree_Pine" },
                { "modelPath", "models/pine_tree.fbx" },
                { "defaultHeight", 1.0f },

                { "physicsConfigId", "tree_standard" },
                //{ "materialPath", "@ref:material_database.materials[1].path" },
                //{ "materialData", "@ref:material_database.materials[1]" },

                {"materialId", "pine_tree_mat"  },

                { "logicScriptIds", new List<object> { "TreeGrowth", "PineNeedle" } },
                { "poolId", "vegetation" }
            }
        });

        builder_map.ExportToFileFormat("Assets/Config/ConfigData/entity_blueprints.json");
        Debug.Log("蓝图配置已生成");
        #endregion

        #region 物理配置
        var builder_physics = new ConfigBuilder();

        builder_physics.Add("physicsList", new List<object>
        {
            new Dictionary<string, object>
            {
                { "configId", "tree_standard" },
                { "mass", 50.0f },
                { "isKinematic", true },
                { "colliders", new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            { "type", "capsule" },
                            { "center", new Dictionary<string, object>
                                {
                                    { "x", 0.0f },
                                    { "y", 1.0f },
                                    { "z", 0.0f }
                                }
                            },
                            { "height", 2.0f },
                            { "radius", 0.5f },
                            { "size", new Dictionary<string, object>
                                {
                                    { "x", 0.0f },
                                    { "y", 0.0f },
                                    { "z", 0.0f }
                                }
                            }
                        }
                    }
                }
            }
        });

        builder_physics.ExportToFileFormat("Assets/Config/ConfigData/physics_configs.json");
        Debug.Log("物理配置已生成");
        #endregion

        #region 材质配置
        var builder_mate = new ConfigBuilder();

        builder_mate.Add("materials", new List<object>
        {
            new Dictionary<string, object>
            {
                { "materialId", "bread_tree_mat" },
                { "path", "materials/bread_tree.mat" },
                { "shaderName", "Standard" },
                { "mainColor", new Dictionary<string, object>
                    {
                        { "r", 0.5f },
                        { "g", 0.3f },
                        { "b", 0.1f },
                        { "a", 1.0f }
                    }
                },
                { "metallic", 0.0f },
                { "smoothness", 0.3f }
            },
            new Dictionary<string, object>
            {
                { "materialId", "pine_tree_mat" },
                { "path", "materials/pine_tree.mat" },
                { "shaderName", "Standard" },
                { "mainColor", new Dictionary<string, object>
                    {
                        { "r", 0.1f },
                        { "g", 0.4f },
                        { "b", 0.1f },
                        { "a", 1.0f }
                    }
                },
                { "metallic", 0.0f },
                { "smoothness", 0.2f }
            }
        });

        builder_mate.ExportToFileFormat("Assets/Config/ConfigData/material_database.json");
        Debug.Log("材质配置已生成");
        #endregion

        #region 逻辑配置
        var builder_logic = new ConfigBuilder();

        builder_logic.Add("scripts", new List<object>
        {
            new Dictionary<string, object>
            {
                {"scriptId", "tree_growth" },
                {"scriptName", "TreeGrowth" },
                {"parameters", new List<object> { "speed=1.0", "maxHeight=10" } }
            },
            new Dictionary<string, object>
            {
                {"scriptId", "fruit_bearing" },
                {"scriptName", "FruitBearing" },
                {"parameters", new List<object>{ "fruitType=bread" } }
            },
            new Dictionary<string, object>
            {
                {"scriptId", "orbit_earth" },
                {"scriptName", "OrbitAroundEarth" },
                {"parameters", new List<object>{ "radius=384400", "speed=0.5" } }
            }
        });

        builder_logic.ExportToFileFormat("Assets/Config/ConfigData/logic_scripts.json");
        Debug.Log("逻辑配置已生成");
        #endregion

        #region 对象池配置
        var builder_pools = new ConfigBuilder();

        builder_pools.Add("pools", new List<object>
        {
            new Dictionary<string, object>
            {
                {"poolId", "vegetation" },
                {"preloadCount", 50 },
                {"maxSize", 200 },
                { "autoExpand", true }
            },
            new Dictionary<string, object>
            {
                {"poolId", "celestial" },
                {"preloadCount", 10 },
                {"maxSize", 20 },
                { "autoExpand", false }
            }
        });

        builder_pools.ExportToFileFormat("Assets/Config/ConfigData/object_pools.json");
        Debug.Log("对象池配置已生成");
        #endregion
    }








    // Start is called before the first frame update
    void Start()
    {
        //CreatConfigsData();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
