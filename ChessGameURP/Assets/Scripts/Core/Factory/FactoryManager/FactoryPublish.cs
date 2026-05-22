using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FactoryPublish : BasePublishSystem
{
    public override string SystemID => "FactoryPublish";
    #region 事件发布

    public void ConfigLogic_InitChessConfig_InitChessEvent(PackageEvent e)
    {
        //if (!EventRouteRegistrar.IsEventCanPublish(e))
        //    return;
        //Debug.Log("成功调用");
        //EventManager.Instance.EmitLogic((dynamic)e);
        //EventManager.Instance.EmitLogic<PackageEvent>(e);
        //Debug.Log("发布事件");
        AutoPublish(e);
    }

    public void ConfigLogic_InitEntityIDConfig_InitEntityID(PackageEvent e)
    {
        AutoPublish(e);
    }
    

    public void GetIdentityConfig(PackageEvent e)
    { 
        AutoPublish(e);
    }
    #endregion
}
