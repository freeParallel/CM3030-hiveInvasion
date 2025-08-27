using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class TowerUpgradeUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject upgradePanel;
    public Button damageUpgradeButton;
    public Button rangeUpgradeButton;
    
    [Header("Upgrade Costs")]
    public int baseDamageUpgradeCost = 50;
    public int baseRangeUpgradeCost = 50;

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip upgradeClip;
    [Range(0f,1f)] public float sfxVolume = 1f;

    private GameObject selectedTower;

    void Start()
    {
        // hide panel at start
        if(upgradePanel != null) 
            upgradePanel.SetActive(false);
    
        // Ensure SFX source
        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f; // 2D UI SFX
            sfxSource.volume = Mathf.Clamp01(sfxVolume);
        }
        // Autoload clip if not assigned
        if (upgradeClip == null)
        {
            upgradeClip = Resources.Load<AudioClip>("Audio/tower_upgrade");
        }
    
        // DEBUG: Check if buttons are assigned
        Debug.Log($"damageUpgradeButton is null: {damageUpgradeButton == null}");
        Debug.Log($"rangeUpgradeButton is null: {rangeUpgradeButton == null}");
    
        // listen for tower selection events
        TowerSelectionManager.OnTowerSelected += ShowUpgradeUI;
        TowerSelectionManager.OnTowerDeselected += HideUpgradeUI;
    
        // setup button listeners
        if (damageUpgradeButton != null)
        {
            damageUpgradeButton.onClick.AddListener(UpgradeDamage);
            Debug.Log("Damage button listener added!");
        }
        else
        {
            Debug.Log("Damage button is NULL - not adding listener!");
        }
    
        if (rangeUpgradeButton != null)
        {
            rangeUpgradeButton.onClick.AddListener(UpgradeRange);
            Debug.Log("Range button listener added!");
        }
        else
        {
            Debug.Log("Range button is NULL - not adding listener!");
        }
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
        if (selectedTower != null)
        {
            TowerData towerData = selectedTower.GetComponent<TowerData>();
        
            bool canAffordDamage = ResourceManager.Instance.CanAfford(GetDamageUpgradeCost(towerData));
            bool canAffordRange = ResourceManager.Instance.CanAfford(GetRangeUpgradeCost(towerData));

            Debug.Log($"Can afford damage: {canAffordDamage}, Can afford range: {canAffordRange}");

            if (damageUpgradeButton != null)
            {
                damageUpgradeButton.interactable = canAffordDamage;
                Debug.Log($"Damage button interactable: {damageUpgradeButton.interactable}");
            }

            if (rangeUpgradeButton != null)
            {
                rangeUpgradeButton.interactable = canAffordRange;
                Debug.Log($"Range button interactable: {rangeUpgradeButton.interactable}");
            }
        }
    }

    int GetDamageUpgradeCost(TowerData towerData)
    {
        int upgradeLevel = towerData.GetLevel() - 1; // this way, Level 1 = 0 upgrades
        return baseDamageUpgradeCost + (upgradeLevel * 25);
    }

    int GetRangeUpgradeCost(TowerData towerData)
    {
        int upgradeLevel = towerData.GetLevel() - 1;
        return baseRangeUpgradeCost + (upgradeLevel * 25);
    }
    
    void UpgradeDamage()
    {
        Debug.Log("UpgradeDamage() called!");

        if (selectedTower != null)
        {
            TowerData towerData = selectedTower.GetComponent<TowerData>();
            int cost = GetDamageUpgradeCost(towerData);

            if (ResourceManager.Instance.SpendPoints(cost))
            {
                towerData.UpgradeDamage();
                if (sfxSource != null && upgradeClip != null) sfxSource.PlayOneShot(upgradeClip, Mathf.Clamp01(sfxVolume));
                UpdateButtonStates(); // refresh state after upgrade
                Debug.Log($"Damages upgraded. Cost: {cost}");
            }
        }
        else
        {
            Debug.Log("selectedTower is null!"); 
        }
    }

    void UpgradeRange()
    {
        Debug.Log("UpgradeRange() called!");
        
        if (selectedTower != null)
        {
            TowerData towerData = selectedTower.GetComponent<TowerData>();
            int cost = GetRangeUpgradeCost(towerData);

            if (ResourceManager.Instance.SpendPoints(cost))
            {
                towerData.UpgradeRange();
                if (sfxSource != null && upgradeClip != null) sfxSource.PlayOneShot(upgradeClip, Mathf.Clamp01(sfxVolume));
                UpdateButtonStates(); // refresh state after upgrade
                Debug.Log($"Range upgraded. Cost: {cost}");
            }
        }
        else
        {
            Debug.Log("selectedTower is null!"); 
        }
    }
    
    
}
