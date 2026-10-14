using System;
using System.Collections.Generic;
using UnityEngine;
using static EntityBlueprintsConfig;


// 实体类型
public enum EntityType
{
    Normal,     // 普通3D实体
    UI,         // UI实体
    Map,        // 地图实体
    Audio       // 音频实体
}

// 实体类
public class Entity
{
    public string EntityId;          // 实体ID（来自蓝图）
    public string UUID;              // 唯一标识（自动生成）
    public string Tag;               // Tag
    public EntityType EntityType;    // 实体类型

    public GameObject Root;          // 根节点
    public GameObject LogicObject;   // 逻辑体
    public GameObject ViewObject;    // 视觉体
    public GameObject AudioObject;   // 音频体

    // 改为基类类型
    public EntityBlueprintBase Blueprint;

    // 渲染相关（方便渲染管理器访问）
    public Renderer Renderer;
    public GameObject ModelInstance;   //模型实例引用

    // 音频相关
    public AudioSource AudioSource;                   //主音频源
    public List<AudioSource> AudioSources = new();    //所有音频源

    // 对象池相关
    public bool IsPooled;

    // 逻辑脚本引用
    public List<MonoBehaviour> LogicScripts = new();
}

