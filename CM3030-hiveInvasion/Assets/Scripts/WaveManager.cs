using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

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
    // Wave lifecycle events for UI and systems
    public static UnityEvent<int> OnWaveStarted = new UnityEvent<int>();
    public static UnityEvent<int> OnWaveCompleted = new UnityEvent<int>();
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
    
    // wave difficulty progression setup
    [Header("Difficulty Scaling")]
    [Tooltip("speed will increase by this % every 3 waves")]
    public float speedScalePerStep = 0.05f;
    [Tooltip("HP will increase by this % every 2 waves")]
    public float healthScalePerStep = 0.10f;

    [Header("Start Offset")]
    // initial delay before this wavemanager starts spawning waves (offset multiple managers)
    public float initialStartDelay = 0f;

    // whether this manager emits wave start/complete announcements for ui
    public bool emitAnnouncements = true;

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
            new { wave = 1,  count = 3,  scout = 0.0f,  armored = 0.0f,  ranged = 0.0f,  swarm = 1.0f },  // Swarm team
            new { wave = 2,  count = 4,  scout = 0.7f,  armored = 0.3f,  ranged = 0.0f,  swarm = 0.0f },  // Introduce armored
            new { wave = 3,  count = 5,  scout = 0.5f,  armored = 0.5f,  ranged = 0.0f,  swarm = 0.0f },  // Tank strategy
            new { wave = 4,  count = 6,  scout = 0.4f,  armored = 0.4f,  ranged = 0.2f,  swarm = 0.0f },  // Introduce ranged
            new { wave = 5,  count = 7,  scout = 0.3f,  armored = 0.3f,  ranged = 0.25f, swarm = 0.15f }, // More swarms
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
    
    // calculate speed multiplier: +5% every 3 waves
    float CalculateSpeedMultiplier(int waveNumber)
    {
        int speedSteps = (waveNumber - 1) / 3; // Wave 1-3: 0 steps, 4-6: 1 step, etc.
        return 1.0f + (speedScalePerStep * speedSteps);
    }

    // calculate HP multiplier: +10% every 2 waves  
    float CalculateHealthMultiplier(int waveNumber)
    {
        int healthSteps = (waveNumber - 1) / 2; // Wave 1-2: 0 steps, 3-4: 1 step, etc.
        return 1.0f + (healthScalePerStep * healthSteps);
    }
    
    void ApplyWaveScaling(GameObject enemy, int waveNumber)
    {
        // Calculate scaling multipliers
        float speedMultiplier = CalculateSpeedMultiplier(waveNumber);
        float healthMultiplier = CalculateHealthMultiplier(waveNumber);
    
        // Apply speed scaling to NavMeshAgent
        var agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            float originalSpeed = agent.speed;
            agent.speed = originalSpeed * speedMultiplier;
        }
    
        // Apply health scaling to EnemyHealth
        var health = enemy.GetComponent<EnemyHP>();
        if (health != null)
        {
            int originalMaxHealth = health.maxHealth;
            int scaledMaxHealth = Mathf.RoundToInt(originalMaxHealth * healthMultiplier);
            health.maxHealth = scaledMaxHealth;
            health.currentHealth = scaledMaxHealth; // Set current to max
        }
    
        Debug.Log($"Applied scaling to {enemy.name} - Speed: {speedMultiplier:F2}x, Health: {healthMultiplier:F2}x");
    }
    
    IEnumerator StartWaveSequence()
    {
        // Optional offset for multi-Manager setups
        if (initialStartDelay > 0f)
        {
            yield return new WaitForSeconds(initialStartDelay);
        }

        while (currentWave < waveProgression.Length)
        {
            currentWave++;

            // Calculate and log scaling for this wave
            float speedMultiplier = CalculateSpeedMultiplier(currentWave);
            float healthMultiplier = CalculateHealthMultiplier(currentWave);

            Debug.Log($"Starting Wave {currentWave} - Speed: {speedMultiplier:F2}x, Health: {healthMultiplier:F2}x");

            if (emitAnnouncements)
                OnWaveStarted.Invoke(currentWave);

            yield return StartCoroutine(SpawnWave());
            if (emitAnnouncements)
                OnWaveCompleted.Invoke(currentWave);
            yield return new WaitForSeconds(timeBetweenWaves);
        }

        Debug.Log("All waves completed!");
    }

    IEnumerator SpawnWave()
    {
        waveInProgress = true;
        
        // get wave data. currentWave is 1 based and array is 0 based.
        WaveData currentWaveData = waveProgression[currentWave - 1];
        
        // Build deterministic exact composition for this wave (with grouped swarms)
        List<SpawnItem> composition = BuildWaveComposition(currentWaveData);
        int totalToSpawn = 0;
        foreach (var item in composition) totalToSpawn += Mathf.Max(1, item.count);
        Debug.Log($"Wave {currentWave}: Spawning exactly {totalToSpawn} enemies (deterministic composition with groups)");

        foreach (var item in composition)
        {
            if (item.count <= 1 || item.prefab != swarmEnemyPrefab)
            {
                // Single spawn
                SpawnSingleEnemy(item.prefab, item.prefab != null ? item.prefab.name : "UNKNOWN");
            }
            else
            {
                // Grouped swarm spawn: spawn 'count' enemies at once with slight offsets
                int swarmSize = item.count;
                for (int i = 0; i < swarmSize; i++)
                {
                    Vector3 spawnPosition = spawnPoint.position + new Vector3(
                        Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)
                    );
                    GameObject enemy = Instantiate(item.prefab, spawnPosition, Quaternion.identity);
    
                    // Apply wave-based scaling to swarm enemy
                    ApplyWaveScaling(enemy, currentWave);
                }           
                Debug.Log($"Spawned: SWARM group of {swarmSize} enemies!");
            }

            yield return new WaitForSeconds(timeBetweenEnemies);
        }

        waveInProgress = false;
    }

    // Deprecated: Legacy per-draw random composition. Unused; kept for reference now that
    // deterministic BuildWaveComposition is used for exact per-wave totals and ordering.
    // void SpawnEnemyFromProgression(WaveData waveData)
    // {
    //     GameObject enemyToSpawn;
    //     string enemyType;
    //     float random = Random.value;
    //
    //     if (random < waveData.scoutPercentage)
    //     {
    //         enemyToSpawn = scoutEnemyPrefab;
    //         enemyType = "SCOUT";
    //         SpawnSingleEnemy(enemyToSpawn, enemyType);
    //     }
    //     else if (random < waveData.scoutPercentage + waveData.armoredPercentage)
    //     {
    //         enemyToSpawn = armoredEnemyPrefab;
    //         enemyType = "ARMORED";
    //         SpawnSingleEnemy(enemyToSpawn, enemyType);
    //     }
    //     else if (random < waveData.scoutPercentage + waveData.armoredPercentage + waveData.rangedPercentage)
    //     {
    //         enemyToSpawn = rangedEnemyPrefab;
    //         enemyType = "RANGED";
    //         SpawnSingleEnemy(enemyToSpawn, enemyType);
    //     }
    //     else
    //     {
    //         // SWARM spawns multiple
    //         SpawnSwarmGroup();
    //     }
    // }

    void SpawnSingleEnemy(GameObject enemyPrefab, string enemyType)
    {
        GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
    
        // wave-based scaling to this enemy
        ApplyWaveScaling(enemy, currentWave);
    
        Debug.Log($"Spawned scaled {enemyType} from {enemyPrefab.name}");
    }

    // Deterministic wave composition with grouped swarms: allocate exact counts by percentages using largest remainder method
    private class SpawnItem
    {
        public GameObject prefab;
        public int count; // 1 for singles; >1 indicates grouped spawns (used for swarm)
        public SpawnItem(GameObject prefab, int count)
        {
            this.prefab = prefab;
            this.count = count;
        }
    }

    List<SpawnItem> BuildWaveComposition(WaveData waveData)
    {
        int n = Mathf.Max(0, waveData.enemyCount);
        var prefabs = new GameObject[] { scoutEnemyPrefab, armoredEnemyPrefab, rangedEnemyPrefab, swarmEnemyPrefab };
        var weights = new float[] { 
            Mathf.Max(0f, waveData.scoutPercentage),
            Mathf.Max(0f, waveData.armoredPercentage),
            Mathf.Max(0f, waveData.rangedPercentage),
            Mathf.Max(0f, waveData.swarmPercentage)
        };

        // Normalize weights if they don't sum close to 1 to preserve ratios
        float weightSum = weights[0] + weights[1] + weights[2] + weights[3];
        if (weightSum > 0f)
        {
            for (int i = 0; i < weights.Length; i++) weights[i] /= weightSum;
        }

        int[] counts = new int[4];
        float[] remainders = new float[4];
        int allocated = 0;
        for (int i = 0; i < 4; i++)
        {
            float exact = weights[i] * n;
            int baseCount = Mathf.FloorToInt(exact);
            counts[i] = baseCount;
            remainders[i] = exact - baseCount;
            allocated += baseCount;
        }
        int remaining = n - allocated;
        // Distribute remaining by largest remainder
        for (int r = 0; r < remaining; r++)
        {
            int bestIdx = 0;
            float bestRem = remainders[0];
            for (int i = 1; i < 4; i++)
            {
                if (remainders[i] > bestRem)
                {
                    bestRem = remainders[i];
                    bestIdx = i;
                }
            }
            counts[bestIdx]++;
            // Reduce remainder to avoid picking same index always when equal
            remainders[bestIdx] = -1f - r; 
        }

        // If all weights were zero, allocate all to first prefab as fallback
        if (weightSum == 0f)
        {
            counts[0] = n;
            counts[1] = counts[2] = counts[3] = 0;
        }

        var items = new List<SpawnItem>(n);

        // Non-swarm singles
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < counts[i]; j++) items.Add(new SpawnItem(prefabs[i], 1));
        }

        // Swarm groups: partition the swarm enemy total into groups of 3-6 (last group may be smaller if needed)
        int swarmTotal = counts[3];
        if (swarmTotal > 0)
        {
            var groupSizes = PartitionSwarm(swarmTotal, 3, 6);
            foreach (var g in groupSizes)
            {
                items.Add(new SpawnItem(swarmEnemyPrefab, g));
            }
        }

        // Fisher–Yates shuffle at the instruction level
        for (int i = items.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var tmp = items[i];
            items[i] = items[j];
            items[j] = tmp;
        }

        Debug.Log($"Wave composition -> Scout:{counts[0]}, Armored:{counts[1]}, Ranged:{counts[2]}, Swarm:{counts[3]} (grouped)");
        return items;
    }

    // Partition a total into groups roughly within [minSize, maxSize].
    // Ensures exact sum; may produce a smaller last group if total < minSize.
    List<int> PartitionSwarm(int total, int minSize, int maxSize)
    {
        var groups = new List<int>();
        if (total <= 0) return groups;
        if (total < minSize)
        {
            groups.Add(total);
            return groups;
        }
        int remaining = total;
        while (remaining > 0)
        {
            if (remaining <= maxSize)
            {
                groups.Add(remaining);
                break;
            }
            int pick = Random.Range(minSize, maxSize + 1);
            groups.Add(pick);
            remaining -= pick;
        }
        return groups;
    }

    void SpawnSwarmGroup()
    {
        // Unused in deterministic composition (grouped swarms). Kept for reference.
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