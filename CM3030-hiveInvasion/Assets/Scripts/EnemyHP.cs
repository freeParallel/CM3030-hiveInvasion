using UnityEngine;

public class EnemyHP : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;
    
    void Start()
    {
        currentHealth = maxHealth;    
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log($"Enemy took {damage}, current health is {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Enemy obliterated.");
        Destroy(gameObject);
    }
}
