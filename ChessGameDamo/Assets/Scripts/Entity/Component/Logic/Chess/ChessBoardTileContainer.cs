using System.Collections.Generic;
using UnityEngine;

public class ChessBoardTileContainer : MonoBehaviour
{
    // 固定格子父物体名字，建模那边统一叫这个就行
    private const string TileRootNodeName = "ChessBoard_Model/Key.001";
    private const string ModeName = "ChessBoard_Model";
    // 坐标匹配误差阈值
    public float MatchThreshold = 0.05f;

    private Transform _tileRoot;
    private readonly List<GameObject> _tileList = new List<GameObject>();

    private void Awake()
    {
        // 自动第一步：从自己子物体里 按名字找 TileRoot
        AutoFindTileRoot();
    }

    // 自动查找格子父节点，不用手动拖
    private void AutoFindTileRoot()
    {
        Transform allChild = transform;
        transform.Find(TileRootNodeName);
        _tileRoot = allChild;
        //foreach (Transform child in allChild)
        //{
        //    if (child.name == TileRootNodeName)
        //    {
                
        //        _tileRoot = child;
        //        break;
        //    }
        //}

        if (_tileRoot == null)
        {
            Debug.LogError("[ChessBoardTileContainer]棋盘自动查找失败：找不到 TileRoot 节点，请检查模型层级命名！");
            return;
        }
    }

    // 外部调用：初始化所有格子绑定数据、身份、坐标、黑白
    public void AutoBindAllTile()
    {
        if (_tileRoot == null) return;

        // 清空列表，收集所有格子子物体
        _tileList.Clear();
        foreach (Transform child in _tileRoot)
        {
            _tileList.Add(child.gameObject);
        }

        // 拿你已有的布局数据表
        var posDict = ChessBoardLayoutData.tilePositions;
        var colorDict = ChessBoardLayoutData.isWhiteTile;
        if (posDict == null || colorDict == null)
        { Debug.LogError("[ChessBoardTileContainer]获取ChessBoardLayoutData数据失败，数据为空！"); return; }

        // 遍历配置坐标，匹配场景格子
        foreach (var kv in posDict)
        {
            Vector2Int logicPos = kv.Key;
            Vector3 worldCenter = kv.Value;

            // 找距离最近的格子物体
            GameObject tileGo = FindNearestTile(worldCenter);
            if (tileGo == null) continue;

            // 1. 给格子打实体身份 ChessTile
            EntityIdentityTag tag = tileGo.GetComponent<EntityIdentityTag>()
                                    ?? tileGo.AddComponent<EntityIdentityTag>();
            tag.IdentityType = EntityIdentityType.ChessTile;

            // 2. 绑定格子自定义数据：坐标 + 黑白
            ChessTileData data = tileGo.GetComponent<ChessTileData>()
                                 ?? tileGo.AddComponent<ChessTileData>();
            data.LogicX = logicPos.x;
            data.LogicY = logicPos.y;

            if (colorDict.TryGetValue(logicPos, out bool isWhite))
            {
                data.IsWhiteTile = isWhite;
            }
        }

        Debug.Log("[ChessBoardTileContainer] ✅棋盘格子全自动绑定完成：身份+坐标+黑白");
    }

    // 根据世界坐标匹配最近格子
    private GameObject FindNearestTile(Vector3 targetPos)
    {
        GameObject result = null;
        float minDis = float.MaxValue;

        foreach (var tile in _tileList)
        {
            float dis = Vector3.Distance(tile.transform.position, targetPos);
            if (dis < MatchThreshold && dis < minDis)
            {
                minDis = dis;
                result = tile;
            }
        }
        return result;
    }
}