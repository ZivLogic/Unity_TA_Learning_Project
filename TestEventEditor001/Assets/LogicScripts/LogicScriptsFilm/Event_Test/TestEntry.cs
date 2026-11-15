using EventSystemV2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class TestEntry : EventEntryBase, IEntityStateMachineProvider
{
    private EntityStateMachineBase _stateMachine;

    public void InjectStateMachine(EntityStateMachineBase stateMachine)
    {
        _stateMachine = stateMachine;
        if (stateMachine != null)
        {
            Debug.Log($"[{Entity.UUID}的事件入口]绑定状态机：{stateMachine.GetType().Name}");
            return;
        }
        Debug.Log($"{Entity.UUID}没有绑定状态机");
    }


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


    [EventListenAttr]
    public void TestInputToListen(int vle, string name)
    {
        Debug.Log($"输入转事件成功，值：{vle}，名字：{name}，实体ID：{Entity.UUID}");
    }

    [EventListenAttr]
    public void Test_Class_Event(string evt)
    {
        if (!string.IsNullOrEmpty(evt))
        {
            var snapshot = JsonUtility.FromJson<Event_Class_Test_001>(evt);

            Debug.Log($"收到自定义类型：{snapshot.TestName}");
        }
    }

    [EventListenAttr]
    public void Test_Class_Event_002(Event_Class_Test_001 evt)
    {
        Debug.Log($"收到自定义类型2：{evt.TestName}");
    }

    [EventListenAttr]
    public void Buff_Test_001(EntityStateSnapshot snapshot)
    {
        Debug.Log("接收到伤害事件");
        foreach (var Buff in snapshot.Tags)
        {
            Debug.Log($"收到状态：{Buff}");
            if (Buff == "Burning")
            {
                Debug.Log($"{Entity.UUID}接收到燃烧攻击事件");
                _stateMachine.AddBuff( Buff );
            }
        }
    }

    public void TEST_001()
    {
        string uuid = "Pawn_0003";
        string uuid2 = "Pawn_0004";
        List<string> list = new List<string>();
        list.Add(uuid);
        list.Add(uuid2);
        int Value = 1;
        string MSG = "test";

        TestEventPublish(list,Value, MSG);
    }
}

public class Event_Class_Test_001
{
    public string TestName;
}
