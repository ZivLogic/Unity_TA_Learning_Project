using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InputKeyConfig
{
    public string KeyBindCode;           //绑定按键名字
    public int MouseButtonIndex;         //鼠标按键序号，-1表示不启用，0表示左键，1右键，2中键
    public bool IsEnable;                //是否使用该输入
    public float ClickCdThreshold;       //点击间隔 
    //跨平台
    public InputDeviceType DeviceType;
    //上下文白名单：该按键允许哪些上下文生效
    public List<InputContext> AllowContext = new List<InputContext>();
    //高级：是否允许UI穿透
    public bool AllowUIOverlayPenetrate;

    public float ClickRadius = 0.5f;            //检测半径

    public string ID = "InputKey";
}