using System;

//顶级父容器注册表：记录所有逻辑脚本的状态容器
[Serializable]
[Config(
    configId: "logic_state_registry", 
    priority: 50 //优先级
)]
public class LogicStateRegistry : ConfigDefinition
{
    public LogicStateEntry[] entries;

    [Serializable]
    public class LogicStateEntry
    {
        public string containerId;      //父容器配置ID
        public string scriptName;       //对应逻辑脚本类名
        public string containerType;    //父容器类型
        public string savePath;         //保存路径
    }
}