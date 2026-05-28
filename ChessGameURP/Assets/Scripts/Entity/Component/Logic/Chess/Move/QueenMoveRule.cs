using UnityEngine;

public static class QueenMoveRule
{
    public static bool CheckLegal(Vector2Int curPos, Vector2Int tarPos, CampType selfCamp, Chess_QueenConfig cfg)
    {
        // 1. 边界
        if (!ChessBoardState.IsInBoard(tarPos.x, tarPos.y))
            return false;

        // 2. 原地不动
        if (curPos == tarPos)
            return false;

        // 3. 方向校验
        bool dirMatch = ChessRuleUtil.IsDirMatch(curPos, tarPos, cfg.Moves);
        if (!dirMatch)
        {
            Debug.LogError("后移动方向不合法");
            return false;
        }

        // 4. 路径阻挡
        if (ChessRuleUtil.IsPathBlocked(curPos, tarPos))
        {
            Debug.LogWarning("后的路径非法或行进路径被阻挡");
            return false;
        }

        // 5. 禁止走到己方位置
        if (ChessBoardState.IsSelfCamp(tarPos.x, tarPos.y, selfCamp))
            return false;

        return true;
    }
}
