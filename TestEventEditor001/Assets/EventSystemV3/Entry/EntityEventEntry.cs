using System;
using System.Collections.Generic;
using UnityEngine;

namespace EventSystemV3
{
    public abstract class EntityEventEntry : LogicScriptBase
    {
        public override string ContainerId => "entity_event_entry_v3";

        #region 接收能力

        private readonly Dictionary<string, Action<EntityEventPayload>> _handlers = new();

        protected virtual void Awake() => RegisterHandlers();

        protected virtual void RegisterHandlers() { }

        protected void Register<T>(string definitionId, Action<T> handler)
            where T : EntityEventPayload
        {
            if (string.IsNullOrEmpty(definitionId) || handler == null) return;

            _handlers[definitionId] = (payload) =>
            {
                if (payload is T typed) handler(typed);
            };
        }

        public void Receive(EntityEventPayload payload)
        {
            if (payload == null) return;
            if (!_handlers.TryGetValue(payload.DefinitionId, out var handler)) return;

            try { handler.Invoke(payload); }
            catch (Exception e) { Debug.LogError($"[EntityEventEntry] {payload.DefinitionId}\n{e}"); }
        }

        #endregion

        #region 发布能力

        /// <summary>
        /// 单对单发布
        /// </summary>
        protected void Publish<T>(string targetUuid, T payload) where T : EntityEventPayload
        {
            if (payload == null)
            {
                Debug.LogError("[EntityEventEntry] payload 为空");
                return;
            }

            if (string.IsNullOrEmpty(targetUuid))
            {
                Debug.LogError("[EntityEventEntry] targetUuid 为空");
                return;
            }

            // 自动填充元信息
            payload.PublisherUuid = Entity?.UUID;
            payload.Frame = Time.frameCount;

            // 调用 EventSystemV3
            EventSystemV_3.Instance.Publish(Entity, new List<string> { targetUuid }, payload);
        }

        /// <summary>
        /// 单对多发布
        /// </summary>
        protected void PublishList<T>(List<string> targetUuids, T payload) where T : EntityEventPayload
        {
            if (payload == null)
            {
                Debug.LogError("[EntityEventEntry] payload 为空");
                return;
            }

            if (targetUuids == null || targetUuids.Count == 0)
            {
                Debug.LogError("[EntityEventEntry] targetUuids 为空");
                return;
            }

            payload.PublisherUuid = Entity?.UUID;
            payload.Frame = Time.frameCount;

            EventSystemV_3.Instance.Publish(Entity, targetUuids, payload);
        }

        #endregion

        #region 公开接口（供外部调用）

        /// <summary>
        /// 公开单对单发布（供输入入口等外部调用）
        /// </summary>
        public void PublishEvent<T>(string targetUuid, T payload) where T : EntityEventPayload
        {
            Publish(targetUuid, payload);
        }

        /// <summary>
        /// 公开单对多发布
        /// </summary>
        public void PublishEventList<T>(List<string> targetUuids, T payload) where T : EntityEventPayload
        {
            PublishList(targetUuids, payload);
        }

        /// <summary>
        /// 公开注册入口
        /// </summary>
        public void RegisterEvent<T>(string definitionId, Action<T> handler)
            where T : EntityEventPayload
        {
            Register(definitionId, handler);
        }

        #endregion
    }
}