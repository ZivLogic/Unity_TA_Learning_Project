using System.Collections.Generic;

//选择结果返回基类
//中转层从这里拿数据

public class OutputResultBase
{
    public bool HasResult;
    public string Action;
    public List<string> Uuids = new List<string>();
    public string SingleUuid;
    public int Frame;

    // 【新增】告诉中转层，这个结果应该发给谁（比如主角的 UUID）
    public List<string> ReceiverUuids = new List<string>();

    public void Clear()
    {
        HasResult = false;
        Action = null;
        Uuids.Clear();
        SingleUuid = null;
        Frame = 0;
        ReceiverUuids.Clear(); // 记得清理
    }
}