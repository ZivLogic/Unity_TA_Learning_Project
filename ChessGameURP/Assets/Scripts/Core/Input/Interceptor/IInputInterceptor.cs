using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//输入拦截器标准接口
public interface IInputInterceptor
{
    //InputAction是指特定行为枚举，InputContext是指上下文
    //返回true放行，返回false拦截
    bool IsPassCheeck(InputAction action, InputContext context);
}