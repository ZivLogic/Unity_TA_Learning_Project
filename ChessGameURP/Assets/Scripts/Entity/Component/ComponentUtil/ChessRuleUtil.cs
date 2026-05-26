using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ChessRuleUtil
{
    //获取两点之间所有的中间路径格子（不含起点，含中点）
    public static List<Vector2Int> GetPathCells(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        if (start == end) return path;
        int dx = end.x - start.x;
        int dy = end.y - start.y;
        int stepX = dx == 0 ? 0 : dx / Mathf.Abs(dx);
        int stepY = dy == 0 ? 0 : dy / Mathf.Abs(dy);
        Vector2Int cur = start;
        while (cur != end)
        {
            cur.x += stepX;
            cur.y += stepY;
            path.Add(cur);
        }
        return path;
    }
    //检测路径上是否有棋子阻挡（马除外）
    public static bool IsPathBlocked(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> path = GetPathCells(start, end);
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
}
