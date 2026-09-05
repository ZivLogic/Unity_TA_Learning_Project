using System;

[Serializable]
[Config(
    configId: "canvas_registry",
    priority: 50  // 最高优先级
)]
public class CanvasRegistryConfig : ConfigDefinition
{
    public CanvasEntry[] canvases;

    [Serializable]
    public class CanvasEntry
    {
        public string canvasId;          // 字典键
        public string canvasTag;         // Tag
        public string scenePath;         // 场景中的路径
        public string cameraId;          // 绑定的摄像机ID
        public int sortingOrder;         // 渲染顺序
        public string renderMode;        // ScreenSpace / WorldSpace
    }
}