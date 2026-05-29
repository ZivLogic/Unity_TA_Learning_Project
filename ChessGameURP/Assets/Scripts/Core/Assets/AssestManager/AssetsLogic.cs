using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssetsLogic : BaseBusinessSystem
{
    public override string SystemID => "AssetsLogic";
    #region  事件回调（路由表自动绑定，不用手动订阅）
    void OnInitCrateChessPrefabs(PackageEvent e)
    {
        AssetsLogic_OnInitCrateChessPrefabs_CreateInitChessPrefabs evt = e as AssetsLogic_OnInitCrateChessPrefabs_CreateInitChessPrefabs;
        var pack = evt.package;
        if (pack.Get<ChessBoardPrefabConfig>(EventPackName.CHESSBOARD_PREFAB, out var boardConfig)) { }
        if (pack.Get<ChessmanPrefabsConfig>(EventPackName.CHESSMAN_PREFABS, out var chessmanConfig)) { }
        if (pack.Get<ChessTilePrefabsConfig>(EventPackName.FactoryManager_PackageChessConfigInit_ChessTile, out var tileConfig)) { }

        if (!pack.ValidsteAll())
        { Debug.LogError($"[AssetsLogic]某值为空！故障事件：{e}"); return; }

        var board = AssetsManager.Instance.Load<GameObject>(boardConfig.Path);
        var manDict = AssetsManager.Instance.LoadAll<GameObject>(chessmanConfig.Path);
        var tile = AssetsManager.Instance.Load<GameObject>(tileConfig.Path);

        pack.Put(EventPackName.CHESSBOARD_ISPREFAB, board);
        pack.Put(EventPackName.CHESSMAN_ISPREFABS, manDict);
        pack.Put(EventPackName.FactoryManager_PackageChessConfigInit_ChessTile, tile);

        //事件
        var pub = new FactoryLogic_OnResponseChessPrefabs_AssetInitPrefabsLoadAll { package = pack };
        AssetsManager.Instance._publish.FactoryLogic_OnResponseChessPrefabs_AssetInitPrefabsLoadAll(pub);
        //EventManager.Instance.EmitLogic(new FactoryLogic_OnResponseChessPrefabs_AssetInitPrefabsLoadAll { package = pack });
    }
    //获取Shader资源，硬编码
    void LoadChessShader(PackageEvent e)
    {
        LoadShader evt = e as LoadShader;
        var pack = evt.package;
        if (pack.Get<string>("1", out var phth)) { }
        if (!pack.ValidsteAll())
        { Debug.LogError($"[AssetsLogic]某值为空！故障事件：{e}"); return; }
        //异步测试
        AssetsManager.Instance.LoadAsync<Material>(phth, (mat) =>
        {
            if (mat == null)
            {
                Debug.LogError($"[AssetsLogic]材质异步加载失败，路径：{phth}");
                return;
            }
            pack.Put("3", mat);
            var pub = new GetChessShader { package = pack };
            EventManager.Instance.EmitLogic(pub);
        });




        //var shader = AssetsManager.Instance.Load<Material>(phth);
        //pack.Put("3", shader);
        //var pub = new GetChessShader { package = pack };
        //EventManager.Instance.EmitLogic(pub);
    }
    #endregion
    //公开业务方法 给Mono壳子调用
    public void Init()
    {
        //硬编码事件快速迭代验证
        EventManager.Instance.Listen<LoadShader>(LoadChessShader);
    }
}