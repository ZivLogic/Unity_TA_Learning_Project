using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public class AssetsManager : MonoBehaviour
{
    //单例实例
    public static AssetsManager Instance { get; private set; }
    
    //内部工具
    private AssetLoader _loader;
    private AssetCache _cache;
    //初始化标记
    private bool _isInitialized = false;

    //事件器
    public AssetsLogic _logic;
    public AssetsPublish _publish;

    #region 单例和初始化
    private void Awake()
    {
        //单例模式：确保全局唯一
        if (Instance != null )
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        //初始化事件
        _logic = new AssetsLogic();
        _publish = new AssetsPublish();

        //初始化内部工具
        _loader = new AssetLoader();
        _cache = new AssetCache();
        SystemInit();

        _isInitialized = true;
        Debug.Log("[AssetManager]初始化完成");
        SubscribeAllEvents();
    }
    #endregion
    #region 事件器专用
    // 外部要调用系统方法 转发给内核
    public void SystemInit()
    {
        _logic.Init();
        _publish.Init();
    }
    #endregion
    #region 同步加载接口（对外公开）
    //同步加载资源
    public T Load<T>(string path) where T : Object
    {
        if (!_isInitialized)
        {
            Debug.LogError($"[AssetManager]未初始化！");
            return null;
        }

        //优先推荐查缓存
        T cachedAsset = _cache.Get<T>(path);
        if ( cachedAsset != null )
        {
            return cachedAsset;
        }

        //缓存未命中，再去加载
        T asset = _loader.Load<T>(path);

        //加载成功则加入缓存
        if ( asset != null )
        {
            _cache.Add(path, asset);
        }
        return asset;
    }
    #endregion
    #region 同步加载文件夹
    public T[] LoadAll<T>(string foldPath) where T : Object
    {
        if (!_isInitialized) 
            return Array.Empty<T>();
        //先读缓存
        T[] cacheArr = _cache.GetFolderAssets<T>(foldPath);
        if (cacheArr != null)
            return cacheArr;
        //没缓存就加载
        T[] assets = _loader.LoadAll<T>(foldPath);
        if ( assets != null && assets.Length > 0 )
        {
            _cache.AddFolderAssets(foldPath, assets);
        }
        return assets;
    }
    #endregion
    #region 异步加载接口（对外公开）

    //异步加载资源（推荐运行时使用，防止卡顿）
    public void LoadAsync<T>(string path, System.Action<T> onLoaded) where T : Object
    {
        if (!_isInitialized)
        {
            Debug.LogError($"[AssetManager]未初始化！");
            onLoaded?.Invoke(null);
            return;
        }
        //优先查缓存
        T cachedAsset = _cache.Get<T>(path);
        if (cachedAsset != null )
        {
            onLoaded?.Invoke(cachedAsset);
            return;
        }
        //查缓存未命中，启动异步加载
        _loader.LoadAsync<T>(path, (asset) =>
        {
            if (asset != null)
            {
                //加入缓存
                _cache.Add(path, asset);
            }
            onLoaded?.Invoke(asset);
        });
    }
    #endregion
    #region 异步加载遍历文件夹接口
    public void LoadAllAsync<T>(string folderPath, Action<T[]> onLoaded) where T : Object
    {
        if (!_isInitialized)
        {
            onLoaded?.Invoke(Array.Empty<T>());
            return;
        }

        // 1. 先看缓存
        T[] cacheArr = _cache.GetFolderAssets<T>(folderPath);
        if (cacheArr != null)
        {
            onLoaded?.Invoke(cacheArr);
            return;
        }

        // 2. 没缓存 → 协程异步加载
        StartCoroutine(LoadAllCoroutine(folderPath, onLoaded));
    }

    // 协程：真正执行加载
    private IEnumerator LoadAllCoroutine<T>(string folderPath, Action<T[]> onLoaded) where T : Object
    {
        // 帧间隙执行，不卡顿
        yield return null;

        T[] assets = _loader.LoadAll<T>(folderPath);

        // 加入缓存
        if (assets != null && assets.Length > 0)
        {
            _cache.AddFolderAssets(folderPath, assets);
        }

        // 通知外部：加载完了
        onLoaded?.Invoke(assets ?? Array.Empty<T>());
    }

    #endregion
    #region 卸载优化（性能优化）
    //卸载单个资源
    public void Unload(string path)
    {
        _cache.Remove(path);
        //_loader.Unload(path);
    }

    //卸载所有未使用的资源，一般在场景切换结束时调用
    public void UnloadUnusedAssets()
    {
        _cache.ClearUnused();
        _loader.UnloadUnused();
    }
    #endregion

    private void OnDestroy()
    {
        //程序退出时清理所有资源
        if (Instance == this)
        {
            _cache.ClearAll();
            _loader.UnloadAll();
            //事件注销
            //如果已被销毁则直接跳过
            if (GameGlobalState.IsQuitting) return;
            // 注销本系统所有事件订阅
            _logic.UnlistenAllEvent();
        }
    }

    //订阅事件方法
    public void SubscribeAllEvents()
    {
        
    }



    //监听事件方法

    //监听初始化棋子棋盘事件
    //void OnInitCrateChessPrefabs(CreateInitChessPrefabs e)
    //{
    //    var pack = e.package;
    //    if (pack.Get<ChessBoardPrefabConfig>(EventPackName.CHESSBOARD_PREFAB,out var boardConfig)) { }
    //    if (pack.Get<ChessmanPrefabsConfig>(EventPackName.CHESSMAN_PREFABS,out var chessmanConfig)) { }

    //    if (boardConfig == null || chessmanConfig == null)
    //    {
    //        Debug.LogError("[AssetManager]配置不存在");
    //        return;
    //    }

    //    var board = Load<GameObject>(boardConfig.Path);
    //    var manDict = LoadAll<GameObject>(chessmanConfig.Path);
        
    //    pack.Put(EventPackName.CHESSBOARD_ISPREFAB,board);
    //    pack.Put(EventPackName.CHESSMAN_ISPREFABS,manDict);
        

    //    EventManager.Instance.EmitLogic(new AssetInitPrefabsLoadAll { package = pack });
    //}











    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
