using System.Collections;
using System.Collections.Generic;
using UnityEngine;



[System.Serializable]
public class ChessBoardTileConfig
{
    public float SizeX;           //棋盘格长
    public float SizeZ;           //棋盘格宽
    public float SizeY;           //棋子与棋盘距离

    //逻辑调用
    public bool IsPrefab = false;
    public bool IsList = false;
    public string ID = "ChessBoardTile";
}