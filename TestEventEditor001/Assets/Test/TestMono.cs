using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EventSystemV2;
public class TestMono : MonoBehaviour
{

    TestPublish _publish = new();
    TestListen _listen = new();

    public void TestUI()
    {
        _publish.TestEvent();
    }

    public void WirtConfig()
    {
        _publish.InitTestConfig();
    }

    public void TestEvent0002()
    {
        _publish.TestEVen002();
    }

    public void TestCLassEvent_003()
    {
        _publish.TestCLASS003();
    }

    public void Awake()
    {

    }






    // Start is called before the first frame update
    void Start()
    {
        _publish.RegistEventTest();
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void OnDestroy()
    {

    }
}
