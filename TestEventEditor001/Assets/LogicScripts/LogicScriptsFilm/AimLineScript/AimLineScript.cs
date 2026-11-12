using UnityEditor.Rendering;
using UnityEngine;

public class AimLineScript : LogicScriptBase
{
    public override string ContainerId => ""; // 随便取个，不需要存数据的话留空即可

    [Header("瞄准线表现")]
    public float maxDistance = 100f;
    public Color lineColor = Color.red;

    private LineRenderer _lineRenderer;
    private Camera _mainCam;


    public string cameraId = "Camera_Main";


    public override void OnAllStateInjected()
    {
        _mainCam = FindCamera();

        // 动态创建一个 LineRenderer
        _lineRenderer = gameObject.AddComponent<LineRenderer>();
        _lineRenderer.startWidth = 0.05f;
        _lineRenderer.endWidth = 0.01f;
        _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        _lineRenderer.startColor = lineColor;
        _lineRenderer.endColor = lineColor;
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

    private void LateUpdate()
    {
        if (_mainCam == null || _lineRenderer == null) return;

        // 1. 射线起点：角色逻辑根节点的位置（稍微抬高一点，比如胸口）
        Vector3 startPos = Entity.LogicObject.transform.position + Vector3.up * 1.2f;

        // 2. 射线终点：获取摄像机屏幕正中心射出的射线，计算交点
        Ray camRay = _mainCam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));

        // 默认终点是最大距离
        Vector3 endPos = startPos + (camRay.direction * maxDistance);
        Vector3 hitPoint = endPos;

        // 3. 从角色原点向目标方向发射物理射线，检测真正的碰撞点
        Vector3 dirToCenter = (camRay.origin + camRay.direction * maxDistance - startPos).normalized;

        if (Physics.Raycast(startPos, dirToCenter, out RaycastHit hit, maxDistance))
        {
            // 如果打到了别人，瞄准线就停在别人身上
            hitPoint = hit.point;
        }
        else
        {
            // 没打到人，瞄准线指向摄像机正中心前方
            hitPoint = startPos + dirToCenter * maxDistance;
        }

        // 4. 绘制
        _lineRenderer.SetPosition(0, startPos);
        _lineRenderer.SetPosition(1, hitPoint);
    }

    public override void LoadFromEntry(ILogicStateEntry entry) { }
    public override ILogicStateEntry SaveToEntry() => null;
}