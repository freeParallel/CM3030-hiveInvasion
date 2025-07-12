using UnityEngine;

public class GateHealth : MonoBehaviour
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
        Debug.Log($"Gate HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            DestroyGate();
        }
    }

    void DestroyGate()
    {
        Debug.Log("Gate destroyed");
        Destroy(gameObject);
    }
}
