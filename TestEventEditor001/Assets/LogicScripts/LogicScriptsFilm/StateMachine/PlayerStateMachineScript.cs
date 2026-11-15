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
}