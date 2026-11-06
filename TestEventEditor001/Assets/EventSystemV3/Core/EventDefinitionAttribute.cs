using System;

namespace EventSystemV3
{
    //事件标记定义
    //发布方法挂此标记，生成器据此生成配置
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class EventDefinitionAttribute : Attribute
    {
        public string DefinitionId { get; }
        public string Description { get; }
        public Type PayloadType { get; }
        public EventQueueType QueueType { get; }
        public float Cooldown { get; }

        public EventDefinitionAttribute(
            string definitionId,
            string description = "",
            Type payloadType = null,
            EventQueueType queueType = EventQueueType.Logic,
            float cooldown = 0f)
        {
            DefinitionId = definitionId;
            Description = description;
            PayloadType = payloadType;
            QueueType = queueType;
            Cooldown = cooldown;
        }
    }
}