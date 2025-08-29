#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Integrates AlienBugsPack Bug_302 into the RangedEnemy prefab in-place
public static class IntegrateBug302Ranged
{
    private const string RangedPrefabPath = "Assets/Prefabs/Enemies/RangedEnemy.prefab";
    private const string BugPrefabPath    = "Assets/AlienBugsPack/Prefabs/Bug_302_Prefab.prefab";
    private const string ControllerPath   = "Assets/AlienBugsPack/AlienBugs/Bug_302/Bug_303_AnimController.controller";
    private const string ProjectilePath   = "Assets/AlienBugsPack/Prefabs/Bug_302_SoundWave.prefab";

    [MenuItem("Tools/Setup/Integrate Bug_302 into RangedEnemy")]
    public static void Integrate()
    {
        var rangedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RangedPrefabPath);
        var bugPrefab    = AssetDatabase.LoadAssetAtPath<GameObject>(BugPrefabPath);
        var controller   = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
        var projectile   = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePath);

        if (rangedPrefab == null)
        {
            Debug.LogError("RangedEnemy prefab not found. Check path: " + RangedPrefabPath);
            return;
        }
        if (bugPrefab == null)
        {
            Debug.LogError("Bug_302 prefab not found. Check path: " + BugPrefabPath);
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(RangedPrefabPath);
        if (root == null)
        {
            Debug.LogError("Failed to load RangedEnemy prefab contents");
            return;
        }

        bool changed = false;

        // Remove prior integrated visuals (idempotent)
        var prior = root.transform.Find("Model_Bug302");
        if (prior != null)
        {
            Object.DestroyImmediate(prior.gameObject);
        }

        // Create a model group and instantiate the Bug_302 prefab under it
        GameObject modelGroup = new GameObject("Model_Bug302");
        modelGroup.transform.SetParent(root.transform, worldPositionStays: false);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(bugPrefab, modelGroup.transform);
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

        // Configure ranged behavior on EnemyMovement
        var move = root.GetComponent<EnemyMovement>();
        if (move != null)
        {
            move.isRangedEnemy = true;
            move.rangedAttackRange = Mathf.Max(move.rangedAttackRange, 12f);
            if (projectile != null)
            {
                move.projectilePrefab = projectile;
            }
            changed = true;
        }
        else
        {
            Debug.LogWarning("EnemyMovement component not found on RangedEnemy root; ranged setup skipped.");
        }

        if (changed)
        {
            PrefabUtility.SaveAsPrefabAsset(root, RangedPrefabPath);
            Debug.Log("RangedEnemy prefab updated: Bug_302 integrated with animation controller.");
        }
        PrefabUtility.UnloadPrefabContents(root);
    }
}
#endif

