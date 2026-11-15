using System;
using System.Collections.Generic;

[Serializable]
public class BuffMetaConfig
{
    public string BuffName;               // 如 "Burning"
    public float Duration;                // 持续时间（永久时此字段无效）
    public bool IsPermanent;              // 是否永久存在
    public float DefaultTickInterval;     // 默认节流（长周期）
    public float PrecisionThreshold;      // 精确阈值（如剩余120秒时触发）
    public float PrecisionTickInterval;   // 精确节流（短周期）
    public bool IsEnable;

    // 是否允许重复触发时刷新（延长时间）
    // true = 重复添加会重置时间（如燃烧、中毒）
    // false = 重复添加会被忽略，维持原时间（如无敌、免控、装备光环）
    public bool IsRefreshable = true;
}

[Serializable]
[Config(configId: "buff_meta_config", priority: 50)]
public class BuffMetaConfigRoot : ConfigDefinition
{
    public List<BuffMetaConfig> Buffs;
}