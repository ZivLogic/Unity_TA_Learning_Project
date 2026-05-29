using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderFactoryManager : MonoBehaviour, IFactory
{
    public string FactoryName => "Render";

    public static Dictionary<string, RenderMajorIDConfig> _renderMajorDict = new Dictionary<string, RenderMajorIDConfig>();
    public static Dictionary<string, RenderMinorIDConfig> _renderMinorDict = new Dictionary<string, RenderMinorIDConfig>();

    public static Dictionary<string, string> _shderConfigDict = new();

    private void Awake()
    {
        //注册自己到中央工厂
        FactoryManager.Instance.RegisterFactory(this);
        //发送事件
        InitShaderConfig();

        EventManager.Instance.Listen<EntityRender_TestEvent>(TestEvent);
        EventManager.Instance.Listen<GetChessShader>(TestLoadShader);
        //先验证逻辑硬编码
        EventManager.Instance.Listen<OnShaderCfg>(OnShaderCfg);
    }

    public void Initialize()
    {
        RenderIdentityRegister.InitRegister();
        _renderMajorDict.Clear();
        _renderMinorDict.Clear();
        
    }

    public void OnRenderIDConfig(Dictionary<string, RenderMajorIDConfig> maj, Dictionary<string, RenderMinorIDConfig> min)
    {
        _renderMajorDict = maj;
        _renderMinorDict = min;
        Debug.Log("[RenderFactoryManager]初始化配置完成");
    }

    //业务方法
    
    public void CreatePrefab(string modelKey, GameObject model, GameObject parent)
    {
        if ( ! RenderIdentityRegister.TryGetIdentity(modelKey, out var renderMajorType, out var renderMinorType))
        {
            Debug.LogError($"[RenderFactoryManager]未注册渲染Key:{modelKey}");
            return;
        }
        GameObject go = RenderSpawnUtil.SpawnModelBindToParent(model, parent);
        if ( go == null ) return;
        RenderMajorTag majorTag = RenderSpawnUtil.AddRenderComponent<RenderMajorTag>(go);
        majorTag.majorType = renderMajorType;
        RenderMinorTag minorTag = RenderSpawnUtil.AddRenderComponent<RenderMinorTag>(go);
        minorTag.minorType = renderMinorType;
        if (_renderMajorDict.TryGetValue(renderMajorType.ToString(), out var majorConfig))
        {
            foreach (string compName in majorConfig.ComponentID)
            {
                RenderSpawnUtil.AddComponentByName(go, compName);
            }
        }
        if (_renderMinorDict.TryGetValue(renderMinorType.ToString(), out var minorConfig))
        {
            foreach (string compName in minorConfig.ComponentID)
            {
                RenderSpawnUtil.AddComponentByName(go, compName);
            }
        }
        //加载材质
        if (_shderConfigDict.TryGetValue(modelKey, out var path))
        {
            //Material mat = AssetsManager.Instance.Load<Material>(path);
            //RenderSpawnUtil.SwapObjectMaterial(go, mat);
            var pack = new Package();
            pack.Put("1", path);
            pack.Put("2", go);
            var pub = new LoadShader { package = pack };
            EventManager.Instance.EmitInput(pub);
        }
    }

    private void TestEvent(PackageEvent e)
    {
        EntityRender_TestEvent evt = e as EntityRender_TestEvent;
        var pack = evt.package;
        if (pack.Get<GameObject>("1", out var model)) { }
        if (pack.Get<GameObject>("2", out var parent)) { }
        if (pack.Get<string>("3", out var name)) { }
        CreatePrefab(name, model, parent);
    }

    private void TestLoadShader(PackageEvent e)
    {
        GetChessShader evt = e as GetChessShader;
        var pack = evt.package;
        if (pack.Get<GameObject>("2", out var model)) { }
        if (pack.Get<Material>("3", out var mater)) { }
        if (!pack.ValidsteAll())
        { Debug.LogError($"[ConfigLogic]某值为空！故障事件：{e}"); return; }
        RenderSpawnUtil.SwapObjectMaterial(model, mater);
    }

    private void InitShaderConfig()
    {
        var chessShader = new ChessShaderConfig { };
        var pack = new Package();
        pack.Put("1", chessShader);
        var pub = new InitShaderCfg { package = pack };
        EventManager.Instance.EmitLogic(pub);
    }

    private void OnShaderCfg(PackageEvent e)
    {
        OnShaderCfg evt = e as OnShaderCfg;
        var pack = evt.package;
        if (pack.Get<Dictionary<string, ChessShaderConfig>>("1", out var pathDict))
        if (!pack.ValidsteAll())
        { Debug.LogError($"[ConfigLogic]某值为空！故障事件：{e}"); return; }
        foreach ( var kv in pathDict )
        {
            _shderConfigDict[kv.Key] = kv.Value.Path;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
