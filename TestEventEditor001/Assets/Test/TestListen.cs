using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EventSystemV2;
public class TestListen
{

    [EventListenAttr]
    public void TestEventListen(int value, string massage)
    {
        Debug.Log($"正确接收到事件，值;{value}， 名字：{massage}");
    }

    [EventListenAttr]
    public void TestEvent002(bool Bool)
    {
        Debug.Log($"接收到事件002,布尔值：{Bool}");
    }

    [EventListenAttr]
    public void TestEventlISTEN_CLASS_003(Test_CONFIG CFG)
    {
        //string Name = CFG.Name;
        //Dictionary<string, int> dict = CFG.Test_DICT;
        //List<string> list = CFG.Test_LIST;
        Debug.Log($"监听方接收到自定义类:{CFG.Name}");
        foreach (var dic in CFG.Test_DICT)
        {
            Debug.Log($"监听方的自定义字典键={dic.Key},值={dic.Value}");
        }
        foreach (var lis in CFG.Test_LIST)
        {
            Debug.Log($"监听方接收到自定义列表值={lis}");
        }
        
    }

    [EventListenAttr]
    public void TestLisenGameObj(GameObject Obj)
    {
        GameObject Pawn = Object.Instantiate(Obj);
        Debug.Log("生成预制体");
    }

    [EventListenAttr]
    public void TestConfigData()
    {
        Debug.Log("接收到配置事件");
        var spawnConfig = ConfigAPI.Get<MapSpawnPointsConfig>();
        if (spawnConfig?.spawnPoints != null)
        {
            foreach (var spawn in spawnConfig.spawnPoints)
            {
                Debug.Log($"生成点：{spawn.id} at ({spawn.x}, {spawn.y}, {spawn.z})");
            }
        }
        else
            Debug.Log($"配置为空/未取得配置,是否为空? {spawnConfig == null}, 子配置是否为空？{spawnConfig.spawnPoints == null}");
        //Debug.Log($"主配置是否可以正常接收{spawnConfig.TestNumber == 1}");
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
