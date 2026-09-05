
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 配置管理器：负责配置的发现、依赖检查、排序、加载
/// </summary>
public class ConfigManager : MonoBehaviour
{
    private static ConfigManager _instance;
    public static ConfigManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ConfigManager>();
                if (_instance == null)
                {
                    var go = new GameObject("ConfigManager");
                    _instance = go.AddComponent<ConfigManager>();
                }
            }
            return _instance;
        }
    }

    // 已加载的配置
    private readonly Dictionary<string, ConfigDefinition> _loadedConfigs = new();

    // 配置元信息
    private readonly Dictionary<string, ConfigMetaInfo> _metaInfos = new();

    // 加载顺序
    private readonly List<string> _loadOrder = new();

    // 配置就绪事件
    public event Action<string, ConfigDefinition> OnConfigLoaded;
    public event Action OnAllConfigsLoaded;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // 1. 扫描所有配置类
        ScanAllConfigClasses();

        // 2. 检查依赖合法性
        ValidateDependencies();

        // 3. 按优先级排序
        SortByPriority();

        // 4. 加载所有配置
        LoadAllConfigs();
    }

    #region 1. 扫描配置类

    /// <summary>
    /// 自动扫描所有继承 ConfigDefinition 且带 [Config] 标记的类
    /// </summary>
    private void ScanAllConfigClasses()
    {
        _metaInfos.Clear();

        // 获取所有程序集中的类型
        var configTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly =>
            {
                try { return assembly.GetTypes(); }
                catch { return Array.Empty<Type>(); }
            })
            .Where(type =>
                typeof(ConfigDefinition).IsAssignableFrom(type) &&
                !type.IsAbstract &&
                type.GetCustomAttribute<ConfigAttribute>() != null
            );

        foreach (var type in configTypes)
        {
            var attr = type.GetCustomAttribute<ConfigAttribute>();

            var metaInfo = new ConfigMetaInfo
            {
                ConfigId = attr.ConfigId,
                JsonPath = attr.JsonPath ?? $"{attr.ConfigId}.json",
                Priority = attr.Priority,
                DependsOn = attr.DependsOn ?? Array.Empty<string>(),
                ConfigType = type
            };

            _metaInfos[metaInfo.ConfigId] = metaInfo;

            Debug.Log($"[ConfigManager] 发现配置: {metaInfo.ConfigId} (优先级: {metaInfo.Priority})");
        }
    }

    #endregion

    #region 2. 依赖检查

    /// <summary>
    /// 检查依赖合法性：
    /// 1. 依赖的配置必须存在
    /// 2. 依赖的配置优先级必须比当前配置低（先加载）
    /// 3. 不能有循环依赖
    /// </summary>
    private void ValidateDependencies()
    {
        Debug.Log("[ConfigManager] 开始依赖检查...");

        foreach (var metaInfo in _metaInfos.Values)
        {
            foreach (var depId in metaInfo.DependsOn)
            {
                // 检查1：依赖的配置是否存在
                if (!_metaInfos.ContainsKey(depId))
                {
                    Debug.LogError(
                        $"[ConfigManager] ❌ 配置 '{metaInfo.ConfigId}' 依赖的配置 '{depId}' 不存在！\n" +
                        $"  请检查 [Config] 标记中的 dependsOn 参数。"
                    );
                    continue;
                }

                // 检查2：依赖的配置优先级必须更低（先加载）
                var depMeta = _metaInfos[depId];
                if (depMeta.Priority >= metaInfo.Priority)
                {
                    Debug.LogError(
                        $"[ConfigManager] ❌ 配置 '{metaInfo.ConfigId}' (优先级:{metaInfo.Priority}) " +
                        $"依赖的配置 '{depId}' (优先级:{depMeta.Priority}) 优先级不合法！\n" +
                        $"  被依赖的配置优先级必须更小（更早加载）。\n" +
                        $"  请修改 '{depId}' 的优先级为小于 {metaInfo.Priority} 的值。"
                    );
                }
                else
                {
                    Debug.Log($"  ✅ '{metaInfo.ConfigId}' 依赖 '{depId}' (优先级:{depMeta.Priority} < {metaInfo.Priority})");
                }
            }
        }

        // 检查3：循环依赖检测
        CheckCircularDependencies();

        Debug.Log("[ConfigManager] 依赖检查完成");
    }

    /// <summary>
    /// 检测循环依赖
    /// </summary>
    private void CheckCircularDependencies()
    {
        var visited = new HashSet<string>();
        var visiting = new HashSet<string>();

        void Visit(string configId)
        {
            if (visiting.Contains(configId))
            {
                Debug.LogError($"[ConfigManager] ❌ 检测到循环依赖: {configId}");
                return;
            }

            if (visited.Contains(configId)) return;

            visiting.Add(configId);

            if (_metaInfos.TryGetValue(configId, out var metaInfo))
            {
                foreach (var depId in metaInfo.DependsOn)
                {
                    Visit(depId);
                }
            }

            visiting.Remove(configId);
            visited.Add(configId);
        }

        foreach (var configId in _metaInfos.Keys)
        {
            Visit(configId);
        }
    }

    #endregion

    #region 3. 优先级排序

    /// <summary>
    /// 按优先级排序（优先级小的先加载）
    /// </summary>
    private void SortByPriority()
    {
        _loadOrder.Clear();
        _loadOrder.AddRange(_metaInfos.Values
            .OrderBy(meta => meta.Priority)
            .ThenBy(meta => meta.ConfigId)
            .Select(meta => meta.ConfigId)
        );

        Debug.Log($"[ConfigManager] 配置加载顺序:\n  {string.Join(" → ", _loadOrder)}");
    }

    #endregion

    #region 4. 加载配置

    /// <summary>
    /// 按顺序加载所有配置
    /// </summary>
    private void LoadAllConfigs()
    {
        foreach (var configId in _loadOrder)
        {
            LoadConfig(configId);
        }

        OnAllConfigsLoaded?.Invoke();
        Debug.Log($"[ConfigManager] ✅ 所有配置加载完成，共 {_loadedConfigs.Count} 个");
    }

    /// <summary>
    /// 加载单个配置
    /// </summary>
    private void LoadConfig(string configId)
    {
        if (_loadedConfigs.ContainsKey(configId)) return;

        var metaInfo = _metaInfos[configId];
        ConfigDefinition config = null;

        try
        {
            // 1. 尝试从 JSON 文件加载
            string fullPath = Path.Combine(Application.dataPath, "Config/ConfigData", metaInfo.JsonPath);

            if (File.Exists(fullPath))
            {
                string jsonText = File.ReadAllText(fullPath);

                // 使用反射调用 ConfigParser.Parse<T>
                var parseMethod = typeof(ConfigParser).GetMethod("Parse");
                var genericParse = parseMethod.MakeGenericMethod(metaInfo.ConfigType);
                config = genericParse.Invoke(null, new object[] { jsonText, this }) as ConfigDefinition;

                Debug.Log($"[ConfigManager] 从文件加载配置: {configId} ← {metaInfo.JsonPath}");
            }
            else
            {
                // 2. 创建默认实例
                config = Activator.CreateInstance(metaInfo.ConfigType) as ConfigDefinition;
                Debug.LogWarning($"[ConfigManager] ⚠️ 配置文件不存在，使用默认值: {configId} (查找路径: {fullPath})");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[ConfigManager] ❌ 加载配置失败: {configId}\n{e}");
            config = Activator.CreateInstance(metaInfo.ConfigType) as ConfigDefinition;
        }

        // 缓存配置
        _loadedConfigs[configId] = config;

        // 触发就绪事件
        OnConfigLoaded?.Invoke(configId, config);
    }

    #endregion

    #region 公共接口

    /// <summary>
    /// 获取配置
    /// </summary>
    public T GetConfig<T>() where T : ConfigDefinition
    {
        var configId = typeof(T).GetCustomAttribute<ConfigAttribute>()?.ConfigId ?? typeof(T).Name;
        return GetConfig<T>(configId);
    }

    /// <summary>
    /// 根据配置ID获取配置
    /// </summary>
    public T GetConfig<T>(string configId) where T : ConfigDefinition
    {
        if (_loadedConfigs.TryGetValue(configId, out var config))
        {
            return config as T;
        }

        Debug.LogError($"[ConfigManager] ❌ 配置未找到: {configId}");
        return null;
    }

    /// <summary>
    /// 判断配置是否已加载
    /// </summary>
    public bool IsConfigLoaded(string configId)
    {
        return _loadedConfigs.ContainsKey(configId);
    }

    /// <summary>
    /// 获取所有已加载的配置ID
    /// </summary>
    public string[] GetLoadedConfigIds()
    {
        return _loadedConfigs.Keys.ToArray();
    }

    /// <summary>
    /// 获取配置元信息（用于调试）
    /// </summary>
    public string GetConfigInfo(string configId)
    {
        if (_metaInfos.TryGetValue(configId, out var meta))
        {
            return $"配置: {meta.ConfigId}\n" +
                   $"  文件: {meta.JsonPath}\n" +
                   $"  优先级: {meta.Priority}\n" +
                   $"  依赖: {(meta.DependsOn.Length > 0 ? string.Join(", ", meta.DependsOn) : "无")}";
        }
        return "配置不存在";
    }

    #endregion

    #region 内部类

    /// <summary>
    /// 配置元信息
    /// </summary>
    private class ConfigMetaInfo
    {
        public string ConfigId;
        public string JsonPath;
        public int Priority;
        public string[] DependsOn;
        public Type ConfigType;
    }

    #endregion


    /// <summary>
    /// 解析引用路径
    /// 格式：configId.fieldPath
    /// 示例：physics_configs.tree_standard
    ///       material_database.materials["bread_tree"].path
    ///       map_spawn_points.spawnPoints[0].position
    /// </summary>
    public object ResolveReference(string refPath)
    {
        if (string.IsNullOrEmpty(refPath))
            return null;

        // 清理：移除可能的引号和空格
        refPath = refPath.Trim().TrimEnd('"').TrimEnd('\'');

        Debug.Log($"[ConfigManager] 解析引用路径: {refPath}");

        // 分割：configId.fieldPath
        string[] parts = refPath.Split('.');
        if (parts.Length < 2)
        {
            Debug.LogWarning($"[ConfigManager] 引用格式错误: {refPath}");
            return null;
        }

        string configId = parts[0];
        string fieldPath = string.Join(".", parts.Skip(1));

        // 获取配置对象
        var config = GetConfigRaw(configId);
        if (config == null)
        {
            Debug.LogWarning($"[ConfigManager] 引用的配置不存在: {configId}");
            return null;
        }

        // 解析字段路径
        return ResolveFieldPath(config, fieldPath);
    }

    private object ResolveFieldPath(object obj, string fieldPath)
    {
        if (obj == null || string.IsNullOrEmpty(fieldPath))
            return null;

        Debug.Log($"[ConfigManager] 解析字段路径: {fieldPath}");

        // 按 . 分割，但要保留方括号内的内容
        string[] segments = SplitFieldPath(fieldPath);

        foreach (var segment in segments)
        {
            if (obj == null) return null;

            string cleanSegment = segment.Trim();

            Debug.Log($"[ConfigManager] 处理片段: {cleanSegment}");

            // 处理数组索引: fieldName[0]
            if (cleanSegment.Contains("["))
            {
                obj = ResolveIndexedField(obj, cleanSegment);
            }
            // 普通字段
            else
            {
                var field = obj.GetType().GetField(cleanSegment);
                if (field == null)
                {
                    Debug.LogWarning($"[ConfigManager] 字段不存在: {cleanSegment}");
                    return null;
                }
                obj = field.GetValue(obj);
            }
        }

        return obj;
    }

    /// <summary>
    /// 分割字段路径，保留方括号内容
    /// </summary>
    private string[] SplitFieldPath(string fieldPath)
    {
        var segments = new List<string>();
        int start = 0;
        bool inBracket = false;

        for (int i = 0; i < fieldPath.Length; i++)
        {
            char c = fieldPath[i];

            if (c == '[')
            {
                inBracket = true;
            }
            else if (c == ']')
            {
                inBracket = false;
            }
            else if (c == '.' && !inBracket)
            {
                // 只在不在方括号内时分割
                segments.Add(fieldPath.Substring(start, i - start));
                start = i + 1;
            }
        }

        // 添加最后一段
        if (start < fieldPath.Length)
        {
            segments.Add(fieldPath.Substring(start));
        }

        return segments.ToArray();
    }

    private object ResolveIndexedField(object obj, string segment)
    {
        int bracketStart = segment.IndexOf('[');
        int bracketEnd = segment.IndexOf(']');

        if (bracketStart < 0 || bracketEnd < 0)
            return null;

        string fieldName = segment.Substring(0, bracketStart).Trim();
        string indexStr = segment.Substring(bracketStart + 1, bracketEnd - bracketStart - 1).Trim();

        Debug.Log($"[ConfigManager] 索引访问: {fieldName}[{indexStr}]");

        // 清理索引字符串：移除引号
        indexStr = indexStr.Trim('"').Trim('\'');

        var field = obj.GetType().GetField(fieldName);
        if (field == null)
        {
            Debug.LogWarning($"[ConfigManager] 字段不存在: {fieldName}");
            return null;
        }

        var fieldValue = field.GetValue(obj);
        if (fieldValue == null) return null;

        // 数组访问
        if (fieldValue is Array array)
        {
            if (int.TryParse(indexStr, out int index))
            {
                return array.GetValue(index);
            }
        }
        // List 访问
        else if (fieldValue is System.Collections.IList list)
        {
            if (int.TryParse(indexStr, out int index))
            {
                return list[index];
            }
        }
        // 字典访问（支持字符串键）
        else if (fieldValue is System.Collections.IDictionary dict)
        {
            return dict[indexStr];
        }

        return null;
    }

    /// <summary>
    /// 获取原始配置对象（不经过类型转换）
    /// </summary>
    public ConfigDefinition GetConfigRaw(string configId)
    {
        if (_loadedConfigs.TryGetValue(configId, out var config))
        {
            return config;
        }

        Debug.LogWarning($"[ConfigManager] 配置未加载: {configId}");
        return null;
    }

    /// <summary>
    /// 通用字典键查找
    /// </summary>
    public T FindByKey<T>(string configId, string keyField, string keyValue) where T : class
    {
        var config = GetConfigRaw(configId);
        if (config == null) return null;

        // 查找数组字段
        var arrayField = config.GetType().GetFields()
            .FirstOrDefault(f => f.FieldType.IsArray);

        if (arrayField == null) return null;

        var array = arrayField.GetValue(config) as Array;
        if (array == null) return null;

        // 遍历匹配
        foreach (var item in array)
        {
            var keyFieldInfo = item.GetType().GetField(keyField);
            if (keyFieldInfo == null) continue;

            var value = keyFieldInfo.GetValue(item)?.ToString();
            if (value == keyValue)
            {
                return item as T;
            }
        }

        Debug.LogWarning($"[ConfigManager] 字典键查找失败: {configId}[{keyField}={keyValue}]");
        return null;
    }

    // 物理配置
    public PhysicsConfigs.PhysicsData FindPhysicsConfig(string configId)
        => FindByKey<PhysicsConfigs.PhysicsData>("physics_configs", "configId", configId);

    // 材质配置
    public MaterialDatabaseConfig.MaterialData FindMaterialConfig(string materialId)
        => FindByKey<MaterialDatabaseConfig.MaterialData>("material_database", "materialId", materialId);

    // 逻辑脚本
    public LogicScriptsConfig.ScriptData FindLogicScript(string scriptId)
        => FindByKey<LogicScriptsConfig.ScriptData>("logic_scripts", "scriptId", scriptId);

    // 对象池
    public ObjectPoolsConfig.PoolData FindObjectPool(string poolId)
        => FindByKey<ObjectPoolsConfig.PoolData>("object_pools", "poolId", poolId);

    // UI预制体
    public UIPrefabsConfig.UIPrefab FindUIPrefab(string prefabId)
        => FindByKey<UIPrefabsConfig.UIPrefab>("ui_prefabs", "prefabId", prefabId);

    //音频
    public AudioConfigs.AudioData FindAudioConfig(string audioId)
        => FindByKey<AudioConfigs.AudioData>("audio_configs", "audioId", audioId);

    //地形
    public TerrainConfigs.TerrainData FindTerrainConfig(string terrainId)
        => FindByKey<TerrainConfigs.TerrainData>("terrain_configs", "terrainId", terrainId);
}