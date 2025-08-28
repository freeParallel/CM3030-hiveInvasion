using UnityEngine;
using System.Collections;

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
    private AudioSource sfx2DSource;
    private Coroutine fadeCoroutine;

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

        // Separate 2D SFX source so music fades don't affect one-shots
        sfx2DSource = gameObject.AddComponent<AudioSource>();
        sfx2DSource.playOnAwake = false;
        sfx2DSource.loop = false;
        sfx2DSource.volume = 1f;
        sfx2DSource.spatialBlend = 0f;

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

    // Play a 2D one-shot SFX without interrupting the current music
    public void PlayOneShot2D(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        if (sfx2DSource != null) sfx2DSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    // Fade music volume to target over duration (unscaled time)
    public void FadeTo(float targetVolume, float duration, bool unscaled = true)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeRoutine(Mathf.Clamp01(targetVolume), Mathf.Max(0.001f, duration), unscaled));
    }

    public void FadeOut(float duration)
    {
        FadeTo(0f, duration, true);
    }

    public void ResetVolumeToDefault()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        source.volume = Mathf.Clamp01(musicVolume);
    }

    private IEnumerator FadeRoutine(float target, float duration, bool unscaled)
    {
        float start = source.volume;
        float t = 0f;
        while (t < duration)
        {
            t += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            source.volume = Mathf.Lerp(start, target, k);
            yield return null;
        }
        source.volume = target;
        fadeCoroutine = null;
    }
}

