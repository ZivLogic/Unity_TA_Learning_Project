// Framework/EntityBlueprintBase.cs
using System;

/// <summary>
/// 所有蓝图的基类
/// </summary>
[Serializable]
public abstract class EntityBlueprintBase
{
    public string blueprintId;
    public string tag;
    public float defaultHeight = 1.0f;
    public string[] logicScriptIds;
}

// 实体蓝图
[Serializable]
[Config(
    configId: "entity_blueprints",
    priority: 200,
    dependsOn: new[] {
        "logic_scripts",
        "object_pools",
        "physics_configs",
        "material_database"
    }
)]
public class EntityBlueprintsConfig : ConfigDefinition
{
    public EntityBlueprint[] blueprints;

    [Serializable]
    public class EntityBlueprint : EntityBlueprintBase
    {
        public string modelPath;
        public string physicsConfigId;
        public string materialId;
        public string poolId;
        public string[] audioIds;  // 音频ID（可选）
    }
}
