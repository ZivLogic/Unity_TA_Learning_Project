using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConfigPublish : BasePublishSystem
{
    public override string SystemID => "ConfigPublish";
    #region 事件发布
    public void AssetsLogic_OnInitCrateChessPrefabs_CreateInitChessPrefabs(PackageEvent e)
    {
        //AssetsLogic_OnInitCrateChessPrefabs_CreateInitChessPrefabs evt = e as AssetsLogic_OnInitCrateChessPrefabs_CreateInitChessPrefabs;
        //if ( ! EventRouteRegistrar.IsEventCanPublish(e))
        //    return;
        //EventManager.Instance.EmitLogic(e);
        AutoPublish(e);
    }
    public void FactoryLogic_OnResponseChessConfig_InitChessConfig(PackageEvent e)
    {
        //FactoryLogic_OnResponseChessConfig_InitChessConfig evt = e as FactoryLogic_OnResponseChessConfig_InitChessConfig;
        //if ( ! EventRouteRegistrar.IsEventCanPublish(e))
        //    return;
        //EventManager.Instance.EmitLogic<FactoryLogic_OnResponseChessConfig_InitChessConfig>(evt);
        //EventManager.Instance.EmitLogic<PackageEvent>(e);
        AutoPublish(e);
    }
    public void InputLogic_OnConfig_IsInputConfig(PackageEvent e)
    {
        //InputLogic_OnConfig_IsInputConfig evt = e as InputLogic_OnConfig_IsInputConfig;
        //if ( ! EventRouteRegistrar.IsEventCanPublish(e))
        //    return;
        //EventManager.Instance.EmitLogic<PackageEvent>(e);
        AutoPublish(e);
    }
    public void FactoryLogic_OnEntityIDConfig_InitEntityID(PackageEvent e)
    {
        AutoPublish(e);
    }
    #endregion

}
