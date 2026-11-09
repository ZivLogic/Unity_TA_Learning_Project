using System;
using System.Collections.Generic;
using UnityEngine;

public class CameraRigScript : LogicScriptBase
{
    public override string ContainerId => "logic_state_camera_rig";

    // ===== 配置（从容器加载） =====
    public string cameraId = "Camera_Main";

    public float attachDistance = 3f;
    public float attachHeight = 1.5f;

    public float camDistance = 5f;
    public float camYaw = 0f;
    public float camPitch = 20f;
    public float camHeight = 1.5f;

    public float snapshotDelay = 0.15f;
    public float followLerp = 12f;

    // ===== 运行时引用 =====
    private Transform _character;   // Entity.LogicObject（有物理的节点）
    private Transform _target;      // LogicObject 下的 AimPoint
    private Camera _cam;

    private struct Snapshot
    {
        public float time;
        public Vector3 pos;
        public Quaternion rot;
    }
    private readonly List<Snapshot> _snapshots = new();
    private Vector3 _camPos;
    private Quaternion _camRot;

    // ===== 数据加载 =====
    public override void LoadFromEntry(ILogicStateEntry entry)
    {
        if (entry is not CameraRigStateContainer.CameraRigEntry data)
        {
            Debug.LogError($"[CameraRigScript] 子容器类型错误：{entry.GetType().Name}");
            return;
        }

        cameraId = data.cameraId;
        attachDistance = data.attachDistance;
        attachHeight = data.attachHeight;
        camDistance = data.camDistance;
        camYaw = data.camYaw;
        camPitch = data.camPitch;
        camHeight = data.camHeight;
        snapshotDelay = data.snapshotDelay;
        followLerp = data.followLerp;
    }

    public override ILogicStateEntry SaveToEntry()
    {
        return new CameraRigStateContainer.CameraRigEntry
        {
            uuid = Entity?.UUID,
            tag = Entity?.Tag,
            cameraId = cameraId,
            attachDistance = attachDistance,
            attachHeight = attachHeight,
            camDistance = camDistance,
            camYaw = camYaw,
            camPitch = camPitch,
            camHeight = camHeight,
            snapshotDelay = snapshotDelay,
            followLerp = followLerp
        };
    }

    // ===== 就绪后初始化 =====
    public override void OnAllStateInjected()
    {
        if (Entity == null) { Debug.LogError("[CameraRigScript] Entity 为空"); return; }
        if (Entity.LogicObject == null)
        {
            Debug.LogError("[CameraRigScript] Entity.LogicObject 为空，不符合 Entity 架构");
            return;
        }

        // 被跟随者 = 逻辑根（有刚体/物理的节点）
        _character = Entity.LogicObject.transform;

        // 挂载点 = 逻辑根下新建/查找 AimPoint
        _target = EnsureAimPoint(_character);
        if (_target == null || _target == _character)
        {
            Debug.LogError("[CameraRigScript] 挂载点创建失败或与逻辑根重合");
            return;
        }

        // 相机
        _cam = FindCamera();
        if (_cam == null)
        {
            Debug.LogWarning($"[CameraRigScript] 未找到相机 cameraId={cameraId}");
            return;
        }

        _camPos = _cam.transform.position;
        _camRot = _cam.transform.rotation;

        Debug.Log($"[CameraRigScript] 初始化完成：逻辑根={_character.name}, 挂载点={_target.name}, 相机={_cam.name}");
    }

    /// <summary>在逻辑根下确保 AimPoint 存在</summary>
    private Transform EnsureAimPoint(Transform logicRoot)
    {
        const string aimName = "AimPoint";

        var existing = logicRoot.Find(aimName);
        if (existing != null) return existing;

        var go = new GameObject(aimName);
        go.transform.SetParent(logicRoot, false);
        go.transform.localPosition = new Vector3(0f, attachHeight, attachDistance);
        go.transform.localRotation = Quaternion.identity;

        Debug.Log($"[CameraRigScript] 在逻辑根下创建挂载点 {aimName}");
        return go.transform;
    }

    private Camera FindCamera()
    {
        if (!string.IsNullOrEmpty(cameraId))
        {
            var registry = ConfigAPI.Get<CameraRegistryConfig>();
            if (registry?.cameras != null)
            {
                foreach (var entry in registry.cameras)
                {
                    if (entry.cameraId != cameraId) continue;

                    if (!string.IsNullOrEmpty(entry.cameraTag))
                    {
                        var go = GameObject.FindGameObjectWithTag(entry.cameraTag);
                        if (go != null) { var c = go.GetComponent<Camera>(); if (c != null) return c; }
                    }
                    if (!string.IsNullOrEmpty(entry.scenePath))
                    {
                        var go = GameObject.Find(entry.scenePath);
                        if (go != null) { var c = go.GetComponent<Camera>(); if (c != null) return c; }
                    }
                }
            }
        }

        return Camera.main;
    }

    // ===== 跟随 =====
    private void Update()
    {
        if (_character == null) return;

        _snapshots.Add(new Snapshot
        {
            time = Time.time,
            pos = _character.position,
            rot = _character.rotation
        });

        float cutoff = Time.time - snapshotDelay;
        while (_snapshots.Count > 0 && _snapshots[0].time < cutoff)
            _snapshots.RemoveAt(0);

        // 挂载点是 LogicObject 的子物体，用 localPosition 更新
        if (_target != null && _target != _character)
        {
            Vector3 desiredLocal = new Vector3(0f, attachHeight, attachDistance);
            _target.localPosition = Vector3.Lerp(_target.localPosition, desiredLocal, 0.5f);
            _target.localRotation = Quaternion.identity;
        }
    }

    private void LateUpdate()
    {
        if (_cam == null || _snapshots.Count == 0) return;

        Snapshot snap = _snapshots[0];

        float yawTotal = snap.rot.eulerAngles.y + camYaw;
        Quaternion rot = Quaternion.Euler(camPitch, yawTotal, 0f);

        Vector3 pivot = snap.pos + Vector3.up * camHeight;
        Vector3 desiredPos = pivot - rot * Vector3.forward * camDistance;
        Quaternion desiredRot = rot;

        float t = 1f - Mathf.Exp(-followLerp * Time.deltaTime);
        _camPos = Vector3.Lerp(_camPos, desiredPos, t);
        _camRot = Quaternion.Slerp(_camRot, desiredRot, t);

        _cam.transform.position = _camPos;
        _cam.transform.rotation = _camRot;
    }
}