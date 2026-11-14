using System;
using System.Collections.Generic;

[Serializable]
[Config(
    configId: "entity_state_machine",
    priority: 100
)]
public class StateMachineStateContainer : ConfigDefinition
{
    public StateMachineEntry[] entries;

    [Serializable]
    public class StateMachineEntry : ILogicStateEntry
    {
        public string uuid;
        public string tag;

        // 全部改成字符串，防止 JsonUtility 解析枚举报错
        public string Lifecycle;
        public string GameFlow;
        public string Action;

        public List<string> Tags;

        public string GetUUID() => uuid;
        public string GetTag() => tag;
    }
}