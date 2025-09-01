using UnityEngine;
using UnityEngine.SceneManagement;

// Minimal ESC pause menu with grey overlay and Resume/Restart/MainMenu
public class PauseMenuController : MonoBehaviour
{
    [Header("Settings")]
    public bool allowPause = true;
    public string mainMenuSceneName = "MainMenu";

    [Header("Overlay")] public float overlayTargetAlpha = 0.55f;
    public float overlayFadeSpeed = 2f; // alpha per second (unscaled)
    public Color overlayColor = new Color(0.2f, 0.2f, 0.2f, 1f);

    private bool isPaused = false;
    private float overlayAlpha = 0f;

    void Update()
    {
        if (!allowPause) return;

        // Do not toggle pause if the game is already over
        var gs = FindObjectOfType<GameStateController>();
        if (gs != null && gs.IsGameOver) return;

        // Block pause while placing towers; ESC should cancel placement instead
        var placement = FindObjectOfType<TowerPlacementManager>();
        if (placement != null && placement.IsInPlacementMode())
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }

        if (isPaused)
        {
            // Fade overlay even while paused
            overlayAlpha = Mathf.MoveTowards(overlayAlpha, overlayTargetAlpha, overlayFadeSpeed * Time.unscaledDeltaTime);

            // Hotkeys while paused
            if (Input.GetKeyDown(KeyCode.R)) RestartScene();
            if (Input.GetKeyDown(KeyCode.M)) GoToMainMenu();
        }
    }

    void TogglePause()
    {
        isPaused = !isPaused;
        if (isPaused)
        {
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = 1f;
            overlayAlpha = 0f; // reset fade when resuming
        }
    }

    void RestartScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void GoToMainMenu()
    {
        Time.timeScale = 1f;
        if (IsSceneInBuild(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Debug.LogWarning($"Main Menu scene '{mainMenuSceneName}' is not in Build Settings.");
        }
    }

    void OnGUI()
    {
        if (!isPaused) return;

        float w = Screen.width;
        float h = Screen.height;

        // grey overlay
        Color prev = GUI.color;
        GUI.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, overlayAlpha);
        GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
        GUI.color = prev;

        var title = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 40, fontStyle = FontStyle.Bold };
        GUI.Label(new Rect(0, h * 0.4f - 40, w, 60), "Paused", title);

        var hint = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 18, normal = { textColor = Color.white } };
        GUI.Label(new Rect(0, h * 0.4f + 10, w, 30), "ESC: Resume  |  R: Restart  |  M: Main Menu", hint);

        // Simple buttons (optional)
        var btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 18 };
        float bx = w * 0.5f - 150f;
        float by = h * 0.4f + 60f;
        if (GUI.Button(new Rect(bx, by, 300f, 40f), "Resume", btnStyle)) TogglePause();
        if (GUI.Button(new Rect(bx, by + 50f, 300f, 40f), "Restart", btnStyle)) RestartScene();
        if (GUI.Button(new Rect(bx, by + 100f, 300f, 40f), "Main Menu", btnStyle)) GoToMainMenu();
    }

    bool IsSceneInBuild(string sceneName)
    {
        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (string.Equals(name, sceneName, System.StringComparison.Ordinal))
                return true;
        }
        return false;
    }
}

