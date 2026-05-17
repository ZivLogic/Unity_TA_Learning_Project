using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PhysicsComponentConfig
{
    //物理组件表
    public Dictionary<string, Dictionary<string,object> >ComponentTable;

    //元配置查找用
    public string ID = "PhysicsComponent";
}
