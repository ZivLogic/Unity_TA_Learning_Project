// Assets/Scripts/Test/FactoryTest.cs
using UnityEngine;

public class FactoryTest : MonoBehaviour
{
    void Start()
    {
        // 延迟一帧，确保所有系统初始化完成
        Invoke("RunTest", 1.0f);
    }

    private void RunTest()
    {
        Debug.Log("========== 开始工厂测试 ==========");

        // 1. 测试对象池预热
        Debug.Log("1. 测试对象池预热...");
        MasterFactory.Instance.PoolFactory.PrewarmPool("pawn", 5);

        //// 2. 测试地图加载
        //Debug.Log("2. 测试地图加载...");
        //MasterFactory.Instance.MapFactory.LoadMap("test_map");

        // 3. 测试UI创建
        Debug.Log("3. 测试UI创建...");
        var uiBlueprint = MasterFactory.Instance.GetBlueprint("pawn_info_ui");
        var uiEntity = MasterFactory.Instance.CreateEntity(uiBlueprint);

        // 4. 测试快速查找
        Debug.Log("4. 测试快速查找...");
        var pawn = MasterFactory.Instance.FindFirstByTag("Pawn");
        if (pawn != null)
        {
            Debug.Log($"找到棋子: {pawn.EntityId} at {pawn.Root.transform.position}");
        }

        // 5. 测试对象池取出
        Debug.Log("5. 测试对象池取出...");
        var pooledPawn = MasterFactory.Instance.PoolFactory.GetFromPool("pawn_pool");
        if (pooledPawn != null)
        {
            pooledPawn.Root.transform.position = new Vector3(5, 0.5f, 5);
            Debug.Log($"从对象池取出棋子: {pooledPawn.EntityId}");
        }

        Debug.Log("========== 工厂测试完成 ==========");

        //所有实体创建完成再调用逻辑数据加载注入
        LogicStateManager.Instance.LoadAllStates();
    }
}