using UnityEngine;

public class AimLineScript : LogicScriptBase
{
    public override string ContainerId => "";

    [Header("瞄准线表现")]
    public Color lineColor = new Color(1f, 0f, 0f, 0.5f); // 半透明红色，更好看

    private LineRenderer _lineRenderer;

    private Transform _muzzlePoint;

    public override void OnAllStateInjected()
    {
        // 动态创建一个 LineRenderer
        _lineRenderer = gameObject.AddComponent<LineRenderer>();
        _lineRenderer.startWidth = 0.1f;
        _lineRenderer.endWidth = 0.0f; // 末端变细
        _lineRenderer.useWorldSpace = true; // 必须设为 true

        // 【修复 URP 不显示的问题】使用 URP 的无光照 Shader
        Shader lineShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (lineShader == null) lineShader = Shader.Find("Unlit/Color"); // 保底
        if (lineShader == null) lineShader = Shader.Find("Sprites/Default"); // 极差保底

        _lineRenderer.material = new Material(lineShader);
        _lineRenderer.material.color = lineColor;

        // 如果你不想让这条线投射阴影
        _lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _lineRenderer.receiveShadows = false;

        // 找自己身上的挂载点
        foreach (var s in Entity.LogicScripts)
        {
            if (s is CharacterAttachmentScript attachment)
            {
                _muzzlePoint = attachment.MuzzlePoint;
                Debug.Log("辅助线挂载成功");
                break;
            }
        }
    }

    private void LateUpdate()
    {
        if (_lineRenderer == null || _muzzlePoint == null) return;

        var camEntry = InputMediator.Instance.GetCameraEntry() as CameraEntry_Default;
        if (camEntry != null && camEntry.CurrentAimPoint != Vector3.zero)
        {
            _lineRenderer.SetPosition(0, _muzzlePoint.position);
            _lineRenderer.SetPosition(1, camEntry.CurrentAimPoint);
        }
    }

    public override void LoadFromEntry(ILogicStateEntry entry) { }
    public override ILogicStateEntry SaveToEntry() => null;
}