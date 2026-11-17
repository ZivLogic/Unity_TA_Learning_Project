using UnityEngine;
using System.Collections.Generic;
using EventSystemV2;

public class E_TEST_EVENT_003 : PackageEvent
{
    public string TargetId;
    public List<string> TargetIds;

    public string TEST;

    public E_TEST_EVENT_003()
    {
        EventClassFullName = "E_TEST_EVENT_003";
        QueueType = EventQueueType.Logic;
    }
}
