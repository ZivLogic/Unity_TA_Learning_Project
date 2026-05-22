using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//挂载在子级渲染模型物体
public class RenderIdentity : MonoBehaviour
{
    //渲染全局ID
    public string RenderID {  get; internal set; }  //internal代表对不同程序集的限制

    private void OnDestroy()
    {
        if ( ! string.IsNullOrEmpty( RenderID ) )
        {
            GlobalIDManager.Instance.UnregisterRender(RenderID);
        }
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
