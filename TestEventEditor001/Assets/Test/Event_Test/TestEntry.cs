using EventSystemV2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class TestEntry : EventEntryBase
{
    [EventListenAttr]
    public void TestEventListen(int value,string msg)
    {
        Debug.Log($"接收方接受到事件，事件值：{value},事件名：{msg},实体ID:{Entity.UUID}");
    }

    [EventPublishAttr]
    public void TestEventPublish(List<string> UUIDS, int value, string msg)
    {
        EventUtil.EmitToTargets(this, nameof(TestEventPublish), UUIDS, value, msg);
        //Debug.Log("事件成功发布");
    }

    public void TEST_001()
    {
        string uuid = "Pawn_0003";
        List<string> list = new List<string>();
        list.Add(uuid);
        int Value = 1;
        string MSG = "test";

        TestEventPublish(list,Value, MSG);
    }
}
