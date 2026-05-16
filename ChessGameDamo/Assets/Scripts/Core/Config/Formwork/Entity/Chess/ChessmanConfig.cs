
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



//单个棋子配置
[System.Serializable]
public class ChessmanConfig
{
    public string displayName;                //中文名
    public string type;                       //类型（一般为棋子名字）
    public int maxHp;                         //最大血量
    public int value;                         //棋子价值
    public int? maxMoveRange;                 //最大移动距离
    public bool canJump;                      //是否可跳跃
    public bool canMoveDiagonal;              //是否斜着走
    public int[][] moves;                     //移动时方向
    public bool canCapture;                   //是否可吃子
    public int[][] captureMoves;              //攻击范围方向
    public bool enPassantable;                //是否支持吃过路兵
    public bool canPromote;                   //是否可升变
    public List<string>? promoteOptions;      //升变选项
    public int? promoteRankWhite;             //升变排数（白方）
    public int? promoteRankBlack;             //升变排数（黑方）
}