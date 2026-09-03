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

    public void Test_GameObj()
    {
        _publish.Test_GameObj();
    }

    public void Test_ConfigData()
    {
        _publish.Test_ConfigData();
    }

    public void Awake()
    {

    }

    //测试生成配置代码
    public void GenerateConfigs()
    {
        // 生成地图生成点配置
        var spawnBuilder = new ConfigBuilder();
        //spawnBuilder.Group("map_spawn_points");
        spawnBuilder.Add("spawnPoints", new List<object>
        {
            new Dictionary<string, object>
            {
                { "id", "forest_01" },
                { "x", 10 },
                { "y", 0 },
                { "z", 20 }
            }
        });
        spawnBuilder.ExportToFileFormat("Assets/Config/ConfigData/map_spawn_points.json");

        // 生成模型放置配置
        var placementBuilder = new ConfigBuilder();
        placementBuilder.Group("model_placements");
        placementBuilder.Add("placements", new List<object>
        {
            new Dictionary<string, object>
            {
                { "spawnPointId", "forest_01" },
                { "modelId", "tree_01" },
                { "rotationY", 45 }
            }
        });
        placementBuilder.ExportToFileFormat("Assets/Config/ConfigData/model_placements.json");
        Debug.Log("生成配置成功");
    }



    // Start is called before the first frame update
    void Start()
    {
        //GenerateConfigs();
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
