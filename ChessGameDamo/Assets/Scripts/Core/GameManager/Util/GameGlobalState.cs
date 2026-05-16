using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameGlobalState
{
    // 是否正在被Unity强制销毁（编辑器停运行、场景销毁）
    public static bool IsQuitting { get; private set; }

    [RuntimeInitializeOnLoadMethod]
    static void Init()
    {
        IsQuitting = false;
        Application.quitting += () => IsQuitting = true;
    }
   
}
