using System;

[Serializable]
[Config(
    configId: "object_pools",
    priority: 100
)]
public class ObjectPoolsConfig : ConfigDefinition
{
    public PoolData[] pools;

    [Serializable]
    public class PoolData
    {
        public string poolId;         // 字典键
        public int preloadCount;      // 预加载数量
        public int maxSize;           // 最大容量
        public bool autoExpand;       // 自动扩容
    }
}