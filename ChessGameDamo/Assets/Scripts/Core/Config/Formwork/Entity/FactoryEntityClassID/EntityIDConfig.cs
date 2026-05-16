using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EntityIDConfig
{
    public Dictionary<string, object> AttrTable;
    public List<string> ClassIDTable;
    public Vector3 WorldPosition;
    public Vector3 ModeLocalOffset;
    public bool IsEnable;

    public string ID = "EntityID";   //元配置顶层查找标识
}