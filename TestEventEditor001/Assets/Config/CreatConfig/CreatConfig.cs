using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatConfig : MonoBehaviour
{
    // 1. 音频配置
    private static void GenerateAudioConfig()
    {
        var builder = new ConfigBuilder();
        builder.Add("audioList", new List<object>
        {
            new Dictionary<string, object>
            {
                { "audioId", "pawn_move" },
                { "clipPath", "Audio/Test_Audio_001" },  // 需要你实际放入音频文件
                { "volume", 0.5f },
                { "pitch", 1.0f },
                { "loop", false },
                { "spatialBlend", 1.0f },
                { "minDistance", 1.0f },
                { "maxDistance", 20.0f },
                { "playMode", "manual" }
            }
        });
        builder.ExportToFileFormat("Assets/Config/ConfigData/audio_configs.json");
    }

    // 2. 材质配置
    private static void GenerateMaterialConfig()
    {
        var builder = new ConfigBuilder();
        builder.Add("materials", new List<object>
        {
            new Dictionary<string, object>
            {
                { "materialId", "pawn_black" },
                { "path", "Materials/pawn_black" },  // 需要创建材质
                { "shaderName", "Universal Render Pipeline/Lit" },
                { "mainColor", new Dictionary<string, object>
                    {
                        { "r", 0.1f },
                        { "g", 0.1f },
                        { "b", 0.1f },
                        { "a", 1.0f }
                    }
                },
                { "metallic", 0.0f },
                { "smoothness", 0.3f },
                { "castShadows", true },
                { "receiveShadows", true },
                { "renderLayer", "Default" }
            }
        });
        builder.ExportToFileFormat("Assets/Config/ConfigData/material_database.json");
    }

    // 3. 物理配置
    private static void GeneratePhysicsConfig()
    {
        var builder = new ConfigBuilder();
        builder.Add("physicsList", new List<object>
        {
            new Dictionary<string, object>
            {
                { "configId", "pawn_physics" },
                { "mass", 1.0f },
                { "isKinematic", false },
                { "useGravity", true },
                { "interpolate", false },
                { "continuousCollision", false },
                { "colliders", new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            { "type", "capsule" },
                            { "center", new Dictionary<string, object>
                                {
                                    { "x", 0.0f },
                                    { "y", 0.5f },
                                    { "z", 0.0f }
                                }
                            },
                            { "radius", 0.3f },
                            { "height", 0.8f },
                            { "size", new Dictionary<string, object>
                                {
                                    { "x", 0.0f },
                                    { "y", 0.0f },
                                    { "z", 0.0f }
                                }
                            },
                            { "direction", 1 },
                            { "convex", false },
                            { "physicsMaterial", "" }
                        }
                    }
                }
            }
        });
        builder.ExportToFileFormat("Assets/Config/ConfigData/physics_configs.json");
    }

    // 4. 逻辑脚本配置
    private static void GenerateLogicScriptsConfig()
    {
        var builder = new ConfigBuilder();
        builder.Add("scripts", new List<object>
        {
            new Dictionary<string, object>
            {
                { "scriptId", "pawn_attack_range" },
                { "scriptName", "PawnAttackRange" },
                { "executionOrder", 0 },
                { "enabled", true }
            },
            new Dictionary<string, object>
            {
                { "scriptId", "follow_target" },
                { "scriptName", "FollowTarget" },
                { "executionOrder", 0 },
                { "enabled", true }
            }
        });
        builder.ExportToFileFormat("Assets/Config/ConfigData/logic_scripts.json");
    }

    // 5. 对象池配置
    private static void GenerateObjectPoolsConfig()
    {
        var builder = new ConfigBuilder();
        builder.Add("pools", new List<object>
        {
            new Dictionary<string, object>
            {
                { "poolId", "pawn_pool" },
                { "preloadCount", 5 },
                { "maxSize", 20 },
                { "autoExpand", true }
            }
        });
        builder.ExportToFileFormat("Assets/Config/ConfigData/object_pools.json");
    }

    // 6. 实体蓝图
    private static void GenerateEntityBlueprints()
    {
        var builder = new ConfigBuilder();
        builder.Add("blueprints", new List<object>
        {
            new Dictionary<string, object>
            {
                { "blueprintId", "pawn" },
                { "tag", "Pawn" },
                { "modelPath", "TestGameObject/Pawn" },  // 你的棋子预制体
                { "defaultHeight", 0.5f },
                { "physicsConfigId", "pawn_physics" },
                { "materialId", "pawn_black" },
                { "audioIds", new List<object> { "pawn_move" } },
                { "logicScriptIds", new List<object> { "pawn_attack_range" } },
                { "poolId", "pawn_pool" }
            },
            new Dictionary<string, object>
            {
                { "blueprintId", "floor_tile" },
                { "tag", "Floor" },
                { "modelPath", "Map/floor_10x10" },  // 地板预制体
                { "defaultHeight", 0.0f },
                { "physicsConfigId", "" },
                { "materialId", "" },
                { "audioIds", new List<object>() },
                { "logicScriptIds", new List<object>() },
                { "poolId", "pawn_pool" }
            }
        });
        builder.ExportToFileFormat("Assets/Config/ConfigData/entity_blueprints.json");
    }

    // 7. UI蓝图
    private static void GenerateUIBlueprints()
    {
        var builder = new ConfigBuilder();
        builder.Add("blueprints", new List<object>
        {
            new Dictionary<string, object>
            {
                { "blueprintId", "pawn_info_ui" },
                { "tag", "UI_Test" },
                { "defaultHeight", 0.0f },
                { "logicScriptIds", new List<object> { "follow_target" } },
                { "audioIds",  new List<object> { "pawn_move" } },
                { "poolId", "" },
                { "prefabPath", "TestImage/TEST_UI_001" },  // 你的图片
                { "canvasName", "Canvas_Main" },
                { "sortingOrder", 100 },
                { "uiLayer", "normal" },
                { "raycastTarget", false },
                { "alwaysOnTop", false }
            }
        });
        builder.ExportToFileFormat("Assets/Config/ConfigData/ui_blueprints.json");
    }

    // 8. 画布注册表
    private static void GenerateCanvasRegistry()
    {
        var builder = new ConfigBuilder();
        builder.Add("canvases", new List<object>
        {
            new Dictionary<string, object>
            {
                { "canvasId", "Canvas_Main" },
                { "canvasTag", "Canvas_Main" },
                { "scenePath", "Canvas_Main" },
                { "cameraId", "Camera_Main" },
                { "sortingOrder", 0 },
                { "renderMode", "ScreenSpace" }
            }
        });
        builder.ExportToFileFormat("Assets/Config/ConfigData/canvas_registry.json");
    }

    // 9. 摄像机注册表
    private static void GenerateCameraRegistry()
    {
        var builder = new ConfigBuilder();
        builder.Add("cameras", new List<object>
        {
            new Dictionary<string, object>
            {
                { "cameraId", "Camera_Main" },
                { "cameraTag", "Camera_Main" },
                { "scenePath", "Camera_Main" },
                { "cameraType", "main" },
                { "priority", 0 }
            }
        });
        builder.ExportToFileFormat("Assets/Config/ConfigData/camera_registry.json");
    }

    // 10. 地图总览
    private static void GenerateMapOverview()
    {
        var builder = new ConfigBuilder();
        builder.Add("mapId", "test_map");
        builder.Add("mapName", "测试地图");
        builder.Add("mapSize", new Dictionary<string, object> { { "x", 100 }, { "y", 100 } });
        builder.Add("terrainPath", "Map/floor_10x10");
        builder.Add("zones", new List<object>
        {
            new Dictionary<string, object>
            {
                { "zoneId", "zone_01" },
                { "zoneCenter", new Dictionary<string, object> { { "x", 0 }, { "y", 0 } } },
                { "zoneSize", new Dictionary<string, object> { { "x", 100 }, { "y", 100 } } },
                { "zoneType", "test" },
                { "terrainConfigId", "test_terrain" }
            }
        });
        builder.ExportToFileFormat("Assets/Config/ConfigData/map_overview.json");
    }

    // 11. 地形配置
    private static void GenerateTerrainConfig()
    {
        var builder = new ConfigBuilder();
        builder.Add("terrains", new List<object>
        {
            new Dictionary<string, object>
            {
                { "terrainId", "test_terrain" },
                { "terrainType", "flat" },
                { "height", 0.0f },
                { "noiseScale", 0.0f },
                { "noiseAmplitude", 0.0f },
                { "groundTexture", "" },
                { "heightMapPath", "" }
            }
        });
        builder.ExportToFileFormat("Assets/Config/ConfigData/terrain_configs.json");
    }

    // 12. 地图放置
    private static void GenerateMapPlacements()
    {
        var builder = new ConfigBuilder();
        builder.Add("placements", new List<object>
        {
            new Dictionary<string, object>
            {
                { "placementId", "pawn" },
                { "zoneId", "zone_01" },
                { "position", new Dictionary<string, object> { { "x", 1f }, { "y", 0.5f }, { "z", 1f } } },
                { "rotation", new Dictionary<string, object> { { "x", 0 }, { "y", 0 }, { "z", 0 } } },
                { "scale", new Dictionary<string, object> { { "x", 1 }, { "y", 1 }, { "z", 1 } } },
                { "blueprintId", "pawn" },
                { "tag", "Pawn" },
                { "spawnCondition", "always" }
            },
            new Dictionary<string, object>
            {
                { "placementId", "floor_tile" },
                { "zoneId", "zone_01" },
                { "position", new Dictionary<string, object> { { "x", 0 }, { "y", 0 }, { "z", 0 } } },
                { "rotation", new Dictionary<string, object> { { "x", 0 }, { "y", 0 }, { "z", 0 } } },
                { "scale", new Dictionary<string, object> { { "x", 1 }, { "y", 1 }, { "z", 1 } } },
                { "blueprintId", "floor_tile" },
                { "tag", "Floor" },
                { "spawnCondition", "always" }
            }
        });
        builder.ExportToFileFormat("Assets/Config/ConfigData/map_placements.json");

    }

    //逻辑状态
    private static void GenerateLogicState()
    {
        var builder = new ConfigBuilder();
        builder.Add("entries", new List<object>
        {
            new Dictionary<string, object>
            {
                {"containerId", "logic_state_pawn_attack_range" },
                {"scriptName", "PawnAttackRange" },
                {"containerType", "PawnAttackRangeStateContainer" },
                {"savePath", "Assets/Config/ConfigData/logic_state_pawn_attack_range.json" }
            }
        });
        builder.ExportToFileFormat("Assets/Config/ConfigData/logic_state_registry.json");
    }








    // Start is called before the first frame update
    void Start()
    {
        GenerateAudioConfig();
        GenerateMaterialConfig();
        GeneratePhysicsConfig();
        GenerateLogicScriptsConfig();
        GenerateObjectPoolsConfig();
        GenerateEntityBlueprints();
        GenerateUIBlueprints();
        GenerateCanvasRegistry();
        GenerateCameraRegistry();
        GenerateMapOverview();
        GenerateTerrainConfig();
        GenerateMapPlacements();
        GenerateLogicState();
        Debug.Log("所有测试配置已生成！");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
