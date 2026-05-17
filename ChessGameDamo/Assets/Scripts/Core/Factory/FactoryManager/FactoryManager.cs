using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FactoryManager : MonoBehaviour
{
    //单例
    public static FactoryManager Instance { get; private set; }

    //子工厂注册表
    private Dictionary<string, IFactory> _factories = new();

    //事件逻辑处理器
    public FactoryLogic _logic;
    public FactoryPublish _publish;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        //创建子工厂空物体
        CreateSystem<MapFactoryManager>();      //创建地图工厂
        CreateSystem<EntityFactoryManager>();   //创建实体工厂
        CreateSystem<LogicFactoryManager>();    //创建逻辑工厂
        CreateSystem<PhysicsFactoryManager>();  //创建物理工厂

        //初始化事件器
        _logic = new FactoryLogic();
        _publish = new FactoryPublish();

        //统一订阅事件
        SubscribeAllEvents();
        Debug.Log("[FactoryManager]初始化完成");
    }

    // 场景销毁/模块关闭时 调用注销
    private void OnDestroy()
    {
        //如果已被销毁则直接跳过
        if (GameGlobalState.IsQuitting) return;
        // 注销本系统所有事件订阅
        _logic.UnlistenAllEvent();
    }

    // 外部要调用系统方法 转发给内核
    public void SystemInit()
    {
        _logic.Init();
    }

    //注册子工厂（子工厂自己调用）
    public void RegisterFactory(IFactory factory)
    {
        string name = factory.FactoryName;
        if (!_factories.ContainsKey(name))
        {
            _factories.Add(name, factory);
            Debug.Log($"[Factory]注册：{name}");
        }
    }

    //获取子工厂（外部唯一入口）
    public T GetFactory<T>(string factoryName) where T : class , IFactory
    {
        _factories.TryGetValue(factoryName, out var factory);
        return factory as T;
    }

    //创建子集
    private void CreateSystem<T>() where T : MonoBehaviour
    {
        GameObject sysObj = new GameObject("[" + typeof(T).Name + "]");
        sysObj.AddComponent<T>();
        DontDestroyOnLoad(sysObj);
    }

    //订阅事件
    public void SubscribeAllEvents()
    {
        EventManager.Instance.Listen<GameInit>(OnGameInit);                                             //订阅初始化事件
        
    }
    
    //游戏初始化，初始化所有工厂
    private void OnGameInit(PackageEvent e)
    {
        foreach (var factory in _factories.Values)
            //调用工厂初始化
            factory.Initialize();
        Debug.Log("[FactoryManager]所有工厂初始化完成");

        //事件
        PackageChessConfigInit();

        
    }

    //打包棋子初始化配置
    //以后应该尽量单独写一个事件，而不是集合在一起初始化，包体太重解析会变慢
    private void PackageChessConfigInit()
    {
        var chessBoard = new ChessBoardConfig { IsPrefab = true };
        var chessBoardTile = new ChessBoardTileConfig { };
        var chessmanPosition = new ChessmanPositionConfig { IsPrefab = true };
        var chessBoardPrefab = new ChessBoardPrefabConfig { };
        var chessmanPrefabs = new ChessmanPrefabsConfig { IsList = true };

        //打包
        var pack = new Package();
        pack.Put(EventPackName.CHESSBOARD_CONFIG, chessBoard);
        pack.Put(EventPackName.CHESSBOARDTILE_CONFIG, chessBoardTile);
        pack.Put(EventPackName.CHESSMAN_POSITIONCONFIG, chessmanPosition);
        pack.Put(EventPackName.CHESSBOARD_PREFAB, chessBoardPrefab);
        pack.Put(EventPackName.CHESSMAN_PREFABS, chessmanPrefabs);

        //事件
        var pub = new ConfigLogic_InitChessConfig_InitChessEvent { package = pack };
        _publish.ConfigLogic_InitChessConfig_InitChessEvent(pub);
    }

    private void AutoLogicScript()
    {
        var logicFactory = GetFactory<LogicFactoryManager>("Logic");
        logicFactory?.AutoAttachAllLogic();
    }
    private void AutoPhysicsScript()
    {
        var physicsFactory = GetFactory<PhysicsFactoryManager>("Physics");
        physicsFactory?.AutoAttachAllPhysics();
    }





    // Start is called before the first frame update
    void Start()
    {
        //最后调用
        //实体全部生成完毕再挂载脚本
        AutoLogicScript();
        //再挂载物理脚本
        AutoPhysicsScript();
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
