using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AttackRangeManager
{
    //白方所有棋子的攻击坐标集合
    public static HashSet<Vector2Int> WhiteAttackPos = new();
    //黑方所有棋子的攻击坐标集合
    public static HashSet<Vector2Int> BlackAttackPos = new();

    //清空所有攻击范围(开局/重置调用)
    public static void ClearAll()
    {
        WhiteAttackPos.Clear();
        BlackAttackPos.Clear();
    }

    //获取指定阵营的攻击集合
    public static HashSet<Vector2Int> GetAttackSet(CampType camp)
    {
        return camp == CampType.White ? WhiteAttackPos : BlackAttackPos;
    }
    
    //判断某个坐标是否被指定阵营攻击
    public static bool IsPosUnderAttack(Vector2Int pos, CampType attackerCamp)
    {
        var set = GetAttackSet(attackerCamp);
        return set.Contains(pos);
    }
}