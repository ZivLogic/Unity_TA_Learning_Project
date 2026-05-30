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





    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
