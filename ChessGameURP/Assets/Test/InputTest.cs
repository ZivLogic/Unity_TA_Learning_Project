using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputTest : MonoBehaviour
{
    private GameObject selectedObj;
    private GameObject ChessMan;
    private GameObject ChessTile;
    
    public void Event()
    {
        EventManager.Instance.Listen<InputMouseSelect>(Test001);
        EventManager.Instance.Listen<GetTileObj>(Test002);
    }
    private void Test001(InputMouseSelect e)
    {
        var pack = e.package;
        if (pack.Get<GameObject>(EventPackName.INPUT_MOUSESELECT, out var obj)) { }
        if (obj == null)
        {
            Debug.LogWarning("获取对象为空");
            return;
        }
        ChessMan = obj;
        //Debug.Log($"获取到物体：{selectedObj},是否为空：{selectedObj == null}");
    }
    private void Test002(PackageEvent e)
    {
        var pack = e.package;
        if (pack.Get<GameObject>(EventPackName.INPUT_MOUSESELECT, out var obj)) { }
        if (obj == null)
        {
            Debug.LogWarning("获取对象为空");
            return;
        }
        ChessTile = obj;
        //Debug.Log($"获取到物体：{selectedObj},是否为空：{selectedObj == null}");
    }

    // Start is called before the first frame update
    void Start()
    {
        Event();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            InputManager.Instance.SwitchInputContext(InputContext.GameWorld);
            InputManager.Instance.SwitchInputMode(InputRunMode.NormalOperate);
            Debug.Log("开始");
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            InputManager.Instance.SwitchInputContext(InputContext.GameWorld);
            InputManager.Instance.SwitchInputMode(InputRunMode.ForbidAll);
            Debug.Log("结束");
        }
        if (Input.GetKeyDown(KeyCode.W) && ChessMan != null)
        {
            Vector3 pos = new Vector3(0, 0.3f, 0);
            ChessMan.transform.position = pos;
            Debug.Log("归零");
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            Debug.Log("按下W键");
        }
        if (Input.GetKeyDown(KeyCode.F) && ChessMan != null)
        {
            Debug.Log("按下F键");
            InputManager.Instance.SwitchInputContext(InputContext.SelectChessTile);
            //InputManager.Instance.SwitchInputMode(InputRunMode.NormalOperate);
            Debug.Log("锁定选中体,切换到选择棋盘格模式");
            
        }
        if (Input.GetKeyDown(KeyCode.G) && ChessMan != null && ChessTile != null)
        {
            Debug.Log("按下G键");
            if (ChessMan == null)
            {
                Debug.LogError("棋子为空，重新选择");
                return;
            }
            var tag = ChessMan.GetComponent<LogicIdentity>();
            if (tag == null)
            {
                Debug.LogError("身份组件为空");
                return ;
            }
            string ID = tag.LogicID;
            if ( ! GlobalIDManager.Instance.LogicIDIsValid(ID))
            {
                Debug.LogError("ID检验不通过");
                return;
            }
            var pack = new Package();
            pack.Put("1", ChessMan);
            pack.Put("2", ChessTile);
            var pub = new MoveChessTest { package = pack };
            EventManager.Instance.EmitInput(pub);
            Debug.Log("锁定棋盘格模型，发布移动事件");
        }
        //if (Input.GetMouseButtonDown(0))
        //{
        //    Debug.Log("按下左键");
        //}
    }
}
