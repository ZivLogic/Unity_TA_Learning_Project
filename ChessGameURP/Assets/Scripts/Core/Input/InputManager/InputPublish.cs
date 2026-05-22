using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputPublish : BasePublishSystem
{
    public override string SystemID => "InputPublish";
    #region 事件发布
    public void ConfigLogic_OnInputConfig_InitInputConfig(PackageEvent e)
    {
        //if (!EventRouteRegistrar.IsEventCanPublish(e))
        //    return;
        //EventManager.Instance.EmitLogic<PackageEvent>(e);
        AutoPublish(e);
    }
    #endregion
}
