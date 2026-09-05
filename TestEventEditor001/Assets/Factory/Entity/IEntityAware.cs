/// <summary>
/// 实体感知接口：逻辑脚本实现此接口可获取实体引用
/// </summary>
public interface IEntityAware
{
    Entity Entity { get; set; }
}