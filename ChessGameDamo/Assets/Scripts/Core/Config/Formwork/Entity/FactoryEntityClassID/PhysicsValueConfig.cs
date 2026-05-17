using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PhysicsValueConfig
{
    //优先级，越小越先被加载
    public int Priority;
    //ID表，记录谁被加载
    public List<string> IDTable;
    //是否启用
    public bool IsEnable;

    //元配置查找用
    public string ID = "PhysicsValue";
}