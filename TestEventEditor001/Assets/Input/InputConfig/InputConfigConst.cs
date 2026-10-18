
public static class InputConfigConst
{
    // ===== 保底配置 =====
    // 编辑器下的工程目录（相对工程根）
    public const string EditorDefaultRootFolder = "Assets/Input/ConfigData/Default";

    // Resources 下的路径（打包后读这里）
    public const string ResourceDefaultRootFolder = "InputConfig/Default";

    // ===== 玩家存档 =====
    // persistentDataPath 下的目录名
    public const string PlayerRootFolderName = "InputConfig/Player";

    // 玩家存档索引
    public const string PlayerIndexFileName = "player_index.json";

    // 玩家存档前缀
    public const string PlayerArchivePrefix = "player_";

    // 玩家存档上限
    public const int MaxPlayerArchives = 5;
}