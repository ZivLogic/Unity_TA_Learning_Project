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
    }
}
