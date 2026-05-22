using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputEnumData
{

}
#region 输入状态
//输入状态
public enum InputKeyState
{
    None,
    Down,    //按下
    Hold,    //持续按住
    Up       //抬起
}
#endregion
#region 输入行为
//输入行为指令枚举
public enum InputAction     
{
    MoveUp,               //向上
    MoveDown,             //向下
    MoveLeft,             //向左
    MoveRight,            //向右
    SelectTarget,         //选择状态
    CancelOperate,        //取消操作
    CameraViewRotate      //摄像机视角旋转
}
#endregion
#region 输入模式
//全局输入运行模式
public enum InputRunMode
{
    ForbidAll,            //禁止所有输入
    OnlyViewDirect,       //特定直通道可输入
    NormalOperate         //完整正常操作模式
}
#endregion
#region 输入背景
//全局上下文枚举
public enum InputContext
{
    GameWorld,            //大世界战斗/场景交互
    BagPanel,             //背包界面
    ShopPanel,            //商城界面
    DialogPanel,          //弹窗/对话
    UIPanelOnly,          //纯UI置顶
    SettingPanel,         //设计面板
    SelSelectObject       //选择物体
}
#endregion
#region 拦截器
//拦截器类型
public enum InterceptorType
{
    CutSceneGlobal,        //全局拦截
    ContextLimit,          //上下文白名单拦截
    UIOcclude              //Ui遮挡&穿透拦截
}
#endregion
#region 设备类型
//输入设备类型
public enum InputDeviceType
{
    KeyboardMouse,
    Gamepad,
    MobileTouch
}
#endregion