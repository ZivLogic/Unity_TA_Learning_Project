using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ChessBoardLayoutData
{
    //格子坐标
    public static Dictionary<Vector2Int, Vector3> tilePositions {  get; private set; }

    //true = 白， false = 黑
    public static Dictionary<Vector2Int, bool> isWhiteTile {  get; private set; }

    //世界坐标列表储存
    public static List<Vector3> worldPositionsList { get; private set; }

    //世界坐标反查找逻辑坐标
    public static Dictionary<Vector3, Vector2Int> find2DPosDict { get; private set; }

    //世界坐标反查找逻辑黑白
    public static Dictionary<Vector3, bool> pos3DIsWhiteDict { get; private set; }

    //一次性初始化两套数据
    public static void Initialize(Dictionary<Vector2Int, Vector3> PositionDict,
        Dictionary<Vector2Int, bool> isWhiteDict,
        List<Vector3> worList,
        Dictionary<Vector3, Vector2Int> find2DPos,
        Dictionary<Vector3, bool> posWhitDict)
    {
        tilePositions = PositionDict;
        isWhiteTile = isWhiteDict;
        worldPositionsList = worList;
        find2DPosDict = find2DPos;
        pos3DIsWhiteDict = posWhitDict;
    }

    //清空
    public static void Clear()
    {
        tilePositions?.Clear();
        isWhiteTile?.Clear();
    }
}