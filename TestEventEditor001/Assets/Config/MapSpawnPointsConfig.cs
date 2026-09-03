using System;

[Serializable]
[Config(
    configId: "map_spawn_points",
    jsonPath: "map_spawn_points.json",
    priority: 100  // 优先级最低，最先加载
)]
public class MapSpawnPointsConfig : ConfigDefinition
{
    public SpawnPoint[] spawnPoints;

    [Serializable]
    public class SpawnPoint
    {
        public string id;
        public float x;
        public float y;
        public float z;
    }

    //public int TestNumber;
}