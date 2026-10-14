using System;

//所有逻辑状态子容器接口
//所有子容器必须实现此接口，统一方法才能传输
public interface ILogicStateEntry
{
    string GetUUID();
    string GetTag();
}