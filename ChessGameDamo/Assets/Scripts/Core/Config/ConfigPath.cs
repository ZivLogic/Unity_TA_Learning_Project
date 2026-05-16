using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ConfigPath
{
    //初始元数据文件
    public const string GAME_CONFIGINDEX = "Config/Json/GameConfigIndex";    //初始中央元配置

    //Json文件
    public const string CHESS_MAN = "Config/Json/Chess/ChessmanJson";                       //棋子的配置属性
    public const string CHESS_BOARD = "Config/Json/Chess/ChessBoardJson";                   //棋盘的配置属性
    public const string CHESS_TILE = "Config/Json/Chess/ChessBoardTileJson";                //棋盘格子的配置属性
    public const string CHESS_MAN_POSITION = "Config/Json/Chess/ChessmanPositionJson";      //棋子相对于棋盘位置




    //预设体
    public const string CHESS_MAN_PREFABS = "Config/Prefabs/Entity/Chessman";                         //棋子预设体
    public const string CHESS_BOARD_PREFAB = "Config/Prefabs/Entity/ChessBoard/ChessBoardTest2";      //棋盘预设体
}