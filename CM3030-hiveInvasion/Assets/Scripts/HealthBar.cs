using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [Header("Health Bar Settings")]
    public Canvas healthCanvas;
    public Image healthBarFill;
    public Image healthBarBackground;

    [Header("Positioning")]
    public Vector3 offset = new Vector3(0, 2f, 0);
    public Vector2 healthBarSize = new Vector2(80f, 8f);

    private Camera playerCamera;
    private Transform targetTransform;

    void Start()
    {
        playerCamera = Camera.main;
        targetTransform = transform;

        SetupHealthBar();
    }

    void SetupHealthBar()
    {
        CreateCanvas();
        CreateBackground();
        CreateFillBar();
    }

    void CreateCanvas()
    {
        GameObject canvasGO = new GameObject("HealthCanvas");
        canvasGO.transform.SetParent(transform, false);
        
        healthCanvas = canvasGO.AddComponent<Canvas>();
        healthCanvas.renderMode = RenderMode.WorldSpace;
        healthCanvas.worldCamera = playerCamera;
        
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100;
        
        var canvasRect = healthCanvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1f, 0.2f);
        healthCanvas.transform.localScale = Vector3.one;
    }

    void CreateBackground()
    {
        GameObject bgGO = new GameObject("HealthBackground");
        bgGO.transform.SetParent(healthCanvas.transform, false);
        healthBarBackground = bgGO.AddComponent<Image>();
        healthBarBackground.color = new Color(0.2f, 0.2f, 0.2f, 0.8f); // Dark gray
        
        RectTransform bgRect = healthBarBackground.rectTransform;
        bgRect.anchorMin = bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        bgRect.pivot = new Vector2(0.5f, 0.5f);
        bgRect.anchoredPosition = Vector2.zero;
        bgRect.sizeDelta = healthBarSize * 0.01f;
    }

    void CreateFillBar()
    {
        GameObject fillGO = new GameObject("HealthFill");
        fillGO.transform.SetParent(healthBarBackground.transform, false);
        healthBarFill = fillGO.AddComponent<Image>();
        healthBarFill.color = Color.green;
        healthBarFill.type = Image.Type.Simple;
        
        RectTransform fillRect = healthBarFill.rectTransform;
        RectTransform bgRect = healthBarBackground.rectTransform;
        
        // Anchor to left side of parent
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = new Vector2(bgRect.sizeDelta.x, 0); // Full width initially
    }

    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        if (healthBarFill != null && healthBarBackground != null)
        {
            float healthPercentage = Mathf.Clamp01(currentHealth / maxHealth);
            
            // manually control width instead of fillAmount
            RectTransform fillRect = healthBarFill.rectTransform;
            RectTransform bgRect = healthBarBackground.rectTransform;
            
            float fullWidth = bgRect.sizeDelta.x;
            fillRect.sizeDelta = new Vector2(fullWidth * healthPercentage, 0);
            
            // color coding remains the same
            if (healthPercentage > 0.6f)
                healthBarFill.color = Color.green;
            else if (healthPercentage > 0.3f)
                healthBarFill.color = Color.yellow;
            else
                healthBarFill.color = Color.red;
        }
    }

    void Update()
    {
        // face camera, always
        if (playerCamera != null && healthCanvas != null)
        {
            healthCanvas.transform.position = targetTransform.position + offset;
            healthCanvas.transform.LookAt(healthCanvas.transform.position + playerCamera.transform.rotation * Vector3.forward,
                                    playerCamera.transform.rotation * Vector3.up);
        }
    }
}