using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EntityIdentityRedister
{
    //单条身份映射数据
    public class IdentityMapItem
    {
        public string EntityKey;
        public EntityIdentityType MajorType;
        public EntitySpecialIdentityType MinorType;
        public EntityIDThreeType ThreeType;
    }
    //全局映射表 Key -> 身份配置
    private static readonly Dictionary<string , IdentityMapItem> _identityMapDict = new Dictionary<string , IdentityMapItem>();
    
    //初始化映射表
    public static void InitRegister()
    {
        _identityMapDict.Clear();
        // 和你JSON顶级Key严格对应
        RegisterItem("ChessBoard", EntityIdentityType.ChessBoard, EntitySpecialIdentityType.None);
        RegisterItem("ChessMan", EntityIdentityType.ChessMan, EntitySpecialIdentityType.None);
        RegisterItem("ChessTile", EntityIdentityType.ChessTile, EntitySpecialIdentityType.None);

        // 棋子细分中类 同样按Key注册
        RegisterItem("Pawn", EntityIdentityType.ChessMan, EntitySpecialIdentityType.ChessMan_Pawn);
        RegisterItem("Rook", EntityIdentityType.ChessMan, EntitySpecialIdentityType.ChessMan_Rook);
        RegisterItem("Knight", EntityIdentityType.ChessMan, EntitySpecialIdentityType.ChessMan_Knight);
        RegisterItem("Bishop", EntityIdentityType.ChessMan, EntitySpecialIdentityType.ChessMan_Bishop);
        RegisterItem("Queen", EntityIdentityType.ChessMan, EntitySpecialIdentityType.ChessMan_Queen);
        RegisterItem("King", EntityIdentityType.ChessMan, EntitySpecialIdentityType.ChessMan_King);

        RegisterItemThree("Pawn_White", EntityIdentityType.ChessMan, EntitySpecialIdentityType.ChessMan_Pawn, EntityIDThreeType.Chess_White);
        RegisterItemThree("Rook_White", EntityIdentityType.ChessMan, EntitySpecialIdentityType.ChessMan_Rook, EntityIDThreeType.Chess_White);
        RegisterItemThree("Knight_White", EntityIdentityType.ChessMan, EntitySpecialIdentityType.ChessMan_Knight, EntityIDThreeType.Chess_White);
        RegisterItemThree("Bishop_White", EntityIdentityType.ChessMan, EntitySpecialIdentityType.ChessMan_Bishop, EntityIDThreeType.Chess_White);
        RegisterItemThree("Queen_White", EntityIdentityType.ChessMan, EntitySpecialIdentityType.ChessMan_Queen, EntityIDThreeType.Chess_White);
        RegisterItemThree("King_White", EntityIdentityType.ChessMan, EntitySpecialIdentityType.ChessMan_King, EntityIDThreeType.Chess_White);
    }
    //注册一条映射关系
    private static void RegisterItem(string key, EntityIdentityType major, EntitySpecialIdentityType minor)
    {
        if (_identityMapDict.ContainsKey(key)) return;
        _identityMapDict.Add(key, new IdentityMapItem
        {
            EntityKey = key,
            MajorType = major,
            MinorType = minor
        });
    }
    //三个条件的注册
    private static void RegisterItemThree(string key, EntityIdentityType major, EntitySpecialIdentityType minor, EntityIDThreeType three)
    {
        if (_identityMapDict.ContainsKey(key)) return;
        _identityMapDict.Add(key, new IdentityMapItem
        {
            EntityKey = key,
            MajorType = major,
            MinorType = minor,
            ThreeType = three
        });
    }
    //根据JSON/实体key 反查出大类，中类
    public static bool TryGetIdentity(string entityKey, out EntityIdentityType major, out EntitySpecialIdentityType minor)
    {
        major = EntityIdentityType.None;
        minor = EntitySpecialIdentityType.None;
        if (string.IsNullOrEmpty(entityKey)) return false;
        if ( ! _identityMapDict.TryGetValue(entityKey, out var item))
            return false;
        major = item.MajorType;
        minor = item.MinorType;
        return true;
    }
}
