using UnityEngine;
using System.Collections.Generic;
using System;

public class StateMachineScript : LogicScriptBase
{
    public override string ContainerId => "entity_state_machine";

    // 运行时计时器字典：Tag名字 -> 剩余存在时间
    private Dictionary<string, float> _timedTags = new Dictionary<string, float>();

    public override void OnAllStateInjected()
    {
        // 如果 Entity 初始为空，你可以给它赋初值
    }

    private void Update()
    {
        if (_timedTags.Count > 0)
        {
            List<string> expiredTags = null;
            foreach (var kv in _timedTags)
            {
                if (Time.time >= kv.Value)
                {
                    if (expiredTags == null) expiredTags = new List<string>();
                    expiredTags.Add(kv.Key);
                }
            }

            if (expiredTags != null)
            {
                foreach (var tag in expiredTags)
                {
                    _timedTags.Remove(tag);
                    Entity.RemoveTag(tag);
                    Debug.Log($"[状态机] {Entity.UUID} 的 {tag} 状态已过期移除");

                    // 注意：带计时的 Buff，重启游戏时一般直接清空，或者恢复剩余时间，这里默认直接加标签

                }
            }
        }
    }

    /// <summary>
    /// 给实体添加一个限时状态标签（比如燃烧 3 秒）
    /// </summary>
    public void AddTimedTag(string tag, float duration)
    {
        Entity.AddTag(tag);
        _timedTags[tag] = Time.time + duration;
    }

    // ===== 数据存读档 =====
    public override void LoadFromEntry(ILogicStateEntry entry)
    {
        if (entry is StateMachineStateContainer.StateMachineEntry data)
        {
            // 【核心修复】使用 Enum.TryParse 进行安全转换
            if (Enum.TryParse(data.Lifecycle, out EntityLifecycleState lifecycle))
                Entity.Lifecycle = lifecycle;
            else
                Entity.Lifecycle = EntityLifecycleState.Alive; // 保底

            if (Enum.TryParse(data.GameFlow, out GameFlowState gameFlow))
                Entity.GameFlow = gameFlow;
            else
                Entity.GameFlow = GameFlowState.Normal;

            if (Enum.TryParse(data.Action, out EntityActionState action))
                Entity.Action = action;
            else
                Entity.Action = EntityActionState.Idle;

            // 恢复 Tags
            if (data.Tags != null)
            {
                foreach (var tag in data.Tags)
                    Entity.AddTag(tag);
            }
        }
    }

    public override ILogicStateEntry SaveToEntry()
    {
        return new StateMachineStateContainer.StateMachineEntry
        {
            uuid = Entity.UUID,
            tag = Entity.Tag,

            // 保存时，直接将枚举 ToString() 存成字符串
            Lifecycle = Entity.Lifecycle.ToString(),
            GameFlow = Entity.GameFlow.ToString(),
            Action = Entity.Action.ToString(),

            Tags = new List<string>(Entity.GetAllTags())
        };
    }
}