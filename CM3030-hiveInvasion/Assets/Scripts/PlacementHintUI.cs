using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Persistent, simple bottom-of-screen hint label.
// Creates itself on first call and persists across scenes.
public class PlacementHintUI : MonoBehaviour
{
    private static PlacementHintUI _instance;

    private Canvas _canvas;
    private TextMeshProUGUI _text;

    public static void ShowBottomHint(string message)
    {
        Ensure();
        _instance.SetLabelText(message);
        _instance.Show();
    }

    public static void Hide()
    {
        if (_instance == null) return;
        _instance.gameObject.SetActive(false);
    }

    public static void UpdateHint(string message)
    {
        if (_instance == null) return;
        _instance.SetLabelText(message);
    }

    private static void Ensure()
    {
        if (_instance != null) return;
        var go = new GameObject("PlacementHintUI");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<PlacementHintUI>();
        _instance.BuildUI();
    }

    private void BuildUI()
    {
        // Reuse an existing Canvas if available; else make a screen overlay
        _canvas = FindObjectOfType<Canvas>();
        if (_canvas == null)
        {
            var cgo = new GameObject("UIRootCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _canvas = cgo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            DontDestroyOnLoad(cgo);
        }

        // Container panel
        var panel = new GameObject("PlacementHintPanel", typeof(RectTransform));
        panel.transform.SetParent(_canvas.transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(800f, 60f);
        rect.anchoredPosition = new Vector2(0f, 24f); // 24px from bottom

        // Text
        var tgo = new GameObject("PlacementHintText", typeof(RectTransform), typeof(TextMeshProUGUI));
        tgo.transform.SetParent(panel.transform, false);
        var tr = tgo.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0, 0);
        tr.anchorMax = new Vector2(1, 1);
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;

        _text = tgo.GetComponent<TextMeshProUGUI>();
        _text.alignment = TextAlignmentOptions.Center;
        _text.fontSize = 28;
        _text.richText = true;
        _text.text = "Press <b>T</b> to place a tower";
        _text.color = new Color(1f, 1f, 1f, 0.9f);

        gameObject.SetActive(true);
    }

    private void SetLabelText(string message)
    {
        if (_text == null) BuildUI();
        _text.text = message ?? string.Empty;
    }

    private void Show()
    {
        if (_text == null) BuildUI();
        gameObject.SetActive(true);
    }
}

