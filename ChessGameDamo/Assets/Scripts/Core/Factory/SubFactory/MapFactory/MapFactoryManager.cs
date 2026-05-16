using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//地图工厂：只负责创建地图，棋盘，格子
public class MapFactoryManager : MonoBehaviour, IFactory
{
    public string FactoryName => "Map";

    private void Awake()        //启动子工厂
    {
        //注册自己到中央
        FactoryManager.Instance.RegisterFactory(this);
    }

    public void Initialize()    //子工厂初始化
    {
        
    }

    public bool CreateChessBoardLayout(ChessBoardConfig boardCfg, ChessBoardTileConfig tileCfg)    //创建棋盘布局预设
    {
        //Debug.Log($"[MapFactory]创建棋盘布局：{configID}");

        //获取布局（自动计算加缓存）
        ChessBoardLayoutCalc.CalculateLayout(boardCfg, tileCfg, out var positions, out var isWhiteTile);

        //存入缓存
        ChessBoardLayoutData.Initialize(positions, isWhiteTile);

        Debug.Log("棋盘布局已加载：ChessBoardLayoutData");

        //test
        //foreach (var tile in layout.tilePositions)
        //{
        //    Debug.Log($"棋子坐标:{tile.Key} => 世界坐标:{tile.Value}");
        //}
        //foreach (var tile in layout.isWhiteTile)
        //{
        //    Debug.Log($"棋子坐标:{tile.Key} => 是否为白:{tile.Value}");
        //}

        //通知世界系统

        return true;
    }

    //public bool CreateChessman(string configID)
    //{
    //    Debug.Log($"[MapFactory]创建棋子：{configID}");
    //    //加载棋子预设体
    //    GameObject[] chessmanPrefabs = Resources.LoadAll<GameObject>(ConfigPath.CHESS_MAN_PREFABS);
    //    if (chessmanPrefabs == null)
    //    {
    //        Debug.LogError("棋子预设不存在" + ConfigPath.CHESS_MAN_PREFABS);
    //        return false;
    //    }
    //    Dictionary<string, GameObject> prefabDict = new Dictionary<string, GameObject>();
    //    foreach (var p in chessmanPrefabs)
    //    {
    //        prefabDict.Add(p.name, p);
    //    }


    //    //读取棋子位置属性配置
    //    var positionList = ConfigManager.Instance.LoadDictConfig<ChessmanPositionConfig>(ConfigPath.CHESS_MAN_POSITION);


    //    //实例化棋子
    //    SpawnChessByType(prefabDict, positionList["Pawn"], "Pawn", true);
    //    SpawnChessByType(prefabDict, positionList["Rook"], "Rook", true);
    //    SpawnChessByType(prefabDict, positionList["Knight"], "Knight", true);
    //    SpawnChessByType(prefabDict, positionList["Bishop"], "Bishop", true);
    //    SpawnChessByType(prefabDict, positionList["Queen"], "Queen", true);
    //    SpawnChessByType(prefabDict, positionList["King"], "King", true);

    //    //实例化黑方棋子
    //    SpawnChessByType(prefabDict, positionList["Pawn"], "Pawn", false);
    //    SpawnChessByType(prefabDict, positionList["Rook"], "Rook", false);
    //    SpawnChessByType(prefabDict, positionList["Knight"], "Knight", false);
    //    SpawnChessByType(prefabDict, positionList["Bishop"], "Bishop", false);
    //    SpawnChessByType(prefabDict, positionList["Queen"], "Queen", false);
    //    SpawnChessByType(prefabDict, positionList["King"], "King", false);
    //    return true;
    //}

    //private void SpawnChessByType(
    //    Dictionary<string, GameObject> prefabDict,
    //    ChessmanPositionConfig config, 
    //    string chessName,
    //    bool isWhite
    //    )
    //{
    //    //获取对应坐标表
    //    var posList = isWhite ? config.positionWhite : config.positionBlack;
    //    Vector3 rotEuler = isWhite ? config.rotationWhite : config.rotationBlack;
    //    Quaternion rot = Quaternion.Euler(rotEuler);

    //    //获取布局
    //    var layout = _boardLayout.GetBoardLayout();

    //    //获取预设
    //    if (!prefabDict.TryGetValue(chessName, out var prefab) )
    //    {
    //        Debug.LogError("找不到棋子预设：" + chessName);
    //        return;
    //    }

    //    //遍历生成
    //    foreach (var pos in posList)
    //    {
    //        Vector2Int logicPos = new Vector2Int(pos[0], pos[1]);
    //        if (layout.tilePositions.TryGetValue(logicPos,out Vector3 worldPos))
    //        {
    //            GameObject chessman = Instantiate(prefab, worldPos, rot);
    //            chessman.name = $"{chessName}_{logicPos.x}_{logicPos.y}";
    //        }
    //        else
    //        {
    //            Debug.LogWarning("找不到棋盘坐标" + logicPos);
    //        }
    //    }
    //}
}
