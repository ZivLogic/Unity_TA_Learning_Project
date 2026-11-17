using System.IO;
using UnityEngine;

/// <summary>
/// 三层路径规则（统一约定，所有配置系统遵守）：
///   DefaultRoot：保底，Application.streamingAssetsPath —— 出厂只读。
///                编辑器下是 <项目>/Assets/StreamingAssets，打包后是 <Game>_Data/StreamingAssets，
///                两者都是真实文件路径，一套 API 通吃，无需编辑器宏。
///   HotRoot：    热更，Application.persistentDataPath —— 服务器下载覆盖保底。
///   SaveRoot：   存档，Application.persistentDataPath —— 运行时写、玩家数据，永不热更。
/// </summary>
public static class ConfigPaths
{
    public static string DefaultRoot => Application.streamingAssetsPath;
    public static string HotRoot => Application.persistentDataPath;
    public static string SaveRoot => Application.persistentDataPath;
}

/// <summary>
/// 目录名与文件名常量，集中管理，禁止在业务代码里散落魔法字符串。
/// 改目录结构只改这里一处。
/// </summary>
public static class ConfigDirs
{
    public const string MainConfig = "Config";                       // 主配置子目录
    public const string EventConfig = "EventConfig";                 // 事件配置子目录
    public const string InputConfig = "InputConfig";                 // 输入配置子目录
    public const string LogicState = "LogicState";                   // 逻辑状态：运行时存档（persistentDataPath 下）
    public const string LogicStateDefault = "LogicState/Default";    // 逻辑状态：保底默认（StreamingAssets 下，预留走热更）
    public const string VersionFile = "version.json";                // 通用热更版本文件名
}
