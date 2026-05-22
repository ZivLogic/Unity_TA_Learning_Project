using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputTest : MonoBehaviour
{
    private GameObject selectedObj;
    
    public void Event()
    {
        EventManager.Instance.Listen<InputMouseSelect>(Test001);
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
        selectedObj = obj;
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
        if (Input.GetKeyDown(KeyCode.W) && selectedObj != null)
        {
            Vector3 pos = new Vector3(0, 0.3f, 0);
            selectedObj.transform.position = pos;
            Debug.Log("归零");
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            Debug.Log("按下W键");
        }
    }
}
