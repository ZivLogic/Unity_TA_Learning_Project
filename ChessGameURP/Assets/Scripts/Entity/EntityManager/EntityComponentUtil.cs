using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//组件静态工具
public static class EntityComponentUtil
{
    //获取棋子属性组件方法
    public static Package GetChessManComponentConfig()
    {
        var pawn = new Chess_PawnConfig { };
        var rook = new Chess_RookConfig { };
        var knight = new Chess_KnightConfig { };
        var bishop = new Chess_BishopConfig { };
        var queen = new Chess_QueenConfig { };
        var king = new Chess_KingConfig { };
        var pack = new Package();
        pack.Put(EventPackName.EntityComponentUtil_GetChessManComponentConfig_Pawn, pawn);
        pack.Put(EventPackName.EntityComponentUtil_GetChessManComponentConfig_Rook, rook);
        pack.Put(EventPackName.EntityComponentUtil_GetChessManComponentConfig_Knight, knight);
        pack.Put(EventPackName.EntityComponentUtil_GetChessManComponentConfig_Bnishop, bishop);
        pack.Put(EventPackName.EntityComponentUtil_GetChessManComponentConfig_Queen, queen);
        pack.Put(EventPackName.EntityComponentUtil_GetChessManComponentConfig_King, king);
        return pack;
    }
}
