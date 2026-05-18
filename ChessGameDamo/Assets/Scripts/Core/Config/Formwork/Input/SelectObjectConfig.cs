using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SelectObjectConfig
{
    public bool CanSelect;        //是否可被选择
    public bool IsEnable;         //是否启用
    //public int SelectWeight;      //选择优先级
    //public float DetectRange;     //被检测半径

    public string ID = "SelectObject";
}