using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 音频工厂：流水线第五步
/// 职责：根据蓝图配置，给音频体添加音频源组件
/// </summary>
public class AudioFactory : IFactoryStep
{
    /// <summary>
    /// 处理实体
    /// </summary>
    public void Process(Entity entity)
    {
        if (entity?.Blueprint == null) return;

        //// 只有普通实体蓝图才有音频配置
        //if (entity.Blueprint is not EntityBlueprintsConfig.EntityBlueprint blueprint)
        //    return;

        var blueprint = entity.Blueprint;

        // 如果没有音频配置ID，跳过
        if (blueprint.audioIds == null || blueprint.audioIds.Length == 0)
        {
            Debug.Log($"[AudioFactory] {blueprint.blueprintId} 没有音频配置，跳过");
            return;
        }

        Debug.Log($"[AudioFactory] 添加音频组件: {blueprint.blueprintId} (共 {blueprint.audioIds.Length} 个)");

        // 遍历每个音频ID
        foreach (var audioId in blueprint.audioIds)
        {
            // 1. 通过字典键查找音频配置
            var audioConfig = ConfigManager.Instance.FindAudioConfig(audioId);

            if (audioConfig == null)
            {
                Debug.LogError($"[AudioFactory] 找不到音频配置: {audioId}");
                continue;
            }

            // 2. 添加音频源
            AddAudioSource(entity.AudioObject, audioConfig, entity);
        }
    }

    #region 音频源设置

    /// <summary>
    /// 添加音频源组件
    /// </summary>
    private void AddAudioSource(GameObject audioObj, AudioConfigs.AudioData audioConfig, Entity entity)
    {
        // 1. 创建音频源
        var audioSource = audioObj.AddComponent<AudioSource>();

        // 2. 加载音频片段
        AudioClip clip = LoadAudioClip(audioConfig);
        if (clip == null)
        {
            Debug.LogError($"[AudioFactory] 音频片段加载失败: {audioConfig.audioId}");
            return;
        }

        audioSource.clip = clip;

        // 3. 设置基本属性
        audioSource.volume = audioConfig.volume;
        audioSource.pitch = audioConfig.pitch;
        audioSource.loop = audioConfig.loop;

        // 4. 设置空间混合（2D/3D）
        audioSource.spatialBlend = audioConfig.spatialBlend;
        audioSource.minDistance = audioConfig.minDistance;
        audioSource.maxDistance = audioConfig.maxDistance;

        // 5. 设置播放模式
        SetPlayMode(audioSource, audioConfig.playMode);

        // 6. 记录到实体
        entity.AudioSource = audioSource;
        entity.AudioSources.Add(audioSource);

        Debug.Log($"[AudioFactory] 音频源添加成功: {audioConfig.audioId} (片段: {clip.name})");
    }

    /// <summary>
    /// 加载音频片段
    /// </summary>
    private AudioClip LoadAudioClip(AudioConfigs.AudioData audioConfig)
    {
        if (string.IsNullOrEmpty(audioConfig.clipPath))
        {
            Debug.LogError($"[AudioFactory] 音频片段路径为空: {audioConfig.audioId}");
            return null;
        }

        // 从 Resources 加载
        var clip = Resources.Load<AudioClip>(audioConfig.clipPath);

        if (clip == null)
        {
            Debug.LogError($"[AudioFactory] 音频片段加载失败: {audioConfig.clipPath}");
        }

        return clip;
    }

    /// <summary>
    /// 设置播放模式
    /// </summary>
    private void SetPlayMode(AudioSource audioSource, string playMode)
    {
        switch (playMode.ToLower())
        {
            case "onawake":
                // 创建时立即播放
                audioSource.playOnAwake = true;
                break;

            case "loop":
                // 循环播放
                audioSource.playOnAwake = true;
                audioSource.loop = true;
                break;

            case "manual":
                // 手动播放（由逻辑脚本控制）
                audioSource.playOnAwake = false;
                break;

            case "trigger":
                // 触发播放（由触发器控制）
                audioSource.playOnAwake = false;
                break;

            default:
                audioSource.playOnAwake = false;
                break;
        }
    }

    #endregion
}