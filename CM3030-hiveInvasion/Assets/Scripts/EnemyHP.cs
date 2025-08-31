using UnityEngine;

public class EnemyHP : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;
    public int pointReward = 10;

    [Header("Audio")]
    public AudioClip deathClip;
    [Range(0f,1f)] public float deathVolume = 1f;

    // Optional UI health bar (auto-attached if missing)
    private HealthBar healthBar;

    void Awake()
    {
        // Autoload death clips based on enemy name if not assigned
        if (deathClip == null)
        {
            string n = gameObject.name;
            if (n.EndsWith("(Clone)")) n = n.Substring(0, n.Length - 7);
            string lower = n.ToLower();
            string key = "enemy_death";
            if (lower.Contains("armored")) key = "armored_death";
            else if (lower.Contains("ranged")) key = "ranged_death";
            else if (lower.Contains("swarm")) key = "swarm_death";
            var clip = Resources.Load<AudioClip>("Audio/" + key);
            if (clip != null) deathClip = clip;
        }

        // Ensure health bar exists so all enemies show HP by default
        healthBar = GetComponent<HealthBar>();
        if (healthBar == null)
        {
            healthBar = gameObject.AddComponent<HealthBar>();
        }
    }
    
    void Start()
    {
        currentHealth = maxHealth;
        if (healthBar != null)
        {
            healthBar.UpdateHealthBar(currentHealth, maxHealth);
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (healthBar != null)
        {
            healthBar.UpdateHealthBar(currentHealth, maxHealth);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.AddPoints(pointReward);
        }

        if (deathClip != null)
        {
            AudioSource.PlayClipAtPoint(deathClip, transform.position, Mathf.Clamp01(deathVolume));
        }

        // Try to play death animation and delay destruction if available
        var anim = GetComponentInChildren<EnemyAnimationController>();
        if (anim != null)
        {
            anim.PlayDie();
            // disable movement to avoid sliding
            var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) agent.enabled = false;
            // disable colliders to prevent further hits
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            // Destroy after a short delay to allow the animation to be seen
            Destroy(gameObject, 1.0f);
            return;
        }
        
        Destroy(gameObject);
    }
}
