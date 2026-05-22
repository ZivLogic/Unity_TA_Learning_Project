using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderFactoryManager : MonoBehaviour, IFactory
{
    public string FactoryName => "Render";

    public static Dictionary<string, RenderMajorIDConfig> _renderMajorDict = new Dictionary<string, RenderMajorIDConfig>();
    public static Dictionary<string, RenderMinorIDConfig> _renderMinorDict = new Dictionary<string, RenderMinorIDConfig>();

    private void Awake()
    {
        //注册自己到中央工厂
        FactoryManager.Instance.RegisterFactory(this);
        EventManager.Instance.Listen<EntityRender_TestEvent>(TestEvent);
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
    

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
