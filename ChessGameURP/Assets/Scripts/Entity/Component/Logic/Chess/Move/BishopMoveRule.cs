using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BishopMoveRule
{
    public static bool CheckLegal(Vector2Int curPos, Vector2Int tarPos, CampType selfCamp, Chess_BishopConfig cfg)
    {
        //边界
        if (!ChessBoardState.IsInBoard(tarPos.x, tarPos.y)) return false;
        //原地不动
        if (curPos == tarPos) return false;
        //方向校验
        bool dirMatch = ChessRuleUtil.IsDirMatch(curPos, tarPos, cfg.Moves);
        if ( ! dirMatch)
        {
            Debug.LogError("象的移动方向不合法");
            return false;
        }
        //路径阻挡
        if (ChessRuleUtil.IsPathBlocked(curPos, tarPos))
        {
            Debug.LogWarning("象的移动路径不合法或被阻挡");
            return false;
        }
        //已方棋子拦截
        if (ChessBoardState.IsSelfCamp(tarPos.x, tarPos.y, selfCamp))
            return false;
        return true;
    }
}
