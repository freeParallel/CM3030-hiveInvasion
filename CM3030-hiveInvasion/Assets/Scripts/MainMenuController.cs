using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Minimal main menu overlay for a dedicated MainMenu scene.
// Add this to an empty GameObject in Assets/Scenes/MainMenu.unity.
public class MainMenuController : MonoBehaviour
{
    [Header("Navigation")]
    public string gameplaySceneName = "SampleScene";

    [Header("Overlay")] public float overlayAlpha = 1f;
    public Color overlayColor = new Color(0f, 0f, 0f, 1f);

    [Header("Music")]
    [Tooltip("If assigned, this clip will be played as the main menu music")] public AudioClip menuMusic;
    [Tooltip("Play menu music when the scene starts")] public bool playMenuMusic = true;
    [Tooltip("Loop the menu music")] public bool loopMusic = true;
    [Range(0f,1f)] public float musicVolume = 0.5f;
    [Tooltip("Editor-only path fallback (GUIDed asset) for loading if menuMusic not assigned")]
    public string menuMusicAssetPath = "Assets/MaxStack/Wirescapes/Audio/Music/WS Urban Decay.wav";

    void Start()
    {
        // Ensure no gameplay BGM in the menu
        if (MusicManager.Instance != null)
        {
            Destroy(MusicManager.Instance.gameObject);
        }

        if (playMenuMusic)
        {
            var src = GetComponent<AudioSource>();
            if (src == null) src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = loopMusic;
            src.volume = Mathf.Clamp01(musicVolume);
            src.spatialBlend = 0f;

            AudioClip clipToPlay = menuMusic;
#if UNITY_EDITOR
            if (clipToPlay == null && !string.IsNullOrEmpty(menuMusicAssetPath))
            {
                var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(menuMusicAssetPath);
                if (clip != null) clipToPlay = clip;
            }
#endif
            if (clipToPlay != null)
            {
                src.clip = clipToPlay;
                src.Play();
            }
        }
    }

    void OnGUI()
    {
        float w = Screen.width;
        float h = Screen.height;

        // dark overlay
        Color prev = GUI.color;
        GUI.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, overlayAlpha);
        GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
        GUI.color = prev;

        var title = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 48, fontStyle = FontStyle.Bold };
        GUI.Label(new Rect(0, h * 0.35f - 40, w, 80), "Hive Invasion", title);

        var hint = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 20, normal = { textColor = Color.white } };
        GUI.Label(new Rect(0, h * 0.375f + 10, w, 30), "Press Enter to Play", hint);

        var btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 20 };
        float bx = w * 0.5f - 150f;
        float by = h * 0.35f + 60f;
        if (GUI.Button(new Rect(bx, by, 300f, 45f), "Play", btnStyle)) StartGame();
        if (GUI.Button(new Rect(bx, by + 55f, 300f, 45f), "Quit", btnStyle)) Application.Quit();

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
        {
            StartGame();
        }
    }

    void StartGame()
    {
        SceneManager.LoadScene(gameplaySceneName);
    }
}

