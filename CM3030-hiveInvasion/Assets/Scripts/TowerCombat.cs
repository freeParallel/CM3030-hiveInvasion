using UnityEngine;

public class TowerCombat : MonoBehaviour
{
    public float range = 20f;
    public int damage = 25;
    public float fireRate = 2f;

    private float lastShotTime = 0f;
    
    void Update()
    {
        // DEBUG: Show current effective range
        TowerData towerData = GetComponent<TowerData>();
        if (towerData != null && Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log($"Tower range: {range * towerData.GetRangeMultiplier()} (base: {range}, multiplier: {towerData.GetRangeMultiplier()})");
        }
        
        GameObject enemy = FindEnemyInRange();
        if (enemy != null)
        {
            ShootAtEnemy(enemy);
        }
    }

    GameObject FindEnemyInRange()
    {
        // get the tower data for upgrade multipliers
        TowerData towerData = GetComponent<TowerData>();
        float finalRange = range;

        if (towerData != null)
        {
            finalRange = range * towerData.GetRangeMultiplier();
        }
        
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance <= range)
            {
                return enemy;
            }
        }
        return null;
    }

    void ShootAtEnemy(GameObject enemy)
    {
        if (Time.time - lastShotTime > 1f / fireRate)
        {
            // get tower data for update multipliers
            TowerData towerData = GetComponent<TowerData>();
            float finalDamage = damage;

            if (towerData != null)
            {
                finalDamage = damage * towerData.GetDamageMultiplier();
            }
            
            EnemyHP enemyHP = enemy.GetComponent<EnemyHP>();
            if (enemyHP != null)
            {
                enemyHP.TakeDamage(damage);
            
                // VISUAL FEEDBACK
                ShowMuzzleFlash();
            }
        
            lastShotTime = Time.time;
        }
    }

    void ShowMuzzleFlash()
    {
        Renderer towerRenderer = GetComponent<Renderer>();
        if (towerRenderer != null)
        {
            StartCoroutine(FlashColor(Color.yellow, 0.1f));
        }

    }

    System.Collections.IEnumerator FlashColor(Color flashColor, float duration)
    {
        Renderer towerRenderer = GetComponent<Renderer>();
        Color originalColor = towerRenderer.material.color;
    
        towerRenderer.material.color = flashColor;
        yield return new WaitForSeconds(duration);
        towerRenderer.material.color = originalColor;
    }
}
