using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConfigFile : MonoBehaviour
{
    //配置编写脚本
    //模板编写如下
    private void TextConfig()
    {
        string path = @"E:/JsonFile/Unity/ChessGame/TextJson__Game.json";
        var builder = new ConfigBuilder();

        builder.Group("GameInit")
            .Add("Config", 330)
            .Add("Speed", 5.5f)
            // 普通字典
            .Add("Dict", new Dictionary<string, object>()
            {
            {"HP",100 },
            {"Damage", 25 }
            })
            // List<int>
            .Add("IdList", new List<int> { 1, 2, 3, 4 })
            // List<string>
            .Add("NameList", new List<string> { "棋子A", "棋子B", "棋子C" })
            // float数组
            .Add("PosArray", new float[] { 0.5f, 1.2f, 3.0f })
            .Add("IsOpen", false);

        // 首次生成、后续不覆盖
        builder.ExportToFileFormatIfNotExist(path);

        // 需要更新配置就换成这行：
        // builder.ExportToFileFormat_Overwrite(path);
    }

    private void TextConfigWrite()
    {
        #region 测试移动配置
        //定义路径
        string TestPath001 = @"E:/JsonFile/Unity/ChessGame/InputInterceptorJson.json";
        //构造配置
        var builder = new ConfigBuilder();
        builder.Group("CutSceneGloball")
            .Add("Priority", 10)
            .Add("IsEnable", true)
            .Group("ContextLimit")
            .Add("Priority", 100)
            .Add("IsEnable", true)
            .Group("UIOcclude")
            .Add("Priority", 50)
            .Add("IsEnable", true);
        builder.ExportToFileFormat_Overwrite(TestPath001);
        #endregion
        #region 事件监听路由配置
        //事件监听路由
        string EventRoutePath = @"E:/JsonFile/Unity/ChessGame/EventRouteJson.json";
        var eventRoute = new ConfigBuilder();
        eventRoute
            //事件结构[系统]_[方法]_[事件名]
            //系统：订阅事件的系统
            //方法：订阅系统里的对应回调方法
            //事件名；监听事件具体名称

            //ConfigLogic事件
            .Group("ConfigLogic_InitChessConfig_InitChessEvent")                                            //组名称
            .Add("SystemID", "ConfigLogic")                                                                 //系统名称
            .Add("EventTypeFullName", "ConfigLogic_InitChessConfig_InitChessEvent")                         //事件名
            .Add("HandlerMethodName", "InitChessConfig")                                                    //事件回调方法
            .Add("QueueType", "Logic")                                                                      //事件队列
            .Add("IsEnable", "true")                                                                        //是否启用该事件链
                                                                                                            //.Add("ControlPublisher", "false")                                                               //是否管控发布方

            .Group("ConfigLogic_OnInputConfig_InitInputConfig")
            .Add("SystemID", "ConfigLogic")
            .Add("EventTypeFullName", "ConfigLogic_OnInputConfig_InitInputConfig")
            .Add("HandlerMethodName", "OnInputConfig")
            .Add("QueueType", "Logic")
            .Add("IsEnable", "true")
            //.Add("ControlPublisher", "false")

            //FactoryLogic事件
            .Group("FactoryLogic_OnResponseChessConfig_InitChessConfig")
            .Add("SystemID", "FactoryLogic")
            .Add("EventTypeFullName", "FactoryLogic_OnResponseChessConfig_InitChessConfig")
            .Add("HandlerMethodName", "OnResponseChessConfig")
            .Add("QueueType", "Logic")
            .Add("IsEnable", "true")

            .Group("FactoryLogic_OnResponseChessPrefabs_AssetInitPrefabsLoadAll")
            .Add("SystemID", "FactoryLogic")
            .Add("EventTypeFullName", "FactoryLogic_OnResponseChessPrefabs_AssetInitPrefabsLoadAll")
            .Add("HandlerMethodName", "OnResponseChessPrefabs")
            .Add("QueueType", "Logic")
            .Add("IsEnable", "true")

            //AssetsLogic事件
            .Group("AssetsLogic_OnInitCrateChessPrefabs_CreateInitChessPrefabs")
            .Add("SystemID", "AssetsLogic")
            .Add("EventTypeFullName", "AssetsLogic_OnInitCrateChessPrefabs_CreateInitChessPrefabs")
            .Add("HandlerMethodName", "OnInitCrateChessPrefabs")
            .Add("QueueType", "Logic")
            .Add("IsEnable", "true")

            //InputLogic事件
            .Group("InputLogic_OnConfig_IsInputConfig")
            .Add("SystemID", "InputLogic")
            .Add("EventTypeFullName", "InputLogic_OnConfig_IsInputConfig")
            .Add("HandlerMethodName", "OnConfig")
            .Add("QueueType", "Logic")
            .Add("IsEnable", "true")

            //新
            //配置系统监听
            .Group("FactoryPublish_GetIdentityConfig_IdConfig")
            .Add("SystemID", "ConfigLogic")
            .Add("EventTypeFullName", "FactoryPublish_GetIdentityConfig_IdConfig")
            .Add("HandlerMethodName", "InitEntityIDConfig")
            .Add("QueueType", "Logic")
            .Add("IsEnable", "true")

            .Group("EntityPublish_GetChessManComponentConfig_GetChessManConfig")
            .Add("SystemID", "ConfigLogic")
            .Add("EventTypeFullName", "EntityPublish_GetChessManComponentConfig_GetChessManConfig")
            .Add("HandlerMethodName", "GetChessManComponentConfig")
            .Add("QueueType", "Logic")
            .Add("IsEnable", "true")

            //工厂系统监听
            .Group("ConfigPublish_OnIdentityConfig_IdConfig")
            .Add("SystemID", "FactoryLogic")
            .Add("EventTypeFullName", "ConfigPublish_OnIdentityConfig_IdConfig")
            .Add("HandlerMethodName", "OnEntityIDConfig")
            .Add("QueueType", "Logic")
            .Add("IsEnable", "true")

            //实体系统监听
            .Group("ConfigPulish_GetChessManComponentConfig_GetChessManConfig")
            .Add("SystemID", "EntityLogic")
            .Add("EventTypeFullName", "ConfigPulish_GetChessManComponentConfig_GetChessManConfig")
            .Add("HandlerMethodName", "GetChessManConfig")
            .Add("QueueType", "Logic")
            .Add("IsEnable", "true");

        eventRoute.ExportToFileFormat_Overwrite(EventRoutePath);
        #endregion
        #region 事件发布路由配置
        //事件发射路由
        string EventPublishAllowJson = @"E:/JsonFile/Unity/ChessGame/EventTriggerRouteJson.json";
        var eventPublishAllow = new ConfigBuilder();
        eventPublishAllow
            //事件结构[系统]_[方法]_[事件名]
            //系统：发布事件的系统
            //方法：发布系统里的对应回调方法
            //事件名；监听事件具体名称

            //ConfigPublish
            .Group("AssetsLogic_OnInitCrateChessPrefabs_CreateInitChessPrefabs")                                  //顶层名
            .Add("SystemID", "ConfigPublish")                                                                     //系统ID
            .Add("EventTypeFullName", "AssetsLogic_OnInitCrateChessPrefabs_CreateInitChessPrefabs")               //事件类别
            .Add("HandlerMethodName", "AssetsLogic_OnInitCrateChessPrefabs_CreateInitChessPrefabs")               //方法名
            .Add("QueueType", "Logic")                                                                            //事件队列
            .Add("IsEnable", "true")                                                                              //是否启用

            .Group("FactoryLogic_OnResponseChessConfig_InitChessConfig")
            .Add("SystemID", "ConfigPublish")
            .Add("EventTypeFullName", "FactoryLogic_OnResponseChessConfig_InitChessConfig")
            .Add("HandlerMethodName", "FactoryLogic_OnResponseChessConfig_InitChessConfig")
            .Add("QueueType", "Logic")
            .Add("IsEnable", "true")

            .Group("InputLogic_OnConfig_IsInputConfig")
            .Add("SystemID", "ConfigPublish")
            .Add("EventTypeFullName", "InputLogic_OnConfig_IsInputConfig")
            .Add("HandlerMethodName", "InputLogic_OnConfig_IsInputConfig")
            .Add("QueueType", "Logic")
            .Add("IsEnable", "true")

            //FactoryPublish
            .Group("ConfigLogic_InitChessConfig_InitChessEvent")
            .Add("SystemID", "FactoryPublish")
            .Add("EventTypeFullName", "ConfigLogic_InitChessConfig_InitChessEvent")
            .Add("HandlerMethodName", "ConfigLogic_InitChessConfig_InitChessEvent")
            .Add("QueueType", "Logic")
            .Add("IsEnable", "true")

            //AssetsPublish
            .Group("FactoryLogic_OnResponseChessPrefabs_AssetInitPrefabsLoadAll")
            .Add("SystemID", "AssetsPublish")
            .Add("EventTypeFullName", "FactoryLogic_OnResponseChessPrefabs_AssetInitPrefabsLoadAll")
            .Add("HandlerMethodName", "FactoryLogic_OnResponseChessPrefabs_AssetInitPrefabsLoadAll")
            .Add("QueueType", "Logic")
            .Add("IsEnable", "true")

            //InputPublish
            .Group("ConfigLogic_OnInputConfig_InitInputConfig")
            .Add("SystemID", "InputPublish")
            .Add("EventTypeFullName", "ConfigLogic_OnInputConfig_InitInputConfig")
            .Add("HandlerMethodName", "ConfigLogic_OnInputConfig_InitInputConfig")
            .Add("QueueType", "Logic")
            .Add("IsEnable", "true")

            //新
            //工厂系统发布
            .Group("FactoryPublish_GetIdentityConfig_IdConfig")
            .Add("SystemID", "FactoryPublish")
            .Add("EventTypeFullName", "FactoryPublish_GetIdentityConfig_IdConfig")
            .Add("HandlerMethodName", "GetIdentityConfig")
            .Add("QueueType", "Logic")
            .Add("IsEnable", "true")

            //配置系统发布
            .Group("ConfigPublish_OnIdentityConfig_IdConfig")
            .Add("SystemID", "ConfigPublish")
            .Add("EventTypeFullName", "ConfigPublish_OnIdentityConfig_IdConfig")
            .Add("HandlerMethodName", "OnIdentityConfig")
            .Add("QueueType", "Logic")
            .Add("IsEnable", "true")

            .Group("ConfigPulish_GetChessManComponentConfig_GetChessManConfig")
            .Add("SystemID", "ConfigPublish")
            .Add("EventTypeFullName", "ConfigPulish_GetChessManComponentConfig_GetChessManConfig")
            .Add("HandlerMethodName", "GetChessManComponentConfig")
            .Add("QueueType", "Logic")
            .Add("IsEnable", "true")

            //实体系统发布
            .Group("EntityPublish_GetChessManComponentConfig_GetChessManConfig")
            .Add("SystemID", "EntityPublish")
            .Add("EventTypeFullName", "EntityPublish_GetChessManComponentConfig_GetChessManConfig")
            .Add("HandlerMethodName", "GetChessManComponentConfig")
            .Add("QueueType", "Logic")
            .Add("IsEnable", "true");
        eventPublishAllow.ExportToFileFormat_Overwrite(EventPublishAllowJson);
        #endregion
        #region 元配置
        //元配置表
        string GameConfig = @"E:/JsonFile/Unity/GameConfigIndex.json";
        var Game = new ConfigBuilder();

        Game

            .Group("ChessBoard")                                                 //顶层：表名字，想获取对应的表配置就写对应的表配置ID
            .Add("ConfigType", "ChessBoardConfig, Assembly-CSharp")              //类型：配置类型/配置模板
            .Add("ConfigPath", "Config/Json/Chess/ChessBoardJson")               //路径：配置所在文件路劲
            .Add("IsTable", false)                                               //是不是表，让配置可被区分
            .Add("IsEnable", true)                                               //是否启用（拓展，是否启用配置）

            .Group("ChessBoardTile")
            .Add("ConfigType", "ChessBoardTileConfig, Assembly-CSharp")
            .Add("ConfigPath", "Config/Json/Chess/ChessBoardTileJson")
            .Add("IsTable", false)
            .Add("IsEnable", true)

            .Group("ChessPawn")
            .Add("ConfigType", "Chess_PawnConfig, Assembly-CSharp")
            .Add("ConfigPath", "Config/Json/Chess/Piece/ChessPawnJson")
            .Add("IsTable", false)
            .Add("IsEnable", true)

            .Group("ChessRook")
            .Add("ConfigType", "Chess_RookConfig, Assembly-CSharp")
            .Add("ConfigPath", "Config/Json/Chess/Piece/ChessRookJson")
            .Add("IsTable", false)
            .Add("IsEnable", true)

            .Group("ChessKnight")
            .Add("ConfigType", "Chess_KnightConfig, Assembly-CSharp")
            .Add("ConfigPath", "Config/Json/Chess/Piece/ChessKnightJson")
            .Add("IsTable", false)
            .Add("IsEnable", true)

            .Group("ChessBishop")
            .Add("ConfigType", "Chess_BishopConfig, Assembly-CSharp")
            .Add("ConfigPath", "Config/Json/Chess/Piece/ChessBishopJson")
            .Add("IsTable", false)
            .Add("IsEnable", true)

            .Group("ChessQueen")
            .Add("ConfigType", "Chess_QueenConfig, Assembly-CSharp")
            .Add("ConfigPath", "Config/Json/Chess/Piece/ChessQueenJson")
            .Add("IsTable", false)
            .Add("IsEnable", true)

            .Group("ChessKing")
            .Add("ConfigType", "Chess_KingConfig, Assembly-CSharp")
            .Add("ConfigPath", "Config/Json/Chess/Piece/ChessKingJson")
            .Add("IsTable", false)
            .Add("IsEnable", true)

            .Group("ChessmanPosition")
            .Add("ConfigType", "ChessmanPositionConfig, Assembly-CSharp")
            .Add("ConfigPath", "Config/Json/Chess/ChessmanPositionJson")
            .Add("IsTable", true)
            .Add("IsEnable", true)

            .Group("ChessmanPrefabs")
            .Add("ConfigType", "ChessmanPrefabsConfig, Assembly-CSharp")
            .Add("ConfigPath", "Config/Json/Prefabs/ChessmanPrefabsJson")
            .Add("IsTable", true)
            .Add("IsEnable", true)

            .Group("ChessBoardPrefab")
            .Add("ConfigType", "ChessBoardPrefabConfig, Assembly-CSharp")
            .Add("ConfigPath", "Config/Json/Prefabs/ChessBoardPrefabJson")
            .Add("IsTable", true)
            .Add("IsEnable", true)

            .Group("ChessTilePrefabs")
            .Add("ConfigType", "ChessTilePrefabsConfig, Assembly-CSharp")
            .Add("ConfigPath", "Config/Json/Prefabs/ChessTilePrefabJson")
            .Add("IsTable", true)
            .Add("IsEnable", true)

            .Group("InputKey")
            .Add("ConfigType", "InputKeyConfig, Assembly-CSharp")
            .Add("ConfigPath", "Config/Json/Input/InputKeyJson")
            .Add("IsTable", true)
            .Add("IsEnable", true)

            .Group("SelectObject")
            .Add("ConfigType", "SelectObjectConfig, Assembly-CSharp")
            .Add("ConfigPath", "Config/Json/Input/SelectObjectJson")
            .Add("IsTable", true)
            .Add("IsEnable", true)

            .Group("InputInterceptor")
            .Add("ConfigType", "InputInterceptorConfig, Assembly-CSharp")
            .Add("ConfigPath", "Config/Json/Input/InputInterceptorJson")
            .Add("IsTable", true)
            .Add("IsEnable", true)

            .Group("EventRoute")
            .Add("ConfigType", "EventRouteItem, Assembly-CSharp")
            .Add("ConfigPath", "Config/Json/Event/EventRouteJson")
            .Add("IsTable", true)
            .Add("IsEnable", true)

            .Group("EventTriggerRoute")
            .Add("ConfigType", "EventTriggerRouteItem, Assembly-CSharp")
            .Add("ConfigPath", "Config/Json/Event/EventTriggerRouteJson")
            .Add("IsTable", true)
            .Add("IsEnable", true)

            .Group("EntityID")
            .Add("ConfigType", "EntityIDConfig, Assembly-CSharp")
            .Add("ConfigPath", "Config/Json/EntityID/EntityIDJson")
            .Add("IsTable", true)
            .Add("IsEnable", true)

            .Group("EntityID_Special")
            .Add("ConfigType", "EntityIDConfig_Special, Assembly-CSharp")
            .Add("ConfigPath", "Config/Json/EntityID/EntityID_SpecialJson")
            .Add("IsTable", true)
            .Add("IsEnable", true)

            .Group("PhysicsValue")
            .Add("ConfigType", "PhysicsValueConfig, Assembly-CSharp")
            .Add("ConfigPath", "Config/Json/EntityID/PhysicsValueJson")
            .Add("IsTable", true)
            .Add("IsEnable", true)

            .Group("PhysicsComponent")
            .Add("ConfigType", "PhysicsComponentConfig, Assembly-CSharp")
            .Add("ConfigPath", "Config/Json/EntityID/PhysicsIDComponentJson")
            .Add("IsTable", true)
            .Add("IsEnable", true)

            .Group("RenderMajorID")
            .Add("ConfigType", "RenderMajorIDConfig, Assembly-CSharp")
            .Add("ConfigPath", "Config/Json/EntityID/RenderMajorJson")
            .Add("IsTable", true)
            .Add("IsEnable", true)

            .Group("RenderMinorID")
            .Add("ConfigType", "RenderMinorIDConfig, Assembly-CSharp")
            .Add("ConfigPath", "Config/Json/EntityID/RenderMinorJson")
            .Add("IsTable", true)
            .Add("IsEnable", true);

        Game.ExportToFileFormat_Overwrite(GameConfig);
        #endregion
        #region 预设体配置
        //棋盘预设体
        string ChessBoardPrefab = @"E:/JsonFile/Unity/ChessGame/ChessBoardPrefabJson.json";
        var chessBoardPrefab = new ConfigBuilder();
        chessBoardPrefab
            .Group("ChessBoardPrefab")
            .Add("Path", "Config/Prefabs/Entity/ChessBoard/ChessBoardTest2")
            .Add("ClassID", "ChessBoard");
        chessBoardPrefab.ExportToFileFormat_Overwrite(ChessBoardPrefab);

        //棋子预设体
        string ChessmanPrefabs = @"E:/JsonFile/Unity/ChessGame/ChessmanPrefabsJson.json";
        var chessmanPrefabs = new ConfigBuilder();
        chessmanPrefabs
            .Group("ChessmanPrefabs")
            .Add("Path", "Config/Prefabs/Entity/ChessBoard/ChessBoardTest2")
            .Add("ClassID", "ChessMan");
        chessmanPrefabs.ExportToFileFormat_Overwrite(ChessmanPrefabs);
        #endregion
        #region 实体类ID配置
        string EntityID = @"E:/JsonFile/Unity/ChessGame/EntityIDJson.json";
        var entityID = new ConfigBuilder();
        entityID
            //棋盘实体
            .Group("ChessBoard")
            .Add("AttrTable", new Dictionary<string, object>()                  //通用属性表
            {
                {"Name", "棋盘"},
                {"CanSelect", false}
            })
            .Add("ClassIDTable", new List<string>()                             //绑定脚本列表
            {
                "TestCS"
            })
            .Add("WorldPosition", new Dictionary<string, object>()               //父级空物体世界坐标
            {
                {"X", 0.0f },
                {"Y", 0.2f },
                {"Z", 0.0f }
            })
            .Add("ParentRotAtion", new Dictionary<string, object>()             //偏移量
            {
                {"X", 0.0f },
                {"Y", 0.0f },
                {"Z", 0.0f }
            })
            .Add("IsEnable", true)
            //棋子实体
            .Group("ChessMan")
            .Add("AttrTable", new Dictionary<string, object>()
            {
                {"Name", "棋子"},
                {"CanSelect", true}
            })
            .Add("ClassIDTable", new List<string>()
            {
                "TestCS"
            })
            .Add("WorldPosition", new Dictionary<string, object>()               //如果类为复数则不计算坐标，外部定义新坐标
            {
                {"X", 0.0f },
                {"Y", 0.0f },
                {"Z", 0.0f }
            })
            .Add("ParentRotAtion", new Dictionary<string, object>()             //偏移量
            {
                {"X", 0.0f },
                {"Y", 0.0f },
                {"Z", 0.0f }
            })
            .Add("IsEnable", true)
            //棋盘格
            .Group("ChessTile")
            .Add("AttrTable", new Dictionary<string, object>()
            {
                {"Name", "棋盘格"},
                {"CanSelect", false}
            })
            .Add("ClassIDTable", new List<string>()
            {
                "TestCS",
                " ChessTileLogic"
            })
            .Add("WorldPosition", new Dictionary<string, object>()               //如果类为复数则不计算坐标，外部定义新坐标
            {
                {"X", 0.0f },
                {"Y", 0.0f },
                {"Z", 0.0f }
            })
            .Add("ParentRotAtion", new Dictionary<string, object>()             //偏移量
            {
                {"X", 0.0f },
                {"Y", 0.0f },
                {"Z", 0.0f }
            })
            .Add("IsEnable", true);
        entityID.ExportToFileFormat_Overwrite(EntityID);
        #endregion
        #region 特殊实体ID配置
        string EntitySpecialID = @"E:/JsonFile/Unity/ChessGame/EntityID_SpecialJson.json";
        var entitySpecialID = new ConfigBuilder();
        entitySpecialID
            //棋子类
            //兵
            .Group("ChessMan_Pawn")                                       //顶层名字对应身份ID枚举
            .Add("AttrTable", new Dictionary<string, object>()            //属性表
            {
                {"Name", "兵"}
            })
            .Add("ClassIDTable", new List<string>()                       //挂载脚本的脚本ID列表
            {
                "TestCS"
            })
            .Add("ParentName", "ChessMan")                                //父级名字
            .Add("IsEnable", true)                                        //是否启用
            //车
            .Group("ChessMan_Rook")
            .Add("AttrTable", new Dictionary<string, object>()
            {
                {"Name", "车"}
            })
            .Add("ClassIDTable", new List<string>()
            {
                "TestCS"
            })
            .Add("ParentName", "ChessMan")
            .Add("IsEnable", true)
            //马
            .Group("ChessMan_Knight")
            .Add("AttrTable", new Dictionary<string, object>()
            {
                {"Name", "马"}
            })
            .Add("ClassIDTable", new List<string>()
            {
                "TestCS"
            })
            .Add("ParentName", "ChessMan")
            .Add("IsEnable", true)
            //象
            .Group("ChessMan_Bishop")
            .Add("AttrTable", new Dictionary<string, object>()
            {
                {"Name", "象"}
            })
            .Add("ClassIDTable", new List<string>()
            {
                "TestCS"
            })
            .Add("ParentName", "ChessMan")
            .Add("IsEnable", true)
            //后
            .Group("ChessMan_Queen")
            .Add("AttrTable", new Dictionary<string, object>()
            {
                {"Name", "后"}
            })
            .Add("ClassIDTable", new List<string>()
            {
                "TestCS"
            })
            .Add("ParentName", "ChessMan")
            .Add("IsEnable", true)
            //王
            .Group("ChessMan_King")
            .Add("AttrTable", new Dictionary<string, object>()
            {
                {"Name", "王"}
            })
            .Add("ClassIDTable", new List<string>()
            {
                "TestCS"
            })
            .Add("ParentName", "ChessMan")
            .Add("IsEnable", true);
        entitySpecialID.ExportToFileFormat_Overwrite(EntitySpecialID);
        #endregion
        #region 物理加载优先级配置
        string PhysicsValue = @"E:/JsonFile/Unity/ChessGame/PhysicsValueJson.json";
        var physicsValue = new ConfigBuilder();
        physicsValue

            .Group("Test1")                             //顶层名，无要求
            .Add("Priority", 0)                         //优先级，数值越小越快加载
            .Add("IDTable", new List<string>()          //需要在这个优先级加载的身份ID表
            {
                "ChessBoard"
                
            })
            .Add("IsEnable", true)                      //是否启用

            .Group("Test2")
            .Add("Priority", 1)
            .Add("IDTable", new List<string>()
            {
                "ChessTile"
            })
            .Add("IsEnable", true)

            .Group("Test3")
            .Add("Priority", 2)
            .Add("IDTable", new List<string>()
            {
                "ChessMan_Pawn",
                "ChessMan_Rook",
                "ChessMan_Knight",
                "ChessMan_Bishop",
                "ChessMan_Queen",
                "ChessMan_King"
            })
            .Add("IsEnable", true);
        physicsValue.ExportToFileFormat_Overwrite(PhysicsValue);
        #endregion
        #region 物理实体组件配置
        string PhysicsID = @"E:/JsonFile/Unity/ChessGame/PhysicsIDComponentJson.json";
        var physicsID = new ConfigBuilder();
        physicsID
            .Group("ChessBoard")                                             //顶层名对应身份枚举ID
            .Add("ComponentTable", new Dictionary<string, object>()          //组件字典
            {
                {"Box", new Dictionary<string , object>()
                {
                    {"Center", new List<float> {
                        0f,
                        0.04f,
                        0f
                    } },
                    {"Size", new List<float>{
                        6.24f,
                        0.16f,
                        6.24f
                    } },
                    {"IsTrigger", false }
                }}
            })

            .Group("ChessTile")                                             
            .Add("ComponentTable", new Dictionary<string, object>()          
            {
                {"Box", new Dictionary<string , object>()
                {
                    {"Center", new List<float> {
                        0f,
                        0.02f,
                        0f
                    } },
                    {"Size", new List<float>{
                        0.78f,
                        0.04f,
                        0.78f
                    } },
                    {"IsTrigger", false }
                }}
            })

            .Group("ChessMan_Pawn")
             .Add("ComponentTable", new Dictionary<string, object>()
            {
                {"Capsule", new Dictionary<string , object>()
                {
                    {"Center", new List<float> {
                        0f,
                        0.35f,
                        0f
                    } },
                    {"Radius", 0.2 },
                    {"Height", 0.7 },
                    {"IsTrigger", false }
                }},
                 {"Rigidbody", new Dictionary<string, object>()
                 {
                     {"Mass", 1 },
                     {"UseGravity", true },
                     {"IsKinematic", false }
                 } }
            })

             .Group("ChessMan_Rook")
             .Add("ComponentTable", new Dictionary<string, object>()
            {
                {"Capsule", new Dictionary<string , object>()
                {
                    {"Center", new List<float> {
                        0f,
                        0.38f,
                        0f
                    } },
                    {"Radius", 0.24 },
                    {"Height", 0.8 },
                    {"IsTrigger", false }
                }},
                 {"Rigidbody", new Dictionary<string, object>()
                 {
                     {"Mass", 1 },
                     {"UseGravity", true },
                     {"IsKinematic", false }
                 } }
            })

             .Group("ChessMan_Knight")
             .Add("ComponentTable", new Dictionary<string, object>()
            {
                {"Capsule", new Dictionary<string , object>()
                {
                    {"Center", new List<float> {
                        0f,
                        0.4f,
                        0f
                    } },
                    {"Radius", 0.24 },
                    {"Height", 0.82 },
                    {"IsTrigger", false }
                }},
                 {"Rigidbody", new Dictionary<string, object>()
                 {
                     {"Mass", 1 },
                     {"UseGravity", true },
                     {"IsKinematic", false }
                 } }
            })

             .Group("ChessMan_Bishop")
             .Add("ComponentTable", new Dictionary<string, object>()
            {
                {"Capsule", new Dictionary<string , object>()
                {
                    {"Center", new List<float> {
                        0f,
                        0.49f,
                        0f
                    } },
                    {"Radius", 0.26 },
                    {"Height", 1 },
                    {"IsTrigger", false }
                }},
                 {"Rigidbody", new Dictionary<string, object>()
                 {
                     {"Mass", 1 },
                     {"UseGravity", true },
                     {"IsKinematic", false }
                 } }
            })

             .Group("ChessMan_Queen")
             .Add("ComponentTable", new Dictionary<string, object>()
            {
                {"Capsule", new Dictionary<string , object>()
                {
                    {"Center", new List<float> {
                        0f,
                        0.51f,
                        0f
                    } },
                    {"Radius", 0.26 },
                    {"Height", 1.04 },
                    {"IsTrigger", false }
                }},
                 {"Rigidbody", new Dictionary<string, object>()
                 {
                     {"Mass", 1 },
                     {"UseGravity", true },
                     {"IsKinematic", false }
                 } }
            })

             .Group("ChessMan_King")
             .Add("ComponentTable", new Dictionary<string, object>()
            {
                {"Capsule", new Dictionary<string , object>()
                {
                    {"Center", new List<float> {
                        0f,
                        0.62f,
                        0f
                    } },
                    {"Radius", 0.31 },
                    {"Height", 1.25 },
                    {"IsTrigger", false }
                }},
                 {"Rigidbody", new Dictionary<string, object>()
                 {
                     {"Mass", 1 },
                     {"UseGravity", true },
                     {"IsKinematic", false }
                 } }
            });
        physicsID.ExportToFileFormat_Overwrite(PhysicsID);
        #endregion
        #region 渲染ID配置(大类)
        string RenderMaajor = @"E:/JsonFile/Unity/ChessGame/RenderMajorJson.json";
        var renderMajor = new ConfigBuilder();
        renderMajor
            .Group("ChessBard_Model")
            .Add("ComponentID", new List<string>
            {
                "TestCS"
            })
            .Add("IsEnale", true)

            .Group("ChessMan_Model")
            .Add("ComponentID", new List<string>
            {
                "TestCS"
            })
            .Add("IsEnale", true)

             .Group("ChessTile_Model")
            .Add("ComponentID", new List<string>
            {
                "TestCS"
            })
            .Add("IsEnale", true);
        renderMajor.ExportToFileFormat_Overwrite (RenderMaajor);
        #endregion
        #region 渲染ID配置(中类)
        string RenderMinor = @"E:/JsonFile/Unity/ChessGame/RenderMinorJson.json";
        var renderMinor = new ConfigBuilder();
        renderMinor
            .Group("ChessMan_Pawn_Model")
            .Add("ComponentID", new List<string>
            {
                "TestCS"
            })
            .Add("IsEnable", true)

            .Group("ChessMan_Rook_Model")
            .Add("ComponentID", new List<string>
            {
                "TestCS"
            })
            .Add("IsEnable", true)

            .Group("ChessMan_Knight_Model")
            .Add("ComponentID", new List<string>
            {
                "TestCS"
            })
            .Add("IsEnable", true)

            .Group("ChessMan_Bishop_Model")
            .Add("ComponentID", new List<string>
            {
                "TestCS"
            })
            .Add("IsEnable", true)

            .Group("ChessMan_Queen_Model")
            .Add("ComponentID", new List<string>
            {
                "TestCS"
            })
            .Add("IsEnable", true)

            .Group("ChessMan_King_Model")
            .Add("ComponentID", new List<string>
            {
                "TestCS"
            })
            .Add("IsEnable", true);
        renderMinor.ExportToFileFormat_Overwrite (RenderMinor);
        #endregion
        #region 选中逻辑配置
        string SelectObjectJson = @"E:/JsonFile/Unity/ChessGame/SelectObjectJson.json";
        var selectObjectJson = new ConfigBuilder();
        selectObjectJson
            .Group("ChessBoard")                        //顶层名对应身份ID
            .Add("CanSelect", false)                    //是否可被选中
            .Add("IsEnable", true)                      //是否启用

            .Group("ChessMan")
            .Add("CanSelect", true)
            .Add("IsEnable", true)

            .Group("ChessTile")
            .Add("CanSelect", true)
            .Add("IsEnable", true);
        selectObjectJson.ExportToFileFormat_Overwrite(SelectObjectJson);
        #endregion
        #region 输入枚举配置
        string InputKeyJson = @"E:/JsonFile/Unity/ChessGame/InputKeyJson.json";
        var inputKeyJson = new ConfigBuilder();
        inputKeyJson
            .Group("MoveUp")                               //顶层名与枚举同名
            .Add("KeyBindCode", "W")                       //对应的键值
            .Add("MouseButtonIndex", -1)                   //是否启用鼠标，-1不启用，0左键，1右键，2中键
            .Add("IsEnable", true)                         //是否启用整个配置
            .Add("ClickCdThreshold", 0.2)                  //输入阈值冷却CD（按下专用）
            .Add("HoldInterval", 0.05f)                    //持续按住的节流间隔（持续按下专用）
            .Add("DeviceType", "KeyboardMouse")            //设备类型
            .Add("AllowContext", new List<string>          //枚举可通行的上下文列表
            {
                "GameWorld"
            })
            .Add("AllowUIOverlayPenetrate", true)          //是否可穿透UI
            .Add("ListenState", new List<string>
            {
                "Down",
                "Hold",
                "Up"
            })

            .Group("MoveDown")
            .Add("KeyBindCode", "S")
            .Add("MouseButtonIndex", -1)
            .Add("IsEnable", true)
            .Add("ClickCdThreshold", 0.2)
            .Add("HoldInterval", 0.05f)
            .Add("DeviceType", "KeyboardMouse")
            .Add("AllowContext", new List<string>
            {
                "GameWorld"
            })
            .Add("AllowUIOverlayPenetrate", true)
            .Add("ListenState", new List<string>
            {
                "Down",
                "Hold",
                "Up"
            })

            .Group("MoveLeft")
            .Add("KeyBindCode", "A")
            .Add("MouseButtonIndex", -1)
            .Add("IsEnable", true)
            .Add("ClickCdThreshold", 0.2)
            .Add("HoldInterval", 0.05f)
            .Add("DeviceType", "KeyboardMouse")
            .Add("AllowContext", new List<string>
            {
                "GameWorld"
            })
            .Add("AllowUIOverlayPenetrate", true)
            .Add("ListenState", new List<string>
            {
                "Down",
                "Hold",
                "Up"
            })

            .Group("MoveRight")
            .Add("KeyBindCode", "D")
            .Add("MouseButtonIndex", -1)
            .Add("IsEnable", true)
            .Add("ClickCdThreshold", 0.2)
            .Add("HoldInterval", 0.05f)
            .Add("DeviceType", "KeyboardMouse")
            .Add("AllowContext", new List<string>
            {
                "GameWorld"
            })
            .Add("AllowUIOverlayPenetrate", true)
            .Add("ListenState", new List<string>
            {
                "Down",
                "Hold",
                "Up"
            })

            .Group("SelectTarget")
            .Add("KeyBindCode", "None")
            .Add("MouseButtonIndex", 0)
            .Add("IsEnable", true)
            .Add("ClickCdThreshold", 0.3)
            .Add("HoldInterval", 0.05f)
            .Add("DeviceType", "KeyboardMouse")
            .Add("AllowContext", new List<string>
            {
                "GameWorld",
                "SelectObject"
            })
            .Add("AllowUIOverlayPenetrate", false)
            .Add("ListenState", new List<string>
            {
                "Down"
            })

            .Group("SelectTile")
            .Add("KeyBindCode", "None")
            .Add("MouseButtonIndex", 0)
            .Add("IsEnable", true)
            .Add("ClickCdThreshold", 0.3)
            .Add("HoldInterval", 0.05f)
            .Add("DeviceType", "KeyboardMouse")
            .Add("AllowContext", new List<string>
            {
                "SelectChessTile"
            })
            .Add("AllowUIOverlayPenetrate", false)
            .Add("ListenState", new List<string>
            {
                "Down"
            })

            .Group("CancelOperate")
            .Add("KeyBindCode", "Escape")
            .Add("MouseButtonIndex", -1)
            .Add("IsEnable", true)
            .Add("ClickCdThreshold", 0.3)
            .Add("HoldInterval", 0.05f)
            .Add("DeviceType", "KeyboardMouse")
            .Add("AllowContext", new List<string>
            {
                "GameWorld",
                "BagPanel",
                "ShopPanel",
                "SettingPanel",
                "DialogPanel",
                "UIPanelOnly",
                "SelectObject"
            })
            .Add("AllowUIOverlayPenetrate", false)
            .Add("ListenState", new List<string>
            {
                "Down"
            });
        inputKeyJson.ExportToFileFormat_Overwrite(InputKeyJson);
        #endregion
        #region 拦截器配置
        string InputInterceptorJson = @"E:/JsonFile/Unity/ChessGame/InputInterceptorJson.json";
        var inputInterceptorJson = new ConfigBuilder();
        inputInterceptorJson
            .Group("CutSceneGlobal")              //顶层名与枚举相同
            .Add("Priority", 10)                  //优先级
            .Add("IsEnable", true)                //是否启用

            .Group("ContextLimit")
            .Add("Priority", 100)
            .Add("IsEnable", true);
        inputInterceptorJson.ExportToFileFormat_Overwrite (InputInterceptorJson);
        #endregion
        #region 棋子属性配置
        string Chess_Pawn = @"E:/JsonFile/Unity/ChessGame/ChessJson/ChessPawnJson.json";
        var chessPawn = new ConfigBuilder();
        chessPawn
            .Group("ChessPawn")                                  //顶层名
            .Add("DisplayName", "兵")                       //名字
            .Add("ChessType", "Pawn")                       //类型
            .Add("MaxHP", 1)                                //血量
            .Add("Value", 1)                                //价值
            .Add("MaxMoveRange", 2)                         //最大移动距离
            .Add("CanJump", false)                          //是否可跳跃
            .Add("CanMoveDiagonal", true)                   //是否斜走（吃子可斜走）
            .Add("Moves", new List<List<int>>               //移动方向列表
            {
                new List<int> { 0, 1 }
            })
            .Add("CanCapture", true)                        //是否可以吃子        
            .Add("CaptureMoves", new List<List<int>>        //吃子方向
            {
                new List<int> { 1, 1 },
                new List<int> { -1, 1 }
            })
            .Add("EnPassantable", true)                      //是否可以吃过路兵
            .Add("CanPromote", true)                         //是否可升变
            .Add("PromoteOptions", new List<string>          //升变选项
            {
                "Queen",
                "Rook",
                "Bishop",
                "Knight"
            })
            .Add("PromoteRankWhite", 7)                      //白方升变行
            .Add("PromoteRankBlack", 0);                     //黑方升变行
        chessPawn.ExportToFileFormat_Overwrite(Chess_Pawn);


        string Chess_Rook = @"E:/JsonFile/Unity/ChessGame/ChessJson/ChessRookJson.json";
        var chessRook = new ConfigBuilder();
        chessRook
            .Group("ChessRook")
            .Add("DisplayName", "车")
            .Add("ChessType", "Rook")
            .Add("MaxHP", 1)
            .Add("Value", 5)
            .Add("MaxMoveRange", 7)
            .Add("CanJump", false)
            .Add("CanMoveDiagonal", true)
            .Add("Moves", new List<List<int>>
            {
                new List<int> { 0, 1 },
                new List<int> { 0, -1 },
                new List<int> { -1, 0 },
                new List<int> { 1, 0 }
            })
            .Add("CanCapture", true)
            .Add("CaptureMoves", new List<List<int>>
            {
                new List<int> { 0, 1 },
                new List<int> { 0, -1 },
                new List<int> { -1, 0 },
                new List<int> { 1, 0 }
            });
        chessRook.ExportToFileFormat_Overwrite(Chess_Rook);

        string Chess_Knight = @"E:/JsonFile/Unity/ChessGame/ChessJson/ChessKnightJson.json";
        var chessKnight = new ConfigBuilder();
        chessKnight
            .Group("ChessKnight")
            .Add("DisplayName", "马")
            .Add("ChessType", "Knight")
            .Add("MaxHP", 1)
            .Add("Value", 3)
            //.Add("MaxMoveRange", null)
            .Add("CanJump", true)
            .Add("CanMoveDiagonal", true)
            .Add("Moves", new List<List<int>>
            {
                new List<int> { 1, 2 },
                new List<int> { -1, 2 },
                new List<int> { 1, -2 },
                new List<int> { -1, -2 },
                new List<int> { 2, 1 },
                new List<int> { -2, 1 },
                new List<int> { 2, -1 },
                new List<int> { -2, -1 }
            })
            .Add("CanCapture", true)
            .Add("CaptureMoves", new List<List<int>>
            {
                new List<int> { 1, 2 },
                new List<int> { -1, 2 },
                new List<int> { 1, -2 },
                new List<int> { -1, -2 },
                new List<int> { 2, 1 },
                new List<int> { -2, 1 },
                new List<int> { 2, -1 },
                new List<int> { -2, -1 }
            });
        chessKnight.ExportToFileFormat_Overwrite(Chess_Knight);

        string Chess_Bishop = @"E:/JsonFile/Unity/ChessGame/ChessJson/ChessBishopJson.json";
        var chessBishop = new ConfigBuilder();
        chessBishop
            .Group("ChessBishop")
            .Add("DisplayName", "象")
            .Add("ChessType", "Bishop")
            .Add("MaxHP", 1)
            .Add("Value", 3)
            .Add("MaxMoveRange", 7)
            .Add("CanJump", false)
            .Add("CanMoveDiagonal", true)
            .Add("Moves", new List<List<int>>
            {
                new List<int> { 1, 1 },
                new List<int> { 1, -1 },
                new List<int> { -1, 1 },
                new List<int> { -1, -1 }
            })
            .Add("CanCapture", true)
            .Add("CaptureMoves", new List<List<int>>
            {
                new List<int> { 1, 1 },
                new List<int> { 1, -1 },
                new List<int> { -1, 1 },
                new List<int> { -1, -1 }
            });
        chessBishop.ExportToFileFormat_Overwrite(Chess_Bishop);

        string Chess_Queen = @"E:/JsonFile/Unity/ChessGame/ChessJson/ChessQueenJson.json";
        var chessQueen = new ConfigBuilder();
        chessQueen
            .Group("ChessQueen")
            .Add("DisplayName", "后")
            .Add("ChessType", "Queen")
            .Add("MaxHP", 1)
            .Add("Value", 9)
            .Add("MaxMoveRange", 7)
            .Add("CanJump", false)
            .Add("CanMoveDiagonal", true)
            .Add("Moves", new List<List<int>>
            {
                new List<int> { 0, 1 },
                new List<int> { 0, -1 },
                new List<int> { -1, 0 },
                new List<int> { 1, 0 },
                new List<int> { 1, 1 },
                new List<int> { 1, -1 },
                new List<int> { -1, 1 },
                new List<int> { -1, -1 }
            })
            .Add("CanCapture", true)
            .Add("CaptureMoves", new List<List<int>>
            {
                new List<int> { 0, 1 },
                new List<int> { 0, -1 },
                new List<int> { -1, 0 },
                new List<int> { 1, 0 },
                new List<int> { 1, 1 },
                new List<int> { 1, -1 },
                new List<int> { -1, 1 },
                new List<int> { -1, -1 }
            });
        chessQueen.ExportToFileFormat_Overwrite(Chess_Queen);

        string Chess_King = @"E:/JsonFile/Unity/ChessGame/ChessJson/ChessKingJson.json";
        var chessKing = new ConfigBuilder();
        chessKing
            .Group("ChessKing")
            .Add("DisplayName", "王")
            .Add("ChessType", "King")
            .Add("MaxHP", 1)
            .Add("Value", 100)
            .Add("MaxMoveRange", 1)
            .Add("CanJump", false)
            .Add("CanMoveDiagonal", true)
            .Add("Moves", new List<List<int>>
            {
                new List<int> { 0, 1 },
                new List<int> { 0, -1 },
                new List<int> { -1, 0 },
                new List<int> { 1, 0 },
                new List<int> { 1, 1 },
                new List<int> { 1, -1 },
                new List<int> { -1, 1 },
                new List<int> { -1, -1 }
            })
            .Add("CanCapture", true)
            .Add("CaptureMoves", new List<List<int>>
            {
                new List<int> { 0, 1 },
                new List<int> { 0, -1 },
                new List<int> { -1, 0 },
                new List<int> { 1, 0 },
                new List<int> { 1, 1 },
                new List<int> { 1, -1 },
                new List<int> { -1, 1 },
                new List<int> { -1, -1 }
            });
        chessKing.ExportToFileFormat_Overwrite(Chess_King);
        #endregion
        #region 新预设体位置
        string BoardPrefab = @"E:/JsonFile/Unity/ChessGame/Prefabs/ChessBoardPrefabJson.json";
        var boardPrefab = new ConfigBuilder();
        boardPrefab
            .Group("ChessBoardPrefab")                                     //单个配置顶层名与元配置名相同
            .Add("Path", "Config/Prefabs/Entity/ChessBoard/Board");
        boardPrefab.ExportToFileFormat_Overwrite (BoardPrefab);

        string TilePrefab = @"E:/JsonFile/Unity/ChessGame/Prefabs/ChessTilePrefabJson.json";
        var tilePrefab = new ConfigBuilder();
        tilePrefab
            .Group("ChessTilePrefabs")
            .Add("Path", "Config/Prefabs/Entity/ChessBoard/Tile");
        tilePrefab.ExportToFileFormat_Overwrite(TilePrefab);
        #endregion
    }





























    // Start is called before the first frame update
    void Start()
    {
        TextConfigWrite();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
