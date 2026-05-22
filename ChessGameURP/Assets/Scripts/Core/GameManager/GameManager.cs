using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    //单例
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        //初始化核心单例
        CreateSystem<EventManager>();          //创建事件系统
        CreateSystem<ConfigManager>();         //创建配置系统

        ////创建ID系统(测试，后归于实体系统管)
        //CreateSystem<GlobalIDManager>();

        CreateSystem<FactoryManager>();        //创建工厂系统
        CreateSystem<AssetsManager>();         //创建资源系统
        CreateSystem<InputManager>();          //创建输入系统

        



        //订阅事件
        SubscribeAllEvents();

        //加载全局事件路由表

        EventRouteRegistrar.InitFromConfigManagerPublish();
        EventRouteRegistrar.InitFromConfigManager();
        Debug.Log("[ConfigManager]事件路由表加载完成");

       
    }

    //自动挂载一个空物体 + 挂载脚本
    private void CreateSystem<T>() where T : MonoBehaviour
    {
        GameObject sysObj = new GameObject("[" + typeof(T).Name + "]");
        sysObj.AddComponent<T>();
        DontDestroyOnLoad (sysObj);
    }

    public void SubscribeAllEvents()        //事件订阅
    {
        
    }

    // Start is called before the first frame update
    void Start()
    {
        //游戏启动自动初始化
        var pack = new Package();
        var pub = new GameInit { package = pack };
        EventManager.Instance.EmitLogic<GameInit>(pub);
        //EventManager.Instance.EmitLogic(pub);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
