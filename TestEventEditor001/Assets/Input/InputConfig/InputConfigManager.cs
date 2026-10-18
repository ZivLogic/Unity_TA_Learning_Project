using System;
using System.Collections.Generic;
using UnityEngine;

// 输入配置管理器
// 负责：加载 / 合并 / 改键 / 重置 / 保存
public class InputConfigManager
{
    // 保底配置
    private InputConfigArchive _defaultArchive;
    // 玩家存档（按时间倒序）
    private List<InputConfigArchive> _playerArchives = new List<InputConfigArchive>();
    // 当前生效
    public InputConfigRoot FinalRoot { get; private set; }
    // 改键工作副本
    public InputConfigRoot WorkingRoot { get; private set; }

    // ========== 加载 ==========
    public void LoadAll()
    {
        _defaultArchive = InputConfigLoader.LoadDefaultArchive();
        _playerArchives = InputConfigLoader.LoadPlayerArchives();
        SortArchivesByTimeDesc();

        if (_playerArchives.Count > 0)
            FinalRoot = _playerArchives[0].Root;
        else
            FinalRoot = InputConfigLoader.CloneRoot(_defaultArchive.Root);

        FinalRoot?.EnsureNotNull();
        WorkingRoot = null;

        Debug.Log($"[InputConfigManager]加载完成，玩家存档数：{_playerArchives.Count}");
    }

    private void SortArchivesByTimeDesc()
    {
        _playerArchives.Sort((a, b) =>
            b.GetCreateTime().CompareTo(a.GetCreateTime()));
    }

    // ========== 改键 ==========
    public InputConfigRoot BeginModify()
    {
        WorkingRoot = InputConfigLoader.CloneRoot(FinalRoot);
        return WorkingRoot;
    }

    public void CommitModify()
    {
        if (WorkingRoot == null)
        {
            Debug.LogWarning("[InputConfigManager]没有正在进行的改键");
            return;
        }

        var archive = new InputConfigArchive
        {
            ArchiveId = Guid.NewGuid().ToString("N"),
            IsDefault = false,
            Root = WorkingRoot
        };
        archive.SetCreateTime(DateTime.Now);

        _playerArchives.Insert(0, archive);

        while (_playerArchives.Count > InputConfigConst.MaxPlayerArchives)
        {
            var oldest = _playerArchives[_playerArchives.Count - 1];
            _playerArchives.RemoveAt(_playerArchives.Count - 1);
            InputConfigLoader.DeleteArchiveFile(oldest.ArchiveId);
        }

        FinalRoot = archive.Root;
        WorkingRoot = null;

        InputConfigLoader.SavePlayerArchives(_playerArchives);
        Debug.Log("[InputConfigManager]改键已保存");
    }

    public void CancelModify()
    {
        WorkingRoot = null;
    }

    // ========== 重置 ==========
    // 软重置：当前生效用保底，玩家存档保留
    public void SoftReset()
    {
        FinalRoot = InputConfigLoader.CloneRoot(_defaultArchive.Root);
        WorkingRoot = null;
    }

    // 硬重置：清空玩家存档，用保底
    public void HardReset()
    {
        foreach (var archive in _playerArchives)
            InputConfigLoader.DeleteArchiveFile(archive.ArchiveId);

        _playerArchives.Clear();
        FinalRoot = InputConfigLoader.CloneRoot(_defaultArchive.Root);
        WorkingRoot = null;

        InputConfigLoader.SavePlayerArchives(_playerArchives);
        Debug.Log("[InputConfigManager]已硬重置");
    }

    // ========== 查询 ==========
    public InputGlobalConfig GetGlobal() => FinalRoot?.GlobalConfig;
    public InputBindContainer GetBindContainer() => FinalRoot?.BindContainer;
    public InputInterceptorContainer GetInterceptorContainer() => FinalRoot?.InterceptorContainer;
    public InputStateContainer GetStateContainer() => FinalRoot?.StateContainer;
    public InputEventMapContainer GetEventMapContainer() => FinalRoot?.EventMapContainer;
    public InputIntentContainer GetIntentContainer() => FinalRoot?.IntentContainer;
    public InputAreaModelContainer GetAreaModelContainer() => FinalRoot?.AreaModelContainer;
    public InputScopeContainer GetScopeContainer() => FinalRoot?.ScopeContainer;
}