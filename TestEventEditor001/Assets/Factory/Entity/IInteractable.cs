/// <summary>
///可交互实体定义接口
///需要被主角交互的物体实现此接口，即可被主角的交互范围查询识别到
/// </summary>
public interface IInteractable
{
    // 可以放一些基础的交互属性，比如交互提示文字
    string GetInteractionHint();
}