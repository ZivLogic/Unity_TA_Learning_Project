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

    public void Clear()
    {
        HasResult = false;
        Action = null;
        Uuids.Clear();
        SingleUuid = null;
        Frame = 0;
    }
}