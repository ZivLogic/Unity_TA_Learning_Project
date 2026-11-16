using System.Collections;
using System.Collections.Generic;
using System.IO;
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
        string filePath = Path.Combine(Application.streamingAssetsPath, "Config", "audio_configs.json");
        builder.ExportToFileFormat(filePath);
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
        string filePath = Path.Combine(Application.streamingAssetsPath, "Config", "material_database.json");
        builder.ExportToFileFormat(filePath);
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
            },
            new Dictionary<string, object>
            {
                { "configId", "ball_physics" },
                { "mass", 1.0f },
                { "isKinematic", false },
                { "useGravity", true },
                { "interpolate", false },
                { "continuousCollision", false },
                { "colliders", new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            { "type", "box" },
                            { "center", new Dictionary<string, object>
                                {
                                    { "x", 0f },
                                    { "y", 1f },
                                    { "z", -0.6f }
                                }
                            },
                            { "radius", 0f },
                            { "height", 0f },
                            { "size", new Dictionary<string, object>
                                {
                                    { "x", 2f },
                                    { "y", 2f },
                                    { "z", 2f }
                                }
                            },
                            { "direction", 0 },
                            { "convex", false },
                            { "physicsMaterial", "" }
                        }
                    }
                }
            }
        });
        string filePath = Path.Combine(Application.streamingAssetsPath, "Config", "physics_configs.json");
        builder.ExportToFileFormat(filePath);
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
            },
            new Dictionary<string, object>
            {
                { "scriptId", "test_entry" },
                { "scriptName", "TestEntry" },
                { "executionOrder", 0 },
                { "enabled", true }
            },
            new Dictionary<string, object>
            {
                { "scriptId", "character_input_entry" },
                { "scriptName", "CharacterInputEntry" },
                { "executionOrder", 0 },
                { "enabled", true }
            },
            new Dictionary<string, object>
            {
                { "scriptId", "player_tag" },
                { "scriptName", "PlayerTag" },
                { "executionOrder", 0 },
                { "enabled", true }
            },
            new Dictionary<string, object>
            {
                { "scriptId", "aim_line_script" },
                { "scriptName", "AimLineScript" },
                { "executionOrder", 0 },
                { "enabled", true }
            },
            new Dictionary<string, object>
            {
                { "scriptId", "character_attachment_script" },
                { "scriptName", "CharacterAttachmentScript" },
                { "executionOrder", 0 },
                { "enabled", true }
            },
            new Dictionary<string, object>
            {
                { "scriptId", "chest_interact_script" },
                { "scriptName", "ChestInteractScript" },
                { "executionOrder", 0 },
                { "enabled", true }
            },
            new Dictionary<string, object>
            {
                { "scriptId", "player_state_machine_script" },
                { "scriptName", "PlayerStateMachineScript" },
                { "executionOrder", 0 },
                { "enabled", true }
            },
            new Dictionary<string, object>
            {
                { "scriptId", "AI_state_machine_script" },
                { "scriptName", "AIStateMachineScript" },
                { "executionOrder", 0 },
                { "enabled", true }
            },
            new Dictionary<string, object>
            {
                { "scriptId", "camera_rig" },
                { "scriptName", "CameraRigScript" },
                { "executionOrder", 0 },
                { "enabled", true }
            }
        });
        string filePath = Path.Combine(Application.streamingAssetsPath, "Config", "logic_scripts.json");
        builder.ExportToFileFormat(filePath);
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
        string filePath = Path.Combine(Application.streamingAssetsPath, "Config", "object_pools.json");
        builder.ExportToFileFormat(filePath);
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
                { "logicScriptIds", new List<object> { "pawn_attack_range", "test_entry", "chest_interact_script" } },
                { "poolId", "pawn_pool" }
            },
            new Dictionary<string, object>
            {
                { "blueprintId", "test_AI" },
                { "tag", "AI" },
                { "modelPath", "TestGameObject/Ball_Test_001" },  
                { "defaultHeight", 0.5f },
                { "physicsConfigId", "ball_physics" },
                { "materialId", "" },
                { "audioIds", new List<object> { "pawn_move" } },
                { "logicScriptIds", new List<object> { "pawn_attack_range", "test_entry", "AI_state_machine_script" } },
                { "poolId", "pawn_pool" }
            },
            new Dictionary<string, object>
            {
                { "blueprintId", "test_player" },
                { "tag", "Player" },
                { "modelPath", "TestGameObject/Ball_Test_001" },  
                { "defaultHeight", 0.5f },
                { "physicsConfigId", "ball_physics" },
                { "materialId", "" },
                { "audioIds", new List<object> { "pawn_move" } },
                { "logicScriptIds", new List<object> { "pawn_attack_range", 
                    "character_input_entry", 
                    "test_entry", 
                    "camera_rig" , 
                    "player_tag",
                    "character_attachment_script",
                    "aim_line_script",
                    "player_state_machine_script"
                } },
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
        string filePath = Path.Combine(Application.streamingAssetsPath, "Config", "entity_blueprints.json");
        builder.ExportToFileFormat(filePath);
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
                { "prefabPath", "TestImage/TEST_UI_003" },  // 你的图片
                { "canvasName", "Canvas_Main" },
                { "sortingOrder", 100 },
                { "uiLayer", "normal" },
                { "raycastTarget", false },
                { "alwaysOnTop", false }
            }
        });
        string filePath = Path.Combine(Application.streamingAssetsPath, "Config", "ui_blueprints.json");
        builder.ExportToFileFormat(filePath);
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
        string filePath = Path.Combine(Application.streamingAssetsPath, "Config", "canvas_registry.json");
        builder.ExportToFileFormat(filePath);
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
        string filePath = Path.Combine(Application.streamingAssetsPath, "Config", "camera_registry.json");
        builder.ExportToFileFormat(filePath);
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
        string filePath = Path.Combine(Application.streamingAssetsPath, "Config", "map_overview.json");
        builder.ExportToFileFormat(filePath);
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
        string filePath = Path.Combine(Application.streamingAssetsPath, "Config", "terrain_configs.json");
        builder.ExportToFileFormat(filePath);
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
        string filePath = Path.Combine(Application.streamingAssetsPath, "Config", "map_placements.json");
        builder.ExportToFileFormat(filePath);

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
            },
            new Dictionary<string, object>
            {
                {"containerId", "player_state_storage" },
                {"scriptName", "PlayerStateMachineScript" },
                {"containerType", "PlayerStateContainer" },
                {"savePath", "Assets/Config/ConfigData/player_state_storage.json" }
            },
            new Dictionary<string, object>
            {
                {"containerId", "ai_state_storage" },
                {"scriptName", "AIStateMachineScript" },
                {"containerType", "AIStateContainer" },
                {"savePath", "Assets/Config/ConfigData/ai_state_storage.json" }
            },
            new Dictionary<string, object>
            {
                {"containerId", "logic_state_camera_rig" },
                {"scriptName", "CameraRigScript" },
                {"containerType", "CameraRigStateContainer" },
                {"savePath", "Assets/Config/ConfigData/logic_state_camera_rig.json" }
            }
        });
        string filePath = Path.Combine(Application.streamingAssetsPath, "Config", "logic_state_registry.json");
        builder.ExportToFileFormat(filePath);
    }

    //蓝图注册表
    private static void GeneratePrductionLauncher()
    {
        var builder = new ConfigBuilder();
        builder.Add("entries", new List<object>
        {
            new Dictionary<string, object>
            {
                {"blueprintId", "pawn" },
                {"count", 20 },
                {"spawnCount", 5 },
                {"prewarm", true},
                {"basePosition", new Dictionary<string, object> { { "x", 505 }, { "y", 2 }, { "z", 505 } } },
                {"spacing", new Dictionary<string, object> { { "x", 1 }, { "y", 0 }, { "z", 0 } } },
                {"tagOverride", ""}
            },
             new Dictionary<string, object>
            {
                {"blueprintId", "test_AI" },
                {"count", 1 },
                {"spawnCount", 1 },
                {"prewarm", true},
                {"basePosition", new Dictionary<string, object> { { "x", 520 }, { "y", 2 }, { "z", 520 } } },
                {"spacing", new Dictionary<string, object> { { "x", 0 }, { "y", 0 }, { "z", 0 } } },
                {"tagOverride", ""}
            },
            new Dictionary<string, object>
            {
                {"blueprintId", "test_player" },
                {"count", 1 },
                {"spawnCount", 1 },
                {"prewarm", true},
                {"basePosition", new Dictionary<string, object> { { "x", 500 }, { "y", 2 }, { "z", 500 } } },
                {"spacing", new Dictionary<string, object> { { "x", 0 }, { "y", 0 }, { "z", 0 } } },
                {"tagOverride", ""}
            },
            new Dictionary<string, object>
            {
                {"blueprintId", "pawn_info_ui" },
                {"count", 1 },
                {"spawnCount", 1 },
                {"prewarm", false},
                {"basePosition", new Dictionary<string, object> { { "x", 0 }, { "y", 0 }, { "z", 0 } } },
                {"spacing", new Dictionary<string, object> { { "x", 0 }, { "y", 0 }, { "z", 0 } } },
                {"tagOverride", ""}
            }
        });
        string filePath = Path.Combine(Application.streamingAssetsPath, "Config", "production_configs.json");
        builder.ExportToFileFormat(filePath);
    }


    //玩家状态机配置
    private static void GenerateEntityState_Player_Config()
    {
        var builder = new ConfigBuilder();
        builder.Add("ActionRules", new List<object>
        {
            new Dictionary<string, object>
            {
                {"SourceState","Idle"  },
                {"BlockedActions", new List<string>
                {
                    "Moving",
                    "Attacking"
                } }
            }
        })
        .Add("LifecycleRules", new List<object>
        {
            new Dictionary<string, object>
        {
            {"SourceState","Dead"  },
            {"BlockedActions", new List<string>
            {
                "Moving",
                "Attacking"
            } }
        }
        });
        string filePath = Path.Combine(Application.streamingAssetsPath, "Config", "player_rule_config.json");
        builder.ExportToFileFormat(filePath);
    }

    //AI状态机配置
    private static void GenerateEntityState_AI_Config()
    {
        var builder = new ConfigBuilder();
        builder.Add("ActionRules", new List<object>
        {
            new Dictionary<string, object>
            {
                {"SourceState","Idle"  },
                {"BlockedActions", new List<string>
                {
                    "Moving",
                    "Attacking"
                } }
            }
        })
        .Add("LifecycleRules", new List<object>
        {
            new Dictionary<string, object>
        {
            {"SourceState","Dead"  },
            {"BlockedActions", new List<string>
            {
                "Moving",
                "Attacking"
            } }
        }
        });
        string filePath = Path.Combine(Application.streamingAssetsPath, "Config", "ai_rule_config.json");
        builder.ExportToFileFormat(filePath);
    }

    //BUFF状态配置
    private static void GenerateEntityStateBuffConfig()
    {
        var builder = new ConfigBuilder();
        builder.Add("Buffs", new List<object>
        {
            new Dictionary<string, object>
            {
                {"BuffName","Burning"  },
                {"Duration", 5f },
                {"IsPermanent",false},
                {"DefaultTickInterval", 1f},
                {"PrecisionThreshold", 2f },
                {"PrecisionTickInterval", 0.5f},
                {"IsEnable", true},
                {"IsRefreshable", false }
            }
        });
        string filePath = Path.Combine(Application.streamingAssetsPath, "Config", "buff_meta_config.json");
        builder.ExportToFileFormat(filePath);
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
        GeneratePrductionLauncher();
        GenerateEntityState_Player_Config();
        GenerateEntityStateBuffConfig();
        GenerateEntityState_AI_Config();
        Debug.Log("所有测试配置已生成！");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
