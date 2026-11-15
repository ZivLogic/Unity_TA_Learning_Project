using System;
using System.Collections.Generic;

// 1. 生命周期维度拦截配置
[Serializable]
public class LifecycleInterceptRule
{
    public EntityLifecycleState SourceState;
    public List<EntityActionState> BlockedActions;
}

// 2. 动作维度拦截配置
[Serializable]
public class ActionInterceptRule
{
    public EntityActionState SourceState;
    public List<EntityActionState> BlockedActions;
}

// 3. 统一的状态拦截配置容器
[Serializable]
[Config(configId: "player_rule_config", priority: 50)]
public class PlayerRuleConfig : ConfigDefinition
{
    public List<LifecycleInterceptRule> LifecycleRules;
    public List<ActionInterceptRule> ActionRules;
}

// 4. AI 配置（和玩家同类结构，独立表）
[Serializable]
[Config(configId: "ai_rule_config", priority: 50)]
public class AIRuleConfig : ConfigDefinition
{
    public List<LifecycleInterceptRule> LifecycleRules;
    public List<ActionInterceptRule> ActionRules;
}