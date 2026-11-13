using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChestInteractScript : LogicScriptBase, IInteractable
{
    public override string ContainerId => ""; // 不需要存状态
    public string GetInteractionHint() => "交互测试_001";


    public override void LoadFromEntry(ILogicStateEntry entry) { }
    public override ILogicStateEntry SaveToEntry() => null;
}
