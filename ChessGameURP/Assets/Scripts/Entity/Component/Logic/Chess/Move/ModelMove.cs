using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelMove : MonoBehaviour
{
    private void Awake()
    {
        EventManager.Instance.Listen<ChessManModelMove>(Move);
    }

    private void Move(PackageEvent e)
    {
        ChessManModelMove evt = e as ChessManModelMove;
        var pack = evt.package;
        if (pack.Get<Vector2Int>("1", out var newPos)) { }
        if (pack.Get<string>("2",out var ID)) { }
        if (newPos == null || ID == null)
        {
            Debug.LogWarning("[ModelMove]获取新坐标失败");
            return;
        }
        var tag = GetComponent<LogicIdentity>();
        if (tag == null)
        {
            Debug.LogError("[ModelMove]ID不存在");
            return;
        }
        if (ID != tag.LogicID)return;
        Vector3 pos = ChessBoardLayoutData.tilePositions[newPos];
        Transform tsf = GetComponent<Transform>();
        tsf.position = pos;
    }

    private void OnDestroy()
    {
        EventManager.Instance.Unlisten<ChessManModelMove>(Move);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
