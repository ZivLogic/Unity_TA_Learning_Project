using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PawnMoveRule
{
    //兵 完整规则校验
    public static bool CeckLegal(Vector2Int curPos, Vector2Int tarPos, CampType selfCamp, Chess_PawnConfig cfg)
    {
        int curX = curPos.x;
        int curY = curPos.y;
        int tarX = tarPos.x;
        int tarY = tarPos.y;
        //基础边界校验
        if ( ! ChessBoardState.IsInBoard(tarX, tarY) ) 
            return false;
        //起点 = 终点 ，无效
        if (curPos == tarPos ) 
            return false;
        //计算实际移动步数，限制最大步长
        int realStep = ChessRuleUtil.GetMoveStepCount(curPos, tarPos);
        if (realStep > cfg.MaxMoveRange ) 
            return false;
        bool isNormalMove = ChessRuleUtil.IsOffsetMatch(curPos, tarPos, cfg.Moves);
        bool isCaptureMove = ChessRuleUtil.IsOffsetMatch(curPos, tarPos, cfg.CaptureMoves);
        //分支：普通移动/吃子
        if (isNormalMove )
        {
            //兵直行：目标格必须为空
            if (ChessBoardState.HasChess(tarX, tarY)) 
                return false;
            //长距离移动（起步走两格）：检测中间格子阻挡
            if (realStep > 1 && ChessRuleUtil.IsPathBlocked(curPos, tarPos))
                return false;
            return true;
        }
        else if (isCaptureMove )
        {
            //兵斜向：必须吃掉敌方棋子
            if ( ! ChessBoardState.IsEnemy(tarX, tarY, selfCamp))
                return false;
            return true;
        }
        //方向不匹配，直接禁止
        return false;
    }
}