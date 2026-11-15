using UnityEngine;
using System.Collections.Generic;
using EventSystemV2;

public class E_BUFF_TEST_001 : PackageEvent
{
    public string TargetId;
    public List<string> TargetIds;

    public EntityStateSnapshot snapshot;

    public E_BUFF_TEST_001()
    {
        EventClassFullName = "E_BUFF_TEST_001";
        QueueType = EventQueueType.Logic;
    }
}
