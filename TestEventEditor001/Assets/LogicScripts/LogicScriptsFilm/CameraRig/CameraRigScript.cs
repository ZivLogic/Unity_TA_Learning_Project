using System;
using System.Collections.Generic;
using UnityEngine;

public class CameraRigScript : LogicScriptBase
{
    public override string ContainerId => "logic_state_camera_rig";

    // 暴露给输入系统拿朝向
    public static Camera ActiveCamera { get; private set; }

    // ===== 配置（从容器加载） =====
    public string cameraId = "Camera_Main";
    public float attachDistance = 3f;
    public float attachHeight = 1.5f;
    public float camDistance = 5f;
    public float camHeight = 1.5f;
    public float snapshotDelay = 0.15f;
    public float followLerp = 12f;

    // ===== 鼠标视角控制参数 =====
    public float mouseSensitivity = 3f;
    public float minPitch = -30f;
    public float maxPitch = 70f;

    // ===== 运行时引用 =====
    private Transform _character;
    private Transform _target;
    private Camera _cam;

    private float _currentYaw = 0f;
    private float _currentPitch = 20f;

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
        if (entry is not CameraRigStateContainer.CameraRigEntry data) return;

        cameraId = data.cameraId;
        attachDistance = data.attachDistance;
        attachHeight = data.attachHeight;
        camDistance = data.camDistance;
        camHeight = data.camHeight;
        snapshotDelay = data.snapshotDelay;
        followLerp = data.followLerp;

        // 初始化角度
        _currentYaw = data.camYaw;
        _currentPitch = data.camPitch;
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
            camYaw = _currentYaw,     // 保存当前实际角度
            camPitch = _currentPitch, // 保存当前实际角度
            camHeight = camHeight,
            snapshotDelay = snapshotDelay,
            followLerp = followLerp
        };
    }

    public override void OnAllStateInjected()
    {
        if (Entity?.LogicObject == null) return;

        _character = Entity.LogicObject.transform;
        _target = EnsureAimPoint(_character);
        _cam = FindCamera();

        if (_cam != null)
        {
            ActiveCamera = _cam;
            _camPos = _cam.transform.position;
            _camRot = _cam.transform.rotation;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private Transform EnsureAimPoint(Transform logicRoot)
    {
        const string aimName = "AimPoint";
        var existing = logicRoot.Find(aimName);
        if (existing != null) return existing;

        var go = new GameObject(aimName);
        go.transform.SetParent(logicRoot, false);
        go.transform.localPosition = new Vector3(0f, attachHeight, attachDistance);
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
                        if (go != null) return go.GetComponent<Camera>();
                    }
                }
            }
        }
        return Camera.main;
    }

    private void Update()
    {
        if (_character == null) return;

        // 1. 鼠标控制视角旋转
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
            float my = Input.GetAxis("Mouse Y") * mouseSensitivity;
            _currentYaw += mx;
            _currentPitch -= my;
            _currentPitch = Mathf.Clamp(_currentPitch, minPitch, maxPitch);

            // 按 ESC 解锁鼠标（方便在编辑器里操作）
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        // 2. 记录快照（注意：快照记录的是角色的位置，不包含相机的旋转）
        _snapshots.Add(new Snapshot
        {
            time = Time.time,
            pos = _character.position,
            rot = _character.rotation
        });

        float cutoff = Time.time - snapshotDelay;
        while (_snapshots.Count > 0 && _snapshots[0].time < cutoff)
            _snapshots.RemoveAt(0);

        // 3. 挂载点（不受视角影响，永远在逻辑根正前方）
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

        // 使用鼠标算出来的角度
        Quaternion rot = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
        Vector3 pivot = snap.pos + Vector3.up * camHeight;
        Vector3 desiredPos = pivot - rot * Vector3.forward * camDistance;

        // 【修正】补充这行，定义 desiredRot
        Quaternion desiredRot = rot;

        float t = 1f - Mathf.Exp(-followLerp * Time.deltaTime);
        _camPos = Vector3.Lerp(_camPos, desiredPos, t);
        _camRot = Quaternion.Slerp(_camRot, desiredRot, t); // 现在这里不会报错了

        _cam.transform.position = _camPos;
        _cam.transform.rotation = _camRot;
    }
}