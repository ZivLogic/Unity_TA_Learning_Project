using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace EventSystemV3
{
    public class EventSystemV_3 : MonoBehaviour
    {
        #region 单例

        private static EventSystemV_3 _instance;
        public static EventSystemV_3 Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<EventSystemV_3>();
                    if (_instance == null)
                    {
                        var go = new GameObject("EventSystemV3");
                        _instance = go.AddComponent<EventSystemV_3>();
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region 配置表

        private readonly Dictionary<string, EventDefinitionEntry> _configTable = new();
        private readonly Dictionary<string, Type> _payloadTypeCache = new();

        #endregion

        #region 多队列

        private readonly Queue<QueuedEvent> _logicQueue = new();
        private readonly Queue<QueuedEvent> _renderQueue = new();
        private readonly Queue<QueuedEvent> _physicsQueue = new();
        private readonly Queue<QueuedEvent> _audioQueue = new();
        private readonly Queue<QueuedEvent> _inputQueue = new();
        private readonly Queue<QueuedEvent> _networkQueue = new();

        #endregion

        #region CD 节流

        private readonly Dictionary<string, float> _lastPublishTime = new();

        #endregion

        #region 调试

        [Header("调试")]
        public bool DebugMode = false;

        #endregion

        #region 初始化

        private bool _initialized = false;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();
        }

        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            LoadConfigs();
            StartAllQueues();

            Debug.Log($"[EventSystemV3] 初始化完成，加载 {_configTable.Count} 个事件定义");
        }

        private void LoadConfigs()
        {
            var config = EventV3ConfigLoader.LoadDefinitions();
            if (config?.entries == null) return;

            _configTable.Clear();
            _payloadTypeCache.Clear();

            foreach (var entry in config.entries)
            {
                if (!entry.Enable) continue;
                _configTable[entry.DefinitionId] = entry;

                if (!string.IsNullOrEmpty(entry.PayloadTypeName))
                {
                    var type = Type.GetType(entry.PayloadTypeName);
                    if (type != null)
                        _payloadTypeCache[entry.DefinitionId] = type;
                    else
                        Debug.LogWarning($"[EventSystemV3] 类型未找到: {entry.PayloadTypeName}");
                }
            }
        }

        public void ReloadConfigs()
        {
            LoadConfigs();
            Debug.Log($"[EventSystemV3] 配置重载完成，{_configTable.Count} 个定义");
        }

        private void StartAllQueues()
        {
            StartCoroutine(ProcessQueue(_logicQueue, EventQueueType.Logic));
            StartCoroutine(ProcessQueue(_renderQueue, EventQueueType.Render));
            StartCoroutine(ProcessQueue(_physicsQueue, EventQueueType.Physics));
            StartCoroutine(ProcessQueue(_audioQueue, EventQueueType.Audio));
            StartCoroutine(ProcessQueue(_inputQueue, EventQueueType.Input));
            StartCoroutine(ProcessQueue(_networkQueue, EventQueueType.Network));
        }

        #endregion

        #region 发布

        /// <summary>
        /// 发布事件（唯一入口）
        /// 目标 UUID 由发布方法传入，不从容器提取
        /// </summary>
        public void Publish<T>(Entity publisher, List<string> targetUuids, T payload)
            where T : EntityEventPayload
        {
            if (publisher == null || payload == null) return;
            if (targetUuids == null || targetUuids.Count == 0) return;

            if (!_initialized) Initialize();

            // 1. 查配置
            if (!_configTable.TryGetValue(payload.DefinitionId, out var cfg))
            {
                Debug.LogWarning($"[EventSystemV3] 未配置事件: {payload.DefinitionId}");
                return;
            }

            // 2. CD 节流
            if (cfg.Cooldown > 0)
            {
                if (_lastPublishTime.TryGetValue(payload.DefinitionId, out var last))
                {
                    if (Time.unscaledTime - last < cfg.Cooldown) return;
                }
                _lastPublishTime[payload.DefinitionId] = Time.unscaledTime;
            }

            // 3. 类型校验
            if (!ValidatePayloadType(payload.DefinitionId, payload)) return;

            // 4. 发布方校验
            if (!ValidatePublisher(publisher, cfg)) return;

            // 5. 入队
            var queued = new QueuedEvent
            {
                Publisher = publisher,
                Payload = payload,
                Config = cfg,
                TargetUuids = new List<string>(targetUuids)
            };

            GetQueue(cfg.QueueType).Enqueue(queued);

            if (DebugMode)
                Debug.Log($"[EventSystemV3] 入队: {payload.DefinitionId} → {targetUuids.Count} 个目标");
        }

        #endregion

        #region 队列处理

        private Queue<QueuedEvent> GetQueue(EventQueueType type)
        {
            return type switch
            {
                EventQueueType.Logic => _logicQueue,
                EventQueueType.Render => _renderQueue,
                EventQueueType.Physics => _physicsQueue,
                EventQueueType.Audio => _audioQueue,
                EventQueueType.Input => _inputQueue,
                EventQueueType.Network => _networkQueue,
                _ => _logicQueue
            };
        }

        private IEnumerator ProcessQueue(Queue<QueuedEvent> queue, EventQueueType type)
        {
            while (true)
            {
                while (queue.Count > 0)
                {
                    var evt = queue.Dequeue();
                    Dispatch(evt);
                }

                yield return type switch
                {
                    EventQueueType.Render => new WaitForSeconds(0.016f),
                    EventQueueType.Physics => new WaitForSeconds(0.01f),
                    EventQueueType.Audio => new WaitForSeconds(0.033f),
                    _ => null
                };
            }
        }

        private void Dispatch(QueuedEvent evt)
        {
            // 直接使用发布方法传入的 UUID 列表
            int delivered = 0;
            foreach (var uuid in evt.TargetUuids)
            {
                var target = MasterFactory.Instance.FindByUUID(uuid);
                if (target == null)
                {
                    Debug.LogWarning($"[EventSystemV3] 实体不存在: {uuid}");
                    continue;
                }

                if (target.IsPooled) continue;

                DeliverToEntity(target, evt.Payload, evt.Config);
                delivered++;
            }

            if (DebugMode)
                Debug.Log($"[EventSystemV3] {evt.Payload.DefinitionId} 投递: {delivered}/{evt.TargetUuids.Count}");
        }

        #endregion

        #region 校验

        private bool ValidatePayloadType(string definitionId, EntityEventPayload payload)
        {
            if (!_payloadTypeCache.TryGetValue(definitionId, out var expectedType)) return true;

            if (payload.GetType() != expectedType)
            {
                Debug.LogError($"[EventSystemV3] 类型不匹配: 期望={expectedType.Name}, 实际={payload.GetType().Name}");
                return false;
            }
            return true;
        }

        private bool ValidatePublisher(Entity publisher, EventDefinitionEntry cfg)
        {
            if (!string.IsNullOrEmpty(cfg.PublisherTag) && publisher.Tag != cfg.PublisherTag)
                return false;
            if (!string.IsNullOrEmpty(cfg.PublisherBlueprintId)
                && publisher.Blueprint.blueprintId != cfg.PublisherBlueprintId)
                return false;
            return true;
        }

        #endregion

        #region 投递

        private void DeliverToEntity(Entity target, EntityEventPayload payload, EventDefinitionEntry cfg)
        {
            if (target?.LogicScripts == null) return;

            foreach (var script in target.LogicScripts)
            {
                if (script is not EntityEventEntry eventEntry) continue;

                // 验证脚本名
                string scriptName = script.GetType().Name;
                bool match = false;

                foreach (var listener in cfg.Listeners)
                {
                    if (listener.ListenerScriptName == scriptName)
                    {
                        match = true;
                        break;
                    }
                }

                if (!match) continue;

                eventEntry.Receive(payload);
            }
        }

        #endregion

        #region 内部类

        private class QueuedEvent
        {
            public Entity Publisher;
            public EntityEventPayload Payload;
            public EventDefinitionEntry Config;
            public List<string> TargetUuids;   // ✅ 发布方法传入
        }

        #endregion
    }
}