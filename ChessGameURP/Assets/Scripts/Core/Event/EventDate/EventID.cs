using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventID
{
    //全局流程
    public const string GameInit = "GameInit";            //游戏初始化
    public const string GameStart = "GameStart";          //游戏开始
    public const string GamePause = "GamePause";          //游戏暂停
    public const string GameSave = "GameSave";            //游戏存档
    public const string GameExit = "GameExit";            //游戏结束

    //工厂相关
    public const string Factory_RequestChessConfig = "Factory_RequestChessConfig";                            //获取初始化配置组
    public const string Factory_RequestBoardConfig = "Factory_RequestBoardConfig";                            //获取棋盘配置
    public const string Factory_RequestBoardTileConfig = "Factory_RequestBoardTileConfig";                    //获取棋盘格配置
    public const string Factory_RequestChessmanPositionConfig = "Factory_RequestChessmanPositionConfig";      //获取棋子位置配置
    public const string Factory_RequestChessmanConfig = "Factory_RequestChessmanConfig";                      //获取棋子属性配置
    //public const string Factory_
    public const string Factory_CreateChessBoard = "Factory_CreateChessBoard ";                               //创建棋盘
    public const string Factory_CreateChessmanPosition = "Factory_CreateChessmanPosition";                    //创建棋子（位置）

    //配置相关
    public const string Config_ResponseChessConfig = "Config_ResponseChessConfig";                            //取出初始化配置组
    public const string Config_ResponseBoardConfig = "Config_ResponseBoardConfig";                            //取出棋盘配置
    public const string Config_ResponseBoardTileConfig = "Config_ResponseBoardTileConfig";                    //取出棋盘格配置
    public const string Config_ResponseChessmanPostionConfig = "Config_ResponseChessmanPostionConfig";        //取出棋子位置配置
    public const string Config_ResponseChessmanConfig = "Config_ResponseChessmanConfig";                      //取出棋子属性配置
}
