using UnityEngine;
using System.Collections.Generic;
using EventSystemV2;

public class E_TestEvent_001 : PackageEvent
{
    public string TargetId;
    public List<string> TargetIds;

    public int value;
    public string msg;

    public E_TestEvent_001()
    {
        EventClassFullName = "E_TestEvent_001";
        QueueType = EventQueueType.Logic;
    }
}
