using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EntityIDConfig_Special
{
    public Dictionary<string, object> AttrTable;
    public List<string> ClassIDTable;
    public string ParentName;
    public bool IsEnable;

    public string ID = "EntityID_Special";   //元配置顶层查找标识
}
