using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InputInterceptorConfig
{
    public int Priority;        //优先级
    public bool IsEnable;       //是否启用

    public string ID = "InputInterceptor";
}
