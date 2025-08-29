#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Integrates AlienBugsPack Bug_101 into the SwarmEnemy prefab automatically
public static class IntegrateBug101Swarm
{
    private const string SwarmPrefabPath = "Assets/Prefabs/Enemies/SwarmEnemy.prefab";
    private const string BugPrefabPath = "Assets/AlienBugsPack/Prefabs/Bug_101_Prefab.prefab";
    private const string BugControllerPath = "Assets/AlienBugsPack/AlienBugs/Bug_101/Bug_101_AnimController.controller";

    [MenuItem("Tools/Setup/Integrate Bug_101 into SwarmEnemy")]
    public static void Integrate()
    {
        var swarmPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SwarmPrefabPath);
        var bugPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BugPrefabPath);
        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(BugControllerPath);
        if (swarmPrefab == null || bugPrefab == null)
        {
            Debug.LogError("SwarmEnemy prefab or Bug_101 prefab not found. Check paths.");
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(SwarmPrefabPath);
        if (root == null) { Debug.LogError("Failed to load SwarmEnemy prefab contents"); return; }

        bool changed = false;

        // Remove prior visual child named "Model_Bug101" (idempotent re-run)
        var prior = root.transform.Find("Model_Bug101");
        if (prior != null) Object.DestroyImmediate(prior.gameObject);

        // Instantiate bug as a child under a group GO
        GameObject modelGroup = new GameObject("Model_Bug101");
        modelGroup.transform.SetParent(root.transform, worldPositionStays: false);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(bugPrefab, modelGroup.transform);
        if (instance != null)
        {
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            changed = true;
        }

        // Ensure animator controller
        Animator animator = instance != null ? instance.GetComponentInChildren<Animator>() : null;
        if (animator == null)
        {
            animator = instance.AddComponent<Animator>();
        }
        if (controller != null && animator != null)
        {
            animator.runtimeAnimatorController = controller;
        }

        // Add animation controller bridge on root (or ensure it exists)
        var bridge = root.GetComponent<EnemyAnimationController>();
        if (bridge == null)
        {
            bridge = root.AddComponent<EnemyAnimationController>();
            changed = true;
        }
        // Bridge finds animator automatically in children if not assigned

        if (changed)
        {
            PrefabUtility.SaveAsPrefabAsset(root, SwarmPrefabPath);
            Debug.Log("SwarmEnemy prefab updated: Bug_101 integrated with animation controller.");
        }
        PrefabUtility.UnloadPrefabContents(root);
    }
}
#endif

