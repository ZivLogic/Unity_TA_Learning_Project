using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityLogic : BaseBusinessSystem
{
    public override string SystemID => "EntityLogic";
    #region  事件回调（路由表自动绑定，不用手动订阅）
    void GetChessManConfig(PackageEvent e)
    {
        ConfigPulish_GetChessManComponentConfig_GetChessManConfig evt = e as ConfigPulish_GetChessManComponentConfig_GetChessManConfig;
        var pack = evt.package;
        if (pack.Get<Chess_PawnConfig>(EventPackName.EntityComponentUtil_GetChessManComponentConfig_Pawn, out var pawn)) { }
        if (pack.Get<Chess_RookConfig>(EventPackName.EntityComponentUtil_GetChessManComponentConfig_Rook, out var rook)) { }
        if (pack.Get<Chess_KnightConfig>(EventPackName.EntityComponentUtil_GetChessManComponentConfig_Knight, out var knight)) { }
        if (pack.Get<Chess_BishopConfig>(EventPackName.EntityComponentUtil_GetChessManComponentConfig_Bnishop, out var bishop)) { }
        if (pack.Get<Chess_QueenConfig>(EventPackName.EntityComponentUtil_GetChessManComponentConfig_Queen, out var queen)) { }
        if (pack.Get<Chess_KingConfig>(EventPackName.EntityComponentUtil_GetChessManComponentConfig_King, out var king)) { }
        if (!pack.ValidsteAll())
        { Debug.LogError($"[EntityLogic]某值为空！故障事件：{e}"); return; }
        //再调用具体方法
        var pub = new ChessComponentCfg { package = pack };
        EventManager.Instance.EmitLogic(pub);
        
    }

    #endregion
}
