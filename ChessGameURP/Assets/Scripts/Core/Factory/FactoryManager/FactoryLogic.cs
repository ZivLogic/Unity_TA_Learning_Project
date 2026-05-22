using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FactoryLogic : BaseBusinessSystem
{
    public override string SystemID => "FactoryLogic";
    #region  事件回调（路由表自动绑定，不用手动订阅）
    //接收配置
    private void OnResponseChessConfig(PackageEvent e)
    {
        FactoryLogic_OnResponseChessConfig_InitChessConfig evt = e as FactoryLogic_OnResponseChessConfig_InitChessConfig;
        var pack = e.package;
        if (pack.Get<ChessBoardConfig>(EventPackName.CHESSBOARD_CONFIG, out var boardConfig)) { }
        if (pack.Get<ChessBoardTileConfig>(EventPackName.CHESSBOARDTILE_CONFIG, out var tileConfig)) { }
        if ( ! pack.ValidsteAll())
        { Debug.LogError($"[FactoryLogic]某值为空！故障事件：{e}"); return; }
        var mapFactory = FactoryManager.Instance.GetFactory<MapFactoryManager>("Map");
        mapFactory?.CreateChessBoardLayout(boardConfig, tileConfig);
    }
    //棋子预设体生成
    private void OnResponseChessPrefabs(PackageEvent e)
    {
        FactoryLogic_OnResponseChessPrefabs_AssetInitPrefabsLoadAll evt = e as FactoryLogic_OnResponseChessPrefabs_AssetInitPrefabsLoadAll;
        var pack = e.package;
        if (pack.Get<GameObject>(EventPackName.CHESSBOARD_ISPREFAB, out var boardPfb)) { }
        if (pack.Get<ChessBoardConfig>(EventPackName.CHESSBOARD_CONFIG, out var boardCfg)) { }
        if (pack.Get<GameObject[]>(EventPackName.CHESSMAN_ISPREFABS, out var manPfb)) { }
        if (pack.Get<Dictionary<string, ChessmanPositionConfig>>(EventPackName.CHESSMAN_POSITIONCONFIG, out var manPos)) { }
        if (pack.Get<GameObject>(EventPackName.FactoryManager_PackageChessConfigInit_ChessTile, out var tilePfb))

            if ( ! pack.ValidsteAll() )
        { Debug.LogError($"[FactoryLogic]某值为空！故障事件：{e}"); return; }
        var entityFactory = FactoryManager.Instance.GetFactory<EntityFactoryManager>("Entity");
        var renderFactory = FactoryManager.Instance.GetFactory<RenderFactoryManager>("Render");
        entityFactory?.CreateChessBoard(boardPfb, boardCfg);
        //棋子生成
        entityFactory?.CreateChessman(manPfb, manPos, "Pawn", true, true, null);
        entityFactory?.CreateChessman(manPfb, manPos, "Rook", true, true, null);
        entityFactory?.CreateChessman(manPfb, manPos, "Knight", true, true, null);
        entityFactory?.CreateChessman(manPfb, manPos, "Bishop", true, true, null);
        entityFactory?.CreateChessman(manPfb, manPos, "Queen", true, true, null);
        entityFactory?.CreateChessman(manPfb, manPos, "King", true, true, null);
        entityFactory?.CreateChessman(manPfb, manPos, "Pawn", false, true, null);
        entityFactory?.CreateChessman(manPfb, manPos, "Rook", false, true, null);
        entityFactory?.CreateChessman(manPfb, manPos, "Knight", false, true, null);
        entityFactory?.CreateChessman(manPfb, manPos, "Bishop", false, true, null);
        entityFactory?.CreateChessman(manPfb, manPos, "Queen", false, true, null);
        entityFactory?.CreateChessman(manPfb, manPos, "King", false, true, null);
        entityFactory?.CreateChessBoardTile(tilePfb);

        

    }
    private void OnEntityIDConfig(PackageEvent e)
    {
        ConfigPublish_OnIdentityConfig_IdConfig evt = e as ConfigPublish_OnIdentityConfig_IdConfig;
        var pack = evt.package;
        if (pack.Get<Dictionary<string, EntityIDConfig>>(EventPackName.ConfigLogic_InitEntityIDConfig_EntityIDConfig, out var entityIDConfig)) { }
        if (pack.Get<Dictionary<string,EntityIDConfig_Special>>(EventPackName.ConfigLogic_InitEntityIDConfig_EntityIDConfigSpecial, out var special)) { }
        if (pack.Get<Dictionary<string, PhysicsValueConfig>>(EventPackName.ConfigLogic_InitEntityIDConfig_PhysicsValueConfig, out var physicsValueConfig)) { }
        if (pack.Get<Dictionary<string, PhysicsComponentConfig>>(EventPackName.ConfigLogic_InitEntityIDConfig_PhysicsComponentConfig, out var physicsComponentConfig)) { }
        if (pack.Get<Dictionary<string, RenderMajorIDConfig>>(EventPackName.ConfigLogic_InitEntityIDConfig_RenderMajor, out var renderMajorIDConfig)) { }
        if (pack.Get<Dictionary<string, RenderMinorIDConfig>>(EventPackName.ConfigLogic_InitEntityIDConfig_RenderMinor, out var renderMinorIDConfig)) { }
        if ( ! pack.ValidsteAll())
        { Debug.LogError($"[FactoryLogic]某值为空！故障事件：{e}"); return; }
        var entityFactory = FactoryManager.Instance.GetFactory<EntityFactoryManager>("Entity");
        var renderFactory = FactoryManager.Instance.GetFactory<RenderFactoryManager>("Render");
        entityFactory?.OnEntityIDConfig(entityIDConfig);
        entityFactory?.OnEntityIDSpecialConfig(special);
        entityFactory?.OnPhysicsConfig(physicsComponentConfig, physicsValueConfig);
        renderFactory?.OnRenderIDConfig(renderMajorIDConfig, renderMinorIDConfig);
    }

    #endregion
    //公开业务方法 给Mono壳子调用
    public void Init()
    {

    }
}
