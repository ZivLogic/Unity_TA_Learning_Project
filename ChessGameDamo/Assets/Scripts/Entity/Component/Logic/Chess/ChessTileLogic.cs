using System.Collections.Generic;
using UnityEngine;


public class ChessTileLogic : MonoBehaviour
{
    private ChessTileData _tileData;

    private void Awake()
    {
        _tileData = GetComponent<ChessTileData>();
        //Debug.Log($"当前格子坐标：{_tileData.LogicX},{_tileData.LogicY} 是否白格：{_tileData.IsWhiteTile}");
    }
}
