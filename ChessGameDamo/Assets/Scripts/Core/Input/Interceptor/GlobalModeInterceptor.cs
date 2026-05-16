using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalModeInterceptor : IInputInterceptor
{
    
    public bool IsPassCheeck(InputAction action , InputContext context)
    {
        //全局禁止模式，全部拦截
        if (InputManager.Instance.CurrentRunMode == InputRunMode.ForbidAll)
        {
            return false;
        }
        return true;
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
