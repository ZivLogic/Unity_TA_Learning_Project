using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitch : MonoBehaviour
{
    //按钮点击调用这个
    public void GoToGame()
    {
        //场景名必须和保存场景完全一致
        SceneManager.LoadScene("GameScene");
    }

    public void GoStartUI()
    {
        SceneManager.LoadScene("StartScene");
    }

    public void GoTestGame()
    {
        SceneManager.LoadScene("SampleScene");
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
