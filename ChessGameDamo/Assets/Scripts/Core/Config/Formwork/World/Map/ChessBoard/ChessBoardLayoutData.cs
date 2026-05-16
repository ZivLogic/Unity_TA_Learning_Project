using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ChessBoardLayoutData
{
    //格子坐标
    public static Dictionary<Vector2Int, Vector3> tilePositions {  get; private set; }

    //true = 白， false = 黑
    public static Dictionary<Vector2Int, bool> isWhiteTile {  get; private set; }

    //一次性初始化两套数据
    public static void Initialize(Dictionary<Vector2Int, Vector3> PositionDict, Dictionary<Vector2Int, bool> isWhiteDict)
    {
        tilePositions = PositionDict;
        isWhiteTile = isWhiteDict;
    }

    //清空
    public static void Clear()
    {
        tilePositions?.Clear();
        isWhiteTile?.Clear();
    }
}