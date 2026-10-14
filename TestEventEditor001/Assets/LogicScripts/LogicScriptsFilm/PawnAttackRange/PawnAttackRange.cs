using System;
using UnityEngine;

public class PawnAttackRange : LogicScriptBase
{
    public override string ContainerId => "logic_state_pawn_attack_range";

    //具体属性
    public Vector2Int attackRange;
    public Vector2Int attackRange2;
    public int level;
    public float experience;

    //从子容器加载数据
    public override void LoadFromEntry(ILogicStateEntry entry)
    {
        if (entry is not PawnAttackRangeStateContainer.PawnAttackRangeEntry data)
        {
            Debug.LogError($"[PawnAttackRange]子容器类型错误：{entry.GetType().Name}");
            return;
        }

        //脚本内部自己拆数据
        attackRange = data.attackRange;
        attackRange2 = data.attackRange2;
        level = data.level;
        experience = data.experience;

        Debug.Log($"[PawnAttackRange]加载数据：UUID = {Entity.UUID}, 攻击范围 = {attackRange}, 等级 = {level}");
    }

    //保存数据到子容器
    public override ILogicStateEntry SaveToEntry()
    {
        return new PawnAttackRangeStateContainer.PawnAttackRangeEntry
        {
            uuid = Entity.UUID,
            tag = Entity.Tag,
            attackRange = attackRange,
            attackRange2 = attackRange2,
            level = level,
            experience = experience
        };
    }
}