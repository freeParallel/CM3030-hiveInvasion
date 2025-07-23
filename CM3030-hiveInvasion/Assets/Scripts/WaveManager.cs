using UnityEngine;
using System.Collections;

public class WaveManager : MonoBehaviour
{
    [Header("Wave Settings")]
    public GameObject enemyPrefab;
    public Transform spawnPoint;
    public Transform targetPoint;
    
    [Header("Wave Configuration")]
    public int enemiesPerWave = 3;
    public float timeBetweenWaves = 5f;
    public float timeBetweenEnemies = 1f;
    public int totalWaves = 5;
    public Transform playerBase;
    
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
            SpawnEnemy();
            yield return new WaitForSeconds(timeBetweenEnemies);
        }
        
        waveInProgress = false;
    }
    
    void SpawnEnemy()
    {
        GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
        EnemyMovement enemyMovement = enemy.GetComponent<EnemyMovement>();
        if (enemyMovement != null)
        {
            enemyMovement.target = targetPoint;
            enemyMovement.secondaryTarget = playerBase;
        }
    }
}