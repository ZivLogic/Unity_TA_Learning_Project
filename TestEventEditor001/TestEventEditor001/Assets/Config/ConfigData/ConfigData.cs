using System;
using System.Collections.Generic;
using System.Reflection;


//定位配置的元数据
[Serializable]
public class ConfigLoacationMetadata    
{
    public string ConfigTypeFullName;                //配置类型完整名
    public string ConfigPath;                        //存储路径
    public string Description;                       //说明
    public Dictionary<string, string> ConfigIDs;     //该类型下的所有ID字典（字典确认唯一ID）ID -> 说明（值可以是随便值，这里可以用作ID的说明）

    //运行时缓存
    [NonSerialized]
    public Type ConfigType;
}


//确认配置路径的总元数据
[Serializable]
public class ConfigLoacationAll
{
    public Dictionary<string, ConfigLoacationMetadata> LoacData = new();        //类型 -》配置
}


//确定配置去哪的元数据
[Serializable]
public class ConfigBindingMetadata
{
    public string ConfigID;                      //配置ID
    public string ConfigTypeFullName;            //配置类型
    public string TargetMethodFullName;          //目标方法完整名

    //运行时缓存
    [NonSerialized]
    public Type ConfigType;                      //配置类型
    [NonSerialized]
    public MethodInfo TargetMethod;              //目标方法
}


//确定配置归属的总数据
[Serializable]
public class ConfigBindingAll
{
    public Dictionary<string, ConfigBindingMetadata> BindData = new();       //ID -》 配置
}


//标记配置容器的标记定义
[AttributeUsage(AttributeTargets.Class)]
public class ConfigSystemAttr : Attribute
{
    public string Description;            //配置说明
}


//标记配置方法的标记定义
[AttributeUsage(AttributeTargets.Method)]
public class ConfigBindAttr : Attribute { }  //不需要指定ID，全部接收


//配置存储路径
public class CONFIG_BASE_PATHS
{
    public const string All_ParentPath = "Assets/Resources/GameConfig/";                  //配置文件总路径
    public const string LoacationPath = All_ParentPath + "Loacation/Loacation.json";      //配置数据存档配置
    public const string BindingPath = All_ParentPath + "Binding/Binding.json";            //配置数据绑定配置
    public const string Child_Config = All_ParentPath + "Children_Config/";               //具体配置容器数据配置
}


//配置更改日志
[Serializable]
public class ConfigLogData
{
    public string ConfigChangeTime;         //配置改变时间
    public string ConfigDataTarget;         //具体改变的配置
    public string ConfigContent;            //配置具体的改变内容
}


//日志总容器
[Serializable]
public class ConfigLogDict
{
    public Dictionary<string, ConfigLogData> ConfigLog = new();         //ID -》 配置
}


//版本管理
[Serializable]
public class ConfingVersion
{
    public string Version;                       //版本号
    public ConfigLoacationMetadata LoacData;     //配置路径配置
    public ConfigBindingMetadata BindData;       //配置绑定配置
}


//版本总容器
[Serializable]
public class ConfigVersionData
{
    public Dictionary<string, ConfigVersionData> ConfigVersion = new();        //版本号 -》 配置
}
