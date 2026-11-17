using EventSystemV2;
using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("远程配置地址（末尾带 /）")]
    [SerializeField] private string remoteBaseUrl = "http://127.0.0.1:8000/Main_Config/";

    [Header("事件配置远程地址（末尾带 /）")]
    [SerializeField] private string eventConfigBaseUrl = "http://127.0.0.1:8000/Event_Config/";


    [Header("是否启用远程配置检查")]
    [SerializeField] private bool enableRemoteCheck = true;

    private readonly MainConfigSource _mainConfigSource = new MainConfigSource();

    private readonly EventConfigSource _eventConfigSource = new EventConfigSource();


    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private IEnumerator Start()
    {
        Debug.Log("================= 启动阶段 1：基础系统 ================");
        CreateSystem<AppLifecycleInitializer>();
        CreateSystem<EventManager>();
        CreateSystem<InputManager>();
        CreateSystem<ConfigManager>();
        CreateSystem<ConfigHotUpdater>();

        // 等 ConfigManager.Awake 跑完（它同步加载本地配置）
        yield return null;

        Debug.Log("================= 启动阶段 2：配置更新 ================");
        yield return CheckAndUpdateConfig();

        Debug.Log("================= 启动阶段 3：依赖配置的系统 ================");
        CreateSystem<MasterFactory>();
        CreateSystem<LogicStateManager>();
        CreateSystem<ProductionLauncher>();
        CreateSystem<EntityManager>();
        CreateSystem<InputMediator>();

        yield return null;

        Debug.Log("================= 启动阶段 4：游戏初始化 ================");
        InitGame();
    }

    private IEnumerator CheckAndUpdateConfig()
    {
        if (!enableRemoteCheck)
        {
            Debug.Log("[GameManager] 已关闭远程检查，使用本地配置");
            yield break;
        }

        // 主配置热更
        yield return CheckAndUpdateOne(
            _mainConfigSource,
            remoteBaseUrl + "version.json",
            remoteBaseUrl);

        // 事件配置热更
        yield return CheckAndUpdateOne(
            _eventConfigSource,
            eventConfigBaseUrl + "version.json",
            eventConfigBaseUrl);
    }

    private IEnumerator CheckAndUpdateOne(IConfigSource source, string versionUrl, string fileBaseUrl)
    {
        bool needUpdate = false;
        ConfigVersionInfo remote = null;

        yield return ConfigHotUpdater.Instance.CheckRemoteVersion(
            source, versionUrl,
            (need, info) => { needUpdate = need; remote = info; });

        if (!needUpdate)
        {
            Debug.Log($"[GameManager] {source.SourceId} 配置已最新");
            yield break;
        }

        bool ok = false;
        string msg = null;

        yield return ConfigHotUpdater.Instance.DownloadAndApply(
            source, remote,
            fileName => fileBaseUrl + fileName,
            (success, message) => { ok = success; msg = message; });

        if (!ok)
        {
            Debug.LogError($"[GameManager] {source.SourceId} 配置更新失败: {msg}，继续用旧配置");
            yield break;
        }

        Debug.Log($"[GameManager] {source.SourceId} 配置已更新到 {remote.version}");
    }



    private void CreateSystem<T>() where T : MonoBehaviour
    {
        var sysObj = new GameObject("[" + typeof(T).Name + "]");
        sysObj.AddComponent<T>();
        DontDestroyOnLoad(sysObj);
    }

    private void InitGame()
    {
        Debug.Log("================= 游戏初始化 ================");
        ProductionLauncher.Instance.Launch();
        LogicStateManager.Instance.LoadAllStates();
        // MasterFactory.Instance.MapFactory.LoadMap("test_map");
        Debug.Log("================= 游戏初始化完成 ===================");
    }
}