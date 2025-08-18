using UnityEngine;

public class TowerData : MonoBehaviour
{
    [Header("Tower Information")]
    [SerializeField] private int towerID;
    [SerializeField] private int level = 1;
    [SerializeField] private float damageMultiplier = 1;
    [SerializeField] private float rangeMultiplier = 1;

    // set tower ID
    public void SetTowerID(int id)
    {
        towerID = id;
    }
    
    // get tower ID
    public int GetTowerID()
    {
        return towerID;
    }
    
    // get tower level
    public int GetLevel()
    {
        return level;
    }
    
    // upgrade methods 
    public void UpgradeDamage()
    {
        damageMultiplier += 0.25f; // +25% damage per upgrade
        level++;
        Debug.Log($"Tower {towerID} damage increased by: {damageMultiplier}");
    }
    
    public void UpgradeRange()
    {
        rangeMultiplier += 0.3f; // +30% range per upgrade
        level++;
        Debug.Log($"Tower {towerID} damage increased by: {rangeMultiplier}");
    }
    
    // get current multipliers
    public float GetDamageMultiplier()
    {
        return damageMultiplier;
    }
    
    public float GetRangeMultiplier()
    {
        return rangeMultiplier;
    }
}
    
    
    
