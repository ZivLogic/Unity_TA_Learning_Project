using UnityEngine;
using System.Collections.Generic;
using EventSystemV2;

public class E_TEST_002 : PackageEvent
{
    public string TargetId;
    public List<string> TargetIds;

    public int vle;
    public string name;

    public E_TEST_002()
    {
        EventClassFullName = "E_TEST_002";
        QueueType = EventQueueType.Logic;
    }
}
