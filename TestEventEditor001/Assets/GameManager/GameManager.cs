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
        CreateSystem<ConfigManager>();
        CreateSystem<MasterFactory>();
        CreateSystem<LogicStateManager>();

        CreateSystem<FactoryTest>();
    }


    //自动挂载一个空物体 + 挂载脚本
    private void CreateSystem<T>() where T : MonoBehaviour
    {
        GameObject sysObj = new GameObject("[" + typeof(T).Name + "]");
        sysObj.AddComponent<T>();
        DontDestroyOnLoad(sysObj);
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
