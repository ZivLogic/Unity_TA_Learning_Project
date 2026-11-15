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
}