using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyText : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //鼠标点击
        if (Input.GetMouseButton(0))
        {
            transform.Rotate(Vector3.up, 1);
        }
        ;
        //前进
        if (Input.GetKey(KeyCode.W))
        {
            transform.Translate(Vector3.forward * 0.1f);
        }
        ;
        //后退
        if (Input.GetKey(KeyCode.S))
        {
            transform.Translate(Vector3.back * 0.1f);
        }
        ;
        //向左
        if (Input.GetKey(KeyCode.A))
        {
            transform.Translate(Vector3.left * 0.1f);
        }
        ;
        //向右
        if (Input.GetKey(KeyCode.D))
        {
            transform.Translate(Vector3.right * 0.1f);
        }
        ;
    }
}
