using System.Collections.Generic;
using UnityEngine;


public class ChessTileLogic : MonoBehaviour
{
    
    public int logicX = 0;
    public int logicY = 0;
    public bool IsWhite = false;

    private void Awake()
    {
        
    }
    private void Start()
    {
        Vector3 pos = transform.position;
        var posDict = ChessBoardLayoutData.find2DPosDict;
        var whitDict = ChessBoardLayoutData.pos3DIsWhiteDict;
        posDict.TryGetValue(pos, out var vector2Int);
        logicX = vector2Int.x;
        logicY = vector2Int.y;
        whitDict.TryGetValue(pos, out var white);
        IsWhite = white;
    }
}
