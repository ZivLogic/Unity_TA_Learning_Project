using System;
using System.Collections.Generic;
using UnityEngine;

public class CameraRigScript : LogicScriptBase
{
    public override string ContainerId => "logic_state_camera_rig";

    public static Camera ActiveCamera { get; private set; }

    // ===== 配置（从容器加载） =====
    public string cameraId = "Camera_Main";
    public float camDistance = 5f;
    public float camHeight = 1.5f;
    public float snapshotDelay = 0.15f;
    public float followLerp = 12f;

    // ===== 越肩视角参数 =====
    public float shoulderOffset = 2.0f; // 摄像机向右偏移，让人物在左边

    // ===== 鼠标视角控制参数 =====
    public float mouseSensitivity = 3f;
    public float minPitch = -30f;
    public float maxPitch = 70f;

    // ===== 运行时引用 =====
    private Transform _character;
    private Camera _cam;

    private float _currentYaw = 0f;
    private float _currentPitch = 20f;

    private struct Snapshot
    {
        public float time;
        public Vector3 pos;
    }
    private readonly List<Snapshot> _snapshots = new();
    private Vector3 _camPos;
    private Quaternion _camRot;

    // ===== 数据加载 =====
    public override void LoadFromEntry(ILogicStateEntry entry)
    {
        if (entry is not CameraRigStateContainer.CameraRigEntry data) return;

        cameraId = data.cameraId;
        camDistance = data.camDistance;
        camHeight = data.camHeight;
        snapshotDelay = data.snapshotDelay;
        followLerp = data.followLerp;

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
            camDistance = camDistance,
            camYaw = _currentYaw,
            camPitch = _currentPitch,
            camHeight = camHeight,
            snapshotDelay = snapshotDelay,
            followLerp = followLerp,
            shoulderOffset = shoulderOffset
        };
    }

    public override void OnAllStateInjected()
    {
        if (Entity?.LogicObject == null) return;

        _character = Entity.LogicObject.transform;
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

        // 1. 鼠标控制视角旋转（完全独立于角色旋转）
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
            float my = Input.GetAxis("Mouse Y") * mouseSensitivity;
            _currentYaw += mx;
            _currentPitch -= my;
            _currentPitch = Mathf.Clamp(_currentPitch, minPitch, maxPitch);

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        // 2. 记录角色位置快照
        _snapshots.Add(new Snapshot
        {
            time = Time.time,
            pos = _character.position
        });

        float cutoff = Time.time - snapshotDelay;
        while (_snapshots.Count > 0 && _snapshots[0].time < cutoff)
            _snapshots.RemoveAt(0);
    }

    private void LateUpdate()
    {
        if (_cam == null || _snapshots.Count == 0) return;

        Snapshot snap = _snapshots[0];

        // 1. 算出包含上下俯仰和左右偏航的最终旋转（摄像机保持这个朝向，不强制看角色）
        Quaternion desiredRot = Quaternion.Euler(_currentPitch, _currentYaw, 0f);

        // 2. 角色的基准点（比如胸口/头部）
        Vector3 pivot = snap.pos + Vector3.up * camHeight;

        // 3. 计算无偏移时的相机位置（在角色后方 camDistance 处）
        // 因为视线保持 desiredRot，所以它就在角色背后的延长线上
        Vector3 basePos = pivot - desiredRot * Vector3.forward * camDistance;

        // 4. 【核心】只平移位置，不改变朝向
        // 使用相机的纯水平右方向去偏移，这样绝对向右
        Quaternion flatYaw = Quaternion.Euler(0, _currentYaw, 0);
        Vector3 rightDir = flatYaw * Vector3.right;
        Vector3 desiredPos = basePos + rightDir * shoulderOffset;

        // 5. 平滑跟随（位置和旋转都平滑）
        float t = 1f - Mathf.Exp(-followLerp * Time.deltaTime);
        _camPos = Vector3.Lerp(_camPos, desiredPos, t);
        _camRot = Quaternion.Slerp(_camRot, desiredRot, t);

        _cam.transform.position = _camPos;
        _cam.transform.rotation = _camRot;
    }

}