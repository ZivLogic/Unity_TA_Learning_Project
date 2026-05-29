using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssetsPublish : BasePublishSystem
{
    public override string SystemID => "AssetsPublish";
    #region 事件发布
    public void FactoryLogic_OnResponseChessPrefabs_AssetInitPrefabsLoadAll(PackageEvent e)
    {
        //if (!EventRouteRegistrar.IsEventCanPublish(e))
        //    return;
        //EventManager.Instance.EmitLogic<PackageEvent>(e);
        AutoPublish(e);
    }
    #endregion
    public void Init()
    {
        //硬编码事件快速迭代验证
    }
}
