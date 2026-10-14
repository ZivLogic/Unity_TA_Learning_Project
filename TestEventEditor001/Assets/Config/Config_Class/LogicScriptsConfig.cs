using System;

[Serializable]
[Config(
    configId: "logic_scripts",
    priority: 100
)]
public class LogicScriptsConfig : ConfigDefinition
{
    public ScriptData[] scripts;

    [Serializable]
    public class ScriptData
    {
        public string scriptId;          // 字典键
        public string scriptName;        // C# 脚本类名
        //public string[] parameters;      // 脚本参数（可选）
        public int executionOrder;       // 执行顺序（可选）
        public bool enabled;             // 是否启用（可选）
    }
}