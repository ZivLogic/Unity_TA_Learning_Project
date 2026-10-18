using UnityEngine;

/// <summary>
/// 输入意图执行器
/// 外部唯一入口：ApplyIntent(意图名)
/// 输入系统读意图配置，批量改自己的状态
/// </summary>
public class InputIntentExecutor
{
    private InputConfigManager _configMgr;
    private InputStateMachine _stateMachine;
    private InputContextManager _contextManager;
    private InputModeManager _modeManager;

    public void Init(
        InputConfigManager configMgr,
        InputStateMachine stateMachine,
        InputContextManager contextManager,
        InputModeManager modeManager)
    {
        _configMgr = configMgr;
        _stateMachine = stateMachine;
        _contextManager = contextManager;
        _modeManager = modeManager;
    }

    /// <summary>
    /// 外部调这个：应用一个意图
    /// </summary>
    public void ApplyIntent(string intentName)
    {
        var container = _configMgr.GetIntentContainer();
        if (container == null) return;
        if (!container.Dict.TryGetValue(intentName, out var list))
        {
            Debug.LogWarning($"[InputIntentExecutor]未找到意图：{intentName}");
            return;
        }

        foreach (var item in list)
        {
            if (!item.IsEnable) continue;
            Execute(item);
        }
    }

    private void Execute(InputIntentConfig item)
    {
        // 上下文
        if (!string.IsNullOrEmpty(item.TargetContext))
            _contextManager.Switch(item.TargetContext);

        // 全局模式
        if (!string.IsNullOrEmpty(item.TargetRunMode))
            _modeManager.Switch(item.TargetRunMode);

        // 基础状态
        if (!string.IsNullOrEmpty(item.TargetBaseState))
            _stateMachine.SwitchBaseState(item.TargetBaseState);

        // 清空附加
        if (item.ClearAllAdditive)
            _stateMachine.ClearAdditiveStates();

        // 弹出附加
        if (item.PopAdditiveStates != null)
        {
            foreach (var s in item.PopAdditiveStates)
                _stateMachine.PopAdditiveState(s);
        }

        // 压入附加
        if (item.PushAdditiveStates != null)
        {
            foreach (var s in item.PushAdditiveStates)
                _stateMachine.PushAdditiveState(s);
        }
    }
}