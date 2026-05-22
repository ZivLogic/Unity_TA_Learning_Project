using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputLogic : BaseBusinessSystem
{
    public override string SystemID => "InputLogic";
    #region  事件回调（路由表自动绑定，不用手动订阅）
    void OnConfig(PackageEvent e)
    {
        InputLogic_OnConfig_IsInputConfig evt = e as InputLogic_OnConfig_IsInputConfig;
        var pack = e.package;
        if (pack.Get<Dictionary<string, InputKeyConfig>>(EventPackName.INPUT_GETCONFIG, out var inputCfg)) { }
        if (pack.Get<Dictionary<string, SelectObjectConfig>>(EventPackName.INPUT_SELECTCONFIG, out var selectCfg)) { }
        if (pack.Get<Dictionary<string, InputInterceptorConfig>>(EventPackName.INPUT_INTERCEPTORCONFIG, out var interceptorCfg)) { }

        if (! pack.ValidsteAll())
        { Debug.LogError($"[InputLogic]某值为空！故障事件：{e}"); return; }

        InputConfigCache.InputBindDict = inputCfg;
        InputConfigCache.ObjectSelectDict = selectCfg;
        InputConfigCache.InterceptorDict = interceptorCfg;
        Debug.Log("[InputManager]输入配置初始化完成");
    }

    #endregion
    //公开业务方法 给Mono壳子调用
    public void Init()
    {

    }
}