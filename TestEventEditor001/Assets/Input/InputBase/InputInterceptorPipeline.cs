using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 拦截层接口
/// </summary>
public interface IInputInterceptor
{
    string Id { get; }
    bool IsPassCheck(string action, string context, IReadOnlyList<InputStateConfig> activeStates);
}

/// <summary>
/// 白名单拦截器
/// </summary>
public class WhiteListInterceptor : IInputInterceptor
{
    public string Id { get; private set; }
    private HashSet<string> _allowActions;

    public WhiteListInterceptor(InputInterceptorConfig cfg)
    {
        Id = cfg.Id;
        _allowActions = new HashSet<string>();
        if (cfg.AllowActions != null)
            foreach (var a in cfg.AllowActions)
                _allowActions.Add(a);
    }

    public bool IsPassCheck(string action, string context, IReadOnlyList<InputStateConfig> activeStates)
    {
        return _allowActions.Contains(action);
    }
}

/// <summary>
/// 黑名单拦截器
/// </summary>
public class BlackListInterceptor : IInputInterceptor
{
    public string Id { get; private set; }
    private HashSet<string> _blockActions;

    public BlackListInterceptor(InputInterceptorConfig cfg)
    {
        Id = cfg.Id;
        _blockActions = new HashSet<string>();
        if (cfg.BlockActions != null)
            foreach (var a in cfg.BlockActions)
                _blockActions.Add(a);
    }

    public bool IsPassCheck(string action, string context, IReadOnlyList<InputStateConfig> activeStates)
    {
        return !_blockActions.Contains(action);
    }
}

/// <summary>
/// 拦截层管道
/// 根据当前状态机启用的拦截层组合，构建运行时管道
/// 多个拦截层同时生效时：全部通过才放行（AND 语义）
/// </summary>
public class InputInterceptorPipeline
{
    // 所有已注册的拦截器（Id → 实例）
    private Dictionary<string, IInputInterceptor> _allInterceptors
        = new Dictionary<string, IInputInterceptor>();

    // 当前生效的拦截器列表（按优先级排序）
    private List<IInputInterceptor> _activeInterceptors = new List<IInputInterceptor>();

    private InputInterceptorContainer _container;

    /// <summary>
    /// 初始化：构建所有拦截器实例
    /// </summary>
    public void Init(InputInterceptorContainer container)
    {
        _container = container;
        _allInterceptors.Clear();

        if (container == null)
        {
            Debug.LogWarning("[InputInterceptorPipeline]拦截层配置为空");
            return;
        }

        foreach (var cfg in container.Items)
        {
            if (cfg == null || string.IsNullOrEmpty(cfg.Id)) continue;
            if (!cfg.IsEnable) continue;

            IInputInterceptor interceptor = cfg.InterceptorMode == "BlackList"
                ? new BlackListInterceptor(cfg)
                : (IInputInterceptor)new WhiteListInterceptor(cfg);

            _allInterceptors[cfg.Id] = interceptor;
        }

        Debug.Log($"[InputInterceptorPipeline]已构建 {_allInterceptors.Count} 个拦截器");
    }

    /// <summary>
    /// 根据状态机提供的拦截层ID列表，刷新当前生效管道
    /// </summary>
    public void RefreshActiveInterceptors(List<string> activeIds)
    {
        _activeInterceptors.Clear();
        if (activeIds == null) return;

        // 按优先级排序
        var sorted = new List<(int priority, IInputInterceptor interceptor)>();
        foreach (var id in activeIds)
        {
            if (!_allInterceptors.TryGetValue(id, out var interceptor)) continue;
            int priority = 0;
            if (_container != null && _container.Dict.TryGetValue(id, out var cfg))
                priority = cfg.Priority;
            sorted.Add((priority, interceptor));
        }
        sorted.Sort((a, b) => a.priority.CompareTo(b.priority));

        foreach (var item in sorted)
            _activeInterceptors.Add(item.interceptor);
    }

    /// <summary>
    /// 统一校验入口
    /// 所有生效的拦截层都通过才放行
    /// </summary>
    public bool PassAllCheck(
        string action,
        string context,
        IReadOnlyList<InputStateConfig> activeStates)
    {
        foreach (var interceptor in _activeInterceptors)
        {
            if (!interceptor.IsPassCheck(action, context, activeStates))
            {
                if (InputGlobalRuntime.DebugMode)
                    Debug.Log($"[Interceptor]被 {interceptor.Id} 拦截：{action}");
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 当前生效的拦截器数量（调试用）
    /// </summary>
    public int ActiveCount => _activeInterceptors.Count;
}

/// <summary>
/// 输入系统全局运行时状态（跨模块共享）
/// </summary>
public static class InputGlobalRuntime
{
    public static bool DebugMode = false;
}