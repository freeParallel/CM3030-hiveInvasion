using System;
using UnityEngine;
using UnityEngine.Events;

public class BaseHealth : MonoBehaviour
{
    [Header("Base Health Settings")]
    public int maxHealth = 1000;
    public int currentHealth;
    
    // events for UI and game management
    public static UnityEvent<int, int> OnHealthChanged = new UnityEvent<int, int>(); // current & max
    public static UnityEvent OnBaseDestroyed = new UnityEvent();

    void Start()
    {
        // initialize hp
        currentHealth = maxHealth;
        
        // initialize healthBar
        HealthBar healthBar = GetComponent<HealthBar>();
        if (healthBar != null)
        {
            healthBar.UpdateHealthBar(currentHealth, maxHealth);
        }
        
        // notify UI 
        OnHealthChanged.Invoke(currentHealth, maxHealth);
        Debug.Log($"Base initialized with {currentHealth}/{maxHealth} HP");
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth); // prevent negative HP values
        
        Debug.Log($"Base took {damage} damage. Health: {currentHealth}/{maxHealth}");
        
        // update health bar
        HealthBar healthBar = GetComponent<HealthBar>();
        if (healthBar != null)
        {
            healthBar.UpdateHealthBar(currentHealth, maxHealth);
        }
        
        // notify systems of HP status
        OnHealthChanged.Invoke(currentHealth, maxHealth);
        
        // check for base HP. eventual destruction
        if (currentHealth <= 0)
        {
            BaseDestroyed();
        }
    }

    private void BaseDestroyed()
    {
        Debug.Log("Base Destroyed. YOU DIED.");
        
        // notify game management system
        OnBaseDestroyed.Invoke();
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public int GetMaxHealth()
    {
        return maxHealth;
    }
}
