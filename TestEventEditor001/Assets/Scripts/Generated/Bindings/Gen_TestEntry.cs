using UnityEngine;
using System;
using System.Collections.Generic;
using EventSystemV2;

public partial class TestEntry
{
    protected override void RegisterEvents()
    {
        // 绑定事件: E_TEST_001
        RegisterMethod("TestEventListen", evt => 
            TestEventListen(((E_TEST_001)evt).value, ((E_TEST_001)evt).msg));
        // 绑定事件: E_TEST_002
        RegisterMethod("TestInputToListen", evt => 
            TestInputToListen(((E_TEST_002)evt).vle, ((E_TEST_002)evt).name));
        // 绑定事件: E_EVENTCLASS_001
        RegisterMethod("Test_Class_Event", evt => 
            Test_Class_Event(((E_EVENTCLASS_001)evt).evt));
        // 绑定事件: E_EVENTTESTCLASS_002
        RegisterMethod("Test_Class_Event_002", evt => 
            Test_Class_Event_002(((E_EVENTTESTCLASS_002)evt).evt));
        // 绑定事件: E_BUFF_TEST_001
        RegisterMethod("Buff_Test_001", evt => 
            Buff_Test_001(((E_BUFF_TEST_001)evt).snapshot));
    }
}
