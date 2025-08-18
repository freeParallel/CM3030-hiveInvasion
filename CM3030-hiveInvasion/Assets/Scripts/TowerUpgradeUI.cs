using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TowerUpgradeUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject upgradePanel;
    public Button damageUpgradeButton;
    public Button rangeUpgradeButton;
    
    [Header("Upgrade Costs")]
    public int damageUpgradeCost = 50;
    public int rangeUpgradeCost = 50;

    private GameObject selectedTower;

    void Start()
    {
        // hide panel at start
        if(upgradePanel != null) 
           upgradePanel.SetActive(false);
        
        // listen for tower selection events
        TowerSelectionManager.OnTowerSelected += ShowUpgradeUI;
        TowerSelectionManager.OnTowerDeselected += HideUpgradeUI;
        
        // setup button listeners
        if (damageUpgradeButton != null)
            damageUpgradeButton.onClick.AddListener(UpgradeDamage);
        
        if (rangeUpgradeButton != null)
            rangeUpgradeButton.onClick.AddListener(UpgradeRange);
    }

    void OnDestroy()
    {
        // unsubscribe events
        TowerSelectionManager.OnTowerSelected -= ShowUpgradeUI;
        TowerSelectionManager.OnTowerDeselected -= HideUpgradeUI;
    }

    void ShowUpgradeUI(GameObject tower)
    {
        selectedTower = tower;
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(true);
            UpdateButtonStates();
        }
    }

    void HideUpgradeUI()
    {
        selectedTower = null;
        if(upgradePanel != null)
            upgradePanel.SetActive(false);
    }

    void UpdateButtonStates()
    {
        // show/hide depending on points
        bool canAffordDamage = ResourceManager.Instance.CanAfford(damageUpgradeCost);
        bool canAffordRange = ResourceManager.Instance.CanAfford(rangeUpgradeCost);

        if (damageUpgradeButton != null)
            damageUpgradeButton.interactable = canAffordDamage;
        
        if (rangeUpgradeButton != null)
            rangeUpgradeButton.interactable = canAffordRange;
    }

    void UpgradeDamage()
    {
        if (selectedTower != null && ResourceManager.Instance.SpendPoints(damageUpgradeCost))
        {
            TowerData towerData = selectedTower.GetComponent < TowerData>();
            if (towerData != null)
            {
                towerData.UpgradeDamage();
                UpdateButtonStates();
                Debug.Log("Damage Upgraded");
            }
        }
    }

    void UpgradeRange()
    {
        if (selectedTower != null && ResourceManager.Instance.SpendPoints(rangeUpgradeCost))
        {
            TowerData towerData = selectedTower.GetComponent < TowerData>();
            if (towerData != null)
            {
                towerData.UpgradeRange();
                UpdateButtonStates();
                Debug.Log("Range Upgraded");
            }
        }
    }
    
    
}
