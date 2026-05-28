using UnityEngine;

public static class KingMoveRule
{
    public static bool CheckLegal(Vector2Int curPos, Vector2Int tarPos, CampType selfCamp, Chess_KingConfig cfg)
    {
        // 1. 边界
        if (!ChessBoardState.IsInBoard(tarPos.x, tarPos.y))
            return false;

        // 2. 原地不动
        if (curPos == tarPos)
            return false;

        // 3. 步长限制：王只能走1格
        int step = ChessRuleUtil.GetMoveStepCount(curPos, tarPos);
        if (step > 1)
        {
            Debug.LogWarning("王每次只能移动一格");
            return false;
        }

        // 4. 方向校验
        bool dirMatch = ChessRuleUtil.IsDirMatch(curPos, tarPos, cfg.Moves);
        if (!dirMatch)
        {
            Debug.LogError("王移动方向不合法");
            return false;
        }

        // 5. 王移动距离为1，无中间格子，无需阻挡检测

        // 6. 禁止走到己方棋子位置
        if (ChessBoardState.IsSelfCamp(tarPos.x, tarPos.y, selfCamp))
            return false;

        // 【预留】后续可加：不能移动到被敌方攻击的格子（将军判定）
        return true;
    }
}
