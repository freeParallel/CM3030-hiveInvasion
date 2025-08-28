using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

// HI-019: Win/Lose conditions + simple on-screen text
// - Win: All waves completed AND no enemies remain AND base health > 0
// - Lose: Base health reaches 0
public class GameStateController : MonoBehaviour
{
    [Header("Display")]
    [Tooltip("Y offset from screen center for the message")] public float messageYOffset = -40f;
    [Tooltip("Victory color")] public Color winColor = new Color(0.1f, 0.9f, 0.2f);
    [Tooltip("Defeat color")] public Color loseColor = new Color(0.95f, 0.2f, 0.2f);

    [Header("Overlay")]
    [Tooltip("Target grey overlay alpha (0-1) shown behind the result text")] public float overlayTargetAlpha = 0.6f;
    [Tooltip("Fade speed (alpha per second, unscaled time)")] public float overlayFadeSpeed = 1.5f;
    private float overlayAlpha = 0f;

    [Header("Navigation")]
    [Tooltip("Scene name for the main menu (must be added to Build Settings later)")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Game Over Delays")]
    [Tooltip("Delay (seconds, unscaled) before showing the defeat overlay after the base is destroyed")]
    public float defeatOverlayDelay = 2f;
    [Tooltip("Delay (seconds, unscaled) before showing the victory overlay after last wave cleared (0 = immediate)")]
    public float victoryOverlayDelay = 0f;

    private bool isGameOver = false;
    private bool isWin = false;
    private string message = string.Empty;
    private bool defeatPending = false;

    private WaveManager waveManager;
    private BaseHealth baseHealth;
    private int totalWaves = 0;
    private bool lastWaveCompleted = false;

    void Awake()
    {
        // Ensure normal time scale at scene start
        Time.timeScale = 1f;

        waveManager = FindObjectOfType<WaveManager>();
        baseHealth = FindObjectOfType<BaseHealth>();

        if (waveManager != null && waveManager.waveProgression != null)
        {
            totalWaves = Mathf.Max(0, waveManager.waveProgression.Length);
        }
    }

    void OnEnable()
    {
        BaseHealth.OnBaseDestroyed.AddListener(OnBaseDestroyed);
        WaveManager.OnWaveCompleted.AddListener(OnWaveCompleted);
    }

    void OnDisable()
    {
        BaseHealth.OnBaseDestroyed.RemoveListener(OnBaseDestroyed);
        WaveManager.OnWaveCompleted.RemoveListener(OnWaveCompleted);
    }

    void OnBaseDestroyed()
    {
        if (isGameOver || defeatPending) return;
        defeatPending = true;

        // Halt the game immediately so gameplay stops, but delay overlay using unscaled time
        Time.timeScale = 0f;
        overlayAlpha = 0f; // reset fade
        Debug.Log("Base destroyed. Scheduling defeat overlay...");
        StartCoroutine(ShowDefeatAfterDelay());
    }

    void OnWaveCompleted(int waveNumber)
    {
        if (isGameOver) return;
        if (waveNumber >= totalWaves)
        {
            lastWaveCompleted = true;
            // Begin checking for remaining enemies to declare victory
            StartCoroutine(WaitForNoEnemiesThenWin());
        }
    }

    System.Collections.IEnumerator WaitForNoEnemiesThenWin()
    {
        // Wait until there are no enemies in the scene
        while (!isGameOver)
        {
            // If base died during wait, lose condition will trigger via event
            if (baseHealth != null && baseHealth.GetCurrentHealth() <= 0)
            {
                yield break;
            }

            var enemies = GameObject.FindGameObjectsWithTag("Enemy");
            if (enemies == null || enemies.Length == 0)
            {
                // Victory if we have finished all waves and still alive
                if (lastWaveCompleted && baseHealth != null && baseHealth.GetCurrentHealth() > 0)
                {
                    // Optional delay for victory overlay
                    float delay = Mathf.Max(0f, victoryOverlayDelay);
                    if (delay > 0f)
                    {
                        yield return new WaitForSecondsRealtime(delay);
                    }
                    isGameOver = true;
                    isWin = true;
                    message = "Victory";

                    // Halt the game on victory as well
                    Time.timeScale = 0f;

                    Debug.Log("Game Won: All waves cleared and base still standing.");
                }
                yield break;
            }
            yield return null; // check next frame
        }
    }

    void Update()
    {
        if (!isGameOver) return;

        // Continue fading the overlay even while paused (uses unscaled time)
        overlayAlpha = Mathf.MoveTowards(overlayAlpha, overlayTargetAlpha, overlayFadeSpeed * Time.unscaledDeltaTime);

        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartScene();
        }
        if (Input.GetKeyDown(KeyCode.M))
        {
            ReturnToMainMenu();
        }
    }

    void OnGUI()
    {
        if (!isGameOver) return;

        var style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 48;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = isWin ? winColor : loseColor;

        float w = Screen.width;
        float h = Screen.height;

        // Draw grey overlay behind text
        Color prev = GUI.color;
        GUI.color = new Color(0.2f, 0.2f, 0.2f, overlayAlpha);
        GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
        GUI.color = prev;

        Rect r = new Rect(0, h * 0.5f + messageYOffset, w, 60);
        GUI.Label(r, message, style);

        var hint = new GUIStyle(GUI.skin.label);
        hint.alignment = TextAnchor.MiddleCenter;
        hint.fontSize = 20;
        hint.normal.textColor = Color.white;
        GUI.Label(new Rect(0, r.y + 50, w, 30), "Press R to Restart", hint);
        GUI.Label(new Rect(0, r.y + 75, w, 30), "Press M for Main Menu", hint);

        // Clickable buttons
        var btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 18 };
        float bx = w * 0.5f - 150f;
        float by = r.y + 120f;
        if (GUI.Button(new Rect(bx, by, 300f, 40f), "Restart", btnStyle)) RestartScene();
        if (GUI.Button(new Rect(bx, by + 50f, 300f, 40f), "Main Menu", btnStyle)) ReturnToMainMenu();
    }

    public bool IsGameOver => isGameOver;

    System.Collections.IEnumerator ShowDefeatAfterDelay()
    {
        float delay = Mathf.Max(0f, defeatOverlayDelay);
        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }
        isGameOver = true;
        isWin = false;
        message = "Defeat";
        defeatPending = false;
        Debug.Log("Game Over: Base destroyed (overlay shown).");
    }

    void RestartScene()
    {
        Time.timeScale = 1f;
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }

    void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(mainMenuSceneName) && IsSceneInBuild(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Debug.LogWarning($"Main Menu scene '{mainMenuSceneName}' is not in Build Settings yet. Add it later.");
        }
    }

    // Utility: check if a scene name is present in Build Settings
    bool IsSceneInBuild(string sceneName)
    {
        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (string.Equals(name, sceneName, System.StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }
}

