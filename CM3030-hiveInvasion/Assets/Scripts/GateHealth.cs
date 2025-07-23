using UnityEngine;
using UnityEngine.Events;

public class GateHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;

    // static even for enemies to listen to
    public static UnityEvent OnGateDestroyed = new UnityEvent();
    
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
        Debug.Log("GATE DESTROYED, Broadcasting to enemies...");

        // Use an event to notify all enemies
        OnGateDestroyed.Invoke();
        
        Destroy(gameObject);
    }

}
