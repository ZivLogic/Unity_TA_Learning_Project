using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//全局身份
public enum EntityIdentityType
{
    None,               //无
    ChessBoard,         //棋盘
    ChessTile,          //棋盘格
    ChessMan,           //棋子
    Ground,             //地面
    NPC                 //角色

}
//特殊身份
public enum EntitySpecialIdentityType
{
    None,
    ChessMan_Pawn,
    ChessMan_Rook,
    ChessMan_Knight,
    ChessMan_Bishop,
    ChessMan_Queen,
    ChessMan_King
}
