using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MoveConfigCache
{
    public static Package movePack = new();

    public static void GetComponentConfig(Package p)
    {
        movePack = p;
    }
}
