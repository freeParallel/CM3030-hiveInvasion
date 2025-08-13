using Unity.VisualScripting;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    [Header("Resource Settings")]
    public int startingPoints = 250;
    public int towerCost = 100;

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
        return towerCost;
    }
}
