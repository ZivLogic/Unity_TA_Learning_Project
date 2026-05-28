using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputBusinessLogic
{
    public void Init()
    {
        //注册 枚举 => 业务方法
        InputBizRouter.Register(InputAction.MoveUp, OnMoveUp);
        InputBizRouter.Register(InputAction.SelectTarget, OnSelectTarget);
        InputBizRouter.Register(InputAction.CancelOperate, OnCancelOperate);
        InputBizRouter.Register(InputAction.SelectTile, OnSelectChessTile);
        Debug.Log("[InputBusinessLogic]输入业务层注册枚举成功");
    }

    //移动-W 三态都会进来
    private void OnMoveUp(InputKeyConfig cfg, InputKeyState state)
    {

    }
    //鼠标选中 只Down触发
    private void OnSelectTarget(InputKeyConfig cfg, InputKeyState state)
    {
        //鼠标在摄像机成像画面的坐标
        Vector2 mousePos = Input.mousePosition;
        //鼠标世界坐标
        Vector3 worldPos = InputHelper.GetMouseGroundPos();
        //是否在UI上
        bool isOnUI = InputHelper.IsMouseOverUI();
        //如果在UI上，取消
        if (isOnUI) return;
        LayerMask selectLayer = LayerMask.GetMask("SelectObject");
        GameObject selectObj = InputHelper.RaycastSelectSingle();
        var pack = new Package();
        pack.Put(EventPackName.INPUT_ACTIONMOUSE, InputAction.SelectTarget);
        pack.Put(EventPackName.INPUT_MOUSEPOS3D, worldPos);
        pack.Put(EventPackName.INPUT_MOUSESELECT, selectObj);
        Debug.Log("选中物体");
        Debug.Log($"获取到物体：{selectObj},是否为空：{selectObj == null}");
        EventManager.Instance.EmitLogic(new InputMouseSelect() { package = pack });
    }
    private void OnSelectChessTile(InputKeyConfig cfg, InputKeyState state)
    {
        //Debug.Log("运行到这");
        //鼠标在摄像机成像画面的坐标
        Vector2 mousePos = Input.mousePosition;
        //鼠标世界坐标
        Vector3 worldPos = InputHelper.GetMouseGroundPos();
        //是否在UI上
        bool isOnUI = InputHelper.IsMouseOverUI();
        //如果在UI上，取消
        if (isOnUI) return;
        LayerMask selectLayer = LayerMask.GetMask("SelectObject");
        GameObject selectObj = InputHelper.RaycastSelectSingle();
        var pack = new Package();
        pack.Put(EventPackName.INPUT_ACTIONMOUSE, InputAction.SelectTile);
        pack.Put(EventPackName.INPUT_MOUSEPOS3D, worldPos);
        pack.Put(EventPackName.INPUT_MOUSESELECT, selectObj);
        Debug.Log("选中物体");
        Debug.Log($"获取到棋盘：{selectObj},是否为空：{selectObj == null}");
        EventManager.Instance.EmitLogic(new GetTileObj { package = pack });
    }
    //取消业务逻辑
    private void OnCancelOperate(InputKeyConfig cfg, InputKeyState state)
    {

    }
}
