using System;

[Serializable]
[Config(
    configId: "camera_registry",
    priority: 50  // 最高优先级，最先加载
)]
public class CameraRegistryConfig : ConfigDefinition
{
    public CameraEntry[] cameras;

    [Serializable]
    public class CameraEntry
    {
        public string cameraId;          // 字典键
        public string cameraTag;         // Tag（用于查找）
        public string scenePath;         // 场景中的路径（用于查找）
        public string cameraType;        // 类型：main, weapon, vehicle
        public int priority;             // 优先级
    }
}