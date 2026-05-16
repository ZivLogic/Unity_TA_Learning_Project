using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class ChessBoardConfig
{
    public float Length;                 //棋盘长
    public float Width;                  //棋盘宽
    public int TileNumberX;              //棋盘横格子数
    public int TileNumberZ;              //棋子竖格子数
    public Vector3 BoardInitPosition;    //棋盘初始世界位置
    public Vector3 TileInitPosition;     //棋盘格子第一个初始位置

    //逻辑调用，外部不写
    public bool IsPrefab = false;
    public bool IsList = false;
    public string ID = "ChessBoard";
}

