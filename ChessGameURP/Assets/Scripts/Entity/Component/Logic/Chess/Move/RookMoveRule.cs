using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RookMoveRule
{
    public static bool CheckLegal(Vector2Int curPos, Vector2Int tarPos, CampType selfCamp, Chess_RookConfig cfg)
    {
        //边界
        if ( ! ChessBoardState.IsInBoard(tarPos.x, tarPos.y) ) return false;
        //原地不动
        if (curPos == tarPos ) return false;
        //方向校验
        bool dirMatch = ChessRuleUtil.IsDirMatch(curPos, tarPos, cfg.Moves);
        if ( ! dirMatch)
        {
            Debug.LogError("车移动方向不合法");
            return false;
        }
        //路径阻挡检测
        if (ChessRuleUtil.IsPathBlocked(curPos, tarPos))
        {
            Debug.LogWarning("车的行进路径不合法或被阻挡");
            return false;
        }
        //禁止走到已方棋子处
        if (ChessBoardState.IsSelfCamp(tarPos.x, tarPos.y, selfCamp) )
            return false;
        return true;
    }
}
