using System;
using UnityEngine;

//Æå×Ó¹¥»÷·¶Î§ - ¸¸×´Ì¬ÈİÆ÷
//´¢´æËùÓĞ PawmAttackRange ÊµÀıµÄ×´Ì¬

[Serializable]
[Config(
    configId: "logic_state_pawn_attack_range",
    priority: 100
    )]
public class PawnAttackRangeStateContainer : ConfigDefinition
{
    public PawnAttackRangeEntry[] entries;

    [Serializable]
    public class PawnAttackRangeEntry : ILogicStateEntry
    {
        public string uuid;
        public string tag;

        //¾ßÌåÊôĞÔ
        public Vector2Int attackRange;
        public Vector2Int attackRange2;
        public int level;
        public float experience;

        //½Ó¿ÚÊµÏÖ
        public string GetUUID() => uuid;
        public string GetTag() => tag;
    }
}