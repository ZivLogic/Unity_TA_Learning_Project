using System;
using System.Collections.Generic;

[Serializable]
[Config(configId: "player_state_storage", priority: 100)]
public class PlayerStateContainer : ConfigDefinition
{
    public PlayerStateEntry[] entries;

    [Serializable]
    public class PlayerStateEntry : ILogicStateEntry
    {
        public string uuid;
        public string tag;
        public string Lifecycle;
        public string Action;
        public string PrePausedLifecycle;
        public string PrePausedAction;
        // ¥Ê¥¢∏Ò Ω£∫"BuffName|RemainingTime"
        public List<string> Buffs;


        public string GetUUID() => uuid;
        public string GetTag() => tag;
    }
}