using System;

// 1. 生命周期状态（最底层，决定生死和暂停）
public enum EntityLifecycleState
{
    Alive,
    Dead,
    Destroying,
    Pooled,
    Paused      // 暂停归为生命周期状态
}

// 2. 动作状态（互斥，决定身体在干嘛）
public enum EntityActionState
{
    Idle,
    Moving,
    Attacking,
    Casting,
    Stunned,
    Hit,
    InventoryOpen // 背包属于动作维度
}