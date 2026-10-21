using System;
using System.Collections.Generic;


/// <summary>
/// 输入系统标准化输出容器
/// 外部逻辑脚本通过 GetStandardOutput() 获取，自己翻译
/// </summary>
[Serializable]
public class InputOutputPayload
{
    // 是否有有效输出
    public bool HasOutput;

    // 功能Key（配置里的 ActionKey）
    public string ActionKey;

    // 功能ID（配置里的 Action）
    public string Action;

    // 上下文
    public string Context;

    // 触发状态
    public InputKeyState KeyState;

    // 设备
    public string Device;

    // 输出帧号
    public int Frame;

    // ===== 范围定义（完整）=====
    public SelectScope Scope;

    public void Clear()
    {
        HasOutput = false;
        ActionKey = null;
        Action = null;
        Context = null;
        KeyState = InputKeyState.None;
        Device = null;
        Frame = 0;
        Scope = null;
    }
}

// =========== 全局配置 ============
[Serializable]
[InputConfig(
    configId: "input_global",
    priority: 50
    )]
public class InputGlobalConfig : InputConfigDefinition
{
    public float DefaultClickCd = 0.1f;                  //默认点击冷却时间
    public float DefaultHoldInterval = 0.1f;             //默认长按触发间隔
    public float GlobalCd = 0f;                          //全局冷却时间
    public string DefaultContext = "GameWorld";          //默认上下文
    public string DefaultState = "Normal";               //默认状态机
    public string DefaultDevice = "KeyboardMouse";       //默认设备类型
    public bool DebugMode = false;                       //调试开关
}


// =========== 单次输入配置 ===========
[Serializable]
public class InputKeyConfig
{
    public string ActionKey;
    public string Action;

    // ===== 显式标记 =====
    public bool IsCombo = false;

    // ===== 键位（1~3 个）=====
    public string Key1;
    public string Key2;
    public string Key3;
    public string AltKey1;
    public string AltKey2;
    public string AltKey3;

    // ===== 鼠标 =====
    public int MouseButtonIndex = -1;

    // ===== 上下文 =====
    public List<string> AllowContext = new List<string>();

    // ===== 状态 =====
    public List<string> ListenState = new List<string>();

    // ===== CD =====
    public float ClickCdThreshold = -1f;
    public float HoldInterval = -1f;

    // ===== 范围 =====
    public string Scope;

    // ===== 组合专用 =====
    public string TriggerMode = "AllDown";
    public float MaxTimeWindow = 0.1f;
    public bool AllowPassthrough = false;

    // ===== 启用 =====
    public bool IsEnable = true;
    public string Device = "KeyboardMouse";
    public string Remark;

    // ===== 工具 =====
    public int KeyCount
    {
        get
        {
            int c = 0;
            if (!string.IsNullOrEmpty(Key1)) c++;
            if (!string.IsNullOrEmpty(Key2)) c++;
            if (!string.IsNullOrEmpty(Key3)) c++;
            return c;
        }
    }
}

// ========== 绑定容器 ==========
[Serializable]
[InputConfig(configId: "input_binds",
    priority: 100
    )]
public class InputBindContainer : InputConfigDefinition
{
    public List<InputKeyConfig> Items = new List<InputKeyConfig>();

    [NonSerialized] private Dictionary<string, InputKeyConfig> _dict;
    public Dictionary<string, InputKeyConfig> Dict
    {
        get
        {
            if (_dict == null)
            {
                _dict = new Dictionary<string, InputKeyConfig>();
                foreach (var item in Items)
                {
                    if (item == null || string.IsNullOrEmpty(item.ActionKey))continue;
                    _dict[item.ActionKey] = item;
                }
            }
            return _dict;
        }
    }
    public void MarkDirty() => _dict = null;     //手动标记字典失效，下次访问Dict时重建
}

// =========== 拦截层配置 =========
[Serializable]
public class InputInterceptorConfig
{
    public string Id;                                         //拦截器唯一标识
    public string InterceptorMode = "WhiteList";              //拦截模式（白名单/黑名单）
    public int Priority;                                      //优先级
    public List<string> AllowActions = new List<string>();    //白名单可通过的动作，此名单之外所有动作被拦截
    public List<string> BlockActions = new List<string>();    //黑名单所拦截的动作，此名单之外所有动作可通过
    public bool IsEnable = true;                              //是否启用此拦截器
    public string Remark;                                     //备注 
}

[Serializable]
[InputConfig(
    configId: "input_interceptors",
    priority: 150
    )]
public class InputInterceptorContainer : InputConfigDefinition
{
    public List<InputInterceptorConfig> Items = new List<InputInterceptorConfig>();

    [NonSerialized] private Dictionary<string, InputInterceptorConfig> _dict;
    public Dictionary<string, InputInterceptorConfig> Dict
    {
        get
        {
            if (_dict == null)
            {
                _dict = new Dictionary<string, InputInterceptorConfig>();
                foreach (var item in Items)
                {
                    if (item == null || string.IsNullOrEmpty(item.Id)) continue;
                    _dict[item.Id] = item;
                }
            }
            return _dict;
        }
    }
    public void MarkDirty() => _dict = null;
}

// ========== 状态机配置 ==========
[Serializable]
public class InputStateConfig
{
    public string StateName;                                   //状态名
    public string StateType = "Base";                          // Base / Additive  状态类型
    public List<string> InterceptorList = new List<string>();  //该状态生效时启用的拦截器
    public string MergeMode = "And";                           // And / Or  拦截器组合的拦截方式
    public bool ClearAdditiveOnBaseSwitch = true;              //切换Base状态时，是否清空当前叠加的Additive状态
    public bool IsEnable = true;                               //是否启用该状态
    public string Remark;                                      //备注

    // ===== 范围限制 =====
    // 允许的范围类型（空 = 不限制）
    public List<string> AllowScopeTypes = new List<string>();
    // 允许的类别（Category 时用，空 = 不限制）
    public List<string> AllowCategories = new List<string>();
    // 允许的数值键（ValueRange 时用，空 = 不限制）
    public List<string> AllowValueKeys = new List<string>();
    // 允许的区域模型ID（Area 时用，空 = 不限制）
    public List<string> AllowAreaModelIds = new List<string>();
}

[Serializable]
[InputConfig(
    configId: "input_states",
    priority: 200)]
public class InputStateContainer : InputConfigDefinition
{
    public List<InputStateConfig> Items = new List<InputStateConfig>();

    [NonSerialized] private Dictionary<string, InputStateConfig> _dict;
    public Dictionary<string, InputStateConfig> Dict
    {
        get
        {
            if (_dict == null)
            {
                _dict = new Dictionary<string, InputStateConfig>();
                foreach (var item in Items)
                {
                    if (item == null || string.IsNullOrEmpty(item.StateName)) continue;
                    _dict[item.StateName] = item;
                }
            }
            return _dict;
        }
    }
    public void MarkDirty() => _dict = null;
}



/// <summary>
/// 可序列化的 Vector3
/// JsonUtility 不支持 Unity 的 Vector3 直接序列化
/// </summary>
[Serializable]
public struct SerializableVector3
{
    public float x;
    public float y;
    public float z;

    public SerializableVector3(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public UnityEngine.Vector3 ToVector3()
    {
        return new UnityEngine.Vector3(x, y, z);
    }

    public static SerializableVector3 FromVector3(UnityEngine.Vector3 v)
    {
        return new SerializableVector3(v.x, v.y, v.z);
    }
}


/// <summary>
/// 选择范围描述
/// 输入系统只负责表达"选什么范围"，不负责选实体
/// 选择范围（扁平化）
/// 所有参数全列出来，按 ScopeType 取需要的
/// </summary>
[Serializable]
public class SelectScope
{
    // 范围ID（唯一标识）
    public string ScopeId;

    // 范围类型
    public string ScopeType;

    // ===== Single =====
    public string SingleEntityId;

    // ===== Self =====
    // 无参数

    // ===== Category =====
    // 单个类别（不是列表）
    public string CategoryTag;
    // 匹配方式：Any / All（单个类别时意义不大，保留兼容）
    public string CategoryMatchMode = "Any";

    // ===== ValueRange =====
    public string ValueKey;
    public float MinValue = float.MinValue;
    public float MaxValue = float.MaxValue;

    // ===== Area =====
    public string AreaModelId;
    public float Radius = 0f;
    public float Height = 0f;
    public float Length = 0f;
    public float Width = 0f;
    public float Angle = 0f;
    public SerializableVector3 AreaCenter;

    // ===== 通用 =====
    public bool IncludeSelf = true;

    public string Remark;

    // ← 运行时转 enum
    public SelectScopeType GetScopeType()
    {
        if (string.IsNullOrEmpty(ScopeType))
            return SelectScopeType.Single;

        if (System.Enum.TryParse<SelectScopeType>(ScopeType, true, out var t))
            return t;

        return SelectScopeType.Single;
    }

    // ← 设置时同步字符串
    public void SetScopeType(SelectScopeType type)
    {
        ScopeType = type.ToString();
    }
}

[Serializable]
[InputConfig(configId: "input_scopes", priority: 125)]
public class InputScopeContainer : InputConfigDefinition
{
    public List<SelectScope> Items = new List<SelectScope>();

    [NonSerialized] private Dictionary<string, SelectScope> _dict;

    public Dictionary<string, SelectScope> Dict
    {
        get
        {
            if (_dict == null)
            {
                _dict = new Dictionary<string, SelectScope>();
                foreach (var item in Items)
                {
                    if (item == null || string.IsNullOrEmpty(item.ScopeId)) continue;
                    _dict[item.ScopeId] = item;
                }
            }
            return _dict;
        }
    }

    public void MarkDirty() => _dict = null;

    public SelectScope Get(string scopeId)
    {
        if (string.IsNullOrEmpty(scopeId)) return null;
        return Dict.TryGetValue(scopeId, out var s) ? s : null;
    }
}


/// <summary>
/// 输入区域模型
/// 输入系统只定义"有哪些区域模型"，不定义具体区域
/// 具体区域由外部实现
/// </summary>
[Serializable]
public class InputAreaModel
{
    // 模型ID（唯一）
    public string ModelId;

    // 区域形状：Sphere / Box / Cylinder / Cone / Polygon
    public string AreaShape = "Sphere";

    // 参数结构描述（给外部解释用，如 "radius" / "sizeX,sizeY,sizeZ"）
    public string ParamSchema;

    // 描述
    public string Remark;
}

/// <summary>
/// 区域模型容器
/// </summary>
[Serializable]
[InputConfig(configId: "input_area_models", priority: 55)]
public class InputAreaModelContainer : InputConfigDefinition
{
    public List<InputAreaModel> Items = new List<InputAreaModel>();

    [NonSerialized] private Dictionary<string, InputAreaModel> _dict;

    public Dictionary<string, InputAreaModel> Dict
    {
        get
        {
            if (_dict == null)
            {
                _dict = new Dictionary<string, InputAreaModel>();
                foreach (var item in Items)
                {
                    if (item == null || string.IsNullOrEmpty(item.ModelId)) continue;
                    _dict[item.ModelId] = item;
                }
            }
            return _dict;
        }
    }

    public void MarkDirty() => _dict = null;
}


/// <summary>
/// 输入意图：外部想使用输入系统的某个功能
/// 一个意图对应一组状态变更
/// </summary>
[Serializable]
public class InputIntentConfig
{
    // 意图ID（唯一）
    public string IntentId;

    // 意图名（给外部调用的字符串，也可以直接用 IntentId）
    public string IntentName;

    // ===== 要变更的上下文 =====
    // 空 = 不改
    public string TargetContext;

    // ===== 要变更的全局模式 =====
    // 空 = 不改
    public string TargetRunMode;

    // ===== 要变更的基础状态 =====
    // 空 = 不改
    public string TargetBaseState;

    // ===== 要压入的附加状态 =====
    public List<string> PushAdditiveStates = new List<string>();

    // ===== 要弹出的附加状态 =====
    public List<string> PopAdditiveStates = new List<string>();

    // ===== 清空所有附加状态 =====
    public bool ClearAllAdditive = false;

    // ===== 执行顺序 =====
    // 同一个意图内多个变更的执行顺序
    public int Order = 0;

    // ===== 是否启用 =====
    public bool IsEnable = true;

    // ===== 描述 =====
    public string Remark;
}

[Serializable]
[InputConfig(
    configId: "input_intents",
    priority: 300)]
public class InputIntentContainer : InputConfigDefinition
{
    public List<InputIntentConfig> Items = new List<InputIntentConfig>();

    [NonSerialized] private Dictionary<string, List<InputIntentConfig>> _dict;

    /// <summary>
    /// IntentName → 该意图下所有变更（按 Order 排序）
    /// </summary>
    public Dictionary<string, List<InputIntentConfig>> Dict
    {
        get
        {
            if (_dict == null)
            {
                _dict = new Dictionary<string, List<InputIntentConfig>>();
                foreach (var item in Items)
                {
                    if (item == null || string.IsNullOrEmpty(item.IntentName)) continue;
                    if (!_dict.ContainsKey(item.IntentName))
                        _dict[item.IntentName] = new List<InputIntentConfig>();
                    _dict[item.IntentName].Add(item);
                }
                // 每个意图内按 Order 排序
                foreach (var kv in _dict)
                    kv.Value.Sort((a, b) => a.Order.CompareTo(b.Order));
            }
            return _dict;
        }
    }

    public void MarkDirty() => _dict = null;
}




//// ========== 事件映射配置 ==========
//[Serializable]
//public class InputEventMapConfig
//{
//    public string OutputId;             //输出事件的唯一标识
//    public string EventName;            //派发出去的事件名
//    public string Param;                //事件附带的参数字符串
//    public bool CanBeBlocked = true;    //该事件是否可被拦截器拦截
//    public string Remark;               //备注
//}

//[Serializable]
//[InputConfig(
//    configId: "input_events",
//    priority: 250)]
//public class InputEventMapContainer : InputConfigDefinition
//{
//    public List<InputEventMapConfig> Items = new List<InputEventMapConfig>();

//    [NonSerialized] private Dictionary<string, InputEventMapConfig> _dict;
//    public Dictionary<string, InputEventMapConfig> Dict
//    {
//        get
//        {
//            if (_dict == null)
//            {
//                _dict = new Dictionary<string, InputEventMapConfig>();
//                foreach (var item in Items)
//                {
//                    if (item == null || string.IsNullOrEmpty(item.OutputId)) continue;
//                    _dict[item.OutputId] = item;
//                }
//            }
//            return _dict;
//        }
//    }
//    public void MarkDirty() => _dict = null;
//}