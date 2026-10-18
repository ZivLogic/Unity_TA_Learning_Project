using System.Collections.Generic;

/// <summary>
/// 中转层组装后的实体输入事件
/// 交给实体系统注入
/// </summary>
public class EntityInputEvent
{
    public string Action;
    public string ActionKey;
    public string Context;
    public InputKeyState KeyState;
    public string Device;
    public string Param;
    public int Frame;

    // 目标实体 UUID 列表
    public List<string> TargetUuids = new List<string>();
}