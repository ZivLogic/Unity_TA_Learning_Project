using System;

[Serializable]
[Config(
    configId: "audio_configs",
    priority: 100
)]
public class AudioConfigs : ConfigDefinition
{
    public AudioData[] audioList;

    [Serializable]
    public class AudioData
    {
        public string audioId;           // 字典键
        public string clipPath;          // 音频片段路径
        public float volume;             // 音量（0-1）
        public float pitch;              // 音调（0.5-2）
        public bool loop;                // 是否循环
        public float spatialBlend;       // 空间混合（0=2D，1=3D）
        public float minDistance;        // 最小距离
        public float maxDistance;        // 最大距离
        public string playMode;          // 播放模式：onAwake, loop, manual, trigger
    }
}