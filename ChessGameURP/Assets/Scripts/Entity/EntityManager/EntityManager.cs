using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityManager : MonoBehaviour
{
    //单例
    public static EntityManager Instance { get; private set; }

    //事件子模块
    public EntityLogic _logic;
    public EntityPublish _publish;

    private void Awake()
    {
        //初始化单例
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        //初始化事件模块
        _logic = new EntityLogic();
        _publish = new EntityPublish();

        //订阅全局事件
        SubscribeAllEvents();

        //播报
        Debug.Log("[EntityManager]初始化完成");
    }




    //获取棋子组件配置
    private void GetChessComponentCongfig()
    {
        var pack = EntityComponentUtil.GetChessManComponentConfig();
        EntityPublish_GetChessManComponentConfig_GetChessManConfig pub = new EntityPublish_GetChessManComponentConfig_GetChessManConfig { package = pack };
        _publish.GetChessManComponentConfig(pub);
    }






    //全局事件订阅方法
    public void SubscribeAllEvents()
    {
        EventManager.Instance.Listen<GameInit>(OnInitEntityManager);

    }

    //处理初始化事件
    private void OnInitEntityManager(PackageEvent e)
    {
        GetChessComponentCongfig();
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
