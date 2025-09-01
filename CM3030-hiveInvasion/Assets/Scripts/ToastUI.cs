using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Lightweight global toast for brief on-screen messages.
// Creates itself on first use and persists between scenes.
public class ToastUI : MonoBehaviour
{
    private static ToastUI _instance;

    private Canvas _canvas;
    private CanvasGroup _group;
    private TextMeshProUGUI _text;
    private Coroutine _routine;

    [Header("Defaults")] public Vector2 anchor = new Vector2(0.5f, 0.15f); // lower-middle
    public Vector2 size = new Vector2(600, 60);
    public int fontSize = 24;

    public static void Show(string message, Color color, float duration = 1.5f)
    {
        Ensure();
        _instance.InternalShow(message, color, duration);
    }

    private static void Ensure()
    {
        if (_instance != null) return;
        var go = new GameObject("ToastUI");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<ToastUI>();
        _instance.BuildUI();
    }

    private void BuildUI()
    {
        // Always create our own topmost Screen Space - Overlay canvas to avoid scale/order issues
        var cgo = new GameObject("ToastCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _canvas = cgo.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 5000; // above most UI
        _canvas.overrideSorting = true;
        var scaler = cgo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        DontDestroyOnLoad(cgo);

        // Panel with CanvasGroup
        var panel = new GameObject("ToastPanel", typeof(RectTransform), typeof(CanvasGroup));
        panel.transform.SetParent(_canvas.transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;
        _group = panel.GetComponent<CanvasGroup>();
        _group.alpha = 0f;
        _group.blocksRaycasts = false;
        _group.interactable = false;

        // Text
        var tgo = new GameObject("ToastText", typeof(RectTransform), typeof(TextMeshProUGUI));
        tgo.transform.SetParent(panel.transform, false);
        var tr = tgo.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0, 0);
        tr.anchorMax = new Vector2(1, 1);
        tr.offsetMin = new Vector2(0, 0);
        tr.offsetMax = new Vector2(0, 0);

        _text = tgo.GetComponent<TextMeshProUGUI>();
        _text.alignment = TextAlignmentOptions.Center;
        _text.fontSize = fontSize;
        _text.text = "";
    }

    private void InternalShow(string message, Color color, float duration)
    {
        if (_group == null || _text == null) BuildUI();
        _text.text = message ?? string.Empty;
        _text.color = color;

        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(ShowRoutine(Mathf.Max(0.1f, duration)));
    }

    private IEnumerator ShowRoutine(float duration)
    {
        // Fade in
        yield return Fade(0f, 1f, 0.15f);
        yield return new WaitForSeconds(duration);
        // Fade out
        yield return Fade(1f, 0f, 0.25f);
        _routine = null;
    }

    private IEnumerator Fade(float from, float to, float time)
    {
        float t = 0f;
        _group.alpha = from;
        while (t < time)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / time);
            _group.alpha = Mathf.Lerp(from, to, u);
            yield return null;
        }
        _group.alpha = to;
    }
}

