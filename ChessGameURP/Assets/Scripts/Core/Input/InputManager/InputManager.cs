using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    //单例
    public static InputManager Instance;
    //事件发布/监听模块
    public InputLogic _logic;
    public InputPublish _publish;

    //输入业务
    public InputBusinessLogic _businessLogic;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        //初始化事件解析逻辑
        _logic = new InputLogic();
        _publish = new InputPublish();

        //订阅事件
        SubscribeAllEvents();

        //初始化输入业务
        _businessLogic = new InputBusinessLogic();

        Debug.Log("[InputManager]初始化完成");
    }

    // 场景销毁/模块关闭时 调用注销
    private void OnDestroy()
    {
        //如果已被销毁则直接跳过
        if (GameGlobalState.IsQuitting) return;
        // 注销本系统所有事件订阅
        _logic.UnlistenAllEvent();
        InputBizRouter.Clear();
    }

    // 外部要调用系统方法 转发给内核
    public void SystemInit()
    {
        _logic.Init();
    }

    #region 全局状态
    //当前输入运行模式
    public InputRunMode CurrentRunMode { get; private set; } = InputRunMode.ForbidAll;

    //当前输入上下文
    public InputContext CurrentContext { get; private set; } = InputContext.GameWorld;

    #endregion

    #region 双帧快照 键盘加鼠标
    //原始快照层：帧硬件状态缓存
    private Dictionary<KeyCode, bool> _keySnapshotCurr = new Dictionary<KeyCode, bool>();
    private Dictionary<KeyCode, bool> _keySnapshotPrev = new Dictionary<KeyCode, bool>();
    private bool[] _mouseSnapshotCurr = new bool[3];
    private bool[] _mouseSnapshotPrev = new bool[3];

    //通用轴快照
    public Vector2 MoveAxis { get; private set; }
    public Vector2 LookAxis { get; private set; }
    #endregion
    #region 冷却&长按计时
    //按冷却计时缓存 阈值校验用
    private Dictionary<KeyCode, float> _keyCdTimer = new Dictionary<KeyCode, float>();
    //长按节流计时
    private Dictionary<KeyCode, float> _keyHoldTimer = new Dictionary<KeyCode, float>();
    #endregion
    #region 拦截管道
    //拦截优先级管道队列
    private SortedDictionary<int, IInputInterceptor> _interceptorPipeline = new SortedDictionary<int, IInputInterceptor>();

    //拦截器映射表
    private Dictionary<InterceptorType, Func<IInputInterceptor>> _interceptorCreator = new Dictionary<InterceptorType, Func<IInputInterceptor>>()
{
    {InterceptorType.CutSceneGlobal, () => new GlobalModeInterceptor() },
    {InterceptorType.ContextLimit, () => new ContextLimitInterceptor() },
};

    #endregion
    #region 改建系统临时配置
    //改建系统专用
    //临时修改缓存
    private Dictionary<string, InputKeyConfig> _tempModifyConfig = new Dictionary<string, InputKeyConfig>();

    //是否在监听新按键录制
    public bool IsRecordingKey { get; private set; }

    //当前正在修改的行为Key
    private string _curModifyActionKey;
    #endregion
    #region 加载拦截器
    //加载拦截器
    public void LoadInterceptorDictConfig()
    {
        //清空旧管道
        _interceptorPipeline.Clear();
        foreach (var kv in InputConfigCache.InterceptorDict)
        {
            string interceptorName = kv.Key;
            InputInterceptorConfig itemCfg = kv.Value;
            //配置关闭，直接跳过
            if (!itemCfg.IsEnable)
                continue;
            //字符串转枚举
            if (!Enum.TryParse<InterceptorType>(interceptorName, out var interceptorType))
            {
                Debug.LogWarning($"[InputManager]未知拦截器配置:{interceptorName}");
                continue;
            }
            if (!_interceptorCreator.TryGetValue(interceptorType, out var createFunc))
            {
                Debug.LogWarning($"无对应拦截器映射：{interceptorType}");
                continue;
            }
            //注册拦截层
            IInputInterceptor interceptor = createFunc.Invoke();
            AddInterceptor(itemCfg.Priority, interceptor);
        }
    }

    public void AddInterceptor(int priority, IInputInterceptor interceptor)   //拦截注册器，输入优先级值和拦截器名字
    {
        if (!_interceptorPipeline.ContainsKey(priority))
        {
            _interceptorPipeline.Add(priority, interceptor);
        }
    }

    public void RegisterDefaultInterceptor()      //可以在这里注册默认拦截层逻辑
    {

    }
    #endregion

    // Update is called once per frame
    private void Update()
    {
        //每帧第一时间：刷新原始硬件快照
        RefreshRawSnapshot();
        //全局总锁：全部禁用输入直接返回
        if (CurrentRunMode == InputRunMode.ForbidAll)
            return;
        //改建录制优先
        if (IsRecordingKey)
        {
            CheckRecordingKey();
            return;
        }
        //轮询检测 键盘 + 鼠标
        CheckKeyboardInput();
        CheckMouseAllInput();
    }

    #region  原始快照采集层
    private void RefreshRawSnapshot()
    {
        //键盘
        //帧位移：当前帧 => 上一帧
        _keySnapshotPrev.Clear();
        foreach (var kv in _keySnapshotCurr)
            _keySnapshotPrev[kv.Key] = kv.Value;
        Array.Copy(_mouseSnapshotCurr, _mouseSnapshotPrev, _mouseSnapshotCurr.Length);
        //刷新当前帧键盘
        _keySnapshotCurr.Clear();
        //遍历配置内所有绑定按键刷新状态快照
        foreach (var kv in InputConfigCache.InputBindDict)
        {
            KeyCode code = InputHelper.StringToKeyCode(kv.Value.KeyBindCode);
            if (code != KeyCode.None)
            {
                _keySnapshotCurr[code] = Input.GetKey(code);
            }
        }
        //鼠标按键快照
        _mouseSnapshotCurr[0] = Input.GetMouseButton(0);
        _mouseSnapshotCurr[1] = Input.GetMouseButton(1);
        _mouseSnapshotCurr[2] = Input.GetMouseButton(2);
        //通用轴
        MoveAxis = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        LookAxis = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
    }
    #endregion
    #region 三态判断工具 
    private InputKeyState GetKeyCodeState(KeyCode key)
    {
        bool curr = _keySnapshotCurr.TryGetValue(key, out var c) ? c : false;
        bool prev = _keySnapshotPrev.TryGetValue(key, out var p) ? p : false;
        if (curr && !prev) return InputKeyState.Down;
        if (curr &&  prev) return InputKeyState.Hold;
        if (!curr && prev) return InputKeyState.Up;
        return InputKeyState.None;
    }
    private InputKeyState GetMouseBtnState(int mouseIndex)
    {
        if (mouseIndex < 0 || mouseIndex >= _mouseSnapshotCurr.Length)
            return InputKeyState.None;
        bool curr = _mouseSnapshotCurr[mouseIndex];
        bool prev = _mouseSnapshotPrev[mouseIndex];
        if (curr && !prev)return InputKeyState.Down;
        if (curr &&  prev)return InputKeyState.Hold;
        if (!curr && prev) return InputKeyState.Up;
        return InputKeyState.None;
    }
    #endregion
    #region 配置工具：解析允许监听的状态
    private List<InputKeyState> GetAllowListenStates(InputKeyConfig cfg)
    {
        List<InputKeyState> list = new List<InputKeyState>();
        if (cfg.ListenState == null)
            return list;
        foreach (var s in cfg.ListenState)
        {
            if (Enum.TryParse<InputKeyState>(s, true, out var st))
                list.Add(st);
        }
        return list;
    }
    #endregion
    #region  阈值合法性校验核心
    private bool IsInputCdValid(KeyCode key, float limitCd)
    {
        if (!_keyCdTimer.ContainsKey(key))
        {
            _keyCdTimer[key] = Time.unscaledTime;
            return true;
        }
        //判定连点冷却阈值
        float interval = Time.unscaledTime - _keyCdTimer[key];
        if (interval >= limitCd)
        {
            _keyCdTimer[key] = Time.unscaledTime;
            return true;
        }
        return false;
    }
    #endregion
    #region 长按节流校验
    private bool IsHoldIntervalValid(KeyCode key, float holdInterval)
    {
        if ( ! _keyHoldTimer.ContainsKey(key))
        {
            _keyHoldTimer[key] = Time.unscaledTime;
            return true;
        }
        float interval = Time.unscaledTime - _keyHoldTimer[key];
        if (interval >= holdInterval)
        {
            _keyHoldTimer[key]= Time.unscaledTime;
            return true;
        }
        return false;
    }
    #endregion
    #region  拦截管道统一校验入口
    private bool PassAllInterceptorCheeck(InputAction action)
    {
        foreach (var interceptor in _interceptorPipeline.Values)
        {
            if (!interceptor.IsPassCheeck(action, CurrentContext))
                return false;
        }
        return true;
    }
    #endregion
    #region  直通便捷层

    #endregion
    #region  键盘输入检测，阈值，拦截，发布
    private void CheckKeyboardInput()
    {
        foreach (var kv in InputConfigCache.InputBindDict)
        {
            var cfg = kv.Value;
            if (!cfg.IsEnable || cfg.MouseButtonIndex != -1)
                continue;
            KeyCode bindKey = InputHelper.StringToKeyCode(cfg.KeyBindCode);
            if (bindKey == KeyCode.None)
                continue;
            //获取三态
            InputKeyState keyState = GetKeyCodeState(bindKey);
            if (keyState == InputKeyState.None)
                continue;
            //配置过滤：只分发配置里允许的状态
            var allowStates = GetAllowListenStates(cfg);
            if ( ! allowStates.Contains(keyState) )
                continue;
            //按下抬起 走点击冷却
            if (keyState is InputKeyState.Down or InputKeyState.Up )
            {
                if ( ! IsInputCdValid(bindKey, cfg.ClickCdThreshold))
                    continue;
            }
            //长按 走时间节流
            else if (keyState == InputKeyState.Hold)
            {
                if (!IsHoldIntervalValid(bindKey, cfg.HoldInterval))
                    continue;
            }
            //解析行为枚举
            if ( ! Enum.TryParse<InputAction>(kv.Key, out InputAction action))
                continue;
            //拦截层校验
            if (!PassAllInterceptorCheeck(action))
                continue;
            //路由发到业务层
            InputBizRouter.Dispatch(action, cfg, keyState);
        }
    }
    #endregion
    #region  鼠标输入统一总入口
    private void CheckMouseAllInput()
    {
        //遍历所有输入配置，筛选鼠标绑定的行为
        foreach (var kv in InputConfigCache.InputBindDict)
        {
            string actionName = kv.Key;
            var cfg = kv.Value;
            //未启用 / 不是鼠标绑定，直接跳过
            if (!cfg.IsEnable || cfg.MouseButtonIndex < 0)
                continue;
            int mouseIndex = cfg.MouseButtonIndex;
            //越界保护
            if (mouseIndex < 0 || mouseIndex >= _mouseSnapshotCurr.Length)
                continue;
            //鼠标三态
            InputKeyState mouseState = GetMouseBtnState(mouseIndex);
            if (mouseState == InputKeyState.None)
                continue;
            //配置状态过滤
            var allowStates = GetAllowListenStates(cfg);
            if ( ! allowStates.Contains(mouseState))
                continue;
            //鼠标对应KeyCode
            KeyCode mouseKey = mouseIndex switch
            {
                0 => KeyCode.Mouse0,
                1 => KeyCode.Mouse1,
                2 => KeyCode.Mouse2,
                _ => KeyCode.None
            };
            //抬起按下冷却
            if (mouseState is InputKeyState.Down or InputKeyState.Up)
            {
                if ( ! IsInputCdValid(mouseKey, cfg.ClickCdThreshold))
                    continue;
            }
            //长按节流
            else if (mouseState == InputKeyState.Hold)
            {
                if ( ! IsHoldIntervalValid(mouseKey, cfg.HoldInterval))
                    continue;
            }
            //解析行为枚举
            if (!Enum.TryParse<InputAction>(kv.Key, out InputAction action))
                continue;
            //拦截层校验
            if (!PassAllInterceptorCheeck(action))
                continue;
            //路由层分发业务
            InputBizRouter.Dispatch(action, cfg, mouseState);
        }
    }
    #endregion
    #region  改建系统 | 开始录制 + 按键监听 + 冲突检测 + 保存/重置
    //开始修改某个行为的逻辑
    public void StartRecordKey(string actionKey)
    {
        _curModifyActionKey = actionKey;
        IsRecordingKey = true;
    }
    //取消录制
    public void CancelRecordKey()
    {
        IsRecordingKey = false;
        _curModifyActionKey = string.Empty;
    }
    //录制中监听任意按键
    private void CheckRecordingKey()
    {
        foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(key) && key != KeyCode.None)
            {
                FinishRecordKey(key);
                break;
            }
        }
    }
    //完成按键录制 + 冲突验证
    private void FinishRecordKey(KeyCode newKey)
    {
        IsRecordingKey = false;
        if (!InputConfigCache.InputBindDict.TryGetValue(_curModifyActionKey, out var originCfg))
            return;
        //上下文冲突检测
        bool hasConflict = InputHelper.CheckKeyConflict(newKey, Enum.Parse<InputAction>(_curModifyActionKey), CurrentContext);
        if (hasConflict)
        {
            Debug.LogWarning("当前按键在该上下文已被占用，冲突！");
            return;
        }
        //写入临时缓存（未保存只改临时）
        if (!_tempModifyConfig.ContainsKey(_curModifyActionKey))
            _tempModifyConfig[_curModifyActionKey] = new InputKeyConfig();
        _tempModifyConfig[_curModifyActionKey] = originCfg;
        _tempModifyConfig[_curModifyActionKey].KeyBindCode = InputHelper.KeyCodeToString(newKey);
        //发送按键修改事件（UI刷新显示）

    }
    //保存全部按键 | 写入正式配置 + 发存档事件
    public void SaveAllModifyConfig()
    {
        foreach (var kv in _tempModifyConfig)
        {
            if (InputConfigCache.InputBindDict.ContainsKey(kv.Key))
                InputConfigCache.InputBindDict[kv.Key] = kv.Value;
        }
        _tempModifyConfig.Clear();
        //发送存档事件，外部监听写入本地存档

    }
    //重置临时修改（不点保存恢复原样）
    public void ResetTempModify()
    {
        _tempModifyConfig.Clear();
        IsRecordingKey = false;
    }
    #endregion

    //切换上下文快捷方法
    public void SwitchInputContext(InputContext ctx)
    {
        CurrentContext = ctx;
    }
    //切换全局输入模式快捷方法
    public void SwitchInputMode(InputRunMode mode)
    {
        CurrentRunMode = mode;
    }



    //事件订阅方法
    public void SubscribeAllEvents()
    {
        EventManager.Instance.Listen<GameInit>(OnInitInputConfig);
    }




    private void OnInitInputConfig(PackageEvent e)
    {
        var pack = new Package();
        //pack.Put(EventPackName.INPUT_GETCONFIG, inputCfg);
        //pack.Put(EventPackName.INPUT_SELECTCONFIG, selectCfg);
        //事件
        var pub = new ConfigLogic_OnInputConfig_InitInputConfig { package = pack };
        _publish.ConfigLogic_OnInputConfig_InitInputConfig(pub);
        //EventManager.Instance.EmitLogic(new ConfigLogic_OnInputConfig_InitInputConfig { package = pack });
    
    }

    
    // Start is called before the first frame update
    void Start()
    {
        LoadInterceptorDictConfig();
        _businessLogic.Init();
    }









    //#region  鼠标点击选中检测
    //private void CheckMouseClickInput()
    //{
    //    if (!InputConfigCache.InputBindDict.TryGetValue(nameof(InputAction.SelectTarget), out var selectCfg))
    //        return;
    //    //-1表示不启用鼠标，0为左键，1为右键，2为中键
    //    if (selectCfg.MouseButtonIndex < 0)  
    //        return;
    //    if (Input.GetMouseButton(selectCfg.MouseButtonIndex))
    //    {
    //        //阈值校验
    //        if (!IsInputCdValid(KeyCode.Mouse0, selectCfg.ClickCdThreshold))
    //            return;
    //        Vector3 worldPos = InputHelper.ScreenToWorld(Input.mousePosition);  //可疑
    //        //调用工具层通用选中方法
    //        GameObject selectObj = InputHelper.GetPrioritySelectObject(worldPos, selectCfg.ClickCdThreshold);
    //        //打包数据
    //        //InputEventPackage pkg = new InputEventPackage();
    //        //拦截管道校验
    //        if (PassAllInterceptorCheck(InputAction.SelectTarget))
    //        {
    //            //发布事件

    //        }
    //    }
    //}
    //#endregion
    //初始化配置
    //void OnConfig(IsInputConfig e)
    //{
    //    var pack = e.package;
    //    if (pack.Get<Dictionary<string, InputKeyConfig>>(EventPackName.INPUT_GETCONFIG, out var inputCfg)) { }
    //    if (pack.Get<Dictionary<string, SelectObjectConfig>>(EventPackName.INPUT_SELECTCONFIG, out var selectCfg)) { }
    //    if (pack.Get<Dictionary<string, InputInterceptorConfig>>(EventPackName.INPUT_INTERCEPTORCONFIG, out var interceptorCfg)) { }
    //    if (inputCfg == null || selectCfg == null || interceptorCfg == null)
    //    {
    //        Debug.LogError("[InputManage]输入配置为空");
    //        return;
    //    }
    //    InputConfigCache.InputBindDict = inputCfg;
    //    InputConfigCache.ObjectSelectDict = selectCfg;
    //    InputConfigCache.InterceptorDict = interceptorCfg;
    //    Debug.Log("[InputManager]输入配置初始化完成");
    //}
}
