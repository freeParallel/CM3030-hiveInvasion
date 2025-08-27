using UnityEngine;

// HI-020: Simple background music manager
// - Drop this on a GameObject in your first scene (e.g., GameSystems)
// - Assign a backgroundMusic clip in the Inspector
// - It persists across scenes and avoids duplicates
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Music Settings")]
    public AudioClip backgroundMusic;
    [Range(0f,1f)] public float musicVolume = 0.35f;
    public bool loop = true;

    private AudioSource source;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        source = gameObject.GetComponent<AudioSource>();
        if (source == null)
        {
            source = gameObject.AddComponent<AudioSource>();
        }
        source.playOnAwake = false;
        source.loop = loop;
        source.volume = Mathf.Clamp01(musicVolume);
        source.spatialBlend = 0f; // 2D music

        if (backgroundMusic == null)
        {
            // Autoload a default from Resources if assigned clip is missing
            backgroundMusic = Resources.Load<AudioClip>("Audio/bgm");
            if (backgroundMusic == null)
            {
                backgroundMusic = Resources.Load<AudioClip>("Audio/background_music");
            }
        }
        if (backgroundMusic != null)
        {
            source.clip = backgroundMusic;
            source.Play();
        }
    }

    public void Play(AudioClip clip, float volume = 1f, bool loopClip = true)
    {
        if (clip == null) return;
        source.clip = clip;
        source.loop = loopClip;
        source.volume = Mathf.Clamp01(volume);
        source.Play();
    }
}

