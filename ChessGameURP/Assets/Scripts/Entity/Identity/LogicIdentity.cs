using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//挂载在父级的逻辑空物体
public class LogicIdentity : MonoBehaviour
{
    //逻辑体全局ID
    public string LogicID {  get; internal set; }

    private void OnDestroy()
    {
        if ( ! string.IsNullOrEmpty( LogicID ) )
        {
            GlobalIDManager.Instance.UnregisterLogic(LogicID);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log($"[LogicIdentity]全局ID：{LogicID},物体名称：{gameObject.name}");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
