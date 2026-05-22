using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//上下文白名单拦截
public class ContextLimitInterceptor : IInputInterceptor
{
    public bool IsPassCheeck(InputAction action, InputContext context)
    {
        //没有限制模式
        if (InputManager.Instance.CurrentRunMode == InputRunMode.NormalOperate)
        {
            string key = action.ToString();
            if (!InputConfigCache.InputBindDict.TryGetValue(key, out var cfg))
            {
                Debug.LogWarning($"[ContextLimitInterceptor]未找到配置：{key}");
                return false;
            }
            return cfg.AllowContext.Contains(context);
        }
        
        return false;
    }











    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
