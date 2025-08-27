using UnityEngine;

public class EnemyHP : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;
    public int pointReward = 10;

    [Header("Audio")]
    public AudioClip deathClip;
    [Range(0f,1f)] public float deathVolume = 1f;

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
    }
    
    void Start()
    {
        currentHealth = maxHealth;    
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        //Debug.Log($"Enemy took {damage}, current health is {currentHealth}/{maxHealth}");

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
        
        //Debug.Log("Enemy obliterated.");
        Destroy(gameObject);
    }
}
