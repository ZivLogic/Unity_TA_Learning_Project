using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;


//逻辑状态管理器：统一保存/加载所有逻辑状态

//保存流程：
//1.读取顶级注册表
//2.遍历每个父容器
//3.收集所有脚本实例的数据
//4.写入对应的父容器JSON

//加载流程：
//1.读取顶级注册表
//2.遍历每个父容器
//3.遍历父容器种的子容器
//4.通过UUID查找实体
//5.调用脚本的LoadFromEntry

public class LogicStateManager : MonoBehaviour
{
    private static LogicStateManager _instance;
    public static LogicStateManager Instance => _instance;

    /// <summary>所有逻辑状态是否注入完成</summary>
    public bool IsAllStateInjected { get; private set; }

    /// <summary>反更改标记（重载/阶段回退时用）</summary>
    public void ResetStateInjected()
    {
        IsAllStateInjected = false;
    }

    // 逻辑状态存档目录（运行时写，游戏存一半的状态，永不热更）
    private static string SaveDir => Path.Combine(ConfigPaths.SaveRoot, ConfigDirs.LogicState);

    // 逻辑状态保底默认目录（预留，每局初始重置用，以后走热更）
    private static string DefaultDir => Path.Combine(ConfigPaths.DefaultRoot, ConfigDirs.LogicStateDefault);



    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log("[LogicStateManager] 逻辑脚本数值注入器初始化完成");
    }

    #region 保存
    public void SaveAllStates()
    {
        Debug.Log("[LogicStateManager] ========= 开始保存所有逻辑状态 =========");

        //1.读取顶级注册表
        var registry = ConfigAPI.Get<LogicStateRegistry>();
        if (registry?.entries == null)
        {
            Debug.LogWarning("[LogicStateManager]注册表为空");
            return;
        }

        //遍历每个父容器
        foreach (var entry in registry.entries)
        {
            SaveContainer(entry);
        }

        Debug.Log("[LogicStateManager] ======== 保存完成 =========");
    }

    //保存单个父容器
    private void SaveContainer(LogicStateRegistry.LogicStateEntry entry)
    {
        //1.获取脚本类型
        var scriptType = FindScriptType(entry.scriptName);
        if (scriptType == null)
        {
            Debug.LogError($"[LogicStateManager] 找不到脚本类型：{entry.scriptName}");
            return;
        }

        //2.查找所有该类型的脚本实例
        var scripts = FindAllScripts(scriptType);
        if (scripts.Count == 0)
        {
            Debug.Log($"[LogicStateManager]没有找到{entry.scriptName} 实例");
            return;
        }

        //3，收集数据
        var entries = new List<object>();
        foreach (var script in scripts)
        {
            if (script is LogicScriptBase logicBase)
            {
                var data = logicBase.SaveToEntry();
                if (data != null)
                {
                    entries.Add(data);
                }
            }
        }

        //写入JSON
        WriteContainerToJson(entry, entries);

        Debug.Log($"[LogicStateManager] 保存{entry.scriptName}: {entries.Count}条数据");
    }


    //对象转字典（通用方法）
    private Dictionary<string, object> ConvertToDict(object obj)
    {
        var dict = new Dictionary<string, object>();
        var type = obj.GetType();

        foreach (var field in type.GetFields())
        {
            var value = field.GetValue(obj);
            if (value == null)continue;

            //处理 Vector2Int / Vector3 等
            if (value is Vector2Int v2i)
            {
                dict[field.Name] = new Dictionary<string, object>
                {
                    {"x", v2i.x},
                    {"y", v2i.y}
                };
            }
            else if (value is Vector3 v3i)
            {
                dict[field.Name] = new Dictionary<string, object>
                {
                    {"x", v3i.x},
                    {"y", v3i.y},
                    {"z", v3i.z}
                };
            }
            else if (value is Vector2 v2)
            {
                dict[field.Name] = new Dictionary<string, object>
                {
                    {"x", v2.x},
                    {"y", v2.y}
                };
            }
            else
            {
                dict[field.Name] = value;
            }
        }

        return dict;
    }

    private void WriteContainerToJson(LogicStateRegistry.LogicStateEntry entry, List<object> entries)
    {
        var builder = new ConfigBuilder();

        var entriesJson = new List<object>();
        foreach (var data in entries)
        {
            entriesJson.Add(ConvertToDict(data));
        }

        builder.Add("entries", entriesJson);

        // 写存档目录（savePath 现在是纯文件名）
        if (!Directory.Exists(SaveDir)) Directory.CreateDirectory(SaveDir);
        string path = Path.Combine(SaveDir, entry.savePath);
        builder.ExportToFileFormat(path);

    }
    #endregion

    #region 加载
    //加载所有逻辑状态
    public void LoadAllStates()
    {
        IsAllStateInjected = false;   // 【新增】重载前重置，防提前就绪

        Debug.Log("[LogicStateManager] ========== 开始加载所有逻辑状态 ===========");

        var registry = ConfigAPI.Get<LogicStateRegistry>();
        if (registry?.entries == null)
        {
            Debug.LogWarning("[LogicStateManager] 注册表为空");
            NotifyAllScriptsReady();   // 即使没注册表，也要标"就绪"
            return;
        }

        foreach (var entry in registry.entries)
            LoadContainer(entry);

        NotifyAllScriptsReady();       // 【新增】统一通知
        Debug.Log("[LogicStateManager] =========== 加载完成 ==========");
    }

    //加载单个父容器
    private void LoadContainer(LogicStateRegistry.LogicStateEntry entry)
    {
        // 1. 拿容器类型（复用 ConfigManager 的扫描结果，避免再反射扫一遍）
        var containerType = ConfigManager.Instance.GetConfigType(entry.containerId);
        if (containerType == null)
        {
            Debug.LogWarning($"[LogicStateManager] 容器类型不存在：{entry.containerId}");
            return;
        }

        // 2. 读存档文件（存档目录优先，回退保底默认目录）
        string json = ReadStateFile(entry.savePath);
        if (string.IsNullOrEmpty(json))
        {
            Debug.Log($"[LogicStateManager] 无存档或默认配置，跳过：{entry.savePath}");
            return;
        }

        // 3. 反序列化到容器类型
        object containerConfig;
        try { containerConfig = JsonUtility.FromJson(json, containerType); }
        catch (Exception e)
        {
            Debug.LogError($"[LogicStateManager] 状态反序列化失败 {entry.savePath}: {e.Message}");
            return;
        }
        if (containerConfig == null) return;

        // 4. 反射拿 entries
        var entriesField = containerType.GetField("entries");
        var entriesArray = entriesField?.GetValue(containerConfig) as Array;
        if (entriesArray == null) return;

        // 5. 遍历子容器（原逻辑不变）
        int LoadedCount = 0;
        foreach (var childEntry in entriesArray)
        {
            if (childEntry is not ILogicStateEntry stateEntry) continue;

            var entity = MasterFactory.Instance.FindByUUID(stateEntry.GetUUID());
            if (entity == null && !string.IsNullOrEmpty(stateEntry.GetTag()))
                entity = MasterFactory.Instance.FindFirstByTag(stateEntry.GetTag());

            if (entity == null)
            {
                Debug.LogWarning($"[LogicStateManager] 找不到实体：{stateEntry.GetUUID()}");
                continue;
            }

            var script = FindScriptOnEntity(entity, entry.scriptName);
            if (script == null)
            {
                Debug.LogWarning($"[LogicStateManager] 实体上没脚本：{entry.scriptName}");
                continue;
            }

            script.LoadFromEntry(stateEntry);
            LoadedCount++;
        }
        Debug.Log($"[LogicStateManager] 加载{entry.scriptName} : {LoadedCount} 条数据");
    }

    /// <summary>读状态文件：存档目录优先，回退保底默认目录；都没有返回 null。</summary>
    private string ReadStateFile(string fileName)
    {
        string savePath = Path.Combine(SaveDir, fileName);
        if (File.Exists(savePath)) return File.ReadAllText(savePath);

        string defPath = Path.Combine(DefaultDir, fileName);
        if (File.Exists(defPath)) return File.ReadAllText(defPath);

        return null;
    }

    #endregion

    #region 工具方法 

    //查找脚本类型
    private readonly Dictionary<string, Type> _typeCache = new();

    private Type FindScriptType(string scriptName)
    {
        if (_typeCache.TryGetValue(scriptName, out var cached)) return cached;

        var type = Type.GetType(scriptName);
        if (type == null)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(scriptName);
                if (type != null) break;
            }
        }
        if (type != null) _typeCache[scriptName] = type;
        return type;
    }

    //查找场景中所有该类型的脚本
    // 原：FindObjectsOfType(scriptType) 全场景扫
    // 新：查 MasterFactory 权威缓存
    private List<LogicScriptBase> FindAllScripts(Type scriptType)
    {
        return MasterFactory.Instance.GetScriptsByType(scriptType);
    }

    //在实体上查找指定脚本
    private LogicScriptBase FindScriptOnEntity(Entity entity, string scriptName)
    {
        foreach (var script in entity.LogicScripts)
        {
            if (script.GetType().Name == scriptName && script is LogicScriptBase logicBase)
            {
                return logicBase;
            }
        }
        return null;
    }

    //统一通知
    private void NotifyAllScriptsReady()
    {
        var allScripts = MasterFactory.Instance.GetAllScripts();

        // 1. 脚本级：逐个回调（try-catch 防单个崩掉整体）
        foreach (var s in allScripts)
        {
            try { s.OnAllStateInjected(); }
            catch (Exception e)
            {
                Debug.LogError($"[LogicStateManager] {s.name} 就绪回调异常：{e}");
            }
        }

        // 2. 全局级：只打标记（不碰事件系统，保持纯净）
        IsAllStateInjected = true;

        Debug.Log($"[LogicStateManager] 所有脚本数据注入完成，共 {allScripts.Count} 个");
    }
    #endregion
}