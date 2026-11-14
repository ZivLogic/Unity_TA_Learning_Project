using UnityEngine;
using System.Collections.Generic;
using EventSystemV2;

public class E_EVENTTESTCLASS_002 : PackageEvent
{
    public string TargetId;
    public List<string> TargetIds;

    public Event_Class_Test_001 evt;

    public E_EVENTTESTCLASS_002()
    {
        EventClassFullName = "E_EVENTTESTCLASS_002";
        QueueType = EventQueueType.Logic;
    }
}
