using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//实体工厂
public class EntityFactoryManager : MonoBehaviour, IFactory
{
    public string FactoryName => "Entity";

    public static Dictionary<EntityIdentityType, EntityIDConfig> _EnableConfigCache = new Dictionary<EntityIdentityType, EntityIDConfig>();
    public static Dictionary<EntitySpecialIdentityType, EntityIDConfig_Special> _SpecialConfigCache = new Dictionary<EntitySpecialIdentityType, EntityIDConfig_Special>();
    //启动子工厂
    private void Awake()
    {
        //注册自己到中央工厂
        FactoryManager.Instance.RegisterFactory(this);
    }

    //初始化
    public void Initialize()
    {
        _EnableConfigCache.Clear();
        _SpecialConfigCache.Clear();
        GetEntityConfigClassID();
    }
    #region 通用：创建空实体逻辑根物体
    //创建空物体逻辑根，固定坐标与旋转，统一命名规范
    private GameObject CreateEmptyEntityRoot(string rootName, Vector3 worldPos, Quaternion rotation)
    {
        GameObject entityRoot = new GameObject(rootName);
        entityRoot.transform.position = worldPos;
        entityRoot.transform.rotation = rotation;
        return entityRoot;
    }
    #endregion
    #region 核心：给生成的实体自动打上身份标识
    private void MarkEntityIdentity(GameObject entity, EntityIdentityType identityType)
    {
        //如果已有就直接赋值，没有就添加
        EntityIdentityTag tag = entity.GetComponent<EntityIdentityTag>();
        if (tag == null)
        {
            tag = entity.AddComponent<EntityIdentityTag>();
        }
        tag.IdentityType = identityType;
    }
    #endregion
    #region 通用：绑定特殊身份标识
    private void MarkSpecialIdentityType(GameObject entity, EntitySpecialIdentityType specialType)
    {
        EntitySpecialIdentityTag tag = entity.GetComponent<EntitySpecialIdentityTag>();
        if (tag == null)
        {
            tag = entity.AddComponent<EntitySpecialIdentityTag>();
        }
        tag.SpecialType = specialType;
    }
    #endregion
    #region 通用：模型绑定父级，自动命名，自动实例化模型，挂到父物体，自动命名为【原名_Model】，设置全局偏移
    private GameObject BindModelToParent(GameObject modelPrefab, Transform parent, string baseName, Vector3 localOffest)
    {
        GameObject model = Instantiate(modelPrefab, parent);
        model.name = $"{baseName}_Model";
        model.transform.localPosition = localOffest;
        model.transform.localRotation = Quaternion.identity;
        return model;
    }
    #endregion
    #region 自动计算模型Y半高偏移，模型底部刚好贴父级地面
    private Vector3 CalcModelHalfHeightOffset(GameObject modelPrefab)
    {
        //找模型里所有的渲染器
        Renderer render = modelPrefab.GetComponentInChildren<Renderer>();
        if (render == null)
        {
            return new Vector3(0, 0.1f, 0);  //默认兜底偏移
        }
        float halfH = render.bounds.extents.y;
        return new Vector3(0, halfH, 0);
    }
    #endregion

    //读取实体身份配置路由
    private void GetEntityConfigClassID()
    {
        var pack = new Package();
        var entityID = new EntityIDConfig { };
        var entityIDSpe = new EntityIDConfig_Special { };
        pack.Put(EventPackName.LogicManager_GetEntityConfigClassID_EntityIDConfig, entityID);
        pack.Put(EventPackName.LogicManager_GetEntityConfigClassID_EntityIDSpecialConfig, entityIDSpe);
        var pub = new ConfigLogic_InitEntityIDConfig_InitEntityID { package = pack };
        FactoryManager.Instance._publish.ConfigLogic_InitEntityIDConfig_InitEntityID(pub);
    }
    //处理配置
    public void OnEntityIDConfig(Dictionary<string, EntityIDConfig> entityIDConfig)
    {
        if (entityIDConfig == null)
        { Debug.LogError($"[EntityFactoryManager]配置为空：{entityIDConfig}"); return; }
        foreach (var kv in entityIDConfig)
        {
            //字符串转枚举
            if (!Enum.TryParse<EntityIdentityType>(kv.Key, out var entityType))
            {
                Debug.LogError($"[EntityFactoryManager]未知实体类型枚举：{kv.Key}");
                continue;
            }
            if (kv.Value.IsEnable)
            {
                _EnableConfigCache[entityType] = kv.Value;
            }

        }
        Debug.Log($"[EntityFactoryManager]实体路由配置加载完成，全局缓存存储数量：{_EnableConfigCache.Count}");

    }
    public void OnEntityIDSpecialConfig(Dictionary<string, EntityIDConfig_Special> entityIDSpecialConfig)
    {
        if (entityIDSpecialConfig == null)
        { Debug.LogError($"[EntityFactoryManager]配置为空：{entityIDSpecialConfig}"); return; }
        foreach (var kv in entityIDSpecialConfig)
        {
            //字符串转枚举
            if (!Enum.TryParse<EntitySpecialIdentityType>(kv.Key, out var entityType))
            {
                Debug.LogError($"[EntityFactoryManager]未知实体类型枚举：{kv.Key}");
                continue;
            }
            if (kv.Value.IsEnable)
            {
                _SpecialConfigCache[entityType] = kv.Value;
            }
            
        }
        Debug.Log($"[EntityFactoryManager]实体路由配置加载完成，特殊缓存存储数量：{_SpecialConfigCache.Count}");
    }

    public bool CreateChessBoard(GameObject boardPfg, ChessBoardConfig boardCfg)
    {
        //根据布局生成棋盘对象

        //读取棋盘世界位置
        Vector3 boardWorldPos = new Vector3(
            boardCfg.BoardInitPosition.x,
            boardCfg.BoardInitPosition.y,
            boardCfg.BoardInitPosition.z
            );

        string name = "ChessBoard";
        //创建棋盘空父根
        GameObject boardRoot = CreateEmptyEntityRoot(name,boardWorldPos,Quaternion.identity);
        //绑定身份
        MarkEntityIdentity(boardRoot, EntityIdentityType.ChessBoard);
        //实例化棋盘，并绑定到父根
        GameObject boardModel = BindModelToParent(boardPfg, boardRoot.transform, name, Vector3.zero);
        //生成后自动打标 棋盘身份
        //给棋盘父物体 自动挂上容器脚本（身份资料先执行，后续属性查找基底）
        ChessBoardTileContainer container = boardRoot.GetComponent<ChessBoardTileContainer>();
        if (container == null)
        {
            container = boardRoot.AddComponent<ChessBoardTileContainer>();
        }
        //全自动：找 TileRoot > 遍历格子 > 坐标匹配 > 打身份加绑数据
        container.AutoBindAllTile();
        Debug.Log("[EntityFactoryManager]棋盘预设生成");

        return true;
    }

    public bool CreateChessman(GameObject[] chessPfbs, Dictionary<string,ChessmanPositionConfig> position, string name, bool isWhite, bool isInit, Vector2Int? pos)
    {
        if (chessPfbs == null)
        {
            Debug.LogError("[EntityFactoryManager]棋子预设体配置为空");
            return false;
        }
        var dict = new Dictionary<string,GameObject>();
        foreach (var chess in chessPfbs)
        {
            dict.Add(chess.name, chess);
        }

        //生成棋子
        if (isInit)
        {
            SpawnChessByTypeInit(dict, position[name], name, isWhite);
            return true;
        }
        else
        {
            SpawnChessByType(dict, position[name], name, isWhite, pos);
        }
        return true;
    }

    private void SpawnChessByTypeInit(
        Dictionary<string, GameObject> prefabDict,
        ChessmanPositionConfig config,
        string name,
        bool isWhite
        )
    {
        //获取对应坐标表
        var posList = isWhite ? config.positionWhite : config.positionBlack;
        Vector3 rotEuler = isWhite ? config.rotationWhite : config.rotationBlack;
        Quaternion rot = Quaternion.Euler(rotEuler);


        //获取预设
        if (!prefabDict.TryGetValue(name, out var prefab))
        {
            Debug.LogError("找不到棋子预设：" + name);
            return;
        }

        //遍历生成
        foreach (var pos in posList)
        {
            Vector2Int logicPos = new Vector2Int(pos[0], pos[1]);
            if (ChessBoardLayoutData.tilePositions == null)
            {
                Debug.LogError("[EntityFactoryManager]地图为空");
                return;
            }    
            if (ChessBoardLayoutData.tilePositions.TryGetValue(logicPos, out Vector3 worldPos))
            {
                string rootName = $"{name}_{logicPos.x}_{logicPos.y}";
                //创建空父级
                GameObject chessRoot = CreateEmptyEntityRoot(rootName, worldPos, rot);
                //绑定全局基础身份
                MarkEntityIdentity(chessRoot, EntityIdentityType.ChessMan);
                //计算偏移
                Vector3 offset = CalcModelHalfHeightOffset(prefab);
                //模型绑父级
                BindModelToParent(prefab, chessRoot.transform, rootName, offset);
                Debug.Log($"[EntityFactoryManager]棋子预设体生成，名字{chessRoot.name}");
                
            }
            else
            {
                Debug.LogWarning("找不到棋盘坐标" + logicPos);
                continue;
            }
        }
    }


    private void SpawnChessByType(
        Dictionary<string, GameObject> prefabDict,
        ChessmanPositionConfig config,
        string name,
        bool isWhite,
        Vector2Int? pos
        )
    {
        Vector3 rotEuler = isWhite ? config.rotationWhite : config.rotationBlack;
        Quaternion rot = Quaternion.Euler(rotEuler);

        if (!pos.HasValue)
        {
            Debug.LogWarning("坐标为空，无法生成棋子");
            return;
        }
        Vector2Int realPos = pos.Value;

        //获取预设
        if (!prefabDict.TryGetValue(name, out var prefab))
        {
            Debug.LogError("找不到棋子预设：" + name);
            return;
        }

        //生成
        if (ChessBoardLayoutData.tilePositions.TryGetValue(realPos, out Vector3 worldPos))
        {
            string rootName = $"{name}_{realPos.x}_{realPos.y}";
            GameObject chessRoot = CreateEmptyEntityRoot(rootName, worldPos, rot);
            MarkEntityIdentity(chessRoot, EntityIdentityType.ChessMan);
            Vector3 offset = CalcModelHalfHeightOffset(prefab);
            BindModelToParent(prefab, chessRoot.transform, rootName, offset);
            
        }
        else
        {
            Debug.LogWarning("找不到棋盘坐标" + realPos);
        }
    }






    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
