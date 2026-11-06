using System;

namespace EventSystemV3
{
    [Serializable]
    public class EventDefinitionConfigRoot
    {
        public EventDefinitionEntry[] entries = Array.Empty<EventDefinitionEntry>();
    }

    [Serializable]
    public class EventDefinitionEntry
    {
        public string DefinitionId;
        public string Description;
        public string PayloadTypeName;
        public bool Enable = true;

        public EventQueueType QueueType = EventQueueType.Logic;
        public float Cooldown = 0f;

        public string PublisherTag;
        public string PublisherBlueprintId;
        public string PublisherMethodFullName;

        public ListenEntry[] Listeners = Array.Empty<ListenEntry>();
    }

    [Serializable]
    public class ListenEntry
    {
        public string ListenerScriptName;
        public string ListenerMethodName;
        public string ListenerPayloadTypeName;
        public string ListenerTag;
        public string ListenerBlueprintId;
    }
}