using UnityEngine;

public class TowerCombat : MonoBehaviour
{
    public float range = 20f;
    public int damage = 25;
    public float fireRate = 2f;

    private float lastShotTime = 0f;
    
    void Update()
    {
        GameObject enemy = FindEnemyInRange();
        if (enemy != null)
        {
            ShootAtEnemy(enemy);
        }
    }

    GameObject FindEnemyInRange()
    {
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
            EnemyHP enemyHP = enemy.GetComponent<EnemyHP>();
            if (enemyHP != null)
            {
                enemyHP.TakeDamage(damage);   
            }
            lastShotTime = Time.time;
        }
    }
}
