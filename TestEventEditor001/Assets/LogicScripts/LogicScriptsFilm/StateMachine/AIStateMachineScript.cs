using System;
using UnityEngine;

public class AIStateMachineScript : EntityStateMachineBase
{
    public override string ContainerId => "ai_state_storage";

    public override void OnAllStateInjected()
    {
        base.OnAllStateInjected();
        RegisterAIBuffHandlers();
    }

    private void RegisterAIBuffHandlers()
    {
        // AI 被燃烧时，不扣血，而是进入狂暴状态
        RegisterBuffTickHandler("Burning", () =>
        {
            Debug.Log("[AI] 被火烧了，进入狂暴状态，攻击力提升！");
            //TryChangeAction(EntityActionState.Attacking);
        });
    }

    protected override void LoadMetaConfigs()
    {
        var config = ConfigAPI.Get<AIRuleConfig>("ai_rule_config");
        _lifecycleRules.Clear();
        _actionRules.Clear();

        if (config != null)
        {
            // 构建生命周期拦截表
            if (config.LifecycleRules != null)
            {
                foreach (var rule in config.LifecycleRules)
                    _lifecycleRules[rule.SourceState] = rule.BlockedActions;
            }

            // 构建动作拦截表
            if (config.ActionRules != null)
            {
                foreach (var rule in config.ActionRules)
                    _actionRules[rule.SourceState] = rule.BlockedActions;
            }
        }
        Debug.Log("[AI] 状态拦截规则表加载完毕");
    }

    protected override void RegisterAllLoaders()
    {
        RegisterStateLoader(typeof(AIStateContainer.AIStateEntry), LoadPlayerState);
    }

    private void LoadPlayerState(ILogicStateEntry entry)
    {
        var data = (AIStateContainer.AIStateEntry)entry;

        if (Enum.TryParse(data.Lifecycle, out EntityLifecycleState lc)) _currentLifecycle = lc;
        if (Enum.TryParse(data.Action, out EntityActionState act)) _currentAction = act;
        if (Enum.TryParse(data.PrePausedLifecycle, out EntityLifecycleState plc)) _prePauseLifecycle = plc;
        if (Enum.TryParse(data.PrePausedAction, out EntityActionState pac)) _prePauseAction = pac;

        Entity.Lifecycle = _currentLifecycle;
        Entity.Action = _currentAction;

        // 清空当前运行时的 Buff 列表
        _activeBuffs.Clear();

        if (data.Buffs != null)
        {
            foreach (var buffStr in data.Buffs)
            {
                // 1. 拆分字符串 "BuffName|RemainingTime"
                string[] parts = buffStr.Split('|');
                if (parts.Length != 2)
                {
                    Debug.LogError($"[状态机] 解析 Buff 字符串失败：{buffStr}");
                    continue;
                }

                string buffName = parts[0];
                string timeStr = parts[1];

                // 2. 查找配置
                if (!_buffMetas.TryGetValue(buffName, out var meta))
                {
                    Debug.LogWarning($"[状态机] 找不到 Buff 配置：{buffName}，跳过加载");
                    continue;
                }

                // 3. 解析时间
                bool isPermanent = (timeStr == "Max");
                float remainingTime = isPermanent ? float.MaxValue : float.Parse(timeStr);

                // 4. 还原为运行时的 BuffInstance
                _activeBuffs.Add(new BuffInstance
                {
                    Meta = meta,
                    EndTime = isPermanent ? float.MaxValue : Time.time + remainingTime,
                    CurrentInterval = meta.DefaultTickInterval,
                    NextTickTime = meta.DefaultTickInterval > 0 ? Time.time + meta.DefaultTickInterval : float.MaxValue,
                    IsPaused = false
                });

                // 5. 恢复 Entity 的 Tags 权威信息
                Entity.AddTag(buffName);
            }
        }
    }
}