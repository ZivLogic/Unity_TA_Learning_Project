using UnityEngine;
using EventSystemV2;

public class E_TestEvent : PackageEvent
{
    public E_TestEvent ()
     {
        EventClassFullName = "E_TestEvent";
        QueueType = EventQueueType.Logic;
     }
}
