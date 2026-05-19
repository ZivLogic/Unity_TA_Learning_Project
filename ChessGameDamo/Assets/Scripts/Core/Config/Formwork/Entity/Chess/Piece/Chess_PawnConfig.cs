using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Chess_PawnConfig
{
    public string DisplayName;                //中文名
    public string Type;                       //类型（一般为棋子名字）
    public int MaxHp;                         //最大血量
    public int Value;                         //棋子价值
    public int MaxMoveRange;                 //最大移动距离
    public bool CanJump;                      //是否可跳跃
    public bool CanMoveDiagonal;              //是否斜着走
    public int[][] Moves;                     //移动时方向
    public bool CanCapture;                   //是否可吃子
    public int[][] CaptureMoves;              //攻击范围方向
    public bool EnPassantable;                //是否支持吃过路兵
    public bool CanPromote;                   //是否可升变
    public List<string> PromoteOptions;      //升变选项
    public int PromoteRankWhite;             //升变排数（白方）
    public int PromoteRankBlack;             //升变排数（黑方）

    public string ID = "ChessPawn";          //元配置专用
}
