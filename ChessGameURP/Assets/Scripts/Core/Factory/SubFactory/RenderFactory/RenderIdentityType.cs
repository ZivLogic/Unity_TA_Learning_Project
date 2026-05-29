using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RenderMajorType
{
    None,
    ChessBoard_Model,
    ChessTile_Model,
    ChessTile_White_Model,
    ChessTile_Black_Model,
    ChessMan_Model,
    ChessMan_White_Model,
    ChessMan_Black_Model,
    Ground_Model,
    NPC_Model
}

public enum RenderMinorType
{
    None,
    ChessMan_Pawn_Model,
    ChessMan_Rook_Model,
    ChessMan_Knight_Model,
    ChessMan_Bishop_Model,
    ChessMan_Queen_Model,
    ChessMan_King_Model,
    
}

public enum RenderThreeType
{
    None,
    ChessMan_White,
    ChessMan_Black,
}