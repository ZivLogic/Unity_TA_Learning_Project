using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 输入系统主管理器
/// 单键 + 多键组合
/// 顺序：组合优先（独立表） → 单键（独立表）
/// 新增：组合释放缓冲窗口（防状态残留）
/// </summary>
public class InputManager : MonoBehaviour
{
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
    private Dictionary<KeyCode, List<InputKeyConfig>> _keyBindGroup
        = new Dictionary<KeyCode, List<InputKeyConfig>>();
    private Dictionary<int, List<InputKeyConfig>> _mouseBindGroup
        = new Dictionary<int, List<InputKeyConfig>>();

    // ========== 拆分后的表 ==========
    private List<InputKeyConfig> _comboBinds = new List<InputKeyConfig>();
    private List<InputKeyConfig> _singleBinds = new List<InputKeyConfig>();

    // ========== 组合快照 ==========
    private Dictionary<string, float> _heldKeys = new Dictionary<string, float>();

    // ========== 组合激活状态 ==========
    private HashSet<string> _comboActive = new HashSet<string>();

    // ========== 标准化输出 ==========
    private InputOutputPayload _currentOutput = new InputOutputPayload();

    // ========== CD 计时 ==========
    private Dictionary<string, float> _clickCdTimer = new Dictionary<string, float>();
    private Dictionary<string, float> _holdTimer = new Dictionary<string, float>();

    // ========== 改键 ==========
    public bool IsRecordingKey { get; private set; }
    private string _curModifyActionKey;
    private Action<KeyCode> _recordCallback;

    // ========== 【新增】组合释放缓冲 ==========
    // 记录刚释放的组合键结束时间（用于给单键 Up 让路）
    private Dictionary<string, float> _comboReleaseTimers = new Dictionary<string, float>();
    private const float RELEASE_BUFFER_TIME = 0.05f; // 50ms，约 2~3 帧的安全窗口


    // ========== 外部入口 ==========
    public InputConfigManager ConfigMgr => _configMgr;
    public InputContextManager ContextMgr => _contextMgr;
    public InputModeManager ModeMgr => _modeMgr;
    public InputStateMachine StateMachine => _stateMachine;
    public InputIntentExecutor IntentExecutor => _intentExecutor;

    // ========== 生命周期 ==========
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
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

        _configMgr = new InputConfigManager();
        _configMgr.LoadAll();

        var global = _configMgr.GetGlobal();
        InputGlobalRuntime.DebugMode = global?.DebugMode ?? false;

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

        RebuildBindGroups();
        RegisterSampleKeys();
        RefreshInterceptorPipeline();

        _stateMachine.OnBaseStateChanged += (_, __) => RefreshInterceptorPipeline();
        _stateMachine.OnAdditiveStateChanged += (_, __) => RefreshInterceptorPipeline();

        Debug.Log("[InputManager]初始化完成");
    }

    // ========== 绑定缓存 ==========
    private void RebuildBindGroups()
    {
        _keyBindGroup.Clear();
        _mouseBindGroup.Clear();
        _comboBinds.Clear();
        _singleBinds.Clear();

        var container = _configMgr.GetBindContainer();
        if (container == null) return;

        foreach (var cfg in container.Items)
        {
            if (cfg == null || !cfg.IsEnable) continue;

            if (cfg.MouseButtonIndex >= 0)
            {
                if (!_mouseBindGroup.ContainsKey(cfg.MouseButtonIndex))
                    _mouseBindGroup[cfg.MouseButtonIndex] = new List<InputKeyConfig>();
                _mouseBindGroup[cfg.MouseButtonIndex].Add(cfg);
            }
            else
            {
                var key1 = InputHelper.GetPrimaryKey(cfg);
                var keyCode1 = InputHelper.StringToKeyCode(key1);
                if (keyCode1 != KeyCode.None)
                {
                    if (!_keyBindGroup.ContainsKey(keyCode1))
                        _keyBindGroup[keyCode1] = new List<InputKeyConfig>();
                    _keyBindGroup[keyCode1].Add(cfg);
                }

                var alt1 = InputHelper.GetPrimaryAltKey(cfg);
                if (!string.IsNullOrEmpty(alt1))
                {
                    var altCode = InputHelper.StringToKeyCode(alt1);
                    if (altCode != KeyCode.None && altCode != keyCode1)
                    {
                        if (!_keyBindGroup.ContainsKey(altCode))
                            _keyBindGroup[altCode] = new List<InputKeyConfig>();
                        _keyBindGroup[altCode].Add(cfg);
                    }
                }
            }

            if (cfg.IsCombo)
                _comboBinds.Add(cfg);
            else
                _singleBinds.Add(cfg);
        }

        _comboBinds.Sort((a, b) => b.KeyCount.CompareTo(a.KeyCount));
    }

    private void RegisterSampleKeys()
    {
        _snapshot.ClearSampleKeys();

        foreach (var key in _keyBindGroup.Keys)
            _snapshot.RegisterSampleKey(key);

        foreach (var cfg in _comboBinds)
        {
            RegisterKey(cfg.Key2);
            RegisterKey(cfg.Key3);
            RegisterKey(cfg.AltKey2);
            RegisterKey(cfg.AltKey3);
        }
    }

    private void RegisterKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        var code = InputHelper.StringToKeyCode(key);
        if (code != KeyCode.None)
            _snapshot.RegisterSampleKey(code);
    }

    private void RefreshInterceptorPipeline()
    {
        var ids = _stateMachine.GetActiveInterceptors();
        _interceptorPipeline.RefreshActiveInterceptors(ids);
    }

    // ========== 主循环 ==========
    private void Update()
    {
        _currentOutput.Clear();
        _snapshot.Refresh();

        if (_modeMgr.IsForbidAll()) return;

        if (IsRecordingKey)
        {
            CheckRecordingKey();
            return;
        }

        UpdateComboSnapshot();
        CleanupReleaseTimers();

        // ==========================================
        // 第一梯队：组合表
        // ==========================================
        foreach (var cfg in _comboBinds)
        {
            var comboState = GetComboState(cfg);
            if (comboState == InputKeyState.None) continue;

            if (comboState == InputKeyState.Down)
            {
                if (!CheckCombo(cfg))
                {
                    _comboActive.Remove(cfg.ActionKey);
                    continue;
                }

                if (cfg.AllowPassthrough && IsThirdKeyNewlyDown(cfg))
                {
                    CheckPassthrough(cfg);
                    return;
                }
            }

            // 【核心修改1】组合键释放，记录安全窗口时间
            if (comboState == InputKeyState.Up)
            {
                _comboReleaseTimers[cfg.ActionKey] = Time.unscaledTime;
            }

            if (!ProcessBind(cfg, comboState)) continue;

            WriteOutput(cfg, comboState, ResolveScope(cfg.Scope));

            // 【核心修改2】如果是 Up，让它输出完后，继续往下走去处理单键的 Up。
            // 如果是 Down/Hold，依然 return 拦截单键。
            if (comboState != InputKeyState.Up)
            {
                return;
            }
        }

        // ==========================================
        // 第二梯队：单键表
        // ==========================================
        foreach (var cfg in _singleBinds)
        {
            var state = GetSingleState(cfg);
            if (state == InputKeyState.None) continue;

            bool isReleaseBufferActive = false;
            if (state == InputKeyState.Up && _comboReleaseTimers.Count > 0)
            {
                isReleaseBufferActive = true;
            }

            if (!ProcessBind(cfg, state)) continue;

            WriteOutput(cfg, state, ResolveScope(cfg.Scope));

            // 【核心修改3】如果在安全窗口内，且是 Up 状态，不 return，让同帧的其他单键也能输出 Up
            if (isReleaseBufferActive && state == InputKeyState.Up)
            {
                continue;
            }

            return;
        }
    }

    // 清理过期的安全窗口
    private void CleanupReleaseTimers()
    {
        if (_comboReleaseTimers.Count == 0) return;

        List<string> expiredKeys = null;
        foreach (var kv in _comboReleaseTimers)
        {
            if (Time.unscaledTime - kv.Value > RELEASE_BUFFER_TIME)
            {
                if (expiredKeys == null) expiredKeys = new List<string>();
                expiredKeys.Add(kv.Key);
            }
        }

        if (expiredKeys != null)
        {
            foreach (var key in expiredKeys)
                _comboReleaseTimers.Remove(key);
        }
    }

    // ========== 组合快照 ==========
    private void UpdateComboSnapshot()
    {
        foreach (var cfg in _comboBinds)
        {
            UpdateHeldKey(cfg.Key1);
            UpdateHeldKey(cfg.Key2);
            UpdateHeldKey(cfg.Key3);
            UpdateHeldKey(cfg.AltKey1);
            UpdateHeldKey(cfg.AltKey2);
            UpdateHeldKey(cfg.AltKey3);
        }
    }

    private void UpdateHeldKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        var code = InputHelper.StringToKeyCode(key);
        if (code == KeyCode.None) return;

        var state = _snapshot.GetKeyState(code);
        if (state == InputKeyState.Down || state == InputKeyState.Hold)
        {
            if (!_heldKeys.ContainsKey(key))
                _heldKeys[key] = Time.unscaledTime;
        }
        else if (state == InputKeyState.Up)
        {
            _heldKeys.Remove(key);
        }
    }

    // ========== 组合状态 ==========
    private InputKeyState GetComboState(InputKeyConfig cfg)
    {
        var keys = new List<string>();
        if (!string.IsNullOrEmpty(cfg.Key1)) keys.Add(cfg.Key1);
        if (!string.IsNullOrEmpty(cfg.Key2)) keys.Add(cfg.Key2);
        if (!string.IsNullOrEmpty(cfg.Key3)) keys.Add(cfg.Key3);

        bool allHeld = true;
        bool anyUp = false;

        foreach (var key in keys)
        {
            var code = InputHelper.StringToKeyCode(key);
            if (code == KeyCode.None) continue;
            var state = _snapshot.GetKeyState(code);

            if (state == InputKeyState.Up) anyUp = true;
            if (state != InputKeyState.Down && state != InputKeyState.Hold)
                allHeld = false;
        }

        if (allHeld)
        {
            if (!_comboActive.Contains(cfg.ActionKey))
            {
                _comboActive.Add(cfg.ActionKey);
                return InputKeyState.Down;
            }
            return InputKeyState.Hold;
        }

        if (anyUp && _comboActive.Contains(cfg.ActionKey))
        {
            _comboActive.Remove(cfg.ActionKey);
            return InputKeyState.Up;
        }

        return InputKeyState.None;
    }

    private bool CheckCombo(InputKeyConfig cfg)
    {
        if (!IsHeld(cfg.Key1)) return false;
        if (!string.IsNullOrEmpty(cfg.Key2) && !IsHeld(cfg.Key2)) return false;
        if (!string.IsNullOrEmpty(cfg.Key3) && !IsHeld(cfg.Key3)) return false;
        return true;
    }

    private bool IsHeld(string key)
    {
        return !string.IsNullOrEmpty(key) && _heldKeys.ContainsKey(key);
    }

    // ========== 单键状态 ==========
    private InputKeyState GetSingleState(InputKeyConfig cfg)
    {
        if (cfg.MouseButtonIndex >= 0)
            return _snapshot.GetMouseState(cfg.MouseButtonIndex);

        var key = InputHelper.GetPrimaryKey(cfg);
        var code = InputHelper.StringToKeyCode(key);
        if (code == KeyCode.None) return InputKeyState.None;
        return _snapshot.GetKeyState(code);
    }

    // ========== 单键穿透 ==========
    private bool IsThirdKeyNewlyDown(InputKeyConfig cfg)
    {
        if (string.IsNullOrEmpty(cfg.Key3)) return false;
        var code = InputHelper.StringToKeyCode(cfg.Key3);
        if (code == KeyCode.None) return false;
        return _snapshot.GetKeyState(code) == InputKeyState.Down;
    }

    private void CheckPassthrough(InputKeyConfig combo)
    {
        foreach (var cfg in _singleBinds)
        {
            if (cfg.Key1 == combo.Key1 || cfg.Key1 == combo.Key2 || cfg.Key1 == combo.Key3) continue;

            var state = GetSingleState(cfg);
            if (state == InputKeyState.None) continue;
            if (!ProcessBind(cfg, state)) continue;
            WriteOutput(cfg, state, ResolveScope(cfg.Scope));
            return;
        }
    }

    // ========== 统一校验 ==========
    private bool ProcessBind(InputKeyConfig cfg, InputKeyState state)
    {
        if (!IsStateAllowed(cfg, state)) return false;
        if (!_contextMgr.IsAllowed(cfg.AllowContext)) return false;

        var activeStates = GetActiveStateConfigs();
        if (!_interceptorPipeline.PassAllCheck(cfg.Action, _contextMgr.CurrentContext, activeStates))
            return false;

        var stateCfg = _stateMachine.GetCurrentBaseConfig();
        var scope = ResolveScope(cfg.Scope);
        if (scope == null) return false;

        _currentOutput.Device = cfg.MouseButtonIndex >= 0 ? "Mouse" : cfg.Device;

        if (!SelectScopeValidator.IsAllowed(scope, stateCfg, _configMgr.GetAreaModelContainer()))
            return false;

        if (!CheckCd(cfg, state)) return false;

        return true;
    }

    private SelectScope ResolveScope(string scopeId)
    {
        if (string.IsNullOrEmpty(scopeId))
        {
            Debug.LogWarning("[InputManager]输入范围解析失败，使用默认Single");
            return new SelectScope { ScopeType = SelectScopeType.Single.ToString() };
        }

        var container = _configMgr.GetScopeContainer();
        if (container == null) return null;

        var scope = container.Get(scopeId);
        if (scope == null)
        {
            Debug.LogWarning($"[InputManager]未找到范围：{scopeId}");
            return null;
        }
        return scope;
    }

    private bool IsStateAllowed(InputKeyConfig cfg, InputKeyState state)
    {
        if (cfg.ListenState == null || cfg.ListenState.Count == 0)
            return state == InputKeyState.Down;

        string stateName = state.ToString();
        foreach (var s in cfg.ListenState)
        {
            if (string.Equals(s, stateName, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private bool CheckCd(InputKeyConfig cfg, InputKeyState state)
    {
        if (state == InputKeyState.Up) return true;

        var global = _configMgr.GetGlobal();
        string cdKey = cfg.ActionKey;

        if (state == InputKeyState.Down)
        {
            float limit = cfg.ClickCdThreshold > 0
                ? cfg.ClickCdThreshold
                : (global?.DefaultClickCd ?? 0.1f);

            if (_clickCdTimer.TryGetValue(cdKey, out var last))
                if (Time.unscaledTime - last < limit) return false;

            _clickCdTimer[cdKey] = Time.unscaledTime;
            return true;
        }

        if (state == InputKeyState.Hold)
        {
            float interval = cfg.HoldInterval > 0
                ? cfg.HoldInterval
                : (global?.DefaultHoldInterval ?? 0.1f);

            if (_holdTimer.TryGetValue(cdKey, out var last))
                if (Time.unscaledTime - last < interval) return false;

            _holdTimer[cdKey] = Time.unscaledTime;
            return true;
        }

        return false;
    }

    private IReadOnlyList<InputStateConfig> GetActiveStateConfigs()
    {
        var list = new List<InputStateConfig>();
        var baseCfg = _stateMachine.GetCurrentBaseConfig();
        if (baseCfg != null) list.Add(baseCfg);
        list.AddRange(_stateMachine.GetActiveAdditiveConfigs());
        return list;
    }

    // ========== 输出 ==========
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
        _currentOutput.Scope = scope;

        if (InputGlobalRuntime.DebugMode)
        {
            Debug.Log($"[InputManager]输出：{cfg.Action} state={state} " +
                      $"scope={SelectScopeTool.Describe(scope)}");
        }
    }

    // ========== 改键 ==========
    public void StartRecordKey(string actionKey, Action<KeyCode> callback = null)
    {
        if (string.IsNullOrEmpty(actionKey)) return;
        _curModifyActionKey = actionKey;
        _recordCallback = callback;
        IsRecordingKey = true;
        Debug.Log($"[InputManager]开始录制：{actionKey}");
    }

    public void CancelRecordKey()
    {
        IsRecordingKey = false;
        _curModifyActionKey = null;
        _recordCallback = null;
    }

    private void CheckRecordingKey()
    {
        if (!Input.anyKeyDown) return;

        foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
        {
            if (key >= KeyCode.Mouse0 && key <= KeyCode.Mouse6) continue;
            if (Input.GetKeyDown(key))
            {
                FinishRecordKey(key);
                return;
            }
        }

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
            _configMgr.BeginModify();

        var working = _configMgr.WorkingRoot;
        if (working?.BindContainer?.Dict.TryGetValue(_curModifyActionKey, out var cfg) == true)
        {
            cfg.Key1 = InputHelper.KeyCodeToString(newKey);
            working.BindContainer.MarkDirty();
        }

        _recordCallback?.Invoke(newKey);
        _recordCallback = null;
        _curModifyActionKey = null;

        Debug.Log($"[InputManager]录制完成：{newKey}");
    }

    public void SaveModify()
    {
        _configMgr.CommitModify();
        RebuildBindGroups();
        RegisterSampleKeys();
    }

    public void CancelModify()
    {
        _configMgr.CancelModify();
    }

    // ========== 暴露接口 ==========
    public void ApplyIntent(string intentName)
    {
        _intentExecutor.ApplyIntent(intentName);
    }

    public InputOutputPayload GetStandardOutput()
    {
        return _currentOutput;
    }

    // ========== 暴露接口 ==========
    /// <summary>
    /// 查询指定动作当前是否处于按住（按下/持续）状态
    /// </summary>
    public bool IsActionHeld(string actionKey)
    {
        if (string.IsNullOrEmpty(actionKey)) return false;

        // 1. 先从单键表找
        foreach (var cfg in _singleBinds)
        {
            if (cfg.ActionKey == actionKey)
            {
                var state = GetSingleState(cfg);
                return state == InputKeyState.Down || state == InputKeyState.Hold;
            }
        }

        // 2. 如果是组合键，检查所有绑定的物理键是否按住
        foreach (var cfg in _comboBinds)
        {
            if (cfg.ActionKey == actionKey)
            {
                // 只要组合里的所有键都按着，就算处于按下状态
                return CheckCombo(cfg);
            }
        }

        return false;
    }

    /// <summary>
    /// 根据物理按键字符串（如 "W"）查询是否按住
    /// </summary>
    public bool IsKeyHeldByString(string keyStr)
    {
        if (string.IsNullOrEmpty(keyStr)) return false;
        var code = InputHelper.StringToKeyCode(keyStr);
        if (code == KeyCode.None) return false;

        // 直接向底层的双帧快照要当前状态
        var state = _snapshot.GetKeyState(code);
        return state == InputKeyState.Down || state == InputKeyState.Hold;
    }

    public void ClearOutput()
    {
        _currentOutput.Clear();
    }

    public void ReloadConfig()
    {
        _configMgr.LoadAll();
        RebuildBindGroups();
        RegisterSampleKeys();
        RefreshInterceptorPipeline();
        Debug.Log("[InputManager]配置已重载");
    }
}