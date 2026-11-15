// IEntityStateMachineProvider.cs
public interface IEntityStateMachineProvider
{
    // 实体内部的所有脚本可以通过实现这个接口，自动接收状态机引用
    void InjectStateMachine(EntityStateMachineBase stateMachine);
}