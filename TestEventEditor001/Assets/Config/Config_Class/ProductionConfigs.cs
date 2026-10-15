using System;
using UnityEngine;

//批量生产配置：定义游戏启动时创建的所有实体
[Serializable]
[Config(
    configId: "production_configs",
    priority: 100 
    )]
public class ProductionConfigs : ConfigDefinition
{
    public ProductionEntry[] entries;

    [Serializable]
    public class ProductionEntry
    {
        public string blueprintId;         //蓝图ID
        public int count;                  //总数量（预热到对象池）
        public int spawnCount;             //实际激活数量（从池中取出）
        public bool prewarm;               //是否预热到对象池
        public Vector3 basePosition;       //基础位置
        public Vector3 spacing;            //间隔（用于批量生成）
        public string tagOverride;         //Tag覆盖 （可选）
    }
}