using UnityEngine;
using System.Collections;

public class WaveManager : MonoBehaviour
{
    [Header("Enemy Types")]
    public GameObject scoutEnemyPrefab;      // Original fast enemy
    public GameObject armoredEnemyPrefab;    // New tank enemy
    
    [Header("Wave Settings")]
    public Transform spawnPoint;
    public Transform targetPoint;
    
    [Header("Wave Configuration")]
    public int enemiesPerWave = 3;
    public float timeBetweenWaves = 5f;
    public float timeBetweenEnemies = 1f;
    public int totalWaves = 5;
    public Transform playerBase;
    
    [Header("Enemy Mix")]
    [Range(0f, 1f)]
    public float scoutPercentage = 0.7f;  // 70% scouts, 30% armored
    
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
        // Choose enemy type based on percentage
        GameObject enemyToSpawn = (Random.value < scoutPercentage) ? scoutEnemyPrefab : armoredEnemyPrefab;
        
        GameObject enemy = Instantiate(enemyToSpawn, spawnPoint.position, Quaternion.identity);
        EnemyMovement enemyMovement = enemy.GetComponent<EnemyMovement>();
        if (enemyMovement != null)
        {
            enemyMovement.target = targetPoint;
            enemyMovement.secondaryTarget = playerBase;
        }
        
        // Debug.Log($"Spawned: {enemyToSpawn.name}");
    }
}