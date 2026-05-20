using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RenderIdentityRegister
{
    //渲染工厂注册层
    public class RenderIdentityMapItem
    {
        public string ModelKey;
        public RenderMajorType majorType;
        public RenderMinorType mindorType;
    }
    private static readonly Dictionary<string, RenderIdentityMapItem> _RenderIdentityMapDict = new Dictionary<string, RenderIdentityMapItem>();

    //初始化注册
    public static void InitRegister()
    {
        _RenderIdentityMapDict.Clear();
        //大类
        RegisterItem("ChessBard_Model", RenderMajorType.ChessBoard_Model, RenderMinorType.None);
        RegisterItem("ChessMan_Model", RenderMajorType.ChessMan_Model, RenderMinorType.None);
        RegisterItem("ChessTile_Model", RenderMajorType.ChessTile_Model, RenderMinorType.None);
        //中类
        RegisterItem("ChessMan_Pawn_Model", RenderMajorType.ChessMan_Model, RenderMinorType.ChessMan_Pawn_Model);
        RegisterItem("ChessMan_Rook_Model", RenderMajorType.ChessMan_Model, RenderMinorType.ChessMan_Rook_Model);
        RegisterItem("ChessMan_Kinght_Model", RenderMajorType.ChessMan_Model, RenderMinorType.ChessMan_Knight_Model);
        RegisterItem("ChessMan_Bishop_Model", RenderMajorType.ChessMan_Model, RenderMinorType.ChessMan_Bishop_Model);
        RegisterItem("ChessMan_Queen_Model", RenderMajorType.ChessMan_Model, RenderMinorType.ChessMan_Queen_Model);
        RegisterItem("ChessMan_King_Model", RenderMajorType.ChessMan_Model, RenderMinorType.ChessMan_King_Model);
    }
    //单次注册
    private static void RegisterItem(string modelKey, RenderMajorType major, RenderMinorType mindor)
    {
        if (_RenderIdentityMapDict.ContainsKey(modelKey)) return;
        _RenderIdentityMapDict.Add(modelKey, new RenderIdentityMapItem
        {
            ModelKey = modelKey,
            majorType = major,
            mindorType = mindor
        });
    }
    //根据实体Key 反向查出大类，中类
    public static bool TryGetIdentity(string modelKey, out RenderMajorType major, out RenderMinorType minor)
    {
        major = RenderMajorType.None;
        minor = RenderMinorType.None;
        if (string.IsNullOrEmpty(modelKey))
            return false;
        if ( ! _RenderIdentityMapDict.TryGetValue(modelKey, out var item) ) 
            return false;
        major = item.majorType;
        minor = item.mindorType;
        return true;
    }
}