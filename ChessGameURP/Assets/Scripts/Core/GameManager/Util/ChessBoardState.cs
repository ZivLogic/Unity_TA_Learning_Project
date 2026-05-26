using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ChessBoardState
{
    //核心静态字典：二维坐标 -> 所属阵营
    public static Dictionary<(int x, int y), CampType> _gridCampDict = new();
    //棋盘坐标最大最小值
    public const int MinCoord = 0;
    public const int MaxCoord = 7;
    #region 基础校验
    //检验是否在棋盘内
    public static bool IsInBoard(int x, int y)
    {
        return x >= MinCoord && x <= MaxCoord && y >= MinCoord && y <= MaxCoord;
    }
    //当前格是否有棋子
    public static bool HasChess(int x, int y)
    {
        return _gridCampDict.ContainsKey((x,  y));
    }
    //获取格子内是否存在棋子
    public static CampType GetCamp(int x, int y)
    {
        _gridCampDict.TryGetValue((x, y), out var camp);
        return camp;
    }
    //判断目标格子是否是敌方棋子
    public static bool IsEnemy(int x, int y, CampType selfCamp)
    {
        if (!HasChess(x, y)) return false;
        return GetCamp(x, y) != selfCamp;
    }
    public static bool IsSelfCamp(int x, int y, CampType selfCamp)
    {
        if (!HasChess(x, y)) return false;
        return GetCamp(x, y) == selfCamp;
    }
    #endregion
    #region 坐标更新(移动核心：删旧键，加新键)
    //坐标更新是否成功
    public static bool UpdateChessPos(int oldX, int oldY, int newX, int newY, CampType camp)
    {
        //先校验新旧坐标都在棋盘内
        if ( ! IsInBoard(oldX, oldY) || ! IsInBoard(newX, newY))
            return false;
        //删除旧坐标
        _gridCampDict.Remove((oldX, oldY));
        //添加新坐标和阵营
        _gridCampDict[(newX, newY)] = camp;
        return true;
    }
    #endregion
    #region 初始化/清空
    //初始化单个棋子位置
    public static void InitChessPos(int x, int y, CampType camp)
    {
        if (IsInBoard(x, y))
        {
            _gridCampDict[(x, y)] = camp;
        }
    }
    //清空整个棋盘（棋盘重置）
    public static void ClearBoard()
    {
        _gridCampDict.Clear();
    }
    #endregion
}