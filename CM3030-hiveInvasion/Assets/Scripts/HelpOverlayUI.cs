using UnityEngine;

// Simple top-right help button that pauses the game and shows a quick how-to overlay.
// Uses IMGUI so it requires no scene setup.
public class HelpOverlayUI : MonoBehaviour
{
    private static HelpOverlayUI _instance;

    [Header("Button")]
    public Vector2 margin = new Vector2(16f, 16f);
    public float size = 40f;
    public string buttonText = "?";

    [Header("Overlay")]
    public Color overlayColor = new Color(0f, 0f, 0f, 1f);
    public float overlayTargetAlpha = 0.8f;
    public float overlayFadeSpeed = 3f; // alpha per second (unscaled)
    public int titleFontSize = 28;
    public int bulletFontSize = 18;

    private bool showing = false;
    private float prevTimeScale = 1f;
    private float overlayAlpha = 0f;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindObjectOfType<HelpOverlayUI>() == null)
        {
            var go = new GameObject("HelpOverlayUI");
            go.AddComponent<HelpOverlayUI>();
        }
    }

    void Update()
    {
        if (showing && Input.GetKeyDown(KeyCode.Escape))
        {
            Hide();
        }
        // Optional keyboard toggle for visibility
        if (Input.GetKeyDown(KeyCode.F1))
        {
            if (!showing) Show(); else Hide();
        }

        // Fade overlay alpha using unscaled time so it fades while paused
        float target = showing ? Mathf.Clamp01(overlayTargetAlpha) : 0f;
        overlayAlpha = Mathf.MoveTowards(overlayAlpha, target, overlayFadeSpeed * Time.unscaledDeltaTime);
    }

    void OnGUI()
    {
        // Ensure our IMGUI draws on top of other IMGUI (like MainMenuController)
        GUI.depth = -10000;

        // Draw help button top-right with margins
        float x = Screen.width - margin.x - size;
        float y = margin.y;
        var btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
        if (GUI.Button(new Rect(x, y, size, size), buttonText, btnStyle))
        {
            if (!showing) Show(); else Hide();
        }

        // Draw overlay even during fade
        if (overlayAlpha > 0f)
        {
            Color prev = GUI.color;
            var dim = new Color(overlayColor.r, overlayColor.g, overlayColor.b, overlayAlpha);
            GUI.color = dim;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = prev;
        }

        if (!showing) return;

        // Centered help box with an opaque backdrop to prevent text collisions
        float w = Mathf.Min(Screen.width * 0.6f, 800f);
        float h = Mathf.Min(Screen.height * 0.6f, 540f);
        float cx = (Screen.width - w) * 0.5f;
        float cy = (Screen.height - h) * 0.5f;
        // Opaque dark rectangle
        Color prevBox = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.95f);
        GUI.DrawTexture(new Rect(cx, cy, w, h), Texture2D.whiteTexture);
        GUI.color = prevBox;
        GUI.Box(new Rect(cx, cy, w, h), "");

        var title = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, fontSize = titleFontSize, fontStyle = FontStyle.Bold };
        GUI.Label(new Rect(cx, cy + 16, w, 40), "How to Play", title);

        var bullet = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft, fontSize = bulletFontSize, wordWrap = true };
        float listX = cx + 24f;
        float listY = cy + 64f;
        float line = 26f;
        DrawBullet(listX, ref listY, line, bullet, "WASD to move camera; Q/E rotate; Mouse wheel zoom; edges scroll");
        DrawBullet(listX, ref listY, line, bullet, "1 = AOE blast; 2 = Ranged lock+fire");
        DrawBullet(listX, ref listY, line, bullet, "Press T to place a tower; left-click to place within hero radius; ESC to cancel");
        DrawBullet(listX, ref listY, line, bullet, "Select a tower to upgrade; spend points shown in HUD");
        DrawBullet(listX, ref listY, line, bullet, "Defend the gate, then the base when the gate falls");

        var closeStyle = new GUIStyle(GUI.skin.button) { fontSize = 18 };
        if (GUI.Button(new Rect(cx + w * 0.5f - 60f, cy + h - 56f, 120f, 36f), "Resume", closeStyle))
        {
            Hide();
        }
    }

    void DrawBullet(float x, ref float y, float step, GUIStyle style, string text)
    {
        GUI.Label(new Rect(x, y, 24f, step), "•", style);
        GUI.Label(new Rect(x + 20f, y, Screen.width, step), text, style);
        y += step + 2f;
    }

    void Show()
    {
        prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        showing = true;
    }

    void Hide()
    {
        showing = false;
        Time.timeScale = prevTimeScale;
    }
}

