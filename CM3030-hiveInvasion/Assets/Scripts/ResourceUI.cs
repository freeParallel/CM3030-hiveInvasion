using UnityEngine;
using TMPro;
using System.Collections;

public class ResourceUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI resourceText;

    [Header("Display")]
    public bool showTowerCost = true;
    public bool useNewLineForCost = true;
    [Range(0.5f, 1.2f)] public float costRelativeSize = 0.75f;
    public Color costColor = new Color(0.82f, 0.93f, 1f, 1f);

    public bool showTowerCount = true;
    [Range(0.5f, 1.2f)] public float countRelativeSize = 0.75f;
    public Color countColor = new Color(0.9f, 1f, 0.9f, 1f);

    [Header("Animation")]
    [Tooltip("Duration of the points count animation")] public float animateDuration = 0.45f;
    [Range(0f, 0.5f)] [Tooltip("How much the text scales up during a gain/spend")] public float punchScale = 0.15f;
    [Tooltip("Highlight color when gaining points")] public Color gainHighlight = new Color(1f, 0.85f, 0.2f, 1f);
    [Tooltip("Highlight color when spending points")] public Color spendHighlight = new Color(1f, 0.35f, 0.35f, 1f);

    private int lastLogicalPoints;
    private int displayedPoints;
    private int lastTowerCost = int.MinValue;
    private int lastTowerCount = -1;
    private int lastTowerMax = -1;
    private Color baseColor = Color.white;
    private Vector3 baseScale = Vector3.one;
    private Coroutine animRoutine;

    void Start()
    {
        // if not assigned, find the text component.
        if (resourceText == null)
        {
            resourceText = GetComponent<TextMeshProUGUI>();
        }

        if (resourceText != null)
        {
            baseColor = resourceText.color;
            baseScale = resourceText.rectTransform.localScale;
        }

        int currentPoints = (ResourceManager.Instance != null) ? ResourceManager.Instance.GetCurrentPoints() : 0;
        displayedPoints = lastLogicalPoints = currentPoints;
        UpdateResourceDisplayText(displayedPoints, true);
    }

    void Update()
    {
        int currentPoints = (ResourceManager.Instance != null) ? ResourceManager.Instance.GetCurrentPoints() : displayedPoints;

        if (currentPoints != lastLogicalPoints)
        {
            bool isGain = currentPoints > lastLogicalPoints;
            StartAnimateTo(currentPoints, isGain);
            lastLogicalPoints = currentPoints;
        }
        else
        {
            // Keep tower cost fresh even if points haven't changed
            EnsureCostRefreshed();
        }
    }

    private void StartAnimateTo(int target, bool isGain)
    {
        if (animRoutine != null) StopCoroutine(animRoutine);
        var highlight = isGain ? gainHighlight : spendHighlight;
        animRoutine = StartCoroutine(AnimatePoints(displayedPoints, target, highlight));
    }

    private IEnumerator AnimatePoints(int from, int to, Color highlight)
    {
        float t = 0f;
        float dur = Mathf.Max(0.05f, animateDuration);

        while (t < dur)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / dur);
            // EaseOutCubic
            float ease = 1f - Mathf.Pow(1f - u, 3f);

            int val = Mathf.RoundToInt(Mathf.Lerp(from, to, ease));
            displayedPoints = val;

            if (resourceText != null)
            {
                resourceText.color = Color.Lerp(highlight, baseColor, ease);
                float s = 1f + punchScale * (1f - ease);
                resourceText.rectTransform.localScale = baseScale * s;
            }

            UpdateResourceDisplayText(displayedPoints);
            yield return null;
        }

        displayedPoints = to;
        if (resourceText != null)
        {
            resourceText.color = baseColor;
            resourceText.rectTransform.localScale = baseScale;
        }
        UpdateResourceDisplayText(displayedPoints, true);
        animRoutine = null;
    }

    private void EnsureCostRefreshed()
    {
        int cost = (ResourceManager.Instance != null) ? ResourceManager.Instance.GetTowerCost() : lastTowerCost;
        int count = (TowerManager.Instance != null) ? TowerManager.Instance.GetTowerCount() : lastTowerCount;
        int max = (TowerManager.Instance != null) ? TowerManager.Instance.GetMaxTowers() : lastTowerMax;
        if (cost != lastTowerCost || count != lastTowerCount || max != lastTowerMax)
        {
            UpdateResourceDisplayText(displayedPoints, true);
        }
    }

    private void UpdateResourceDisplayText(int points, bool force = false)
    {
        if (resourceText == null) return;

        string costSuffix = string.Empty;
        if (showTowerCost && ResourceManager.Instance != null)
        {
            int cost = ResourceManager.Instance.GetTowerCost();

            if (force || cost != lastTowerCost)
            {
                string hex = ColorUtility.ToHtmlStringRGB(costColor);
                string sizeTag = Mathf.Clamp(Mathf.RoundToInt(costRelativeSize * 100f), 10, 300) + "%";
                string separator = useNewLineForCost ? "\n" : "   ";
                costSuffix = separator + $"<size={sizeTag}><color=#{hex}>Tower Cost {cost:N0}</color></size>";
                lastTowerCost = cost;
            }
            else
            {
                string hex = ColorUtility.ToHtmlStringRGB(costColor);
                string sizeTag = Mathf.Clamp(Mathf.RoundToInt(costRelativeSize * 100f), 10, 300) + "%";
                string separator = useNewLineForCost ? "\n" : "   ";
                costSuffix = separator + $"<size={sizeTag}><color=#{hex}>Tower Cost {lastTowerCost:N0}</color></size>";
            }
        }

        string towersSuffix = string.Empty;
        if (showTowerCount && TowerManager.Instance != null)
        {
            int count = TowerManager.Instance.GetTowerCount();
            int max = TowerManager.Instance.GetMaxTowers();
            string hex = ColorUtility.ToHtmlStringRGB(countColor);
            string sizeTag = Mathf.Clamp(Mathf.RoundToInt(countRelativeSize * 100f), 10, 300) + "%";
            string separator = "\n"; // always on new line for clarity
            towersSuffix = separator + $"<size={sizeTag}><color=#{hex}>Towers {count} / {max}</color></size>";
            lastTowerCount = count;
            lastTowerMax = max;
        }

        resourceText.text = $"Points {points:N0}{costSuffix}{towersSuffix}";
    }
}
