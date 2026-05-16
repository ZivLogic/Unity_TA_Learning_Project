using UnityEngine;
using System.Collections.Generic;

public static class ChessBoardLayoutCalc
{
    /// <summary>
    /// 计算整个棋盘的格子坐标 + 黑白颜色
    /// </summary>
    public static void CalculateLayout(
        ChessBoardConfig boardCfg,
        ChessBoardTileConfig tileCfg,
        out Dictionary<Vector2Int, Vector3> tilePositions,
        out Dictionary<Vector2Int, bool> isWhiteTile)
    {
        tilePositions = new Dictionary<Vector2Int, Vector3>();
        isWhiteTile = new Dictionary<Vector2Int, bool>();

        Vector3 start = boardCfg.TileInitPosition;
        int TileX = boardCfg.TileNumberX;
        int TileZ = boardCfg.TileNumberZ;
        float sx = tileCfg.SizeX;
        float sz = tileCfg.SizeZ;

        for (int x = 0; x < TileX; x++)
        {
            for (int z = 0; z < TileZ; z++)
            {
                // 世界坐标（格子中心点）
                Vector3 worldPos = new Vector3(
                    start.x + x * sx,
                    start.y,
                    start.z + z * sz
                );

                // 黑白格规则：(x+z) 偶数为黑，奇数为白
                bool white = (x + z) % 2 == 1;

                Vector2Int key = new Vector2Int(x, z);
                tilePositions[key] = worldPos;
                isWhiteTile[key] = white;
            }
        }
    }
}