using UnityEngine;
using UnityEngine.Events;

public class GateHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;

    private HealthBar healthBar;

    // static events for enemies to listen to
    public static UnityEvent OnGateDestroyed = new UnityEvent();
    public static UnityEvent OnGateOpened = new UnityEvent();
    public static UnityEvent OnGateClosed = new UnityEvent();

    // global open flag so AI can ignore gate without destroying it
    public static bool IsGateOpen = false;
    
    void Start()
    {
        currentHealth = maxHealth;
        // Ensure a world-space health bar is present above the gate
        healthBar = GetComponent<HealthBar>();
        if (healthBar == null) healthBar = gameObject.AddComponent<HealthBar>();
        if (healthBar != null)
        {
            // Slightly taller offset for large gates
            healthBar.offset = new Vector3(0, 2.5f, 0);
            healthBar.healthBarSize = new Vector2(120f, 10f);
            healthBar.UpdateHealthBar(currentHealth, maxHealth);
        }
    }
    
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        Debug.Log($"Gate HP: {currentHealth}/{maxHealth}");
        if (healthBar != null) healthBar.UpdateHealthBar(currentHealth, maxHealth);

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
