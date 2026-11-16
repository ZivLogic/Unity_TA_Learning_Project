using UnityEngine;
using System;
using System.Collections.Generic;

public class PlayerStateMachineScript : EntityStateMachineBase
{
    public override string ContainerId => "player_state_storage";

    // 用来测试的假血量
    private float _hp = 100f;

    public override void OnAllStateInjected()
    {
        // 1. 先让基类加载配置、注入接口
        base.OnAllStateInjected();

        // 2. 注册 Buff 的节流触发逻辑
        RegisterPlayerBuffHandlers();
    }

    private void RegisterPlayerBuffHandlers()
    {
        // 注册燃烧：每次触发扣 5 点血
        RegisterBuffTickHandler("Burning", () =>
        {
            _hp -= 5f;
            Debug.Log($"[玩家] 受到燃烧伤害，HP -5，剩余 HP: {_hp}");
            if (_hp <= 0) Debug.Log("[玩家] 死亡了！");
        });

        // 注册中毒：每次触发扣 2 点血
        RegisterBuffTickHandler("Poison", () =>
        {
            _hp -= 2f;
            Debug.Log($"[玩家] 受到中毒伤害，HP -2，剩余 HP: {_hp}");
        });

        // 注册回血：每次触发回 10 点血
        RegisterBuffTickHandler("Regen", () =>
        {
            _hp += 10f;
            Debug.Log($"[玩家] 受到恢复效果，HP +10，剩余 HP: {_hp}");
        });
    }

    protected override void LoadMetaConfigs()
    {
        var config = ConfigAPI.Get<PlayerRuleConfig>("player_rule_config");
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
        Debug.Log("[玩家] 状态拦截规则表加载完毕");
    }

    protected override void RegisterAllLoaders()
    {
        RegisterStateLoader(typeof(PlayerStateContainer.PlayerStateEntry), LoadPlayerState);
    }

    private void LoadPlayerState(ILogicStateEntry entry)
    {
        var data = (PlayerStateContainer.PlayerStateEntry)entry;

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