using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EventSystemV2
{
    //基础队列
    public enum EventQueueType
    {
        Logic,         //逻辑
        Render,        //渲染
        Physics,       //物理
        Audio,         //音频 
        Input,         //输入
        Network        //网络
    }

    //标记业务发布方法
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class EventPublishAttr : Attribute { }

    //标记业务监听方法
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class EventListenAttr : Attribute { }

    //工具自动生成代码标记
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class EventAutoGenAttr : Attribute { }

    //事件基类
    public abstract class PackageEvent
    {
        //代码内部定义的事件名
        public string EventClassFullName;
        //所属队列
        public EventQueueType QueueType;
        //携带数据包
        public Package Package = new();
    }


    //===============-----事件配置-----===============//

    //系统ID模板
    [Serializable]
    public class SysIdItem
    {
        public string SysId;    //唯一系统标识
        public string Remark;   //模块备注

        public Dictionary<string, bool> PublishEvent;  //当前系统下对应的事件发布情况
        public Dictionary<string, bool> ListenEvent;   //当前系统下对应的事件监听情况

        //public Dictionary<string, bool> LstMethodEnableToSystem;    //当前系统下所有的具体监听方法启用状态（发布方唯一所以想禁用发布直接关全局总开关就行）

        //public List<string> PublishMethod; //该系统下所有的发布业务方法名
        //public List<string> ListenMethod;  //该系统下所有的监听业务方法名
    }

    //系统ID配置容器
    [Serializable]
    public class SysIdConfigRoot
    {
        public Dictionary<string, SysIdItem> ItemsIsEnable = new();            //启用的系统ID列表
        public Dictionary<string, SysIdItem> ItemsNoEnable = new();            //未启用的系统ID列表    
        public Dictionary<string, List<string>> SysIDToPublsihMethod = new();   //系统ID对应的发布方法，用于快速检索
        public Dictionary<string, List<string>> SysIDToListenMethod = new();    //系统ID对应的监听方法，用于快速检索
    }

    //事件基础属性模板
    [Serializable]
    public class EventItem
    {
        public string EventName;                                    //事件名字
        public string EventClassFullName;                           //生成事件代码内具体名
        public EventQueueType QueueType;                            //所属队列
        public bool GlobalEnable;                                   //事件总开关

        public string EventPublish;                                 //系统的发布方，类型.方法名,确定唯一
        public List<string> EventListen;                            //系统的监听方，类名.方法名，确定唯一
        public Dictionary<string, string> ListenMethodDict;         //系统监听的类名.方法名 <-> 方法名,用于快速注入回调


        public List<string> PublishSysId;                           //发布所属系统ID列表，支持多系统发布同一事件
        public List<string> ListenSysId;                            //监听所属系统ID列表，支持多系统监听同一事件


        public Dictionary<string, string> PublishMethodToSystemID;  //发布方类.方法名 <-> 系统ID, 用于控制是否被系统拦截
        public Dictionary<string, string> ListenMethodToSystemID;   //监听方类.方法名 <-> 系统ID， 用于控制具体监听方法是否被系统拦截


        public List<string> PublishClassList;                       //所有发布方法的类名
        public List<string> ListenClassList;                        //所有监听方法的类名
        public Dictionary<string, string> PublishClassName;         //发布方法的具体方法名 <-> 发布方的类名
        public Dictionary<string, string> ListenClassName;          //监听方法的具体方法名 <-> 监听方法的类名


        public Dictionary<string, List<string>> PublishKey;         //发布包的KEY，方法名称 <-> 包KEY列表
        public Dictionary<string, List<string>> ListenKey;          //监听包的KEY，方法名称 <-> 包KEY列表
        public Dictionary<string, Dictionary<string, string>> KeyConnection;     //包内字段映射 发布方法名-监听方法名 <->《监听KEY <-> 发布KEY》



        public Dictionary<string, string> CodePath;         //生成代码路径，事件名 <-> 《事件文件名称 <-> 事件文件路径》
        public bool IsOldEvent;                                                 //是否是脏数据标识，如果对比原来的快照有改变，则判断为更改事件
    }

    //事件基础配置容器
    [Serializable]
    public class EventConfigRoot
    {
        public Dictionary<string, EventItem> Items = new();              //事件名字 <-> 具体事件
        public Dictionary<string, List<string>> MethodInstances = new();  //实例化的类名字 <-> 对应类名下的方法列表

        public Dictionary<string, List<string>> ListenMethodInstances = new();  //监听的实例化类名字 <-> 对应类名下的方法列表

        public Dictionary<string, string> PublishMethodToEvent = new();  //发布类.方法 <-> 事件名字，用于快速匹配事件
        public Dictionary<string, string> ListenMethodToEvent = new();   //监听类.方法 <-> 事件名字, 用于具体判断事件
    }

    //操作日志模板
    [Serializable]
    public class OperateLogItem
    {
        public string OperateTime;     //操作时间
        public string TargetEvent;     //关联事件
        public string Content;         //操作内容
    }

    //操作日志容器
    [Serializable]
    public class LogConfigRoot
    {
        public List<OperateLogItem> Items = new();
    }

    //版本记录
    [Serializable]
    public class VersionItems
    {
        public string Version;               //版本记录
        public int EventNum;                 //事件数量
        public EventConfigRoot EventConfig;  //事件配置
        public SysIdConfigRoot SysIdItems;   //系统配置
    }

    public class VersionConfigRoot
    {
        public Dictionary<string, VersionItems> items = new();      //版本号 《-》 版本信息
    }


    //测试事件
    public class TestEvent : PackageEvent
    {
        public TestEvent()
        {
            EventClassFullName = "TestEvent";
            QueueType = EventQueueType.Logic;
        }
    }


    public class EVent_TEst
    {
        [EventPublishAttr]
        public void EVentINit()      //默认占位方法
        {

        }

        [EventListenAttr]
        public void EVentOUTit()    //默认占位监听方法
        {

        }
    }

    [Serializable]
    public class Test_CONFIG
    {
        //public Test_CONFIG() { }
        public string Name = "我是测试类";
        public Dictionary<string, int> Test_DICT;
        public List<string> Test_LIST;
    }
}
