using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PawnMoveRule
{
    private static Dictionary<string, bool> _pawnIdDicit = new();
    //注册逻辑
    public static void InitID(string ID, bool IsNoInit)
    {
        _pawnIdDicit[ID] = IsNoInit;
    }
    private static bool PawnIsInit(string ID)
    {
        //没注册
        if ( ! _pawnIdDicit.ContainsKey(ID))return false;
        //返回结果
        return _pawnIdDicit[ID];
    }
    public static void RemovePawn(string ID)
    {
        _pawnIdDicit.Remove(ID);
    }
    //一局游戏结束时调用
    public static void ClearAll()
    {
        _pawnIdDicit.Clear();
    }
    //兵 完整规则校验
    public static bool CheckLegal(Vector2Int curPos, Vector2Int tarPos, CampType selfCamp, Chess_PawnConfig cfg, string ID)
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
        //Debug.LogWarning($"实际范围：{realStep}");
        if (realStep > cfg.MaxMoveRange ) 
            return false;
        //根据阵营反转方向
        int[][] normalDir = ChessRuleUtil.ReverseDirByCamp(cfg.Moves, selfCamp);
        int[][] captureDir = ChessRuleUtil.ReverseDirByCamp(cfg.CaptureMoves, selfCamp);

        bool isNormalMove = ChessRuleUtil.IsDirMatch(curPos, tarPos, normalDir);
        bool isCaptureMove = ChessRuleUtil.IsDirMatch(curPos, tarPos, captureDir);
        //分支：普通移动/吃子
        if (isNormalMove )
        {
            //兵直行：目标格必须为空
            if (ChessBoardState.HasChess(tarX, tarY)) 
                return false;
            //检验是否是第一次移动
            if (realStep > 1 && PawnIsInit(ID) == false)
            {
                Debug.LogWarning("只有第一步可以走两格");
                return false;
            }
            //长距离移动（起步走两格）：检测中间格子阻挡
            if (realStep > 1 && ChessRuleUtil.IsPathBlocked(curPos, tarPos))
            {
                Debug.LogWarning("中间有棋子");
                return false;
            }
            if (PawnIsInit(ID) == true)
            {
                //刷新状态
                _pawnIdDicit[ID] = false;
            }
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
        Debug.LogError("方向不匹配，移动失败");
        return false;
    }
}