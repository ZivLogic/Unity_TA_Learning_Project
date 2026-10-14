using System;
using UnityEngine;

//逻辑脚本基类：只接收数据，不主动加载
public abstract class LogicScriptBase : MonoBehaviour, IEntityAware
{
    public Entity Entity { get; set; }

    //父容器ID(子类重写)
    public abstract string ContainerId { get; }

    //从容器加载数据（子类重写）
    //统一方法只负责传输，脚本自己拆分数据
    public abstract void LoadFromEntry(ILogicStateEntry entry);

    //保存数据到子容器（子类重写）
    public abstract ILogicStateEntry SaveToEntry();
}