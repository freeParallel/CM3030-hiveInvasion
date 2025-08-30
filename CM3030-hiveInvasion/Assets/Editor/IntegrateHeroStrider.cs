#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Integrates the Synty Strider model into the scene Hero (object with HeroController)
public static class IntegrateHeroStrider
{
    private const string StriderPath = "Assets/PolygonSciFiWorlds/Prefabs/Characters/SM_Chr_ScifiWorlds_Strider_Male_01.prefab";

    [MenuItem("Tools/Hero/Integrate Strider Model Into Scene Hero")]
    public static void Integrate()
    {
        var hero = Object.FindObjectOfType<HeroController>();
        if (hero == null)
        {
            Debug.LogError("IntegrateHeroStrider: No HeroController found in the open scene.");
            return;
        }

        var striderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StriderPath);
        if (striderPrefab == null)
        {
            Debug.LogError("IntegrateHeroStrider: Strider prefab not found. Check path: " + StriderPath);
            return;
        }

        var heroGO = hero.gameObject;
        bool changed = false;

        // Remove previous integrated visuals (if any)
        var prior = heroGO.transform.Find("Model_Strider");
        if (prior != null)
        {
            Object.DestroyImmediate(prior.gameObject);
            changed = true;
        }

        // Create a model group and instantiate the Strider prefab under it
        var modelGroup = new GameObject("Model_Strider");
        modelGroup.transform.SetParent(heroGO.transform, worldPositionStays: false);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(striderPrefab, modelGroup.transform);
        if (instance == null)
        {
            Debug.LogWarning("IntegrateHeroStrider: Failed to instantiate Strider prefab under hero.");
        }
        else
        {
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            changed = true;
        }

        // Disable placeholder MeshRenderer on the hero root if present
        var rootRenderer = heroGO.GetComponent<MeshRenderer>();
        if (rootRenderer != null && rootRenderer.enabled)
        {
            rootRenderer.enabled = false;
            changed = true;
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(heroGO.scene);
            Debug.Log("IntegrateHeroStrider: Strider model integrated into Hero (scene). Follow the checklist to enable the correct mesh and set up Animator.");
        }
        else
        {
            Debug.Log("IntegrateHeroStrider: No changes made.");
        }
    }
}
#endif

