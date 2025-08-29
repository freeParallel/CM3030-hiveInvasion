#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Integrates AlienBugsPack Bug_104 into the ArmoredEnemy prefab in-place
public static class IntegrateBug104Armored
{
    private const string ArmoredPrefabPath = "Assets/Prefabs/Enemies/ArmoredEnemy.prefab";
    private const string BugPrefabPath     = "Assets/AlienBugsPack/Prefabs/Bug_104_Prefab.prefab";
    private const string ControllerPath    = "Assets/AlienBugsPack/AlienBugs/Bug_104/Bug_104_AnimController.controller";

    [MenuItem("Tools/Setup/Integrate Bug_104 into ArmoredEnemy")]
    public static void Integrate()
    {
        var armoredPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArmoredPrefabPath);
        var bugPrefab     = AssetDatabase.LoadAssetAtPath<GameObject>(BugPrefabPath);
        var controller    = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);

        if (armoredPrefab == null)
        {
            Debug.LogError("ArmoredEnemy prefab not found. Check path: " + ArmoredPrefabPath);
            return;
        }
        if (bugPrefab == null)
        {
            Debug.LogError("Bug_104 prefab not found. Check path: " + BugPrefabPath);
        }

        GameObject root = PrefabUtility.LoadPrefabContents(ArmoredPrefabPath);
        if (root == null)
        {
            Debug.LogError("Failed to load ArmoredEnemy prefab contents");
            return;
        }

        bool changed = false;

        // Remove prior integrated visuals (idempotent)
        var prior = root.transform.Find("Model_Bug104");
        if (prior != null)
        {
            Object.DestroyImmediate(prior.gameObject);
        }

        // Create a model group and instantiate the Bug_104 prefab under it
        GameObject modelGroup = new GameObject("Model_Bug104");
        modelGroup.transform.SetParent(root.transform, worldPositionStays: false);
        var instance = bugPrefab != null ? (GameObject)PrefabUtility.InstantiatePrefab(bugPrefab, modelGroup.transform) : null;
        if (instance != null)
        {
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale    = Vector3.one;
            changed = true;
        }

        // Disable placeholder MeshRenderer on the root if present
        var rootRenderer = root.GetComponent<MeshRenderer>();
        if (rootRenderer != null && rootRenderer.enabled)
        {
            rootRenderer.enabled = false;
            changed = true;
        }

        // Ensure there is an Animator on the visual instance and assign controller if available
        Animator animator = instance != null ? instance.GetComponentInChildren<Animator>() : null;
        if (animator == null && instance != null)
        {
            animator = instance.AddComponent<Animator>();
        }
        if (controller != null && animator != null)
        {
            animator.runtimeAnimatorController = controller;
        }

        // Ensure animation controller bridge exists on root
        var bridge = root.GetComponent<EnemyAnimationController>();
        if (bridge == null)
        {
            bridge = root.AddComponent<EnemyAnimationController>();
            changed = true;
        }

        // Configure melee behavior on EnemyMovement (if present)
        var move = root.GetComponent<EnemyMovement>();
        if (move != null)
        {
            move.isRangedEnemy = false;
            changed = true;
        }

        if (changed)
        {
            PrefabUtility.SaveAsPrefabAsset(root, ArmoredPrefabPath);
            Debug.Log("ArmoredEnemy prefab updated: Bug_104 integrated with animation controller.");
        }
        PrefabUtility.UnloadPrefabContents(root);
    }
}
#endif

