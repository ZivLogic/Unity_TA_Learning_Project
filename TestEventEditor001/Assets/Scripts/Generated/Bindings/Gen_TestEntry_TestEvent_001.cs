using UnityEngine;
using System;
using System.Collections.Generic;
using EventSystemV2;

public partial class TestEntry
{
    protected override void RegisterEvents()
    {
        // 绑定: TestEntry.TestEventListen
        RegisterMethod("TestEventListen", evt => 
            TestEventListen(((E_TestEvent_001)evt).value, ((E_TestEvent_001)evt).msg));
    }
}
