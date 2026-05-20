using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConfigLogic : BaseBusinessSystem
{
    //系统ID
    public override string SystemID => "ConfigLogic";
    #region  事件回调（路由表自动绑定，不用手动订阅）
    //初始化棋子配置
    void InitChessConfig(PackageEvent e)
    {
        //Debug.Log($"[ConfigLogic]订阅方收到事件实际类型：{e.GetType().Name}");
        ConfigLogic_InitChessConfig_InitChessEvent evt = e as ConfigLogic_InitChessConfig_InitChessEvent;
        //拆包
        var pack = evt.package;
        if (pack.Get<ChessBoardConfig>(EventPackName.CHESSBOARD_CONFIG, out var boardConfig)) { }

        if (pack.Get<ChessBoardTileConfig>(EventPackName.CHESSBOARDTILE_CONFIG, out var tileConfig)) { }

        if (pack.Get<ChessmanPositionConfig>(EventPackName.CHESSMAN_POSITIONCONFIG, out var positionConfig)) { }

        if (pack.Get<ChessBoardPrefabConfig>(EventPackName.CHESSBOARD_PREFAB, out var boardPrefabConfig)) { }

        if (pack.Get<ChessmanPrefabsConfig>(EventPackName.CHESSMAN_PREFABS, out var manPrefabsConfig)) { }
        //if (pack.Get<ChessTilePrefabsConfig>(EventPackName.FactoryManager_PackageChessConfigInit_ChessTile, out var tilePrefabsConfig)) { }

        if ( ! pack.ValidsteAll())
        { Debug.LogError($"[ConfigLogic]某值为空！故障事件：{e}"); return; }

        //读配置
        var board = ConfigManager.Instance.GetConfig<ChessBoardConfig>(boardConfig.ID);
        var tile = ConfigManager.Instance.GetConfig<ChessBoardTileConfig>(tileConfig.ID);
        var posDict = ConfigManager.Instance.GetAllConfigsInTable<ChessmanPositionConfig>(positionConfig.ID);
        var boardPfb = ConfigManager.Instance.GetConfig<ChessBoardPrefabConfig>(boardPrefabConfig.ID);
        var manPfb = ConfigManager.Instance.GetConfig<ChessmanPrefabsConfig>(manPrefabsConfig.ID);
        //var tilePfb = ConfigManager.Instance.GetConfig<ChessTilePrefabsConfig>(tilePrefabsConfig.ID);

        //打包
        var Pack = new Package();
        Pack.Put(EventPackName.CHESSBOARD_CONFIG, board);
        Pack.Put(EventPackName.CHESSBOARDTILE_CONFIG, tile);
        Pack.Put(EventPackName.CHESSMAN_POSITIONCONFIG, posDict);
        Pack.Put(EventPackName.CHESSBOARD_PREFAB, boardPfb);
        Pack.Put(EventPackName.CHESSMAN_PREFABS, manPfb);
        //Pack.Put(EventPackName.FactoryManager_PackageChessConfigInit_ChessTile, tilePfb);

        //if (manPfb == null) { Debug.LogError("man空"); }

        //判断
        if (boardConfig.IsPrefab && positionConfig.IsPrefab) {
            var pub = new AssetsLogic_OnInitCrateChessPrefabs_CreateInitChessPrefabs { package = Pack };
            ConfigManager.Instance._publish.AssetsLogic_OnInitCrateChessPrefabs_CreateInitChessPrefabs(pub);
            //EventManager.Instance.EmitLogic(new AssetsLogic_OnInitCrateChessPrefabs_CreateInitChessPrefabs { package = Pack });
        }


        //事件
        var pub1 = new FactoryLogic_OnResponseChessConfig_InitChessConfig { package = Pack };
        ConfigManager.Instance._publish.FactoryLogic_OnResponseChessConfig_InitChessConfig(pub1);
        //EventManager.Instance.EmitLogic(new FactoryLogic_OnResponseChessConfig_InitChessConfig { package = Pack } );
    }
    //输入系统要配置
    void OnInputConfig(PackageEvent e)
    {
        ConfigLogic_OnInputConfig_InitInputConfig evt = e as ConfigLogic_OnInputConfig_InitInputConfig;
        //定位配置模板地址
        var inputCfg = new InputKeyConfig { };
        var selectCfg = new SelectObjectConfig { };
        var interceptorCfg = new InputInterceptorConfig { };
        //访问元配置
        var inputDict = ConfigManager.Instance.GetAllConfigsInTable<InputKeyConfig>(inputCfg.ID);
        var selectDict = ConfigManager.Instance.GetAllConfigsInTable<SelectObjectConfig>(selectCfg.ID);
        var interceptorDict = ConfigManager.Instance.GetAllConfigsInTable<InputInterceptorConfig>(interceptorCfg.ID);
        var Pack = new Package();
        Pack.Put(EventPackName.INPUT_GETCONFIG, inputDict);
        Pack.Put(EventPackName.INPUT_SELECTCONFIG, selectDict);
        Pack.Put(EventPackName.INPUT_INTERCEPTORCONFIG, interceptorDict);
        //事件
        var pub = new InputLogic_OnConfig_IsInputConfig { package = Pack };
        ConfigManager.Instance._publish.InputLogic_OnConfig_IsInputConfig(pub);
        //EventManager.Instance.EmitLogic(new InputLogic_OnConfig_IsInputConfig { package = Pack });
    }
    //实体工厂要实体ID配置
    void InitEntityIDConfig(PackageEvent e)
    {
        FactoryPublish_GetIdentityConfig_IdConfig evt = e as FactoryPublish_GetIdentityConfig_IdConfig;
        //拆包
        var pack = evt.package;
        if (pack.Get<EntityIDConfig>(EventPackName.EntityFactoryManager_GetEntityConfigClassID_EntityIDConfig, out var entityClassID)) { }
        if (pack.Get<EntityIDConfig_Special>(EventPackName.EntityFactoryManager_GetEntityConfigClassID_EntityIDSpecialConfig, out var specialConfig)) { }
        if (pack.Get<PhysicsComponentConfig>(EventPackName.EntityFactoryManager_GetEntityConfigClassID_PhysicsComponentConfig, out var physicComponentConfig)) { }
        if (pack.Get<PhysicsValueConfig>(EventPackName.EntityFactoryManager_GetEntityConfigClassID_PhysicsValueConfig, out var physicValueConfig)) { }
        if (pack.Get<RenderMajorIDConfig>(EventPackName.EntityFactoryManager_GetEntityConfigClassID_RenderMajor, out var renderMajorIDConfig)) { }
        if (pack.Get<RenderMinorIDConfig>(EventPackName.EntityFactoryManager_GetEntityConfigClassID_RenderMinor, out var renderMinorIDConfig)) { }
        if ( ! pack.ValidsteAll())
        { Debug.LogError($"[ConfigLogic]某值为空！故障事件：{e}"); return; }
        if (physicValueConfig == null)
            Debug.LogError("为空");
        //读配置
        var entID = ConfigManager.Instance.GetAllConfigsInTable<EntityIDConfig>(entityClassID.ID);
        var speID = ConfigManager.Instance.GetAllConfigsInTable<EntityIDConfig_Special>(specialConfig.ID);
        var phyCom = ConfigManager.Instance.GetAllConfigsInTable<PhysicsComponentConfig>(physicComponentConfig.ID);
        var phyVal = ConfigManager.Instance.GetAllConfigsInTable<PhysicsValueConfig>(physicValueConfig.ID);
        var renMaj = ConfigManager.Instance.GetAllConfigsInTable<RenderMajorIDConfig>(renderMajorIDConfig.ID);
        var renMin = ConfigManager.Instance.GetAllConfigsInTable<RenderMinorIDConfig>(renderMinorIDConfig.ID);
        var newPack = new Package();
        newPack.Put(EventPackName.ConfigLogic_InitEntityIDConfig_EntityIDConfig, entID);
        newPack.Put(EventPackName.ConfigLogic_InitEntityIDConfig_EntityIDConfigSpecial, speID);
        newPack.Put(EventPackName.ConfigLogic_InitEntityIDConfig_PhysicsComponentConfig, phyCom);
        newPack.Put(EventPackName.ConfigLogic_InitEntityIDConfig_PhysicsValueConfig, phyVal);
        newPack.Put(EventPackName.ConfigLogic_InitEntityIDConfig_RenderMajor, renMaj);
        newPack.Put(EventPackName.ConfigLogic_InitEntityIDConfig_RenderMinor, renMin);
        var pub = new ConfigPublish_OnIdentityConfig_IdConfig { package = newPack };
        ConfigManager.Instance._publish.OnIdentityConfig(pub);
    }
    #endregion
    //公开业务方法 给Mono壳子调用
    public void Init()
    {

    }
}
