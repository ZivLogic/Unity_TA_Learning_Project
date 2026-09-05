using System;
using UnityEngine;

[Serializable]
[Config(
    configId: "map_cache",
    priority: 50  // 最高优先级
)]
public class MapCacheConfig : ConfigDefinition
{
    public string mapId;
    public string cacheVersion;
    public MapCacheEntry[] entries;

    [Serializable]
    public class MapCacheEntry
    {
        public string uuid;
        public string tag;
        public string blueprintId;
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;
    }
}