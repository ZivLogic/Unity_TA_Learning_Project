using System;
using System.Collections.Generic;
using UnityEngine;


// 实体类型
public enum EntityType
{
    Normal,     // 普通3D实体
    UI,         // UI实体
    Map,        // 地图实体
    Audio       // 音频实体
}

// 实体类
public class Entity
{
    public string EntityId;          // 实体ID（来自蓝图）
    public string UUID;              // 唯一标识（自动生成）
    public string Tag;               // Tag
    public EntityType EntityType;    // 实体类型

    public GameObject Root;          // 根节点
    public GameObject LogicObject;   // 逻辑体
    public GameObject ViewObject;    // 视觉体
    public GameObject AudioObject;   // 音频体

    // 改为基类类型
    public EntityBlueprintBase Blueprint;

    // 渲染相关（方便渲染管理器访问）
    public Renderer Renderer;
    public GameObject ModelInstance;   //模型实例引用

    // 音频相关
    public AudioSource AudioSource;                   //主音频源
    public List<AudioSource> AudioSources = new();    //所有音频源

    // 对象池相关
    public bool IsPooled;

    // 逻辑脚本引用
    public List<MonoBehaviour> LogicScripts = new();

    #region ===== 四大维度状态黑板 =====

    // 1. 生命周期
    public EntityLifecycleState Lifecycle { get; set; } = EntityLifecycleState.Alive;

    // 2. 游戏流程
    public GameFlowState GameFlow { get; set; } = GameFlowState.Normal;

    // 3. 动作状态（封装一下，方便触发事件）
    private EntityActionState _action = EntityActionState.Idle;
    public EntityActionState Action
    {
        get => _action;
        set
        {
            if (_action != value)
            {
                var old = _action;
                _action = value;
                OnActionChanged?.Invoke(old, value); // 通知外部（如动画、AI）
            }
        }
    }

    // 4. 叠加标签（Buff/状态依赖）
    private readonly HashSet<string> _stateTags = new HashSet<string>();

    public void AddTag(string tag) => _stateTags.Add(tag);
    public void RemoveTag(string tag) => _stateTags.Remove(tag);
    public bool HasTag(string tag) => _stateTags.Contains(tag);
    public HashSet<string> GetAllTags() => _stateTags;

    // ===== 事件监听 =====
    public event Action<EntityActionState, EntityActionState> OnActionChanged;

    #endregion

    //示例写法，实体根不定义有什么状态，一般不在实体根快捷查找
    #region ===== 快捷判断（供业务层极速查询） =====

    /// <summary>是否存活</summary>
    public bool IsAlive => Lifecycle == EntityLifecycleState.Alive;

    /// <summary>是否能够行动（活着 + 正常流程 + 没被眩晕）</summary>
    public bool CanAct => IsAlive
                          && GameFlow == GameFlowState.Normal
                          && Action != EntityActionState.Stunned
                          && Action != EntityActionState.Hit;
    #endregion
}

