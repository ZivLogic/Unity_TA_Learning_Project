using System;
using System.Collections.Generic;

[Serializable]
[Config(configId: "ai_state_storage", priority: 100)]
public class AIStateContainer : ConfigDefinition
{
    public AIStateEntry[] entries;

    [Serializable]
    public class AIStateEntry : ILogicStateEntry
    {
        public string uuid;
        public string tag;
        public string Lifecycle;
        public string Action;
        public string PrePausedLifecycle;
        public string PrePausedAction;
        public List<string> Buffs;

        public string GetUUID() => uuid;
        public string GetTag() => tag;
    }
}