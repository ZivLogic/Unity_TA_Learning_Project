using UnityEngine;
using EventSystemV2;

public class E_TestGameObj : PackageEvent
{
    public E_TestGameObj ()
     {
        EventClassFullName = "E_TestGameObj";
        QueueType = EventQueueType.Logic;
     }
}
