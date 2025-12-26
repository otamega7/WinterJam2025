using UnityEngine;

public class SpawnerController : MonoBehaviour
{
    [Header("References")]
    public CourseManager courseManager;
    public GameObject enemyPrefab;
    public Transform playerCar;

    [Header("Spawn Settings")]
    public float spawnRadius = 5f;
    public int spawnCount = 3;
    public float minDistanceFromPlayer = 20f;

    void Start()
    {
        SpawnEnemies();
    }

    public void SpawnEnemies()
    {
        if (courseManager == null || enemyPrefab == null)
        {
            Debug.LogError("SpawnerController: Reference missing");
            return;
        }

        int waypointCount = courseManager.Waypoints.Count;

        for (int i = 0; i < spawnCount; i++)
        {
            int index = Random.Range(0, waypointCount);
            Vector3 basePos = courseManager.Waypoints[index].position;

            // Waypoint 周辺にランダムオフセット
            Vector2 rand = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPos = basePos + new Vector3(rand.x, 0f, rand.y);

            // プレイヤーに近すぎたらスキップ
            if (playerCar != null)
            {
                float dist = Vector3.Distance(spawnPos, playerCar.position);
                if (dist < minDistanceFromPlayer)
                {
                    i--;
                    continue;
                }
            }

            GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

            // AI に CourseManager / Player を渡す
            AICarController ai = enemy.GetComponent<AICarController>();
            if (ai != null)
            {
                ai.course = courseManager;
                ai.playerCar = playerCar;
            }
        }
    }
}
