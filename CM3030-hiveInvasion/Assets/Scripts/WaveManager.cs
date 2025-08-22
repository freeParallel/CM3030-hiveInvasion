using UnityEngine;
using System.Collections;

public class WaveManager : MonoBehaviour
{
    [Header("Enemy Types")] public GameObject scoutEnemyPrefab; // Original fast enemy
    public GameObject armoredEnemyPrefab; // New tank enemy
    public GameObject rangedEnemyPrefab;
    public GameObject swarmEnemyPrefab;

    [Header("Wave Settings")] public Transform spawnPoint;
    public Transform targetPoint;

    [Header("Wave Configuration")] public int enemiesPerWave = 3;
    public float timeBetweenWaves = 5f;
    public float timeBetweenEnemies = 1f;
    public int totalWaves = 5;
    public Transform playerBase;

    [Header("Enemy Mix")] [Range(0f, 1f)] public float scoutPercentage = 0.4f; // 40% scouts, 20% armored, 20% ranged
    [Range(0f, 1f)] public float armoredPercentage = 0.2f;
    [Range(0f, 1f)] public float rangedPercentage = 0.2f;
    // the remaining 20% will be given to swarm groups

    private int currentWave = 0;
    private bool waveInProgress = false;

    void Start()
    {
        StartCoroutine(StartWaveSequence());
    }

    IEnumerator StartWaveSequence()
    {
        while (currentWave < totalWaves)
        {
            currentWave++;
            Debug.Log($"Starting Wave {currentWave}");

            yield return StartCoroutine(SpawnWave());
            yield return new WaitForSeconds(timeBetweenWaves);
        }

        Debug.Log("All waves completed!");
    }

    IEnumerator SpawnWave()
    {
        waveInProgress = true;

        for (int i = 0; i < enemiesPerWave; i++)
        {
            SpawnRandomEnemy();
            yield return new WaitForSeconds(timeBetweenEnemies);
        }

        waveInProgress = false;
    }

    void SpawnRandomEnemy()
    {
        GameObject enemyToSpawn;
        string enemyType;
        float random = Random.value;

        if (random < scoutPercentage)
        {
            enemyToSpawn = scoutEnemyPrefab;
            enemyType = "SCOUT";
            SpawnSingleEnemy(enemyToSpawn, enemyType);
        }
        else if (random < scoutPercentage + armoredPercentage)
        {
            enemyToSpawn = armoredEnemyPrefab;
            enemyType = "ARMORED";
            SpawnSingleEnemy(enemyToSpawn, enemyType);
        }
        else if (random < scoutPercentage + armoredPercentage + rangedPercentage)
        {
            enemyToSpawn = rangedEnemyPrefab;
            enemyType = "RANGED";
            SpawnSingleEnemy(enemyToSpawn, enemyType);
        }
        else
        {
            // SWARM spawns multiple
            SpawnSwarmGroup();
        }
    }

    void SpawnSingleEnemy(GameObject enemyPrefab, string enemyType)
    {
        GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
        EnemyMovement enemyMovement = enemy.GetComponent<EnemyMovement>();
        if (enemyMovement != null)
        {
            enemyMovement.target = targetPoint;
            enemyMovement.secondaryTarget = playerBase;
        }

        Debug.Log($"Spawned {enemyType} from {enemyPrefab.name}");
    }

    void SpawnSwarmGroup()
    {
        int swarmSize = Random.Range(3, 6);

        for (int i = 0; i < swarmSize; i++)
        {
            // position offset to avoid all spawning at same point
            Vector3 spawnPosition = spawnPoint.position + new Vector3(
                Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)
            );

            GameObject enemy = Instantiate(swarmEnemyPrefab, spawnPosition, Quaternion.identity);
            EnemyMovement enemyMovement = enemy.GetComponent<EnemyMovement>();
            if (enemyMovement != null)
            {
                enemyMovement.target = targetPoint;
                enemyMovement.secondaryTarget = playerBase;
            }
        }

        Debug.Log($"Spawned: SWARM group of {swarmSize} enemies!");
    }
}