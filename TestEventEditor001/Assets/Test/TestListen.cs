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


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
