using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using EventSystemV2;

public class TestPublish
{
    public GameObject Obj = null;

    [EventPublishAttr]
    public void TestEventPublish(int Value, string Message)
    {
        Debug.Log("正常发布");
        EventUtil.EmitEventFromMethod(this, nameof(TestEventPublish), Value, Message);
    }

    [EventPublishAttr]
    public void TestEventPublish002(bool TestBool)
    {
        Debug.Log("正常发布第二个事件");
        EventUtil.EmitEventFromMethod(this, nameof(TestEventPublish002), TestBool);
    }

    [EventPublishAttr]
    public void Test_CLASSevnt(Test_CONFIG Test_cfg)
    {
        Debug.Log("测试自定义类");
        EventUtil.EmitEventFromMethod(this, nameof(Test_CLASSevnt), Test_cfg);
    }

    [EventPublishAttr]
    public void GameOBJ(GameObject Pawn)
    {
        Debug.Log("尝试传递GameObject");
        EventUtil.EmitEventFromMethod(this, nameof (GameOBJ), Pawn);
    }

    public void Test_GameObj()
    {
        Obj = Resources.Load<GameObject>("TestGameObject/Pawn");
        GameOBJ(Obj);
    }

    public void TestEvent()
    {
        int value = 1;
        string meg = "Test";
        TestEventPublish(value, meg);
    }


    public void TestEVen002()
    {
        bool Bool = true;
        TestEventPublish002(Bool);
    }

    public void TestCLASS003()
    {
        Test_CONFIG cfg = new();
        cfg.Test_DICT = new Dictionary<string, int>();
        cfg.Test_DICT["测试字典键001"] = 1;
        cfg.Test_LIST = new List<string>();
        cfg.Test_LIST.Add("测试列表02");
        cfg.Test_LIST.Add("测试列表03");
        Test_CLASSevnt(cfg);
    }

    //保存测试配置
    public void InitTestConfig()
    {
        var config = EventGlobalCache.AllEvent;

        //创建事件配置
        var eventItem = new EventItem
        {
            EventName = "TestEvent",
            EventClassFullName = "E_TestEvent",
            QueueType = EventQueueType.Logic,
            GlobalEnable = true,

            EventPublish = "TestPublish.TestEventPublish",                 //唯一发布类
            EventListen = new List<string>                                 //所有的监听类.具体方法列表
            {
                "TestListen.TestEventListen"
            },
            ListenMethodDict = new Dictionary<string, string>             //所有的监听类.具体方法列表 <-> 具体方法
            {
                {"TestListen.TestEventListen", "TestEventListen" }
            },


            PublishSysId = new List<string> { "TestSysID" },               //发布系统名称
            ListenSysId = new List<string> { "TestSysID" },                //监听系统名称


            //迭代取代之前的系统判断，系统ID内可以有多个方法，但是方法只能有一个ID，所有将他们调换位置
            PublishMethodToSystemID = new Dictionary<string, string>
            {
                {"TestEventPublish", "TestSysID" }
            },
            ListenMethodToSystemID = new Dictionary<string, string>
            {
                {"TestEventListen", "TestSysID" }
            },


            PublishClassList = new List<string>                            //发布方所有的类实例
            {
                "TestPublish"
            },
            ListenClassList = new List<string>                              //监听方所有的类实例
            {
                "TestListen"
            },


            PublishClassName = new Dictionary<string, string>               //发布方具体类方法对应类名字
            {
                {"TestPublish.TestEventPublish", "TestPublish" }
            },
            ListenClassName = new Dictionary<string, string>                //监听方具体类方法对应类名字
            {
                {"TestListen.TestEventListen", "TestListen" }
            },


            PublishKey = new Dictionary<string, List<string>>                //发布方法内的类.方法名字 <-> 《包内值》
            {
                {"TestPublish.TestEventPublish", new List<string>
                {
                    "Value",
                    "Message"
                }
                }
            },
            ListenKey = new Dictionary<string, List<string>>                 //监听方法内的类.方法名字 <-> 《包内值》
            {
                {"TestListen.TestEventListen", new List<string>
                {
                    "value",
                    "massage"
                }
                }
            },


            KeyConnection = new Dictionary<string, Dictionary<string, string>>          //包内联系映射,发布类.方法 <-> 监听类.方法
            {
                {"TestPublish.TestEventPublish_TestListen.TestEventListen", new Dictionary<string, string>
                {
                    { "value", "Value"},       //监听名对应发布名，顺序不能反
                    {"massage", "Message" }
                }
                }
            }
        };
        config["TestEvent"] = eventItem;    //事件名字 《-》 事件
        //手动注册一次获得配置，让发布和监听可以快速根据具体方法名匹配事件
        EventGlobalCache.RefeshMethodInstancesToEvent();

        var SysCfg = EventGlobalCache.SysId_IsEnable;

        var sysItem = new SysIdItem
        {
            SysId = "TestSysID",
            Remark = "测试系统ID",
            PublishEvent = new Dictionary<string, bool>
            {
                {"E_TestEvent", true}
            },
            ListenEvent = new Dictionary<string, bool>
            {
                {"E_TestEvent", true}
            },
            //PublishMethod = new List<string>
            //{
            //    "TestEventPublish"
            //},
            //ListenMethod = new List<string>
            //{
            //    "TestEventListen"
            //}
        };
        SysCfg["TestSysID"] = sysItem;

        

        Dictionary<string, List<string>> sysIDToPublsihMethod = new Dictionary<string, List<string>>
            {
                { "TestSysID", new List<string>{ "TestPublish.TestEventPublish" } }
            };
        Dictionary<string, List<string>> sysIDToListenMethod = new Dictionary<string, List<string>>
            {
                {"TestSysID", new List<string>{ "TestListen.TestEventListen" } }
            };
       
        EventGlobalCache.GlobalSysConfig.SysIDToPublsihMethod.Clear();
        EventGlobalCache.GlobalSysConfig.SysIDToListenMethod.Clear();
        EventGlobalCache.GlobalSysConfig.SysIDToPublsihMethod = sysIDToPublsihMethod;
        EventGlobalCache.GlobalSysConfig.SysIDToListenMethod = sysIDToListenMethod;


        string version = "0.1.0";
        var versionItem = new VersionItems
        {
            Version = version,
            EventNum = EventGlobalCache.AllEvent.Count,
            EventConfig = EventConfigHelper.DeepCopy(EventGlobalCache.GlobalEventConfig),
            SysIdItems = EventConfigHelper.DeepCopy(EventGlobalCache.GlobalSysConfig)
        };

        EventGlobalCache.AllVersion[version] = versionItem;
        
        //刷新并保存
        EventGlobalCache.RefeshMethodInstances();
        EventGlobalCache.SaveAllToDisk();
    }


    public void RegistEventTest()
    {
        var publisher = new TestPublish();
        var listener = new TestListen();

        EventManager.Instance.Listen<TestEvent>(evt =>
        {
            int value = evt.Package.GetSafe<int>("Value");
            string message = evt.Package.GetSafe<string>("Message");
            listener.TestEventListen(value, message);
        });
    }

    [RuntimeInitializeOnLoadMethod]
    public static void TestAutoMethod()
    {
        Debug.Log("我是自动代码");
    }

}
