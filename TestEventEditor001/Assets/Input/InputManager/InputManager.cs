using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 输入系统主管理器
/// 单例，负责初始化、主循环、改键入口、意图入口
/// 只负责调度，具体逻辑在各自模块
/// </summary>
public class InputManager : MonoBehaviour
{
    // ========== 单例 ==========
    public static InputManager Instance { get; private set; }

    // ========== 配置 ==========
    private InputConfigManager _configMgr;

    // ========== 运行时模块 ==========
    private InputContextManager _contextMgr;
    private InputModeManager _modeMgr;
    private InputStateMachine _stateMachine;
    private InputInterceptorPipeline _interceptorPipeline;
    private InputSnapshotBuffer _snapshot;
    private InputIntentExecutor _intentExecutor;

    // ========== 绑定缓存 ==========
    // 键盘：KeyCode → 绑定列表
    private Dictionary<KeyCode, List<InputKeyConfig>> _keyBindGroup
        = new Dictionary<KeyCode, List<InputKeyConfig>>();
    // 鼠标：索引 → 绑定列表
    private Dictionary<int, List<InputKeyConfig>> _mouseBindGroup
        = new Dictionary<int, List<InputKeyConfig>>();

    // ======== 标准化输出容器 ==========
    private InputOutputPayload _currentOutput = new InputOutputPayload();

    // ========== CD 计时 ==========
    // 按功能Key记CD（不是按物理键，因为改键后功能才是主体）
    private Dictionary<string, float> _clickCdTimer = new Dictionary<string, float>();
    private Dictionary<string, float> _holdTimer = new Dictionary<string, float>();

    // ========== 改键 ==========
    public bool IsRecordingKey { get; private set; }
    private string _curModifyActionKey;
    private Action<KeyCode> _recordCallback;

    // ========== 外部入口 ==========
    public InputConfigManager ConfigMgr => _configMgr;
    public InputContextManager ContextMgr => _contextMgr;
    public InputModeManager ModeMgr => _modeMgr;
    public InputStateMachine StateMachine => _stateMachine;
    public InputIntentExecutor IntentExecutor => _intentExecutor;

    // ========== 生命周期 ==========
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitAll();

        Debug.Log("[InputManager] 输入系统初始化完成");
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ========== 初始化 ==========
    private void InitAll()
    {
        Debug.Log("[InputManager]开始初始化");

        // 1. 配置
        _configMgr = new InputConfigManager();
        _configMgr.LoadAll();

        var global = _configMgr.GetGlobal();
        InputGlobalRuntime.DebugMode = global?.DebugMode ?? false;

        // 2. 运行时模块
        _contextMgr = new InputContextManager();
        _contextMgr.Init(global?.DefaultContext);

        _modeMgr = new InputModeManager();
        _modeMgr.Init("NormalOperate");

        _stateMachine = new InputStateMachine();
        _stateMachine.Init(_configMgr.GetStateContainer(), global?.DefaultState);

        _interceptorPipeline = new InputInterceptorPipeline();
        _interceptorPipeline.Init(_configMgr.GetInterceptorContainer());

        _snapshot = new InputSnapshotBuffer();

        _intentExecutor = new InputIntentExecutor();
        _intentExecutor.Init(_configMgr, _stateMachine, _contextMgr, _modeMgr);

        // 3. 绑定缓存
        RebuildBindGroups();

        // 4. 注册采样按键
        RegisterSampleKeys();

        // 5. 刷新拦截管道（按初始状态）
        RefreshInterceptorPipeline();

        // 6. 监听状态机变化，自动刷新管道
        _stateMachine.OnBaseStateChanged += (_, __) => RefreshInterceptorPipeline();
        _stateMachine.OnAdditiveStateChanged += (_, __) => RefreshInterceptorPipeline();

        Debug.Log("[InputManager]初始化完成");
    }

    /// <summary>
    /// 重建绑定缓存：配置 → 物理键分组
    /// </summary>
    private void RebuildBindGroups()
    {
        _keyBindGroup.Clear();
        _mouseBindGroup.Clear();

        var container = _configMgr.GetBindContainer();
        if (container == null) return;

        foreach (var cfg in container.Items)
        {
            if (cfg == null || !cfg.IsEnable) continue;

            // 鼠标
            if (cfg.MouseButtonIndex >= 0)
            {
                if (!_mouseBindGroup.ContainsKey(cfg.MouseButtonIndex))
                    _mouseBindGroup[cfg.MouseButtonIndex] = new List<InputKeyConfig>();
                _mouseBindGroup[cfg.MouseButtonIndex].Add(cfg);
                continue;
            }

            // 键盘主键
            var key = InputHelper.StringToKeyCode(cfg.KeyBindCode);
            if (key != KeyCode.None)
            {
                if (!_keyBindGroup.ContainsKey(key))
                    _keyBindGroup[key] = new List<InputKeyConfig>();
                _keyBindGroup[key].Add(cfg);
            }

            // 键盘备用键
            if (!string.IsNullOrEmpty(cfg.AltKeyBindCode))
            {
                var altKey = InputHelper.StringToKeyCode(cfg.AltKeyBindCode);
                if (altKey != KeyCode.None && altKey != key)
                {
                    if (!_keyBindGroup.ContainsKey(altKey))
                        _keyBindGroup[altKey] = new List<InputKeyConfig>();
                    _keyBindGroup[altKey].Add(cfg);
                }
            }
        }
    }

    /// <summary>
    /// 注册需要采样的物理按键
    /// </summary>
    private void RegisterSampleKeys()
    {
        _snapshot.ClearSampleKeys();
        foreach (var key in _keyBindGroup.Keys)
            _snapshot.RegisterSampleKey(key);
    }

    /// <summary>
    /// 刷新拦截管道
    /// </summary>
    private void RefreshInterceptorPipeline()
    {
        var ids = _stateMachine.GetActiveInterceptors();
        _interceptorPipeline.RefreshActiveInterceptors(ids);
    }

    // ========== 主循环 ==========
    private void Update()
    {
        //清理旧输入
        _currentOutput.Clear();

        // 1. 刷新快照
        _snapshot.Refresh();

        // 2. 模式检查
        if (_modeMgr.IsForbidAll()) return;

        // 3. 改键录制优先
        if (IsRecordingKey)
        {
            CheckRecordingKey();
            return;
        }

        // 4. 键盘
        CheckKeyboardInput();

        // 5. 鼠标
        CheckMouseInput();
    }

   
    // ========== 键盘检测 ==========
    private void CheckKeyboardInput()
    {
        foreach (var kv in _keyBindGroup)
        {
            KeyCode key = kv.Key;
            var bindList = kv.Value;

            var state = _snapshot.GetKeyState(key);
            if (state == InputKeyState.None) continue;

            foreach (var cfg in bindList)
            {
                ProcessBind(cfg, state, key, isMouse: false);
            }
        }
    }

    // ========== 鼠标检测 ==========
    private void CheckMouseInput()
    {
        foreach (var kv in _mouseBindGroup)
        {
            int mouseIdx = kv.Key;
            var bindList = kv.Value;

            var state = _snapshot.GetMouseState(mouseIdx);
            if (state == InputKeyState.None) continue;

            KeyCode mouseKey = mouseIdx switch
            {
                0 => KeyCode.Mouse0,
                1 => KeyCode.Mouse1,
                2 => KeyCode.Mouse2,
                _ => KeyCode.None
            };

            foreach (var cfg in bindList)
            {
                ProcessBind(cfg, state, mouseKey, isMouse: true);
            }
        }
    }

    // ========== 单条绑定处理 ==========
    private void ProcessBind(InputKeyConfig cfg, InputKeyState state, KeyCode physicalKey, bool isMouse)
    {
        
        // 1. 状态过滤
        if (!IsStateAllowed(cfg, state)) return;

        
        // 2. 上下文过滤
        if (!_contextMgr.IsAllowed(cfg.AllowContext)) return;

        
        // 3. CD / 长按节流
        if (!CheckCd(cfg, state)) return;

        
        // 4. 功能解析
        if (string.IsNullOrEmpty(cfg.Action)) return;

        
        // 5. 拦截层
        var activeStates = GetActiveStateConfigs();
        if (!_interceptorPipeline.PassAllCheck(cfg.Action, _contextMgr.CurrentContext, activeStates))
            return;

        
        // 6. 范围校验
        var stateCfg = _stateMachine.GetCurrentBaseConfig();
        var areaModelContainer = _configMgr.GetAreaModelContainer();

        var scope = ResolveScope(cfg.Scope);
        if (scope == null)
        {
            Debug.LogWarning($"[InputManager] 范围未找到:{cfg.Scope}");
            return;
        }

        
        if (!SelectScopeValidator.IsAllowed(scope, stateCfg, areaModelContainer))
        {
            if (InputGlobalRuntime.DebugMode)
                Debug.Log($"[InputManager]范围被拦：{cfg.ActionKey} scope={scope?.ScopeType}");
            //Debug.Log("返回空");
            return;
        }

        
        // 7. 输出事件
        WriteOutput(cfg, state, scope);

        
    }

    private SelectScope ResolveScope(string scopeId)
    {
        if (string.IsNullOrEmpty(scopeId))
        {
            Debug.Log("[InputManager]输入范围解析失败，使用默认输入");
            return new SelectScope { ScopeType = SelectScopeType.Single.ToString() };
        }

        var container = _configMgr.GetScopeContainer();
        if (container == null)
        {
            Debug.Log("[InputManager]输入范围配置缓存为空");
            return null;
        }

        var scope = container.Get(scopeId);
        if (scope == null)
        {
            Debug.LogWarning($"[InputManager]未找到范围：{scopeId}");
            return null;
        }
        return scope;
    }

    // ========== 状态过滤 ==========
    private bool IsStateAllowed(InputKeyConfig cfg, InputKeyState state)
    {
        if (cfg.ListenState == null || cfg.ListenState.Count == 0)
        {
            // 默认只响应按下
            return state == InputKeyState.Down;
        }
        string stateName = state.ToString();
        foreach (var s in cfg.ListenState)
        {
            if (string.Equals(s, stateName, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    // ========== CD / 长按 ==========
    private bool CheckCd(InputKeyConfig cfg, InputKeyState state)
    {
        var global = _configMgr.GetGlobal();
        string cdKey = cfg.ActionKey;

        if (state == InputKeyState.Up)
        {
            // Up 不检查 CD，直接放行
            return true;
        }

        if (state == InputKeyState.Down)
        {
            float limit = cfg.ClickCdThreshold > 0
                ? cfg.ClickCdThreshold
                : (global?.DefaultClickCd ?? 0.1f);

            if (_clickCdTimer.TryGetValue(cdKey, out var last))
            {
                if (Time.unscaledTime - last < limit) return false;
            }
            _clickCdTimer[cdKey] = Time.unscaledTime;
            return true;
        }

        if (state == InputKeyState.Hold)
        {
            float interval = cfg.HoldInterval > 0
                ? cfg.HoldInterval
                : (global?.DefaultHoldInterval ?? 0.1f);

            if (_holdTimer.TryGetValue(cdKey, out var last))
            {
                if (Time.unscaledTime - last < interval) return false;
            }
            _holdTimer[cdKey] = Time.unscaledTime;
            return true;
        }

        return false;
    }

    // ========== 当前生效状态配置 ==========
    private IReadOnlyList<InputStateConfig> GetActiveStateConfigs()
    {
        var list = new List<InputStateConfig>();
        var baseCfg = _stateMachine.GetCurrentBaseConfig();
        if (baseCfg != null) list.Add(baseCfg);
        list.AddRange(_stateMachine.GetActiveAdditiveConfigs());
        return list;
    }

    // ========== 标准值输出 ==========
    private void WriteOutput(InputKeyConfig cfg, InputKeyState state, SelectScope scope)
    {
        _currentOutput.Clear();

        _currentOutput.HasOutput = true;
        _currentOutput.ActionKey = cfg.ActionKey;
        _currentOutput.Action = cfg.Action;
        _currentOutput.Context = _contextMgr.CurrentContext;
        _currentOutput.KeyState = state;
        _currentOutput.Device = cfg.Device;
        _currentOutput.Frame = Time.frameCount;
        _currentOutput.Scope = scope;   // ← 在这里设

        var eventContainer = _configMgr.GetEventMapContainer();
        if (eventContainer != null
            && eventContainer.Dict.TryGetValue(cfg.Action, out var map))
        {
            _currentOutput.Param = map.Param;
        }

        if (InputGlobalRuntime.DebugMode)
        {
            Debug.Log($"[InputManager]输出：{cfg.Action} state={state} " +
                      $"scope={SelectScopeTool.Describe(scope)}");
        }

        Debug.Log($"[InputManager]输入输出成功，具体范围：{scope.ScopeId}");
    }

    // ========== 改键入口 ==========
    /// <summary>
    /// 开始录制某个功能的按键
    /// </summary>
    public void StartRecordKey(string actionKey, Action<KeyCode> callback = null)
    {
        if (string.IsNullOrEmpty(actionKey))
        {
            Debug.LogWarning("[InputManager]改键ActionKey为空");
            return;
        }
        _curModifyActionKey = actionKey;
        _recordCallback = callback;
        IsRecordingKey = true;
        Debug.Log($"[InputManager]开始录制：{actionKey}");
    }

    /// <summary>
    /// 取消录制
    /// </summary>
    public void CancelRecordKey()
    {
        IsRecordingKey = false;
        _curModifyActionKey = null;
        _recordCallback = null;
    }

    /// <summary>
    /// 录制中监听按键
    /// </summary>
    private void CheckRecordingKey()
    {
        // 任意键按下（键盘 + 鼠标）
        if (!Input.anyKeyDown) return;

        // 键盘：遍历所有 KeyCode
        foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
        {
            // 排除鼠标键，鼠标单独处理
            if (key >= KeyCode.Mouse0 && key <= KeyCode.Mouse6) continue;
            // 排除摇杆/特殊键（可选）
            if (Input.GetKeyDown(key))
            {
                FinishRecordKey(key);
                return;
            }
        }

        // 鼠标
        for (int i = 0; i < 3; i++)
        {
            if (Input.GetMouseButtonDown(i))
            {
                FinishRecordKey(i switch
                {
                    0 => KeyCode.Mouse0,
                    1 => KeyCode.Mouse1,
                    2 => KeyCode.Mouse2,
                    _ => KeyCode.None
                });
                return;
            }
        }
    }

    private void FinishRecordKey(KeyCode newKey)
    {
        IsRecordingKey = false;

        if (_configMgr.WorkingRoot == null)
        {
            // 没在改键流程里，自动开一个
            _configMgr.BeginModify();
        }

        var working = _configMgr.WorkingRoot;
        if (working?.BindContainer?.Dict.TryGetValue(_curModifyActionKey, out var cfg) == true)
        {
            cfg.KeyBindCode = InputHelper.KeyCodeToString(newKey);
            working.BindContainer.MarkDirty();
        }

        _recordCallback?.Invoke(newKey);
        _recordCallback = null;
        _curModifyActionKey = null;

        Debug.Log($"[InputManager]录制完成：{newKey}");
    }

    /// <summary>
    /// 保存改键
    /// </summary>
    public void SaveModify()
    {
        _configMgr.CommitModify();
        RebuildBindGroups();
        RegisterSampleKeys();
    }

    /// <summary>
    /// 取消改键
    /// </summary>
    public void CancelModify()
    {
        _configMgr.CancelModify();
    }

    // ========== 暴露接口 ==========
    /// <summary>
    /// 外部唯一入口：应用意图
    /// </summary>
    public void ApplyIntent(string intentName)
    {
        _intentExecutor.ApplyIntent(intentName);
    }

    /// <summary>
    /// 获取当前标准输出
    /// 外部逻辑脚本每帧调用
    /// </summary>
    public InputOutputPayload GetStandardOutput()
    {
        return _currentOutput;
    }

    /// <summary>
    /// 清空输出（外部消费后可调，或每帧开始时自动清）
    /// </summary>
    public void ClearOutput()
    {
        _currentOutput.Clear();
    }

    // ========== 配置热重载（可选） ==========
    public void ReloadConfig()
    {
        _configMgr.LoadAll();
        RebuildBindGroups();
        RegisterSampleKeys();
        RefreshInterceptorPipeline();
        Debug.Log("[InputManager]配置已重载");
    }
}