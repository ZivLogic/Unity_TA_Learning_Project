using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using System.Linq;

public class ConfigManager : MonoBehaviour
{
    //关于反射，我的理解是Class类型本身不能直接作为字典的T值进行传入识别，所以需要通过Type转换出来，但是Type本身是动态类型，
    //所以现需要先把方法转换成动态的，再通过安全转换转为通用方法，就变相实现了通过配置获取类。这个先转动态再转通用的方法，我理解为反射

    //单例
    public static ConfigManager Instance { get; private set; }

    //缓存器，缓存对象，方便取值
    private Dictionary<Type, Dictionary<string, object>> _configTypeCache = new();
    //配置索引
    private Dictionary<string, ConfigIndexData> _configIndexDict = new();

    //事件回调逻辑器
    public ConfigLogic _logic;
    public ConfigPublish _publish;

    #region 初始化与事件
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        //加载全局元配置ConfigIndex
        var indexData = LoadDictConfig<ConfigIndexData>(ConfigPath.GAME_CONFIGINDEX);
        //检验是否启用配置，如果不起用，删除多余配置
        var keys = indexData.Keys.ToList();
        foreach (var key in keys)
        {
            if ( !indexData[key].IsEnable)
            {
                indexData.Remove(key);
            }
        }
        _configIndexDict = indexData;

        //初始化事件逻辑器
        _logic = new ConfigLogic();
        _publish = new ConfigPublish();

        //事件器初始化
        SystemInit();

        //订阅事件
        SubscribeAllEvents();

        Debug.Log("[ConfigManager]初始化完成，元配置加载完毕");
        //Debug.Log($"ChessBoardConfig完整类型名:{typeof(ChessBoardConfig).FullName}");
    }
    #endregion
    #region 【核心通用方法】GetConfig<T>(key) -> 自动反射加载
    public T GetConfig<T>(string key) where T : class
    {
        //安全校验
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogError($"[ConfigManager]获取配置失败，{key}为空！");
                return null;
        }
        //先从缓存获取
        Type targetType = typeof(T);
        if (_configTypeCache.TryGetValue(targetType,out var typeCache))
        {
            if (typeCache.TryGetValue(key,out var cachedObj))
            {
                return cachedObj as T;
            }
        }
        else
        {
            //该类型不存在，初始化一个新字典
            typeCache = new Dictionary<string, object>();
            _configTypeCache[targetType] = typeCache;
        }
        //缓存没有，进入加载流程
        return LoadConfigInternal<T>(key);
    }

    private T LoadConfigInternal<T>(string key) where T : class
    {
        //从全局元配置ConfigIndex中获取信息
        if (!_configIndexDict.TryGetValue(key, out var meta))
        {
            Debug.LogError($"[ConfigManager]全局元配置中不存在Key:{key}");
            return null;
        }
        //Debug.Log($"元配置找到：ConfigType = {meta.ConfigType},ConfigPath = {meta.ConfigPath}");
        //解析Type字符串
        Type configType = Type.GetType(meta.ConfigType);
        if (configType == null)
        {
            Debug.LogError($"[ConfingManager]无法找到类型:{meta.ConfigType}");
            return null;
        }
        //Debug.Log($"类型解析成功：{configType.FullName}");
        //拆分真实key
        //string actualKey = key;
        //if (!string.IsNullOrEmpty(meta.KeySplit) && key.Contains(meta.KeySplit))
        //{
        //    actualKey = key.Split(new[] { meta.KeySplit }, StringSplitOptions.None)[^1];
        //}
        try
        {
            //动态生成泛型方法
            MethodInfo method = typeof(ConfigManager).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == nameof(LoadDictConfig) && m.IsGenericMethod);
            if (method == null)
            {
                Debug.LogError($"[ConfigManager]找不到LoadDictConfig方法");
            }
            //Debug.Log($"[1]method是否为空：{method == null}");

            //绑定泛型参数
            MethodInfo genericMethod = method.MakeGenericMethod(configType);
            //Debug.Log($"[2]genericMethod是否为空：{genericMethod == null}");

            //执行方法，得到对应字典对象
            object dictObj = genericMethod.Invoke(this, new object[] { meta.ConfigPath });
            //Debug.Log($"dictObj类型：{dictObj?.GetType()},是否为null:{dictObj == null}");

            //安全转换
            var dictType = typeof(Dictionary<,>).MakeGenericType(typeof(string) ,configType);
            //Debug.Log($"[4]dictType是否为空：{dictType == null}");

            if (dictObj != null && dictObj.GetType() == dictType)
            {
                var prop = dictType.GetProperty("Item",typeof(object),new[] {typeof(string)});
                if (prop == null)
                {
                    //Debug.LogError("GetProperty返回null!尝试其他方法获取索引器");
                    //备用方案：不指定参数类型
                    prop = dictType.GetProperty("Item");
                    //Debug.Log($"[5]备用：prop是否为空：{prop == null}");
                }
                var configObj = prop.GetValue(dictObj, new object[] {key});
                //Debug.Log($"[6]configObj是否为空：{configObj == null}");

                if(configObj != null)
                {
                    _configTypeCache[typeof(T)][key] = configObj;
                    return configObj as T;
                }
                else
                {
                    Debug.LogError($"[ConfigManager]配置表{meta.ConfigPath}中找不到具体项：{key}");
                    return null;
                }
            }
            else
            {
                Debug.LogError($"[ConfigManager]加载失败，路径{meta.ConfigPath}不是标准字典格式。");
                return null;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[ConfigManager]加载配置发生异常！key:{key} 异常信息：{e.Message}");
            return null;
        }
    }
    #endregion
    #region  【基础加载工具】
    private Dictionary<string, T> LoadDictConfig<T>(string path) where T : class     //where T : class表示的是确保T是可引用类型，而不是int,float这种类型。
    {
        ///通过JSON工具加载配置
        Dictionary<string, T> config = JsonUtils.LoadDictFromResources<T>(path);     //获取配置，path为路径名称
        
        return config;                           
    }
    #endregion
    #region 【批量加载工具】
    public Dictionary<string, T> GetConfigs<T>(string[] keys) where T : class
    {
        Dictionary<string, T> result = new();
        foreach (var key in keys)
        {
            var cfg = GetConfig<T>(key);
            if (cfg != null)
                result.Add(key, cfg);
        }
        return result;
    }
    #endregion
    #region 【整张表批量加载】
    public Dictionary<string, T> GetAllConfigsInTable<T>(string tableKey) where T : class
    {
        Type targetType = typeof(T);
        //查原配置
        if (!_configIndexDict.TryGetValue(tableKey, out var meta))
        {
            Debug.LogError($"表{tableKey}不在元配置里");
            return null;
        }
        //必须标记成表
        if (!meta.IsTable)
        {
            Debug.LogError($"{tableKey}不是表类型，不能批量加载");
            return null;
        }
        //先看看缓存里有没有整张表
        if (_configTypeCache.TryGetValue(targetType, out var cache))
        {
            //简单判断，有缓存就直接返回同类型所有
            var dict = new Dictionary<string, T>();
            foreach (var kv  in cache)
            {
                if (kv.Value is T tVal)
                    dict[kv.Key] = tVal;
            }
            if (dict.Count > 0)
                return dict;
        }

        //没缓存，直接加载整张JSON字典
        var fullTable = LoadDictConfig<T>(meta.ConfigPath);
        if (fullTable == null)
        {
            Debug.LogError($"加载表失败：{meta.ConfigPath}");
            return null;
        }
        //写入缓存
        if (!_configTypeCache.ContainsKey(targetType)) _configTypeCache[targetType] = new Dictionary<string, object>();
        var typeCache = _configTypeCache[targetType];
        foreach (var kv in fullTable)
        {
            typeCache[kv.Key] = kv.Value;
        }
        return fullTable;
    }
    #endregion
    #region 转键值/混合批量
    //事件值object[]转换为string
    //通用方法
    private bool TryGetConfigKeyFromParams(object[] data, out string configKey)
    {
        configKey = null;
        //空参数转换失败
        if (data == null || data.Length == 0)
            return false;
        //取第一个参数
        var firstObj = data[0];
        //如果已经是string
        if (firstObj is string keyStr)
        {
            configKey = keyStr;
            return !string.IsNullOrEmpty(configKey);
        }
        //不是string，尝试安全转字符串
        try
        {
            configKey = firstObj?.ToString();
            return !string.IsNullOrEmpty(configKey);
        }
        catch
        {
            return false;
        }
    }

    //批量方法
    private string[] GetConfigKeyArray(object[] data)
    {
        if (data == null || data.Length == 0)
            return Array.Empty<string>();
        //按顺序收集有效字符串Key
        List<string> keyList = new List<string>();
        foreach (object obj in data)
        {
            if (obj is string key && !string.IsNullOrEmpty(key))
            {
                keyList.Add(key);
            }
        }
        return keyList.ToArray();
    }

    //安全取值
    private string[] GetConfigKeyArrayStrict(object[] data)
    {
        var keys = GetConfigKeyArray(data);

        foreach (var key in keys)
        {
            if (!_configIndexDict.ContainsKey(key))
            {
                Debug.LogError($"配置Key不存在：{key}");
            }
        }
        return keys;
    }

    //混合类型批量获取方法
    //只在初始化时调用这个方法，因为是不确定类，所以需要在拆分数据时主动强转类型
    public IReadOnlyDictionary<string, object> GetConfigsMixed(params string[] configKeys)
    {
        Dictionary<string, object> result = new Dictionary<string, object>();
        foreach (string key in configKeys)
        {
            //差元配置索引
            if (!_configIndexDict.TryGetValue(key,out var indexData))
            {
                Debug.LogError($"[ConfigManager]索引不存在：{key}");
                continue;
            }
            //解析类型
            Type configType = Type.GetType(indexData.ConfigType);
            if (configType == null)
            {
                Debug.LogError($"[ConfigManager]类型解析失败：{indexData.ConfigType}");
                continue;
            }
            //切割字符
            //string actualKey = key;
            //if (!string.IsNullOrEmpty(indexData.KeySplit) && key.Contains(indexData.KeySplit))
            //{
            //    actualKey = key.Split(new[] { indexData.KeySplit }, StringSplitOptions.None)[^1];
            //}
            //先读全局缓存
            if (_configTypeCache.TryGetValue(configType, out var typeCache) && typeCache.TryGetValue(key, out var cachedConfig))
            {
                    result[key] = cachedConfig;
                    continue;
            }
            try
            {
                //反射构造泛型
                var genericMethod = GetType().GetMethod(nameof(GetConfig), BindingFlags.Public | BindingFlags.Instance).MakeGenericMethod(configType);
                //执行加载
                object config = genericMethod.Invoke(this, new object[] { key });
                result[key] = config;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ConfigManager]加载失败{key}:{e.Message}");
            }
        }
        return result;
    }
    #endregion
    #region  事件逻辑器专用
    // 场景销毁/模块关闭时 调用注销
    private void OnDestroy()
    {
        //如果已被销毁则直接跳过
        if (GameGlobalState.IsQuitting) return;
        // 注销本系统所有事件订阅
        _logic.UnlistenAllEvent();
    }

    // 外部要调用系统方法 转发给内核
    public void SystemInit()
    {
        _logic.Init();
        _publish.Init();
    }
    #endregion
    //事件订阅方法
    public void SubscribeAllEvents()
    {
        EventManager.Instance.Listen<GameInit>(InitConfig);
       
    }

    //初始化方法
    private void InitConfig(PackageEvent e)
    {
        

    }


    //监听事件

    //init初始化配置
    //void InitChessConfig(InitChessEvent e)
    //{
    //    //拆包
    //    var pack = e.package;
    //    if (pack.Get<ChessBoardConfig>(EventPackName.CHESSBOARD_CONFIG , out var boardConfig)) { }
        
    //    if (pack.Get<ChessBoardTileConfig>(EventPackName.CHESSBOARDTILE_CONFIG ,out var tileConfig)) { }

    //    if (pack.Get<ChessmanPositionConfig>(EventPackName.CHESSMAN_POSITIONCONFIG ,out var positionConfig)) { }

    //    if (pack.Get<ChessBoardPrefabConfig>(EventPackName.CHESSBOARD_PREFAB,out var boardPrefabConfig)) { }

    //    if (pack.Get<ChessmanPrefabsConfig>(EventPackName.CHESSMAN_PREFABS,out var manPrefabsConfig)) { }

    //    if (
    //        boardConfig == null || 
    //        tileConfig == null || 
    //        positionConfig == null || 
    //        boardPrefabConfig == null || 
    //        manPrefabsConfig == null
    //        ) 
    //    { Debug.LogError("[ConfigManager]配置不存在"); return; }

    //    //读配置
    //    var board = GetConfig<ChessBoardConfig>(boardConfig.ID);
    //    var tile  = GetConfig<ChessBoardTileConfig>(tileConfig.ID);
    //    var posDict = GetAllConfigsInTable<ChessmanPositionConfig>(positionConfig.ID);
    //    var boardPfb = GetConfig<ChessBoardPrefabConfig>(boardPrefabConfig.ID);
    //    var manPfb = GetConfig<ChessmanPrefabsConfig>(manPrefabsConfig.ID);

    //    //打包
    //    var Pack = new Package();
    //    Pack.Put(EventPackName.CHESSBOARD_CONFIG,board);
    //    Pack.Put(EventPackName.CHESSBOARDTILE_CONFIG,tile);
    //    Pack.Put(EventPackName.CHESSMAN_POSITIONCONFIG, posDict);
    //    Pack.Put(EventPackName.CHESSBOARD_PREFAB,boardPfb);
    //    Pack.Put(EventPackName.CHESSMAN_PREFABS,manPfb);

    //    //if (manPfb == null) { Debug.LogError("man空"); }

    //    //判断
    //    //if (boardConfig.IsPrefab && positionConfig.IsPrefab) { EventManager.Instance.EmitLogic(new CreateInitChessPrefabs { package = Pack }); }

        
    //    //事件
    //    EventManager.Instance.EmitLogic(new InitChessConfig { package = Pack });
    //}

    //获取创建棋盘配置
    //void OnRequestChessBoardConfig(object[] data)
    //{
    //    if (!TryGetConfigKeyFromParams(data, out string configKey))
    //    {
    //        Debug.LogError("配置Key获取失败");
    //        return;
    //    }

    //    var config = GetConfig<ChessBoardConfig>(configKey);
    //    EventManager.Instance.EmitLogic(EventID.Config_ResponseBoardConfig,config);
    //}

    ////获取棋盘格配置
    //void OnRequestBoardTileConfig(object[] data)
    //{
    //    if (!TryGetConfigKeyFromParams(data, out string configKey))
    //    {
    //        Debug.LogError("配置Key获取失败");
    //        return;
    //    }
    //    var config = GetConfig<ChessBoardTileConfig>(configKey);
    //    EventManager.Instance.EmitLogic(EventID.Config_ResponseBoardTileConfig,config);
    //}

    ////获取创建棋子配置（位置）
    //void OnRequestChessmanPositionConfing(object[] data)
    //{
    //    if (!TryGetConfigKeyFromParams(data, out string configKey))
    //    {
    //        Debug.LogError("配置Key获取失败");
    //        return;
    //    }
    //    var config = GetAllConfigsInTable<ChessmanPositionConfig>(configKey);
    //    EventManager.Instance.EmitLogic(EventID.Config_ResponseChessmanPostionConfig, config);
    //}

    ////获取棋子属性配置
    //void OnRequestChessmanConfig(object[] data)
    //{
    //    if (!TryGetConfigKeyFromParams(data, out string configKey))
    //    {
    //        Debug.LogError("配置Key获取失败");
    //        return;
    //    }
    //    var config = GetAllConfigsInTable<ChessmanConfig>(configKey);
    //    EventManager.Instance.EmitLogic(EventID.Config_ResponseChessmanConfig, config);
    //}


    //void OnInputConfig(InitInputConfig e)
    //{
    //    //定位配置模板地址
    //    var inputCfg = new InputKeyConfig { };
    //    var selectCfg = new SelectObjectConfig { };
    //    var interceptorCfg = new InputInterceptorConfig { };
    //    //访问元配置
    //    var inputDict = GetAllConfigsInTable<InputKeyConfig>(inputCfg.ID);
    //    var selectDict = GetAllConfigsInTable<SelectObjectConfig>(selectCfg.ID);
    //    var interceptorDict = GetAllConfigsInTable<InputInterceptorConfig>(interceptorCfg.ID);
    //    var Pack = new Package();
    //    Pack.Put(EventPackName.INPUT_GETCONFIG, inputDict);
    //    Pack.Put(EventPackName.INPUT_SELECTCONFIG, selectDict);
    //    Pack.Put(EventPackName.INPUT_INTERCEPTORCONFIG, interceptorDict);
    //    EventManager.Instance.EmitLogic(new IsInputConfig { package = Pack });
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
