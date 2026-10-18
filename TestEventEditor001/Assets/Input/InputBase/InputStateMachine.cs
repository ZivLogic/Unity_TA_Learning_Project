using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 输入状态机
/// 基础状态：互斥，同一时刻一个
/// 附加状态：可叠加，可多个
/// 状态决定：当前启用哪些拦截层
/// </summary>
public class InputStateMachine
{
    // 当前基础状态
    public string CurrentBaseState { get; private set; }

    // 当前附加状态列表（有序，后压入的在前）
    private List<string> _additiveStates = new List<string>();

    // 状态配置查询
    private InputStateContainer _stateContainer;

    // 状态变更事件
    public event Action<string, string> OnBaseStateChanged;        // (old, new)
    public event Action<string, bool> OnAdditiveStateChanged;      // (state, isAdd)

    /// <summary>
    /// 初始化
    /// </summary>
    public void Init(InputStateContainer stateContainer, string defaultBaseState)
    {
        _stateContainer = stateContainer;

        string defaultState = string.IsNullOrEmpty(defaultBaseState)
            ? "Normal"
            : defaultBaseState;

        // 校验默认状态存在
        if (_stateContainer != null && !_stateContainer.Dict.ContainsKey(defaultState))
        {
            Debug.LogWarning($"[InputStateMachine]默认状态不存在：{defaultState}，使用第一个可用状态");
            foreach (var kv in _stateContainer.Dict)
            {
                defaultState = kv.Key;
                break;
            }
        }

        CurrentBaseState = defaultState;
        _additiveStates.Clear();
        Debug.Log($"[InputStateMachine]初始基础状态：{CurrentBaseState}");
    }

    // ========== 基础状态 ==========
    /// <summary>
    /// 切换基础状态
    /// </summary>
    public void SwitchBaseState(string stateName)
    {
        if (string.IsNullOrEmpty(stateName))
        {
            Debug.LogWarning("[InputStateMachine]基础状态为空，忽略");
            return;
        }
        if (stateName == CurrentBaseState) return;

        // 校验存在
        if (_stateContainer != null && !_stateContainer.Dict.ContainsKey(stateName))
        {
            Debug.LogWarning($"[InputStateMachine]基础状态不存在：{stateName}");
            return;
        }

        // 读取旧状态配置，判断是否清空附加
        bool clearAdditive = true;
        if (_stateContainer != null
            && _stateContainer.Dict.TryGetValue(CurrentBaseState, out var oldCfg))
        {
            clearAdditive = oldCfg.ClearAdditiveOnBaseSwitch;
        }

        string old = CurrentBaseState;
        CurrentBaseState = stateName;

        if (clearAdditive && _additiveStates.Count > 0)
        {
            var copy = new List<string>(_additiveStates);
            _additiveStates.Clear();
            foreach (var s in copy)
                OnAdditiveStateChanged?.Invoke(s, false);
        }

        OnBaseStateChanged?.Invoke(old, stateName);
        Debug.Log($"[InputStateMachine]基础状态切换：{old} → {stateName}");
    }

    // ========== 附加状态 ==========
    /// <summary>
    /// 压入附加状态
    /// </summary>
    public void PushAdditiveState(string stateName)
    {
        if (string.IsNullOrEmpty(stateName))
        {
            Debug.LogWarning("[InputStateMachine]附加状态为空，忽略");
            return;
        }

        // 校验存在
        if (_stateContainer != null && !_stateContainer.Dict.ContainsKey(stateName))
        {
            Debug.LogWarning($"[InputStateMachine]附加状态不存在：{stateName}");
            return;
        }

        // 已存在则不重复压入
        if (_additiveStates.Contains(stateName)) return;

        _additiveStates.Insert(0, stateName);
        OnAdditiveStateChanged?.Invoke(stateName, true);
        Debug.Log($"[InputStateMachine]附加状态压入：{stateName}");
    }

    /// <summary>
    /// 弹出附加状态
    /// </summary>
    public void PopAdditiveState(string stateName)
    {
        if (string.IsNullOrEmpty(stateName)) return;
        if (_additiveStates.Remove(stateName))
        {
            OnAdditiveStateChanged?.Invoke(stateName, false);
            Debug.Log($"[InputStateMachine]附加状态弹出：{stateName}");
        }
    }

    /// <summary>
    /// 清空所有附加状态
    /// </summary>
    public void ClearAdditiveStates()
    {
        if (_additiveStates.Count == 0) return;

        var copy = new List<string>(_additiveStates);
        _additiveStates.Clear();
        foreach (var s in copy)
            OnAdditiveStateChanged?.Invoke(s, false);
        Debug.Log("[InputStateMachine]已清空所有附加状态");
    }

    /// <summary>
    /// 当前是否有某个附加状态
    /// </summary>
    public bool HasAdditiveState(string stateName)
    {
        return _additiveStates.Contains(stateName);
    }

    /// <summary>
    /// 获取当前所有生效状态（基础 + 附加）
    /// </summary>
    public List<string> GetAllActiveStates()
    {
        var result = new List<string> { CurrentBaseState };
        result.AddRange(_additiveStates);
        return result;
    }

    // ========== 拦截层组合 ==========
    /// <summary>
    /// 获取当前生效的拦截层组合
    /// 基础状态 + 所有附加状态，按各自 MergeMode 合并
    /// 简化策略：收集所有状态启用的拦截层，去重
    /// </summary>
    public List<string> GetActiveInterceptors()
    {
        var result = new List<string>();
        var seen = new HashSet<string>();

        // 基础状态
        if (_stateContainer != null
            && _stateContainer.Dict.TryGetValue(CurrentBaseState, out var baseCfg))
        {
            if (baseCfg.IsEnable && baseCfg.InterceptorList != null)
            {
                foreach (var id in baseCfg.InterceptorList)
                {
                    if (seen.Add(id)) result.Add(id);
                }
            }
        }

        // 附加状态
        foreach (var stateName in _additiveStates)
        {
            if (_stateContainer == null) continue;
            if (!_stateContainer.Dict.TryGetValue(stateName, out var cfg)) continue;
            if (!cfg.IsEnable || cfg.InterceptorList == null) continue;
            foreach (var id in cfg.InterceptorList)
            {
                if (seen.Add(id)) result.Add(id);
            }
        }

        return result;
    }

    // ========== 当前状态的配置 ==========
    public InputStateConfig GetCurrentBaseConfig()
    {
        if (_stateContainer == null) return null;
        _stateContainer.Dict.TryGetValue(CurrentBaseState, out var cfg);
        return cfg;
    }

    public List<InputStateConfig> GetActiveAdditiveConfigs()
    {
        var result = new List<InputStateConfig>();
        if (_stateContainer == null) return result;
        foreach (var s in _additiveStates)
        {
            if (_stateContainer.Dict.TryGetValue(s, out var cfg))
                result.Add(cfg);
        }
        return result;
    }
}