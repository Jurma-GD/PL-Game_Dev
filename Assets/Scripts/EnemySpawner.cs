using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject enemyPrefab;
    public int maxEnemies = 5;
    public float spawnInterval = 3f;
    public bool isActive = true;
    public int totalSpawnLimit = 20; // max total enemies ever spawned before stopping

    private int activeEnemies = 0;
    private int totalSpawned = 0;
    private float timer = 0f;

    private void Update()
    {
        if (!isActive) return;
        if (activeEnemies >= maxEnemies) return;
        if (totalSpawned >= totalSpawnLimit) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            SpawnEnemy();
            timer = 0f;
        }
    }

    private void SpawnEnemy()
    {
        GameObject enemy = Instantiate(enemyPrefab, transform.position, Quaternion.identity);
        activeEnemies++;
        totalSpawned++;

        // Listen for when this enemy is disabled (dies)
        EnemyLifecycleNotifier notifier = enemy.AddComponent<EnemyLifecycleNotifier>();
        notifier.spawner = this;
    }

    public void OnEnemyDied()
    {
        activeEnemies = Mathf.Max(0, activeEnemies - 1);
    }
}

// Helper component added at runtime to notify spawner when enemy dies
public class EnemyLifecycleNotifier : MonoBehaviour
{
    public EnemySpawner spawner;

    private void OnDisable()
    {
        if (spawner != null)
            spawner.OnEnemyDied();
    }
}
