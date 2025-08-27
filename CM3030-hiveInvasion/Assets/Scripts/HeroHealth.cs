using UnityEngine;
using UnityEngine.Events;

// Hero health + damage SFX wiring (minimal). This does not change gameplay targeting.
// Attach to the Hero GameObject. Call TakeDamage(dmg) from any attacker to play a random hurt sound.
public class HeroHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;

    public static UnityEvent<int, int> OnHealthChanged = new UnityEvent<int, int>(); // current, max
    public static UnityEvent OnHeroDied = new UnityEvent();

    [Header("Audio")]
    public AudioSource sfxSource;
    [Range(0f,1f)] public float sfxVolume = 1f;
    [Tooltip("Hurt SFX variants; if empty, will autoload from Resources/Audio/hero_damage_1..3 if present")] 
    public AudioClip[] damageClips;

    private int _lastClipIndex = -1;

    void Awake()
    {
        // Ensure audio source
        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f; // 2D SFX for player hurt
        }
        sfxSource.volume = Mathf.Clamp01(sfxVolume);

        // Autoload default damage clips if none assigned
        if (damageClips == null || damageClips.Length == 0)
        {
            var list = new System.Collections.Generic.List<AudioClip>();
            for (int i = 1; i <= 3; i++)
            {
                var clip = Resources.Load<AudioClip>("Audio/hero_damage_" + i);
                if (clip != null) list.Add(clip);
            }
            damageClips = list.ToArray();
        }
    }

    void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0) return;
        currentHealth = Mathf.Max(0, currentHealth - damage);
        OnHealthChanged.Invoke(currentHealth, maxHealth);
        PlayRandomHurt();

        if (currentHealth <= 0)
        {
            OnHeroDied.Invoke();
        }
    }

    private void PlayRandomHurt()
    {
        if (sfxSource == null || damageClips == null || damageClips.Length == 0) return;
        int idx;
        if (damageClips.Length == 1)
        {
            idx = 0;
        }
        else
        {
            // Pick a different index than last time if possible
            do { idx = Random.Range(0, damageClips.Length); } while (idx == _lastClipIndex);
        }
        _lastClipIndex = idx;
        var clip = damageClips[idx];
        if (clip != null) sfxSource.PlayOneShot(clip, Mathf.Clamp01(sfxVolume));
    }
}

