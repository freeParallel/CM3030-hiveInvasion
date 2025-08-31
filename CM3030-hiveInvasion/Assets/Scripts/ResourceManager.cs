using Unity.VisualScripting;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    [Header("Resource Settings")]
    public int startingPoints = 250;
    [Tooltip("Base cost for the first tower")] public int towerCost = 100;

    [Header("Cost Scaling")]
    [Tooltip("Use linear cost increase (base + increment * count). If false, use multiplier^")] public bool useLinearScaling = true;
    [Tooltip("Amount added per existing tower when linear scaling is enabled")] public int costIncrement = 25;
    [Tooltip("Multiplier applied per existing tower when linear scaling is disabled")] public float costMultiplier = 1.15f;

    [Header("Current State")] public int currentPoints;
    
    // singleton pattern to simplify access
    public static ResourceManager Instance;

    void Awake()
    {
        // singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Initialize starting points
        currentPoints = startingPoints;
        Debug.Log($"Initial points count {currentPoints} ");
    }
    
    // check if we can afford item
    public bool CanAfford(int cost)
    {
        return currentPoints >= cost;
    }
    
    // spend points (return true if success)
    public bool SpendPoints(int cost)
    {
        if (CanAfford(cost))
        {
            currentPoints -= cost;
            Debug.Log($"Spent {cost} points. Points remaining = {currentPoints} ");
            return true;
        }
        else
        {
            Debug.Log($"Not enough points! Need {cost}, you have {currentPoints}");
            return false;
        }
    }
    
    // add points (implement later as reward for destroying enemies
    public void AddPoints(int amount)
    {
        currentPoints += amount;
        Debug.Log($"Gained {amount} points. Total = {currentPoints} ");
    }
    
    // get current points (UI display)
    public int GetCurrentPoints()
    {
        return currentPoints;
    }
    // get tower cost (UI display)
    public int GetTowerCost()
    {
        int baseCost = Mathf.Max(0, towerCost);
        int count = (TowerManager.Instance != null) ? TowerManager.Instance.GetTowerCount() : 0;
        if (count <= 0) return baseCost;

        if (useLinearScaling)
        {
            long scaled = (long)baseCost + (long)costIncrement * (long)count;
            return (int)Mathf.Clamp(scaled, 0, int.MaxValue);
        }
        else
        {
            double scaled = (double)baseCost * System.Math.Pow(System.Math.Max(0.0001, costMultiplier), count);
            int rounded = Mathf.Max(0, Mathf.RoundToInt((float)scaled));
            return rounded;
        }
    }
}
