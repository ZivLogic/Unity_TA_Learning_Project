using System.Collections.Generic;
using UnityEngine;

public class SimpleCameraRig : MonoBehaviour
{
    [Header("目标查找")]
    [Tooltip("按名字查找（留空则用Tag）")]
    public string targetName = "TargetObject";
    [Tooltip("按Tag查找（targetName为空时生效）")]
    public string targetTag = "Target";

    [Header("挂载参数（目标对象挂到角色前方）")]
    [Tooltip("目标挂在角色前方的距离（米）")]
    public float attachDistance = 3f;
    [Tooltip("挂载点相对角色的高度偏移")]
    public float attachHeight = 1.5f;

    [Header("相机偏移（球坐标，相对角色）")]
    [Tooltip("相机离注视点的距离")]
    public float camDistance = 5f;
    [Tooltip("水平角（绕Y轴，0=正后方，正值绕一侧）")]
    public float camYaw = 0f;
    [Tooltip("俯角（绕X轴，正数=从上往下看）")]
    public float camPitch = 20f;
    [Tooltip("注视点高度（看角色胸口/头）")]
    public float camHeight = 1.5f;

    [Header("快照与跟随")]
    [Tooltip("快照延迟时长（秒），越大相机越“慢”")]
    public float snapshotDelay = 0.15f;
    [Tooltip("相机平滑速度")]
    public float followLerp = 12f;

    [Header("引用")]
    public Transform character;   // 角色根节点
    public Camera cam;            // 相机

    // ---- 内部 ----
    private Transform _target;
    private bool _attached;
    private struct Snapshot
    {
        public float time;
        public Vector3 pos;
        public Quaternion rot;
    }
    private readonly List<Snapshot> _snapshots = new List<Snapshot>();
    private Vector3 _camPos;
    private Quaternion _camRot;

    void Start()
    {
        if (!cam) cam = Camera.main;
        if (!character) character = transform;

        _target = FindTarget();
        if (_target == null)
            Debug.LogWarning($"[SimpleCameraRig] 未找到目标 name={targetName} tag={targetTag}");

        _camPos = cam.transform.position;
        _camRot = cam.transform.rotation;
    }

    Transform FindTarget()
    {
        if (!string.IsNullOrEmpty(targetName))
        {
            var go = GameObject.Find(targetName);
            if (go) return go.transform;
        }
        if (!string.IsNullOrEmpty(targetTag))
        {
            var go = GameObject.FindWithTag(targetTag);
            if (go) return go.transform;
        }
        return null;
    }

    void Update()
    {
        // 1) 每帧记录角色快照
        _snapshots.Add(new Snapshot
        {
            time = Time.time,
            pos = character.position,
            rot = character.rotation
        });

        float cutoff = Time.time - snapshotDelay;
        while (_snapshots.Count > 0 && _snapshots[0].time < cutoff)
            _snapshots.RemoveAt(0);

        // 2) 目标挂到角色前方指定距离
        if (_target != null)
        {
            Vector3 attachPos = character.position
                              + character.forward * attachDistance
                              + Vector3.up * attachHeight;

            if (!_attached)
            {
                _target.position = attachPos;
                _target.rotation = character.rotation;
                _attached = true;
            }
            else
            {
                // 持续跟随（不想跟就删这段）
                _target.position = Vector3.Lerp(_target.position, attachPos, 0.5f);
                _target.rotation = character.rotation;
            }
        }
    }

    void LateUpdate()
    {
        if (cam == null || _snapshots.Count == 0) return;

        // 3) 取最旧快照作为相机基准（形成延迟）
        Snapshot snap = _snapshots[0];

        // 4) 球坐标：俯角 + 水平角
        float yawTotal = snap.rot.eulerAngles.y + camYaw;
        Quaternion rot = Quaternion.Euler(camPitch, yawTotal, 0f);

        // 注视点：角色胸口/头
        Vector3 pivot = snap.pos + Vector3.up * camHeight;

        // 相机位置 = 注视点沿朝向反方向退 camDistance
        Vector3 desiredPos = pivot - rot * Vector3.forward * camDistance;
        Quaternion desiredRot = rot;

        // 5) 平滑跟随（帧率无关）
        float t = 1f - Mathf.Exp(-followLerp * Time.deltaTime);
        _camPos = Vector3.Lerp(_camPos, desiredPos, t);
        _camRot = Quaternion.Slerp(_camRot, desiredRot, t);

        cam.transform.position = _camPos;
        cam.transform.rotation = _camRot;
    }
}