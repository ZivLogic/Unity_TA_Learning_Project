using UnityEngine;
using System;
using System.Collections.Generic;

public abstract class EntityStateMachineBase : LogicScriptBase
{
    // 规则表
    protected Dictionary<EntityLifecycleState, List<EntityActionState>> _lifecycleRules = new();
    protected Dictionary<EntityActionState, List<EntityActionState>> _actionRules = new();

    // Buff 配置池
    protected Dictionary<string, BuffMetaConfig> _buffMetas = new();

    // 运行时状态
    protected EntityLifecycleState _currentLifecycle = EntityLifecycleState.Alive;
    protected EntityActionState _currentAction = EntityActionState.Idle;

    // 暂停备份
    protected EntityLifecycleState _prePauseLifecycle;
    protected EntityActionState _prePauseAction;
    protected float _pauseStartTime = 0f;

    // 激活的 Buff
    protected List<BuffInstance> _activeBuffs = new();

    public override void OnAllStateInjected()
    {

        // 自动注入接口
        if (Entity?.LogicScripts != null)
        {
            foreach (var script in Entity.LogicScripts)
            {
                if (script is IEntityStateMachineProvider provider)
                    provider.InjectStateMachine(this);
            }
        }

        _isStateReady = true;
        Debug.Log($"[状态机] {Entity.UUID} 状态就绪，开始计算 Buff");
    }

    protected abstract void LoadMetaConfigs();

    // 加载 Buff 配置表（通用加载）
    protected void LoadBuffMetaConfigs()
    {
        var config = ConfigAPI.Get<BuffMetaConfigRoot>("buff_meta_config");
        if (config?.Buffs != null)
            foreach (var b in config.Buffs)
                _buffMetas[b.BuffName] = b;
    }

    private void Update()
    {
        // 统一处理所有 Buff 的时间流逝
        UpdateBuffTimers();
    }

    #region ===== 状态拦截与变更 =====

    public virtual bool TryChangeAction(EntityActionState newAction)
    {
        // 未就绪时拒绝状态变更，防止在初始化阶段被乱改
        if (!_isStateReady)
        {
            Debug.LogWarning($"[状态机] 未就绪，拒绝动作变更：{newAction}");
            return false;
        }

        // 1. 生命周期拦截（比如死亡禁止攻击）
        if (_lifecycleRules.TryGetValue(_currentLifecycle, out var blockedByLife) && blockedByLife.Contains(newAction))
        {
            Debug.LogWarning($"[状态机] 拦截：生命周期 {_currentLifecycle} 禁止动作 {newAction}");
            return false;
        }

        // 2. 动作拦截（比如背包禁止移动）
        if (_actionRules.TryGetValue(_currentAction, out var blockedByAction) && blockedByAction.Contains(newAction))
        {
            Debug.LogWarning($"[状态机] 拦截：动作 {_currentAction} 禁止动作 {newAction}");
            return false;
        }

        if (_currentAction == newAction)
        {
            Debug.Log($"{Entity.UUID}状态维持中，当前状态：{_currentAction}");
            return true;
        }

        _currentAction = newAction;
        Entity.Action = newAction;
        Debug.Log($"{Entity.UUID}的状态变更！新状态：{newAction}");
        return true;
    }

    #endregion

    #region ===== 暂停与恢复 =====

    public void PauseEntity()
    {
        if (_currentLifecycle == EntityLifecycleState.Paused) return;

        _prePauseLifecycle = _currentLifecycle;
        _prePauseAction = _currentAction;

        _currentLifecycle = EntityLifecycleState.Paused;
        Entity.Lifecycle = EntityLifecycleState.Paused;

        _pauseStartTime = Time.time;

        foreach (var buff in _activeBuffs) buff.IsPaused = true;
    }

    public void ResumeEntity()
    {
        if (_currentLifecycle != EntityLifecycleState.Paused) return;

        float pauseDuration = Time.time - _pauseStartTime;

        _currentLifecycle = _prePauseLifecycle;
        _currentAction = _prePauseAction;
        Entity.Lifecycle = _prePauseLifecycle;
        Entity.Action = _prePauseAction;

        foreach (var buff in _activeBuffs)
        {
            buff.IsPaused = false;

            // 永久 Buff 的时间不动
            if (!buff.Meta.IsPermanent)
            {
                buff.EndTime += pauseDuration;
                if (buff.NextTickTime != float.MaxValue)
                    buff.NextTickTime += pauseDuration;
            }
            else
            {
                // 永久 Buff 如果有节流检测（比如光环每秒扣血），时间也需要往后推
                if (buff.NextTickTime != float.MaxValue)
                    buff.NextTickTime += pauseDuration;
            }
        }
    }

    #endregion

    #region ===== Buff 系统 =====

    [Serializable]
    public class BuffInstance
    {
        public BuffMetaConfig Meta;
        public float EndTime;
        public float NextTickTime;
        public float CurrentInterval;
        public bool IsPaused;
    }

    /// <summary>状态是否已完全就绪（防止未初始化完就开始计算）</summary>
    protected bool _isStateReady = false;

    // ===== Buff 触发方法注册表 =====
    // Key: BuffName (如 "Burning")
    // Value: 具体触发时执行的逻辑
    protected Dictionary<string, Action> _buffTickHandlers = new Dictionary<string, Action>();

    /// <summary>
    /// 注册 Buff 的节流触发回调（外部业务脚本在初始化时调用）
    /// </summary>
    public void RegisterBuffTickHandler(string buffName, Action handler)
    {
        if (string.IsNullOrEmpty(buffName) || handler == null) return;
        _buffTickHandlers[buffName] = handler;
    }

    /// <summary>
    /// 为自己添加一个BUFF（依赖BuffConfig配置定义）
    /// </summary>
    public virtual void AddBuff(string buffName)
    {
        if (!_isStateReady)
        {
            Debug.LogWarning($"[状态机] 未就绪，拒绝添加 Buff：{buffName}");
            return;
        }

        if (!_buffMetas.TryGetValue(buffName, out var meta) || !meta.IsEnable) return;

        var existing = _activeBuffs.Find(b => b.Meta.BuffName == buffName);
        if (existing != null)
        {
            // 如果已经存在，检查是否允许刷新
            if (!meta.IsRefreshable)
            {
                Debug.Log($"[Buff] {buffName} 已存在且不可刷新，忽略此次添加");
                return;
            }

            // 允许刷新：且不是永久，则刷新时间
            if (!meta.IsPermanent)
            {
                existing.EndTime = Time.time + meta.Duration;
                existing.CurrentInterval = meta.DefaultTickInterval;
                existing.NextTickTime = Time.time + meta.DefaultTickInterval;
                Debug.Log($"[Buff] {buffName} 刷新持续时间");
            }
            return;
        }

        // 如果不存在，正常添加
        _activeBuffs.Add(new BuffInstance
        {
            Meta = meta,
            EndTime = meta.IsPermanent ? float.MaxValue : Time.time + meta.Duration,
            CurrentInterval = meta.DefaultTickInterval,
            NextTickTime = meta.DefaultTickInterval > 0 ? Time.time + meta.DefaultTickInterval : float.MaxValue,
            IsPaused = false
        });

        // 反注入权威信息
        Entity.AddTag(buffName);
        Debug.Log($"[Buff] 成功添加 {buffName}，{(meta.IsPermanent ? "永久存在" : $"持续 {meta.Duration} 秒")}");
    }

    /// <summary>
    /// 主动消除指定的 Buff（装备脱下、驱散魔法等）
    /// </summary>
    public virtual void RemoveBuff(string buffName)
    {
        var existing = _activeBuffs.Find(b => b.Meta.BuffName == buffName);
        if (existing != null)
        {
            _activeBuffs.Remove(existing);
            Debug.Log($"[Buff] 主动消除了 {buffName}");

            // Entity 移除对应的 Tag
            Entity.RemoveTag(buffName);
        }
    }


    protected virtual void UpdateBuffTimers()
    {
        // 未就绪时完全不处理 Buff 计时
        if (!_isStateReady) return;

        if (_activeBuffs.Count == 0) return;

        for (int i = _activeBuffs.Count - 1; i >= 0; i--)
        {
            var buff = _activeBuffs[i];
            if (buff.IsPaused) continue;
            if (buff.Meta.IsPermanent) continue; // 永久 Buff 不参与过期判定

            float now = Time.time;
            float remaining = buff.EndTime - now;

            // 1. 处理过期
            if (remaining <= 0)
            {
                _activeBuffs.RemoveAt(i);
                Entity.RemoveTag(buff.Meta.BuffName); // 移出 Entity 权威标签
                Debug.Log($"[Buff] {buff.Meta.BuffName} 已过期移除");
                continue;
            }

            // 动态步长切换
            if (remaining <= buff.Meta.PrecisionThreshold && buff.CurrentInterval != buff.Meta.PrecisionTickInterval)
            {
                buff.CurrentInterval = buff.Meta.PrecisionTickInterval;
                buff.NextTickTime = now + buff.CurrentInterval;
            }

            if (now >= buff.NextTickTime)
            {
                TriggerBuffTick(buff.Meta.BuffName);
                buff.NextTickTime = now + buff.CurrentInterval;
            }
        }
    }

    protected virtual void TriggerBuffTick(string buffName)
    {
        // 如果在注册表里找到了对应的逻辑，就执行
        if (_buffTickHandlers.TryGetValue(buffName, out var handler))
        {
            handler?.Invoke();
        }
        else
        {
            // 注册表里没找到，说明这个 Buff 可能只有状态没有节流伤害，或者没注册
            Debug.Log($"[Buff] {buffName} 触发了一次 Tick，但没有注册回调逻辑。");
        }
    }

    #endregion

    #region ===== 快照与存读档 =====

    public EntityStateSnapshot GetSnapshot()
    {
        return new EntityStateSnapshot
        {
            UUID = Entity.UUID,
            Lifecycle = Entity.Lifecycle.ToString(),
            Action = Entity.Action.ToString(),
            Tags = new List<string>(Entity.GetAllTags()).ToArray()
        };
    }

    public override ILogicStateEntry SaveToEntry()
    {
        // 1. 把当前的 Buff 实例，全部拍平成 "BuffName|RemainingTime" 格式的字符串
        List<string> buffStrings = new List<string>();
        foreach (var buff in _activeBuffs)
        {
            // 如果是永久 Buff，剩余时间存为 "Max"，否则保留两位小数
            string timeStr = buff.Meta.IsPermanent
                ? "Max"
                : (buff.EndTime - Time.time).ToString("F2");

            buffStrings.Add($"{buff.Meta.BuffName}|{timeStr}");
        }

        // 2. 返回全新的数据
        return new PlayerStateContainer.PlayerStateEntry
        {
            uuid = Entity.UUID,
            tag = Entity.Tag,
            Lifecycle = _currentLifecycle.ToString(),
            Action = _currentAction.ToString(),
            PrePausedLifecycle = _prePauseLifecycle.ToString(),
            PrePausedAction = _prePauseAction.ToString(),

            // 传入纯字符串列表
            Buffs = buffStrings
        };
    }

    // 存储读取逻辑的注册表：Key = 数据类型（如 typeof(PlayerStateEntry)）
    protected Dictionary<Type, Action<ILogicStateEntry>> _stateLoaders = new();

    /// <summary>
    /// 子类必须实现此方法，在方法里调用 RegisterStateLoader 注册自己的类型
    /// </summary>
    protected abstract void RegisterAllLoaders();

    /// <summary>
    /// 子类注册自己的状态读取逻辑
    /// </summary>
    protected void RegisterStateLoader(Type entryType, Action<ILogicStateEntry> loader)
    {
        if (entryType == null || loader == null) return;
        _stateLoaders[entryType] = loader;
    }


    public override void LoadFromEntry(ILogicStateEntry entry)
    {
        // 1. 【时序修复】强制子类注册，防子类没来得及注册
        _stateLoaders.Clear(); // 防止重复添加
        RegisterAllLoaders();

        // 2. 加载配置
        LoadMetaConfigs();
        LoadBuffMetaConfigs();

        if (entry == null) return;

        // 3. 查表分发
        if (_stateLoaders.TryGetValue(entry.GetType(), out var loader))
        {
            loader.Invoke(entry);
        }
        else
        {
            Debug.LogWarning($"[状态机] 未注册该类型的状态加载器：{entry.GetType().Name}");
        }
    }


    #endregion
}