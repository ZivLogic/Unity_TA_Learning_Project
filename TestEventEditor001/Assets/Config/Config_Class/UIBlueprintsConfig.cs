using System;

[Serializable]
[Config(
    configId: "ui_blueprints",
    priority: 200,
    dependsOn: new[] {
        "ui_prefabs",
        "logic_scripts",
        "canvas_registry",    // 依赖画布注册表
        "camera_registry"     // 依赖摄像机注册表
    }
)]
public class UIBlueprintsConfig : ConfigDefinition
{
    public UIBlueprint[] blueprints;

    [Serializable]
    public class UIBlueprint : EntityBlueprintBase
    {
        public string prefabPath;        // UI预制体路径
        public string canvasName;        // 所属画布ID（引用canvas_registry）
        public int sortingOrder;         // 渲染顺序

        // 新增字段
        public string uiLayer;           // UI层级：background, normal, popup, tooltip, overlay
        public bool raycastTarget;       // 是否可穿透
        public bool alwaysOnTop;         // 是否始终顶层
    }
}