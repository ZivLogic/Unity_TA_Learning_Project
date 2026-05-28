using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class KnightMoveRule
{
    public static bool CheckLegal(Vector2Int curPos, Vector2Int tarPos, CampType selfCamp, Chess_KnightConfig cfg)
    {
        //边界校验
        if ( ! ChessBoardState.IsInBoard(tarPos.x, tarPos.y) ) return false;
        //原地不动
        if (curPos ==  tarPos ) return false;
        //方向匹配
        bool dirMatch = ChessRuleUtil.IsOffsetMatch(curPos, tarPos, cfg.Moves);
        if ( ! dirMatch)
        {
            Debug.LogError("马移动方向不合法");
            return false;
        }
        //不能走到已方棋子位置
        if (ChessBoardState.IsSelfCamp(tarPos.x, tarPos.y, selfCamp) )
            return false;
        return true;
    }
}