using System;

// 1. 生命周期状态（最底层，决定生死）
public enum EntityLifecycleState
{
    Alive,
    Dead,
    Destroying,
    Pooled
}

// 2. 游戏流程状态（决定是否能操作）
public enum GameFlowState
{
    Normal,
    Paused,     // 暂停
    Cutscene,   // 过场动画
    InMenu      // 打开背包/菜单
}

// 3. 动作状态（互斥，决定身体在干嘛）
public enum EntityActionState
{
    Idle,
    Moving,
    Attacking,
    Casting,
    Stunned,    // 硬直/眩晕
    Hit         // 受击
}