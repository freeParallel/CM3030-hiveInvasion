using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WaveTransitionUI : MonoBehaviour
{
    [Header("UI References (optional)")]
    public Canvas targetCanvas;               // Existing Canvas (e.g., "UI canvas"). If null, script will try to find one.
    public CanvasGroup canvasGroup;           // Fades the whole overlay panel
    public TextMeshProUGUI messageText;       // Displays the message text (TMP)

    [Header("Behavior")]
    public string waveStartFormat = "Wave {0}";
    public float fadeInDuration = 0.25f;
    public float holdDuration = 1.0f;
    public float fadeOutDuration = 0.35f;
    // suppress repeated messages for the same wave within this interval (useful with multiple wavemanagers)
    public float debounceSeconds = 1.0f;

    private Coroutine _displayRoutine;
    private int _lastStartedWave = -1;
    private float _lastStartShownAt = -999f;

    void Awake()
    {
        EnsureRuntimeUI();
        SetVisible(false, immediate: true);
    }

    void OnEnable()
    {
        WaveManager.OnWaveStarted.AddListener(HandleWaveStarted);
    }

    void OnDisable()
    {
        WaveManager.OnWaveStarted.RemoveListener(HandleWaveStarted);
    }

    private void HandleWaveStarted(int waveNumber)
    {
        if (waveNumber == _lastStartedWave && Time.time - _lastStartShownAt < debounceSeconds)
            return;

        _lastStartedWave = waveNumber;
        _lastStartShownAt = Time.time;
        ShowMessage(string.Format(waveStartFormat, waveNumber));
    }


    private void ShowMessage(string msg)
    {
        if (messageText == null || canvasGroup == null)
        {
            EnsureRuntimeUI();
        }

        if (_displayRoutine != null)
        {
            StopCoroutine(_displayRoutine);
        }
        _displayRoutine = StartCoroutine(FadeRoutine(msg));
    }

    private IEnumerator FadeRoutine(string msg)
    {
        messageText.text = msg;

        // Fade in
        yield return FadeCanvas(0f, 1f, fadeInDuration);
        // Hold
        yield return new WaitForSeconds(holdDuration);
        // Fade out
        yield return FadeCanvas(1f, 0f, fadeOutDuration);

        SetVisible(false, immediate: true);
        _displayRoutine = null;
    }

    private IEnumerator FadeCanvas(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            canvasGroup.alpha = to;
            yield break;
        }
        SetVisible(true, immediate: false);
        float t = 0f;
        canvasGroup.alpha = from;
        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);
            canvasGroup.alpha = Mathf.Lerp(from, to, u);
            yield return null;
        }
        canvasGroup.alpha = to;
    }

    private void SetVisible(bool visible, bool immediate)
    {
        if (canvasGroup == null) return;
        canvasGroup.blocksRaycasts = visible;
        canvasGroup.interactable = visible;
        if (immediate)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
        }
    }

    private void EnsureRuntimeUI()
    {
        if (canvasGroup != null && messageText != null) return;

        // Try to find an existing Canvas if not assigned
        if (targetCanvas == null)
        {
            var go = GameObject.Find("UI canvas");
            if (go != null) targetCanvas = go.GetComponent<Canvas>();
            if (targetCanvas == null)
            {
                targetCanvas = FindObjectOfType<Canvas>();
            }
        }

        // Create a dedicated overlay panel under the existing canvas
        Transform parent = targetCanvas != null ? targetCanvas.transform : this.transform;
        GameObject panel = new GameObject("WaveTransitionPanel", typeof(RectTransform), typeof(CanvasGroup));
        panel.transform.SetParent(parent, false);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        canvasGroup = panel.GetComponent<CanvasGroup>();

        // Message Text (TMP)
        GameObject txtGO = new GameObject("WaveMessage", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGO.transform.SetParent(panel.transform, false);
        var txtRect = txtGO.GetComponent<RectTransform>();
        txtRect.anchorMin = new Vector2(0.5f, 0.5f);
        txtRect.anchorMax = new Vector2(0.5f, 0.5f);
        txtRect.sizeDelta = new Vector2(800, 120);
        txtRect.anchoredPosition = new Vector2(0, 0);
        messageText = txtGO.GetComponent<TextMeshProUGUI>();
        messageText.text = "";
        messageText.alignment = TextAlignmentOptions.Center;
        messageText.fontSize = 64;
        messageText.color = Color.white;
    }
}

