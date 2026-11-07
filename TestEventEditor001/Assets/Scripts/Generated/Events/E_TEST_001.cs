using UnityEngine;
using System.Collections.Generic;
using EventSystemV2;

public class E_TEST_001 : PackageEvent
{
    public string TargetId;
    public List<string> TargetIds;

    public int value;
    public string msg;

    public E_TEST_001()
    {
        EventClassFullName = "E_TEST_001";
        QueueType = EventQueueType.Logic;
    }
}
