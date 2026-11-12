using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 1. 标记脚本
public class PlayerTag : LogicScriptBase
{
    public static PlayerTag Current { get; private set; }
    public override string ContainerId => ""; // 不需要状态配置可以留空

    // 在数据注入完成后自动注册为主角
    public override void OnAllStateInjected()
    {
        if (Current != null && Current != this)
        {
            Debug.LogError($"场景中出现多个主角！已注销：{Entity.UUID}");
            return;
        }
        Current = this;

        // 把 UUID 发给中转层
        if (InputMediator.Instance != null)
            InputMediator.Instance.SetMainCharacter(Entity.UUID);

        Debug.Log($"[PlayerTag] 自动绑定主角：{Entity.UUID}");
    }

    private void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    public override void LoadFromEntry(ILogicStateEntry entry) { }
    public override ILogicStateEntry SaveToEntry() => null;
}