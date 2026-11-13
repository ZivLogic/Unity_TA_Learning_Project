using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterAttachmentScript : LogicScriptBase
{
    public override string ContainerId => ""; // 不需要存状态

    // 对外暴露枪口
    public Transform MuzzlePoint { get; private set; }

    public override void OnAllStateInjected()
    {
        if (Entity?.LogicObject == null) return;

        // 在主角的逻辑根节点下，生成枪口挂载点
        var muzzleGo = new GameObject("MuzzlePoint");
        muzzleGo.transform.SetParent(Entity.LogicObject.transform, false);

        // 设置偏移（你可以以后把它写进配置里）
        muzzleGo.transform.localPosition = new Vector3(0f, 1.2f, 1f);

        MuzzlePoint = muzzleGo.transform;
        Debug.Log("[CharacterAttachment] 主角枪口挂载点已生成");
    }

    public override void LoadFromEntry(ILogicStateEntry entry) { }
    public override ILogicStateEntry SaveToEntry() => null;
}
