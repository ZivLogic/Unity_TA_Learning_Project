using EventSystemV2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    //单例
    public static GameManager Instance { get; private set; }


    //初始化
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        //按顺序加载系统
        CreateSystem<EventManager>();
        CreateSystem<InputManager>();
        CreateSystem<ConfigManager>();
        CreateSystem<MasterFactory>();
        CreateSystem<LogicStateManager>();
        CreateSystem<ProductionLauncher>();
        CreateSystem<EntityManager>();
        //CreateSystem<EntitySystem>();
        CreateSystem<InputMediator>();

        //CreateSystem<FactoryTest>();
    }


    //自动挂载一个空物体 + 挂载脚本
    private void CreateSystem<T>() where T : MonoBehaviour
    {
        GameObject sysObj = new GameObject("[" + typeof(T).Name + "]");
        sysObj.AddComponent<T>();
        DontDestroyOnLoad(sysObj);
    }

    //工厂初始化创建
    private void InitGame()
    {
        Debug.Log("================= 游戏初始化 ================");

        //1.批量生产
        ProductionLauncher.Instance.Launch();

        //2.加载逻辑状态
        LogicStateManager.Instance.LoadAllStates();

        //3.加载地图
        //MasterFactory.Instance.MapFactory.LoadMap("test_map");

        Debug.Log("================= 游戏初始化完成 ===================");
    }

    // Start is called before the first frame update
    void Start()
    {
        //延迟一帧，确保配置系统初始化
        Invoke("InitGame", 0.5f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
