using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventTest
{
    [EventPublishMethod]
    public void TestPublishEvent(out int test1, out int test2)
    {
        test1 = 1;
        test2 = 2;
    }

    [EventListenMethod]
    public void TestListenEvent(int test1, int test2)
    {
        var test = test1 + test2;
    }



}