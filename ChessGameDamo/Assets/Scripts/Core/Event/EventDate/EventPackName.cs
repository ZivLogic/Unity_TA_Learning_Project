using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventPackName
{
    public const string CHESSBOARD_CONFIG = "CHESSBOARD_CONFIG";                  //棋盘属性配置
    public const string CHESSBOARDTILE_CONFIG = "CHESSBOARDTILE_CONFIG";          //棋盘格属性配置
    public const string CHESSMAN_CONFIG = "CHESSMAN_CONFIG";                      //棋子属性配置
    public const string CHESSMAN_POSITIONCONFIG = "CHESSMAN_POSITIONCONFIG";      //棋子位置配置
    public const string CHESSBOARD_PREFAB = "CHESSBOARD_PREFAB";                  //棋盘预设体配置
    public const string CHESSMAN_PREFABS = "CHESSMAN_PREFABS";                    //棋子预设体配置
    public const string CHESSBOARD_ISPREFAB = "CHESSBOARD_ISPREFAB";              //棋盘取得预设体
    public const string CHESSMAN_ISPREFABS = "CHESSMAN_ISPREFABS";                //棋子取得预设体
    public const string CHESSMAN_ISWHITE = "CHESSMAN_ISWHITE";                    //棋子阵营
    public const string INPUT_GETCONFIG = "INPUT_GETCONFIG";                      //获取输入配置
    public const string INPUT_SELECTCONFIG = "INPUT_SELECTCONFIG";                //获取选择物体配置
    public const string INPUT_INTERCEPTORCONFIG = "INPUT_INTERCEPTORCONFIG";      //获取输入拦截层配置
    public const string INPUT_ACTIONKEY = "INPUT_ACTIONkEY";                      //键盘事件转枚举类型
    public const string INPUT_ACTIONMOUSE = "INPUT_ACTIONMOUSE";                  //鼠标事件转枚举类型
    public const string INPUT_MOUSEPOS2D = "INPUT_MOUSEPOS2D";                    //鼠标位置2D
    public const string INPUT_MOUSEPOS3D = "INPUT_MOUSEPOS3D";                    //鼠标位置3D
    public const string INPUT_MOUSESELECT = "INPUT_MOUSESELECT";                  //鼠标选中物体
    //规范用语，统一为：发布方_方法名_值名称
    public const string Test_string = "test";
    public const string EntityManager_GetEntityConfigClassID_EntityIDConfig = "EntityManager_GetEntityConfigClassID_EntityIDConfig";
    public const string EntityManager_GetEntityConfigClassID_EntityIDSpecialConfig = "EntityManager_GetEntityConfigClassID_EntityIDSpecialConfig";
    public const string EntityManager_GetEntityConfigClassID_PhysicsValueConfig = "EntityManager_GetEntityConfigClassID_PhysicsValueConfig";
    public const string EntityManager_GetEntityConfigClassID_PhysicsComponentConfig = "EntityManager_GetEntityConfigClassID_PhysicsComponentConfig";
    public const string ConfigLogic_InitEntityIDConfig_EntityIDConfig = "ConfigLogic_InitEntityIDConfig_EntityIDConfig";
    public const string ConfigLogic_InitEntityIDConfig_EntityIDConfigSpecial = "ConfigLogic_InitEntityIDConfig_EntityIDConfigSpecial";
    public const string ConfigLogic_InitEntityIDConfig_PhysicsValueConfig = "ConfigLogic_InitEntityIDConfig_PhysicsValueConfig";
    public const string ConfigLogic_InitEntityIDConfig_PhysicsComponentConfig = "ConfigLogic_InitEntityIDConfig_PhysicsComponentConfig";
}