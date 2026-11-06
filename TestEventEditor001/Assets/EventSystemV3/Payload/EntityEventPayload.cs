using System;

namespace EventSystemV3
{
    /// <summary>
    /// 实体事件容器基类
    /// 规范：接收方只读，永不修改
    /// </summary>
    [Serializable]
    public abstract class EntityEventPayload
    {
        /// <summary>事件定义 ID</summary>
        public string DefinitionId;

        /// <summary>发布帧号</summary>
        public int Frame;

        /// <summary>发布方 UUID（权威来源）</summary>
        public string PublisherUuid;
    }
}