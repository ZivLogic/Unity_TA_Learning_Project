using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//基类
public abstract class EventBase
{

}

//包事件基类
public abstract class PackageEvent : EventBase
{
    //绑定所属事件对列
    public EventQueueType QueueType;
    //承载动态数据包
    public Package package;
}

//队列管道枚举
public enum EventQueueType
{
    Logic,
    Render,
    Physics,
    Audio,
    Input
}

//标记：发布事件方法，用来标记发布事件的具体业务，用out获取事件值
[AttributeUsage(AttributeTargets.Method)]
public class EventPublishMethodAttribute : Attribute { }

//标记：事件监听业务方法，用来标记接收事件的具体业务
[AttributeUsage(AttributeTargets.Method)]
public class  EventListenMethodAttribute : Attribute { }

//标记：工具自动生成的转发入口（禁止手动修改）
[AttributeUsage(AttributeTargets.Method)]
public class EventAutoGenerateEntryAttribute : Attribute { }

//规范写法：
//发布事件：发布事件业务方法统一签名void XXX (out var 数据1，out int 数据2，3，4等)，在方法之前标记[EventPublishMethodAttribute]
//监听事件：监听业务方法，对应接收发布方数据，签名随部定，在方法前标记[EventListenMethodAttribute]


//字段映射模型（包KEY <-> 业务逻辑参数 绑定关系）
//实现按需选择字段注入
[Serializable]
public class  FieldMapItem
{
    public string packageKey;          //数据包内KEY
    public string paramName;           //目标业务方法名
    public string typeFullName;        //类型全限定名（类型校验用）
    public bool isRequired = true;     //是否必填
}
[Serializable]
public class EventFieldMapping
{
    public string eventTypeFullName;    //事件类全名
    public string entryMethodName;    //自动生成的转发入口名
    public string businessMethodName;   //目标业务方法名
    public FieldMapItem[] fieldMaps;    //字段映射列表
}
//事件基础定义模型（绑定枚举，队列，基础信息）
[Serializable]
public class EventDefine
{
    public string eventName;    //事件自定义名称
    public string eventClassName;    //自动生成的事件类名
    public string eventTypeFullName;    //事件类全路径名
    public EventQueueType queueType;    //绑定事件队列枚举
    public bool isGlobalEnable = true;    //事件总开关
    public List<string> packageKeys;    //该事件所有数据包KEY
}

//操作日志模型
[Serializable]
public class EventOperateLog
{
    public string operateTime;   //操作时间
    public string eventName;     //关联事件
    public string operateContent;    //操作内容
    public string oldValue;      //旧值
    public string newValue;      //新值
}

public class EventRootCfg
{
    public EventDefineWrap EventDefine;
    public FieldMappingWrap FieldMapping;
    public LogWrap OperateLog;
    public SystemIdWrap SystemIdList;//新增系统ID注册表
}

public class EventDefineWrap
{
    public List<EventDefine> Items = new();
}

public class FieldMappingWrap
{
    public List<EventFieldMapping> Items = new();
}

public class LogWrap
{
    public List<EventOperateLog> Items = new();
}

//系统ID注册表
public class SystemIdItem
{
    public string sysId;
    public string remark;//备注：所属模块备注
}
//在EventRootCfg、三个包裹类追加
public class SystemIdWrap { public List<SystemIdItem> Items = new(); }



//全局事件类
public class GameInit : PackageEvent { }

public class CreateChessmanPrefabs : PackageEvent { }    //创建棋子预设体

//输入系统发送选中事件
public class InputMouseSelect : PackageEvent { }         //鼠标选中事件


//配置系统监听
public class ConfigLogic_InitChessConfig_InitChessEvent : PackageEvent { }
public class ConfigLogic_OnInputConfig_InitInputConfig :  PackageEvent { }
public class ConfigLogic_InitEntityIDConfig_InitEntityID : PackageEvent { }

//工厂系统监听
public class FactoryLogic_OnResponseChessConfig_InitChessConfig  : PackageEvent { }
public class FactoryLogic_OnResponseChessPrefabs_AssetInitPrefabsLoadAll  : PackageEvent { }
public class FactoryLogic_OnEntityIDConfig_InitEntityID : PackageEvent { }


//资源系统监听
public class AssetsLogic_OnInitCrateChessPrefabs_CreateInitChessPrefabs  : PackageEvent { }

//输入系统监听
public class InputLogic_OnConfig_IsInputConfig  : PackageEvent { }

//改为发布方发布事件名
//测试事件
public class Test_Event : PackageEvent { }

//实体系统发布(新)
public class EntityPublish_GetChessManComponentConfig_GetChessManConfig  : PackageEvent { }
public class ConfigPulish_GetChessManComponentConfig_GetChessManConfig : PackageEvent { }

//工厂系统发布
public class FactoryPublish_GetIdentityConfig_IdConfig :  PackageEvent { }

public class EntityRender_TestEvent : PackageEvent { }

//配置系统发布
public class ConfigPublish_OnIdentityConfig_IdConfig : PackageEvent { }


//硬逻辑快速迭代
public class ChessComponentCfg :  PackageEvent { }

public class MoveChessTest :  PackageEvent { }

public class GetTileObj :  PackageEvent { }

public class ChessManModelMove :  PackageEvent { }

public class InitShaderCfg : PackageEvent { }

public class OnShaderCfg  : PackageEvent { }

public class LoadShader :  PackageEvent { }

public class GetChessShader :  PackageEvent { }

public class ChessBeCapturedEvent : PackageEvent { }

public class GetChessMan_TestUI  : PackageEvent { }

public class GetChessTile_TestUI  : PackageEvent { }

public class MoveChessMan_TestUI : PackageEvent { }