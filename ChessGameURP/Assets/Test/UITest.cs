using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UITest : MonoBehaviour
{
    public void GetChessMan()
    {
        var pack = new Package();
        var pub = new GetChessMan_TestUI { package = pack };
        EventManager.Instance.EmitLogic(pub);
    }

    public void GetTile()
    {
        var pack = new Package();
        var pub = new GetChessTile_TestUI { package = pack };
        EventManager.Instance.EmitLogic(pub);
    }

    public void OnMove()
    {
        var pack = new Package();
        var pub = new MoveChessMan_TestUI { package = pack };
        EventManager.Instance.EmitLogic(pub);
    }

    [EventPublishMethod]
    public void TestEventUI(out int EventTest1)
    {
        Debug.Log("事件正常发送");
        EventTest1 = 1;
    }

    public void TestEvent()
    {
        Debug.Log("UI正常按下");
        int EventTest1 = 0;
        TestEventUI(out EventTest1);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
