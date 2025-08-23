using UnityEngine;
using System.Collections;

// wave progression management 
[System.Serializable]
public class WaveData
{
    public int waveNumber;
    public int enemyCount;
    public float scoutPercentage;
    public float armoredPercentage;
    public float rangedPercentage;
    public float swarmPercentage;
    public bool isBossWave;
}

public class WaveManager : MonoBehaviour
{
    [Header("Enemy Types")] 
    public GameObject scoutEnemyPrefab;
    public GameObject armoredEnemyPrefab;
    public GameObject rangedEnemyPrefab;
    public GameObject swarmEnemyPrefab;

    [Header("Wave Progression")]
    public WaveData[] waveProgression = new WaveData[10];
        
    [Header("Wave Settings")] 
    public Transform spawnPoint;
    public Transform targetPoint;
    public Transform playerBase;

    [Header("Wave Timing")] 
    public float timeBetweenWaves = 5f;
    public float timeBetweenEnemies = 1f;

    private int currentWave = 0;
    private bool waveInProgress = false;

    void Start()
    {
        SetupWaveProgression();
        StartCoroutine(StartWaveSequence());
    }

    void SetupWaveProgression()
    {
        var waves = new[]
        {
            new { wave = 1,  count = 3,  scout = 0.0f,  armored = 0.0f,  ranged = 0.0f,  swarm = 1.0f },  // Pure scouts
            new { wave = 2,  count = 4,  scout = 0.7f,  armored = 0.3f,  ranged = 0.0f,  swarm = 0.0f },  // Introduce armored
            new { wave = 3,  count = 5,  scout = 0.5f,  armored = 0.5f,  ranged = 0.0f,  swarm = 0.0f },  // Tank strategy
            new { wave = 4,  count = 6,  scout = 0.4f,  armored = 0.4f,  ranged = 0.2f,  swarm = 0.0f },  // Introduce ranged
            new { wave = 5,  count = 7,  scout = 0.3f,  armored = 0.3f,  ranged = 0.25f, swarm = 0.15f }, // Introduce swarms
            new { wave = 6,  count = 8,  scout = 0.25f, armored = 0.25f, ranged = 0.3f,  swarm = 0.2f },  // Full variety
            new { wave = 7,  count = 10, scout = 0.2f,  armored = 0.3f,  ranged = 0.25f, swarm = 0.25f }, // Balanced chaos
            new { wave = 8,  count = 12, scout = 0.15f, armored = 0.4f,  ranged = 0.25f, swarm = 0.2f },  // Tank heavy
            new { wave = 9,  count = 15, scout = 0.25f, armored = 0.2f,  ranged = 0.35f, swarm = 0.2f },  // Sniper hell
            new { wave = 10, count = 20, scout = 0.2f,  armored = 0.25f, ranged = 0.3f,  swarm = 0.25f }  // FINAL REGULAR WAVE
        };

        // convert to WaveData objects
        for (int i = 0; i < waves.Length; i++)
        {
            waveProgression[i] = new WaveData
            {
                waveNumber = waves[i].wave,
                enemyCount = waves[i].count,
                scoutPercentage = waves[i].scout,
                armoredPercentage = waves[i].armored,
                rangedPercentage = waves[i].ranged,
                swarmPercentage = waves[i].swarm,
                isBossWave = false  // Boss will have his own dedicated wave.
            };
        }

        Debug.Log($"Normal wave progression initialized - {waves.Length} waves loaded!");
    }
    
    IEnumerator StartWaveSequence()
    {
        while (currentWave < waveProgression.Length)  // Use array length instead of totalWaves
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
        
        // get wave data. currentWave is 1 based and array is 0 based.
        WaveData currentWaveData = waveProgression[currentWave - 1];
        
        Debug.Log($"Wave {currentWave}: Spawning {currentWaveData.enemyCount} enemies");

        for (int i = 0; i < currentWaveData.enemyCount; i++)  // Use progression data
        {
            SpawnEnemyFromProgression(currentWaveData);
            yield return new WaitForSeconds(timeBetweenEnemies);
        }

        waveInProgress = false;
    }

    void SpawnEnemyFromProgression(WaveData waveData)
    {
        GameObject enemyToSpawn;
        string enemyType;
        float random = Random.value;

        if (random < waveData.scoutPercentage)
        {
            enemyToSpawn = scoutEnemyPrefab;
            enemyType = "SCOUT";
            SpawnSingleEnemy(enemyToSpawn, enemyType);
        }
        else if (random < waveData.scoutPercentage + waveData.armoredPercentage)
        {
            enemyToSpawn = armoredEnemyPrefab;
            enemyType = "ARMORED";
            SpawnSingleEnemy(enemyToSpawn, enemyType);
        }
        else if (random < waveData.scoutPercentage + waveData.armoredPercentage + waveData.rangedPercentage)
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
        // EnemyMovement now auto-acquires targets on Start()
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
            // EnemyMovement now auto-acquires targets on Start()
        }

        Debug.Log($"Spawned: SWARM group of {swarmSize} enemies!");
    }
}