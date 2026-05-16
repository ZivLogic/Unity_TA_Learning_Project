using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputCache
{

}
//全局静态输入字典缓存器
public static class InputConfigCache
{
    //按键绑定总字典
    public static Dictionary<string, InputKeyConfig> InputBindDict {  get; set; }
    //物体选中规则字典
    public static Dictionary<string, SelectObjectConfig> ObjectSelectDict { get; set; }
    //拦截器总字典
    public static Dictionary<string, InputInterceptorConfig> InterceptorDict { get; set; }
}