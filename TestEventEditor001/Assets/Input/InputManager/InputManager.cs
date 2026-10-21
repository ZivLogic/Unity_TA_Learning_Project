using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 输入系统主管理器
/// 单键 + 多键组合
/// 顺序：状态 → 上下文 → 状态机/拦截 → 范围 → CD → 输出
/// 组合支持 Down/Hold/Up，Up 优先组合解除，再走单键
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

    // ========== 排序后的配置（组合优先）==========
    private List<InputKeyConfig> _sortedBinds = new List<InputKeyConfig>();

    // ========== 组合快照 ==========
    // 键名字符串 → 按下时间
    private Dictionary<string, float> _heldKeys = new Dictionary<string, float>();

    // ========== 组合激活状态 ==========
    // 正在生效的组合 ActionKey
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
        _sortedBinds.Clear();

        var container = _configMgr.GetBindContainer();
        if (container == null) return;

        foreach (var cfg in container.Items)
        {
            if (cfg == null || !cfg.IsEnable) continue;
            //Debug.Log($"[Bind] {cfg.ActionKey} MouseIndex={cfg.MouseButtonIndex} Key1={cfg.Key1}");

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

            _sortedBinds.Add(cfg);
        }

        _sortedBinds.Sort((a, b) =>
        {
            if (a.IsCombo != b.IsCombo) return b.IsCombo.CompareTo(a.IsCombo);
            return b.KeyCount.CompareTo(a.KeyCount);
        });
        //Debug.Log($"[Input] 键盘绑定数={_keyBindGroup.Count} 鼠标绑定数={_mouseBindGroup.Count}");
    }

    private void RegisterSampleKeys()
    {
        _snapshot.ClearSampleKeys();
        foreach (var key in _keyBindGroup.Keys)
            _snapshot.RegisterSampleKey(key);

        foreach (var cfg in _sortedBinds)
        {
            if (!cfg.IsCombo) continue;
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

        foreach (var cfg in _sortedBinds)
        {
            if (!cfg.IsEnable) continue;

            if (cfg.IsCombo)
            {
                var comboState = GetComboState(cfg);
                if (comboState == InputKeyState.None) continue;

                // 组合还没成立，但状态是 Down/Hold → 检查是否命中
                if (comboState == InputKeyState.Down)
                {
                    if (!CheckCombo(cfg))
                    {
                        _comboActive.Remove(cfg.ActionKey);
                        continue;
                    }

                    // 单键穿透：第三键 Down 那一帧只输出单键
                    if (cfg.AllowPassthrough && IsThirdKeyNewlyDown(cfg))
                    {
                        CheckPassthrough(cfg);
                        return;
                    }
                }

                // Up：组合解除，输出 Up
                if (!ProcessBind(cfg, comboState)) continue;
                WriteOutput(cfg, comboState, ResolveScope(cfg.Scope));
                return;
            }
            else
            {
                var state = GetSingleState(cfg);
                if (state == InputKeyState.None) continue;
                if (!ProcessBind(cfg, state)) continue;
                WriteOutput(cfg, state, ResolveScope(cfg.Scope));
                return;
            }
        }
    }

    // ========== 组合快照 ==========
    private void UpdateComboSnapshot()
    {
        foreach (var cfg in _sortedBinds)
        {
            if (!cfg.IsEnable) continue;
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
        if (!IsInTimeWindow(cfg)) return false;
        return true;
    }

    private bool IsHeld(string key)
    {
        return !string.IsNullOrEmpty(key) && _heldKeys.ContainsKey(key);
    }

    private bool IsInTimeWindow(InputKeyConfig cfg)
    {
        if (cfg.MaxTimeWindow <= 0) return true;
        var keys = new List<string>();
        if (!string.IsNullOrEmpty(cfg.Key1)) keys.Add(cfg.Key1);
        if (!string.IsNullOrEmpty(cfg.Key2)) keys.Add(cfg.Key2);
        if (!string.IsNullOrEmpty(cfg.Key3)) keys.Add(cfg.Key3);
        if (keys.Count < 2) return true;

        float min = float.MaxValue, max = float.MinValue;
        foreach (var k in keys)
        {
            if (!_heldKeys.TryGetValue(k, out var t)) return false;
            min = Mathf.Min(min, t);
            max = Mathf.Max(max, t);
        }
        return (max - min) <= cfg.MaxTimeWindow;
    }

    // ========== 单键状态 ==========
    private InputKeyState GetSingleState(InputKeyConfig cfg)
    {
        // 鼠标
        if (cfg.MouseButtonIndex >= 0)
        {
            return _snapshot.GetMouseState(cfg.MouseButtonIndex);
        }

        // 键盘
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
        foreach (var cfg in _sortedBinds)
        {
            if (!cfg.IsEnable || cfg.IsCombo) continue;
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
        if (container == null)
        {
            Debug.LogWarning("[InputManager]范围配置缓存为空");
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