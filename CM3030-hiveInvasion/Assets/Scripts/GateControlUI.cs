using UnityEngine;

// Minimal IMGUI toggle to open/close the gate from the top-right UI, next to the help button.
// Draws only if a GateController is present in the scene.
public class GateControlUI : MonoBehaviour
{
    public Vector2 margin = new Vector2(16f, 16f);
    public float width = 120f;
    public float height = 32f;
    public float offsetFromHelp = 8f; // gap below the "?" button

    private GateController controller;
    private float lastFindTime;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        // Occasionally re-resolve controller (scene changes)
        if (controller == null && Time.unscaledTime - lastFindTime > 1f)
        {
            controller = FindObjectOfType<GateController>();
            lastFindTime = Time.unscaledTime;
        }
    }

    void OnGUI()
    {
        if (controller == null) return;
        GUI.depth = -9999; // draw above most UI but below help overlay depth

        float x = Screen.width - margin.x - width;
        // assume help button is 40px high; place below it
        float y = margin.y + 40f + offsetFromHelp;

        var style = new GUIStyle(GUI.skin.button) { fontSize = 16 };
        if (GUI.Button(new Rect(x, y, width, height), "Open Gate", style))
        {
            controller.OpenGate();
        }
        if (GUI.Button(new Rect(x, y + height + 6f, width, height), "Close Gate", style))
        {
            controller.CloseGate();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindObjectOfType<GateControlUI>() == null)
        {
            var go = new GameObject("GateControlUI");
            go.AddComponent<GateControlUI>();
        }
    }
}

