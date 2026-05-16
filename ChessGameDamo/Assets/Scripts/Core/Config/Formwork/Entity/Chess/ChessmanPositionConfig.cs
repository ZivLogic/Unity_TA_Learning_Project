using System.Collections;
using System.Collections.Generic;
using UnityEngine;




[System.Serializable]
public class ChessmanPositionConfig
{
    public List<List<int>> positionWhite;
    public List<List<int>> positionBlack;
    public Vector3 rotationWhite;
    public Vector3 rotationBlack;

    //外部不写，但逻辑调用
    public bool IsPrefab = false;
    public bool IsList = true;
    public string ID = "ChessmanPosition";
}
