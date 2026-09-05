using System;
using static UnityEngine.EventSystems.EventTrigger;


// 工厂步骤接口
public interface IFactoryStep
{
    void Process(Entity entity);
}
