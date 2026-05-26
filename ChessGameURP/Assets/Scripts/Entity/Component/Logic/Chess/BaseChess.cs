using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseChess : MonoBehaviour
{
    [Header("棋子基础信息")]
    public CampType CampType;
    public Vector2Int InitLogicPos;
    //本地缓存
    private string _logicID;
    private Vector2Int _curLogicPos;
    //专属配置缓存
    private Chess_PawnConfig _PawnConfig;
    private Chess_RookConfig _RookConfig;
    private Chess_KnightConfig _KnightConfig;
    private Chess_BishopConfig _BishopConfig;
    private Chess_QueenConfig _QueenConfig;
    private Chess_KingConfig _KingConfig;
    //规则委托
    private delegate bool RuleCheck(Vector2Int cur, Vector2Int tar, CampType camp);
    private RuleCheck _ruleChecker;
    #region 订阅事件
    //订阅配置事件
    private void Awake()
    {
        EventManager.Instance.Listen<ChessComponentCfg>(OnReceiveConfig);
        EventManager.Instance.Listen<MoveChessTest>(OnReceiveMoveCommand);
    }
    #endregion
    #region 初始化：接收配置，绑定规则，注册棋盘
    private void OnReceiveConfig(PackageEvent e)
    {
        ChessComponentCfg evt = e as ChessComponentCfg;
        var pack = evt.package;
        if (pack.Get<Chess_PawnConfig>(EventPackName.EntityComponentUtil_GetChessManComponentConfig_Pawn, out var pawn)) { }
        if (pack.Get<Chess_RookConfig>(EventPackName.EntityComponentUtil_GetChessManComponentConfig_Rook, out var rook)) { }
        if (pack.Get<Chess_KnightConfig>(EventPackName.EntityComponentUtil_GetChessManComponentConfig_Knight, out var knight)) { }
        if (pack.Get<Chess_BishopConfig>(EventPackName.EntityComponentUtil_GetChessManComponentConfig_Bnishop, out var bishop)) { }
        if (pack.Get<Chess_QueenConfig>(EventPackName.EntityComponentUtil_GetChessManComponentConfig_Queen, out var queen)) { }
        if (pack.Get<Chess_KingConfig>(EventPackName.EntityComponentUtil_GetChessManComponentConfig_King, out var king)) { }
        if (!pack.ValidsteAll())
        { Debug.LogError($"[BaseChess]某值为空！故障事件：{e}"); return; }
        _PawnConfig = pawn;
        _RookConfig = rook;
        _KnightConfig = knight;
        _BishopConfig = bishop;
        _QueenConfig = queen;
        _KingConfig = king;
        //初始化坐标
        _curLogicPos = InitLogicPos;
        //根据棋子类型 绑定对应的移动规则
        BindRuleByChessType();
        //数据全部就绪后，注册到全局棋盘状态
        ChessBoardState.InitChessPos(_curLogicPos.x, _curLogicPos.y, CampType);
        Debug.Log($"[{_logicID}]配置加载完成，棋盘注册成功");
    }
    //根据类型，绑定规则
    private void BindRuleByChessType()
    {
        //通过ChessManType确定身份
        var tag = GetComponent<ChessManType>();
        if (tag == null)return;
        string chrssType = tag.ChessName;
        switch (chrssType)
        {
            case "Pawn":
                _ruleChecker = (cur, tar, camp) => PawnMoveRule.CeckLegal(cur, tar, camp, _PawnConfig);
                break;
            case "Rook":

                break;
            case "Knight":

                break ;
            case "Bishop":

                break ;
            case "Queen":

                break ;
            case "King":

                break ;
            default:
                Debug.LogWarning($"[BaseChess]未知棋子类型：{chrssType}");
                break;
        }
    }
    //获取自身ID
    private void GetLogicID()
    {
        var tag = gameObject.GetComponent<LogicIdentity>();
        if (tag == null)
        {
            Debug.LogError($"[BaseChess]此物体没有ID！物体名称：{gameObject.name}");
            return;
        }
        _logicID = tag.LogicID;
    }
    //获取自身属性
    private void GetInitPos()
    {
        var tag = gameObject.GetComponent<ChessManType>();
        if (tag == null)
        {
            Debug.LogError($"[BaseChess]此物体没有属性！物体名称：{gameObject.name}");
        }
        CampType = tag.CampType;
        InitLogicPos = tag.LogicPos;
    }
    #endregion
    #region 接收移动指令 & 执行移动逻辑
    private void OnReceiveMoveCommand(PackageEvent e)
    {
        MoveChessTest evt = e as MoveChessTest;
        var pack = evt.package;
        if (pack.Get<GameObject>("1", out var chessMan)) { }
        if (pack.Get<GameObject>("2", out var chessTile)) { }
        if (!pack.ValidsteAll())
        { Debug.LogError($"[BaseChess]某值为空！故障事件：{e}"); return; }
        var tag = chessMan.GetComponent<LogicIdentity>();
        if (tag == null)
        {
            Debug.LogError("[BaseChess]棋子身份组件为空");
            return;
        }
        string ID = tag.LogicID;
        var tag2 = chessMan.GetComponent<ChessManType>();
        if (tag2 == null)
        {
            Debug.LogError("[BaseChess]棋子属性组件为空");
            return;
        }
        Vector2Int manPos = tag2.LogicPos;
        
        var tag3 = chessTile.GetComponent<ChessTileLogic>();
        if (tag3 == null)
        {
            Debug.LogError("[BaseChess]棋格属性组件为空");
            return;
        }
        int x = tag3.logicX;
        int y = tag3.logicY;
        Vector2Int tilePos = new Vector2Int(x, y);
        //ID校验：只处理发给自己的指令
        if (ID != _logicID)
            return;
        //规则检验
        bool canMove = _ruleChecker.Invoke(_curLogicPos, tilePos, CampType);
        if ( ! canMove )
        {
            OnMoveComplete();
            return;
        }
        //更新全局棋盘状态
        bool updateSuccess = ChessBoardState.UpdateChessPos(_curLogicPos.x, _curLogicPos.y, x, y, CampType);
        if ( ! updateSuccess )
        {
            OnMoveComplete();
            return;
        }
        //更新本地当前坐标
        Vector2Int oldPos = _curLogicPos;
        _curLogicPos = tilePos;
        tag2.LogicPos = tilePos;
        //检测吃子
        if (ChessBoardState.IsEnemy(x, y, CampType))
        {
            OnChessComplete(tilePos);
        }
        //推送移动完成事件
        OnMoveComplete(oldPos, tilePos);
    }
    //移动失败回调
    private void OnMoveComplete()
    {
        //自行触发移动失败事件
        Debug.LogError("移动非法！");
    }
    //吃子回调
    private void OnChessComplete(Vector2Int capturePos)
    {
        //获取原坐标棋子，注销他
        Debug.Log($"[{_logicID}]吃子：{capturePos}");
    }
    //移动完成回调
    private void OnMoveComplete(Vector2Int oldPos, Vector2Int newPos)
    {
        //发布事件
    }
    #endregion
    #region 销毁清理
    private void OnDestroy()
    {
        //移除棋盘占用
        ChessBoardState._gridCampDict.Remove((_curLogicPos.x, _curLogicPos.y));
        //注销逻辑ID
        GlobalIDManager.Instance.UnregisterLogic(_logicID);
        //取消事件订阅
        EventManager.Instance.Unlisten<ChessComponentCfg>(OnReceiveConfig);
        EventManager.Instance.Unlisten<MoveChessTest>(OnReceiveMoveCommand);
    }
    #endregion



    // Start is called before the first frame update
    void Start()
    {
        GetLogicID();
        GetInitPos();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
