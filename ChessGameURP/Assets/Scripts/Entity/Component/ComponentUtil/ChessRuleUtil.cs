using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ChessRuleUtil
{
    //获取两点之间所有的中间路径格子（不含起点，含终点）
    public static List<Vector2Int> GetPathCells(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        if (start == end) return path;
        int dx = end.x - start.x;
        int dy = end.y - start.y;
        //确保棋子在同行/同列/同对角线
        bool sameRow = (dy == 0); //同行，左右走
        bool sameCol = (dx == 0); //同列，上下走
        bool sameDiag = (Mathf.Abs(dx) == Mathf.Abs(dy));  //正斜线
        //三者都不满足 -> 非直线/斜线，直接判定非法
        if ( ! sameRow && ! sameCol && ! sameDiag)
        {
            return path;
        }
        int stepX = dx == 0 ? 0 : dx / Mathf.Abs(dx);
        int stepY = dy == 0 ? 0 : dy / Mathf.Abs(dy);

        Vector2Int cur = start;

        //添加最大迭代次数保护（棋盘最多8*8，最多需要8步）
        int maxSteps = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
        int steps = 0;

        while (cur != end)
        {
            cur.x += stepX;
            cur.y += stepY;
            path.Add(cur);
            steps++;
        }
        //如果因为步数限制退出而没有到达终点，说明路径有问题
        if (cur != end)
        {
            Debug.LogError($"计算棋盘路径错误，起点：{cur},终点：{end}");
            path.Clear();
        }
        return path;
    }
    //检测路径上是否有棋子阻挡（马除外）
    public static bool IsPathBlocked(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> path = GetPathCells(start, end);
        //路径为空 + 起点 != 终点 = 形态非法（日子，拐线等）
        if (path.Count == 0 && start != end)
        {
            return true;
        }
        //遍历中间所有格子，只要有棋子就判断为阻挡
        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector2Int cell = path[i];
            if (ChessBoardState.HasChess(cell.x, cell.y)) return true;
        }
        return false;
    }
    //计算两点曼哈顿步长（格子数）
    public static int GetMoveStepCount(Vector2Int start, Vector2Int end)
    {
        int dx = Mathf.Abs(end.x - start.x);
        int dy = Mathf.Abs(end.y - start.y);
        //斜走/直走统一取最大差值 = 实际行走格数
        return Mathf.Max(dx, dy);
    }
    //判断目标偏移是否在配置的偏移列表内
    public static bool IsOffsetMatch(Vector2Int start, Vector2Int end, int[][] offsetList)
    {
        int dx = end.x - start.x;
        int dy = end.y - start.y;
        foreach (var offset in offsetList)
        {
            if (offset[0]  == dx && offset[1] == dy)
                return true;
        }
        return false;
    }
    //只判断偏移方向，不看具体偏移量
    public static bool IsDirMatch(Vector2Int start, Vector2Int end, int[][] dieArray)
    {
        int rawDx = end.x - start.x;
        int rawDy = end.y - start.y;
        int dirX = rawDx == 0 ? 0 : rawDx/Mathf.Abs(rawDx);
        int dirY = rawDy == 0 ? 0 : rawDy/Mathf.Abs(rawDy);
        foreach (var dir in dieArray)
        {
            if (dir[0] == dirX && dir[1] == dirY)
                return true;
        }
        return false;
    }
}
