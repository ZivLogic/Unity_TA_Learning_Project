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