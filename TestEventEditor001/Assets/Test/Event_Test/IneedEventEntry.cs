/// <summary>
/// 需要事件入口的脚本实现此接口（如输入脚本）
/// 由 LogicFactory 在挂载完成后注入
/// </summary>
public interface INeedEventEntry
{
    EventEntryBase EventEntry { get; set; }
}