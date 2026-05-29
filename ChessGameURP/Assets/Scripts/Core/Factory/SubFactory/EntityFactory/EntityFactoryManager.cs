using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//实体工厂
public class EntityFactoryManager : MonoBehaviour, IFactory
{
    public string FactoryName => "Entity";
    //泛用身份类，如人，狗这种大类别
    public static Dictionary<EntityIdentityType, EntityIDConfig> _EnableConfigCache = new Dictionary<EntityIdentityType, EntityIDConfig>();
    //精确身份类，如主角，NPC1这种区别
    public static Dictionary<EntitySpecialIdentityType, EntityIDConfig_Special> _SpecialConfigCache = new Dictionary<EntitySpecialIdentityType, EntityIDConfig_Special>();
    //物理优先级，比如先加载地板，再加载人
    public static Dictionary<string, PhysicsValueConfig> PhysicsValueCache = new Dictionary<string, PhysicsValueConfig>();
    //物理组件添加表,指定谁添加物理组件
    public static Dictionary<string, PhysicsComponentConfig> PhysicsComponentCache = new Dictionary<string, PhysicsComponentConfig>();
    //启动子工厂
    private void Awake()
    {
        //注册自己到中央工厂
        FactoryManager.Instance.RegisterFactory(this);
    }

    //初始化
    public void Initialize()
    {
        EntityIdentityRedister.InitRegister();
        _EnableConfigCache.Clear();
        _SpecialConfigCache.Clear();
        PhysicsValueCache.Clear();
        PhysicsComponentCache.Clear();
        GetIdentityConfig();
    }
    
    #region 获取实体配置
    private void GetIdentityConfig()
    {
        var pack = new Package();
        var entityID = new EntityIDConfig { };
        var entityIDSpe = new EntityIDConfig_Special { };
        var physicsValue = new PhysicsValueConfig { };
        var physicsComponent = new PhysicsComponentConfig { };
        var renderMajor = new RenderMajorIDConfig { };
        var renderMinor = new RenderMinorIDConfig { };
        pack.Put(EventPackName.EntityFactoryManager_GetEntityConfigClassID_EntityIDConfig, entityID);
        pack.Put(EventPackName.EntityFactoryManager_GetEntityConfigClassID_EntityIDSpecialConfig, entityIDSpe);
        pack.Put(EventPackName.EntityFactoryManager_GetEntityConfigClassID_PhysicsValueConfig, physicsValue);
        pack.Put(EventPackName.EntityFactoryManager_GetEntityConfigClassID_PhysicsComponentConfig, physicsComponent);
        pack.Put(EventPackName.EntityFactoryManager_GetEntityConfigClassID_RenderMajor, renderMajor);
        pack.Put(EventPackName.EntityFactoryManager_GetEntityConfigClassID_RenderMinor, renderMinor);
        var pub = new FactoryPublish_GetIdentityConfig_IdConfig { package = pack };
        FactoryManager.Instance._publish.GetIdentityConfig(pub);

    }
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
    public void OnPhysicsConfig(Dictionary<string, PhysicsComponentConfig> phyCom, Dictionary<string, PhysicsValueConfig> phyVal)
    {
        PhysicsValueCache = phyVal;
        PhysicsComponentCache = phyCom;
        return;
    }
    #endregion
    #region 通用事件（测试版）
    private void RenderEvent(string name, GameObject model, GameObject parent)
    {
        var pack = new Package();
        pack.Put("1", model);
        pack.Put("2", parent);
        pack.Put("3", name);
        var pub = new EntityRender_TestEvent { package = pack };
        EventManager.Instance.EmitLogic(pub);
    }
    #endregion
    #region 棋盘
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
        GameObject boardRoot = EntitySpawnUtil.CreateEmptyEntityRoot(name, boardWorldPos, Vector3.zero);
        if (EntityIdentityRedister.TryGetIdentity(name, out var major, out var minor))
        {
            EntitySpawnUtil.SetEntityFullIdentity(boardRoot, major, minor);
        }
        //发布事件
        string modelName = $"{name}_Model";
        RenderEvent(modelName, boardPfg, boardRoot);
        Debug.Log("[EntityFactoryManager]棋盘预设生成");

        return true;
    }
    #endregion
    #region 棋子
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
        Vector3 rotEuler = isWhite ? config.rotationBlack : config.rotationWhite;
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
                //GameObject chessRoot = CreateEmptyEntityRoot(rootName, worldPos, rot);
                GameObject chessRoot = EntitySpawnUtil.CreateEmptyEntityRoot(rootName, worldPos, rotEuler);
                string newName = $"{name}_White";
                //手动添加阵营组件
                var tag = chessRoot.AddComponent<ChessManType>();
                tag.IsWhite = isWhite;
                if (isWhite)
                {
                    tag.CampType = CampType.White;
                    tag.LogicPos = logicPos;
                    tag.ChessName = name;
                    newName = $"{name}_White";
                }
                if (!isWhite)
                {
                    tag.CampType = CampType.Black;
                    tag.LogicPos = logicPos;
                    tag.ChessName = name;
                    newName = $"{name}_Black";
                }
                if (EntityIdentityRedister.TryGetIdentity(newName, out var major, out var minor))
                {
                    EntitySpawnUtil.SetEntityFullIdentity(chessRoot, major, minor);
                }
                string CHE = $"ChessMan_{newName}_Model";
                //逻辑体ID事件
                GlobalIDManager.Instance.EventToAddLogicID(chessRoot);
                //发布渲染事件
                RenderEvent(CHE, prefab, chessRoot);
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
            GameObject chessRoot = EntitySpawnUtil.CreateEmptyEntityRoot(rootName, worldPos, rotEuler);
            if (EntityIdentityRedister.TryGetIdentity(name, out var major, out var minor))
            {
                EntitySpawnUtil.SetEntityFullIdentity(chessRoot, major, minor);
            }
            
        }
        else
        {
            Debug.LogWarning("找不到棋盘坐标" + realPos);
        }
    }
    #endregion
    #region 棋盘格
    public void CreateChessBoardTile( GameObject tile)
    {
        if (ChessBoardLayoutData.tilePositions == null)
        {
            Debug.LogError("[EntityFactoryManager]地图为空");
            return;
        }
        var worList = ChessBoardLayoutData.worldPositionsList;
        var IsWhiteDict = ChessBoardLayoutData.pos3DIsWhiteDict;
        foreach ( var wor in worList )
        {
            string name = $"ChessTile_White";
            if (!IsWhiteDict[wor])
                name = $"ChessTile_Black";
            GameObject tileRoot = EntitySpawnUtil.CreateEmptyEntityRoot(name, wor, Vector3.zero);
            if (EntityIdentityRedister.TryGetIdentity(name, out var major, out var minor))
            {
                EntitySpawnUtil.SetEntityFullIdentity(tileRoot, major, minor);
            }
            string modelName = $"{name}_Model";
            RenderEvent(modelName, tile, tileRoot);
        }
    }
    #endregion





    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //#region 通用：创建空实体逻辑根物体
    ////创建空物体逻辑根，固定坐标与旋转，统一命名规范
    //private GameObject CreateEmptyEntityRoot(string rootName, Vector3 worldPos, Quaternion rotation)
    //{
    //    GameObject entityRoot = new GameObject(rootName);
    //    entityRoot.transform.position = worldPos;
    //    entityRoot.transform.rotation = rotation;
    //    return entityRoot;
    //}
    //#endregion
    //#region 核心：给生成的实体自动打上身份标识（泛类）
    //private void MarkEntityIdentity(GameObject entity, EntityIdentityType identityType)
    //{
    //    //如果已有就直接赋值，没有就添加
    //    EntityIdentityTag tag = entity.GetComponent<EntityIdentityTag>();
    //    if (tag == null)
    //    {
    //        tag = entity.AddComponent<EntityIdentityTag>();
    //    }
    //    tag.IdentityType = identityType;
    //}
    //#endregion
    //#region 核心：给生成的实体自动打上身份标识（特殊）
    //private void MarkEntityIdentitySpecial(GameObject entity, EntitySpecialIdentityType specialType)
    //{
    //    EntitySpecialIdentityTag tag = entity.GetComponent<EntitySpecialIdentityTag>();
    //    if (tag == null)
    //    {
    //        tag = entity.AddComponent<EntitySpecialIdentityTag>();
    //    }
    //    tag.SpecialType = specialType;
    //}
    //#endregion
    //#region 通用：绑定特殊身份标识
    //private void MarkSpecialIdentityType(GameObject entity, EntitySpecialIdentityType specialType)
    //{
    //    EntitySpecialIdentityTag tag = entity.GetComponent<EntitySpecialIdentityTag>();
    //    if (tag == null)
    //    {
    //        tag = entity.AddComponent<EntitySpecialIdentityTag>();
    //    }
    //    tag.SpecialType = specialType;
    //}
    //#endregion
    //#region 通用：模型绑定父级，自动命名，自动实例化模型，挂到父物体，自动命名为【原名_Model】，设置全局偏移
    //private GameObject BindModelToParent(GameObject modelPrefab, Transform parent, string baseName, Vector3 localOffest)
    //{
    //    GameObject model = Instantiate(modelPrefab, parent);
    //    model.name = $"{baseName}_Model";
    //    model.transform.localPosition = localOffest;
    //    model.transform.localRotation = Quaternion.identity;
    //    return model;
    //}
    //#endregion
    //#region 自动计算模型Y半高偏移，模型底部刚好贴父级地面
    //private Vector3 CalcModelHalfHeightOffset(GameObject modelPrefab)
    //{
    //    //找模型里所有的渲染器
    //    Renderer render = modelPrefab.GetComponentInChildren<Renderer>();
    //    if (render == null)
    //    {
    //        return new Vector3(0, 0.1f, 0);  //默认兜底偏移
    //    }
    //    float halfH = render.bounds.extents.y;
    //    return new Vector3(0, halfH, 0);
    //}
    //#endregion
}
