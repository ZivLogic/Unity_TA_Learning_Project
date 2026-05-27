using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//ID系统，给每个实体具体挂载ID
public class GlobalIDManager : MonoBehaviour
{
    public static GlobalIDManager Instance {  get; private set; }
    //逻辑ID -> 逻辑实体对象
    private readonly Dictionary<string, LogicIdentity> _logicPool = new();
    //渲染ID -> 渲染实体对象
    private readonly Dictionary<string, RenderIdentity> _renderPool = new();
    //双向映射：逻辑ID <=> 渲染ID
    private readonly Dictionary<string, string> _logicToRender = new();
    private readonly Dictionary<string, string> _renderToLogic = new();
    //自增序列号，保证全局ID唯一
    private int _logicSeed;
    private int _renderSeed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[GlobalIDManager]ID系统初始化完成");
    }

    #region 事件挂载ID
    //事件挂载逻辑ID
    public void EventToAddLogicID(GameObject logicObj)
    {
        LogicIdentity tag = logicObj.GetComponent<LogicIdentity>();
        if (tag == null)
        {
            tag = logicObj.AddComponent<LogicIdentity>();
        }
        tag.LogicID = RegisterLogicEntity(tag);
    }
    //事件挂载渲染ID
    public void EventToAddRenderID(GameObject renderObj)
    {
        RenderIdentity tag = renderObj.GetComponent<RenderIdentity>();
        if (tag == null)
        {
            tag = renderObj.AddComponent<RenderIdentity>();
        }
        tag.RenderID = RegisterRenderEntity(tag);
    }
    //事件关联双ID
    public void EventRenderAndLogic(GameObject renderObj, GameObject logicObj)
    {
        RenderIdentity renderTag = renderObj.GetComponent<RenderIdentity>();
        if (renderTag == null)
        {
            renderTag = renderObj.AddComponent<RenderIdentity>();
        }
        renderTag.RenderID = RegisterRenderEntity(renderTag);
        LogicIdentity logicTag = logicObj.GetComponent<LogicIdentity>();
        if (logicTag == null)
        {
            logicTag = logicObj.AddComponent<LogicIdentity>();
        }
        logicTag.LogicID = RegisterLogicEntity(logicTag);
        //关联
        BindPair(logicTag.LogicID, renderTag.RenderID);
    }
    #endregion
    #region ID生成注册
    //生成并注册逻辑ID
    public string RegisterLogicEntity(LogicIdentity logicEntity)
    {
        if (logicEntity == null)return string.Empty;
        string newID = $"Logic_{_logicSeed++}";
        logicEntity.LogicID = newID;
        _logicPool[newID] = logicEntity;
        return newID;
    }
    //生成并注册渲染ID
    public string RegisterRenderEntity(RenderIdentity renderEntity)
    {
        if (renderEntity == null)return string.Empty;
        string newID = $"Render_{_renderSeed++}";
        renderEntity.RenderID = newID;
        _renderPool[newID] = renderEntity;
        return newID;
    }
    //绑定成对的逻辑ID和渲染ID
    public void BindPair(string logicID, string renderID)
    {
        if (string.IsNullOrEmpty(logicID) || string.IsNullOrEmpty(renderID)) return;
        _logicToRender[logicID] = renderID;
        _renderToLogic[renderID] = logicID;
    }
    #endregion
    #region 有效性校验 & 实体检索
    //校验逻辑ID是否存在有效实体
    public bool LogicIDIsValid(string logicID)
    {
        return _logicPool.ContainsKey(logicID);
    }
    //校验渲染ID是否存在有效实体
    public bool RenderIDIsValid(string renderID)
    {
        return _renderPool.ContainsKey(renderID);
    }
    //根据逻辑ID查找实体
    public LogicIdentity GetLogicEntity(string logicID)
    {
        _logicPool.TryGetValue(logicID, out var entity);
        return entity;
    }
    //根据渲染ID查找实体
    public RenderIdentity GetRenderEntity(string renderID)
    {
        _renderPool.TryGetValue(renderID, out var entity);
        return entity;
    }
    //通过逻辑ID获取渲染ID
    public string GetBindRenderID(string logicID)
    {
        _logicToRender.TryGetValue(logicID, out var rid);
        return rid;
    }
    //通过渲染ID获取逻辑ID
    public string GetBindLogic(string renderID)
    {
        _renderToLogic.TryGetValue(renderID, out var lid);
        return lid;
    }
    #endregion
    #region 注销 & 场景重置
    //注销单个逻辑实体及关联关系
    public void UnregisterLogic(string logicID)
    {
        _logicPool.Remove(logicID);
        if (_logicToRender.TryGetValue(logicID, out var rid))
        {
            _renderToLogic.Remove(rid);
            _logicToRender.Remove(logicID);
        }
    }
    //注销单个渲染实体及关联关系
    public void UnregisterRender(string renderID)
    {
        _renderPool.Remove(renderID);
        if (_renderToLogic.TryGetValue(renderID, out var lid))
        {
            _logicToRender.Remove(lid);
            _renderToLogic.Remove(renderID);
        }
    }
    //切换场景清空全部ID数据
    public void ClearAllIDData()
    {
        _logicPool.Clear();
        _renderPool.Clear();
        _logicToRender.Clear();
        _renderToLogic.Clear();
        _logicSeed = 0;
        _renderSeed = 0;
    }
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
