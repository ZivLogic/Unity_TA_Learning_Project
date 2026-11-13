using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 输入中转层
/// 职责：
/// 1. 拉输入系统标准输出
/// 2. 维护摄像机入口（可切换）
/// 3. 维护主角信息
/// 4. 注入摄像机入口，拿返回结果
/// 5. 查工厂总表，注入实体输入入口
/// </summary>
public class InputMediator : MonoBehaviour
{
    public static InputMediator Instance { get; private set; }

    [Header("摄像机入口")]
    public CameraEntryBase CameraEntry;

    [Header("调试")]
    public bool DebugLog = true;

    //主角的UUID
    private string _mainCharacterUuid;
    private int _lastFrame = -1;

    public Entity MainCharacterEntity { get; private set; }


    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ========== 摄像机入口切换 ==========
    /// <summary>
    /// 外部唯一入口：切换摄像机入口
    /// </summary>
    public void SetCameraEntry(CameraEntryBase entry)
    {
        if (_cameraEntry == entry) return;

        // 旧入口清理
        if (_cameraEntry != null)
            _cameraEntry.OnDeactivated();

        _cameraEntry = entry;

        // 新入口激活 + 同步主角
        if (_cameraEntry != null)
        {
            _cameraEntry.SetMainCharacter(_mainCharacterUuid);
            _cameraEntry.OnActivated();
        }

        Log($"切换摄像机入口：{entry?.name ?? "null"}");
    }

    public CameraEntryBase GetCameraEntry()
    {
        return _cameraEntry;
    }

    // 内部引用（避免外部直接改字段）
    private CameraEntryBase _cameraEntry
    {
        get => CameraEntry;
        set => CameraEntry = value;
    }

    // ========== 主角 ==========

    public void SetMainCharacter(string uuid)
    {
        _mainCharacterUuid = uuid;
        Log($"主角注册：{uuid ?? "null"}");

        if (!string.IsNullOrEmpty(uuid) && MasterFactory.Instance != null)
        {
            // 只拿 Entity
            MainCharacterEntity = MasterFactory.Instance.FindByUUID(uuid);
        }
        else
        {
            MainCharacterEntity = null;
        }

        if (_cameraEntry != null)
            _cameraEntry.SetMainCharacter(uuid);
    }

    public string GetMainCharacter()
    {
        return _mainCharacterUuid;
    }

    // ========== 主循环 ==========
    private void Update()
    {
        var output = InputManager.Instance.GetStandardOutput();
        if (output == null || !output.HasOutput) return;
        if (output.Frame == _lastFrame) return;
        _lastFrame = output.Frame;

        if (_cameraEntry == null) return;

        // 1. 摄像机处理输入
        _cameraEntry.ReceiveInput(output);

        var result = _cameraEntry.GetResult();
        if (result == null || !result.HasResult) return;

        // 2. 处理主角切换（这个全游戏唯一，中转层需要特殊接住）
        if (result.Action == "SelectMainCharacter")
        {
            SetMainCharacter(result.SingleUuid);
            return;
        }

        // 3. 纯中立的数据转发
        // 不管 Action 是什么，只看接收者是谁（ReceiverUuids）
        var receivers = result.ReceiverUuids.Count > 0 ? result.ReceiverUuids : result.Uuids;

        foreach (var uuid in receivers)
        {
            var entity = MasterFactory.Instance.FindByUUID(uuid);
            if (entity == null) continue;

            var entry = FindInputEntry(entity);
            if (entry == null) continue;

            var evt = new EntityInputEvent
            {
                Action = result.Action,     // "Attack"
                KeyState = output.KeyState,
                Frame = result.Frame,
                TargetUuids = result.Uuids  // 注意：这里装的是被击中的人（可能为空）
            };

            entry.ReceiveInput(evt);
            //Debug.Log($"{evt.Action}, {uuid}");
        }
    }

    // ========== 注入实体 ==========
    private void InjectToEntities(EntityInputEvent evt)
    {
        foreach (var uuid in evt.TargetUuids)
        {
            var entity = MasterFactory.Instance.FindByUUID(uuid);
            if (entity == null) continue;

            var entry = FindInputEntry(entity);
            if (entry == null) continue;

            entry.ReceiveInput(evt);
        }
    }

    private EntityInputEntry FindInputEntry(Entity entity)
    {
        if (entity?.LogicScripts == null) return null;
        foreach (var s in entity.LogicScripts)
        {
            if (s is EntityInputEntry entry)
                return entry;
        }
        return null;
    }

    // ========== 启动 ==========
    private void Start()
    {
        // 自动找默认摄像机入口
        if (CameraEntry == null)
        {
            CameraEntry = FindObjectOfType<CameraEntry_Default>();
            if (CameraEntry != null)
            {
                CameraEntry.SetMainCharacter(_mainCharacterUuid);
                CameraEntry.OnActivated();
                Log($"自动找到摄像机入口：{CameraEntry.name}");
            }
        }

        //// 默认主角
        //var player = MasterFactory.Instance.FindFirstByTag("Player");
        //if (player != null)
        //{
        //    SetMainCharacter(player.UUID);
        //    Log($"启动默认主角：{player.UUID}");
        //}
    }

    private void Log(string msg)
    {
        if (DebugLog) Debug.Log($"[Mediator] {msg}");
    }
}