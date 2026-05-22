using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityPublish : BasePublishSystem
{
    public override string SystemID => "EntityPublish";

    #region 事件发布
    public void GetChessManComponentConfig(PackageEvent e)
    {
        AutoPublish(e);
    }
    #endregion
}
