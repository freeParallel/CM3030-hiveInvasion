using UnityEngine;
using TMPro;
public class ResourceUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI resourceText;

    void Start()
    {
        // if not assigned, find the text component.
        if (resourceText == null)
        {
            resourceText = GetComponent<TextMeshProUGUI>();
        }
    }

    void Update()
    {
        // update the display every frame
        UpdateResourceDisplay();
    }

    void UpdateResourceDisplay()
    {
        if (ResourceManager.Instance != null && resourceText != null)
        {
            int currentPoints = ResourceManager.Instance.GetCurrentPoints();
            int towerCost = ResourceManager.Instance.GetTowerCost();

            resourceText.text = $"Points: {currentPoints} | Tower Cost: {towerCost}";
        }
    }
}
