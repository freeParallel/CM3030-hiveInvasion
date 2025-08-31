using UnityEngine;
using System.Collections.Generic;

public class TowerManager : MonoBehaviour
{
    [Header("Tower Management")]
    public List<GameObject> placedTowers = new List<GameObject>();

    [Header("Limits")] public int maxTowers = 10;
    
    // singleton access
    public static TowerManager Instance;

    private int nextTowerID = 1;

    void Awake()
    {
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
    
    // register new tower in tower system
    public GameObject RegisterTower(GameObject towerPrefab, Vector3 position)
    {
        if (IsAtLimit())
        {
            Debug.Log("TowerManager: Cannot register tower — limit reached.");
            return null;
        }

        // create the tower
        GameObject newTower = Instantiate(towerPrefab, position, Quaternion.identity);
        
        // assign tag
        newTower.tag = "Tower";
        
        // add tower data component for tracking purposes
        TowerData towerData = newTower.AddComponent<TowerData>();
        towerData.SetTowerID(nextTowerID);
        nextTowerID++;
        
        // add to registry
        placedTowers.Add(newTower);
        
        Debug.Log($"Tower registered with ID: {towerData.GetTowerID()} at position: {position}. Total towers: {placedTowers.Count}");

        return newTower;
    }
    
    // remove tower from registry upon destruction
    public void UnregisterTower(GameObject tower)
    {
        if (placedTowers.Contains(tower))
        {
            placedTowers.Remove(tower);
            Debug.Log($"Tower unregistered. Total tower: {placedTowers.Count}");
        }
    }
    
    // get all towers
    public List<GameObject> GetAllTowers()
    {
        return new List<GameObject>(placedTowers);
    }
    
    // get tower count
    public int GetTowerCount()
    {
        return placedTowers.Count;
    }

    // at limit?
    public bool IsAtLimit()
    {
        return GetTowerCount() >= Mathf.Max(0, maxTowers);
    }

    public int GetMaxTowers()
    {
        return Mathf.Max(0, maxTowers);
    }
    
    // get tower by ID for upgrades
    public GameObject GetTowerByID(int towerID)
    {
        foreach (GameObject tower in placedTowers)
        {
            TowerData data = tower.GetComponent<TowerData>();
            if (data != null && data.GetTowerID() == towerID)
            {
                return tower;
            }
        }
        return null;
    }
    
    
}
