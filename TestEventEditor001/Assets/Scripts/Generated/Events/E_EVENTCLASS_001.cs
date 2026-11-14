using UnityEngine;
using System.Collections.Generic;
using EventSystemV2;

public class E_EVENTCLASS_001 : PackageEvent
{
    public string TargetId;
    public List<string> TargetIds;

    public string evt;

    public E_EVENTCLASS_001()
    {
        EventClassFullName = "E_EVENTCLASS_001";
        QueueType = EventQueueType.Logic;
    }
}
