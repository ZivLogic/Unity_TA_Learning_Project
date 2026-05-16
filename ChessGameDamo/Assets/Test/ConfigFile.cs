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

            .Group("ConfigLogic_InitEntityIDConfig_InitEntityID")
            .Add("SystemID", "ConfigLogic")
            .Add("EventTypeFullName", "ConfigLogic_InitEntityIDConfig_InitEntityID")
            .Add("HandlerMethodName", "InitEntityIDConfig")
            .Add("QueueType", "Logic")
            .Add("IsEnable", "true")

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

            .Group("FactoryLogic_OnEntityIDConfig_InitEntityID")
            .Add("SystemID", "FactoryLogic")
            .Add("EventTypeFullName", "FactoryLogic_OnEntityIDConfig_InitEntityID")
            .Add("HandlerMethodName", "OnEntityIDConfig")
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
            .Add("IsEnable", "true");

        eventRoute.ExportToFileFormat_Overwrite(EventRoutePath);
        #endregion
        #region 事件发布路由配置
        //事件发射路由
        string EventPublishAllowJson = @"E:/JsonFile/Unity/ChessGame/EventTriggerRouteJson.json";
        var eventPublishAllow = new ConfigBuilder();
        eventPublishAllow
            //事件结构[系统]_[方法]_[事件名]
            //系统：订阅事件的系统
            //方法：订阅系统里的对应回调方法
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

            .Group("FactoryLogic_OnEntityIDConfig_InitEntityID")
            .Add("SystemID", "ConfigPublish")
            .Add("EventTypeFullName", "FactoryLogic_OnEntityIDConfig_InitEntityID")
            .Add("HandlerMethodName", "FactoryLogic_OnEntityIDConfig_InitEntityID")
            .Add("QueueType", "Logic")
            .Add("IsEnable", "true")

            //FactoryPublish
            .Group("ConfigLogic_InitChessConfig_InitChessEvent")
            .Add("SystemID", "FactoryPublish")
            .Add("EventTypeFullName", "ConfigLogic_InitChessConfig_InitChessEvent")
            .Add("HandlerMethodName", "ConfigLogic_InitChessConfig_InitChessEvent")
            .Add("QueueType", "Logic")
            .Add("IsEnable", "true")

            .Group("ConfigLogic_InitEntityIDConfig_InitEntityID")
            .Add("SystemID", "FactoryPublish")
            .Add("EventTypeFullName", "ConfigLogic_InitEntityIDConfig_InitEntityID")
            .Add("HandlerMethodName", "ConfigLogic_InitEntityIDConfig_InitEntityID")
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

            .Group("ChessmanPosition")
            .Add("ConfigType", "ChessmanPositionConfig, Assembly-CSharp")
            .Add("ConfigPath", "Config/Json/Chess/ChessmanPositionJson")
            .Add("IsTable", true)
            .Add("IsEnable", true)

            .Group("Chessman")
            .Add("ConfigType", "ChessmanConfig, Assembly-CSharp")
            .Add("ConfigPath", "Config/Json/Chess/ChessmanJson")
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
                {"Y", 0.0f },
                {"Z", 0.0f }
            })
            .Add("ModeLocalOffset", new Dictionary<string, object>()             //偏移量
            {
                {"X", 0.0f },
                {"Y", 0.2f },
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
            .Add("ModeLocalOffset", new Dictionary<string, object>()             //偏移量
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
            .Add("ModeLocalOffset", new Dictionary<string, object>()             //偏移量
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
            .Group("ChessMan_Pawn")
            .Add("AttrTable", new Dictionary<string, object>()
            {
                {"Name", "兵"}
            })
            .Add("ClassIDTable", new List<string>()
            {
                "TestCS"
            })
            .Add("ParentName", "ChessMan")
            .Add("IsEnable", true)
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
