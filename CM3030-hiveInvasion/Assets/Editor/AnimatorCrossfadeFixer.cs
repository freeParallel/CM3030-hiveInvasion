#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Tools to analyze/deduplicate Animator Controllers so CrossFade works reliably and changes persist in git.
public static class AnimatorCrossfadeFixer
{
    private const string EnemyPrefabPath = "Assets/Prefabs/Enemies/Enemy.prefab";
    private const string ArmoredPrefabPath = "Assets/Prefabs/Enemies/ArmoredEnemy.prefab";
    private const string RangedPrefabPath = "Assets/Prefabs/Enemies/RangedEnemy.prefab";
    private const string SwarmPrefabPath = "Assets/Prefabs/Enemies/SwarmEnemy.prefab";

    private const string SrcBug201Controller = "Assets/AlienBugsPack/AlienBugs/Bug_201/Bug_201_AnimController.controller";
    private const string SrcBug104Controller = "Assets/AlienBugsPack/AlienBugs/Bug_104/Bug_104_AnimController.controller";
    private const string SrcBug302Controller = "Assets/AlienBugsPack/AlienBugs/Bug_302/Bug_302_AnimController.controller";
    private const string SrcBug101Controller = "Assets/AlienBugsPack/AlienBugs/Bug_101/Bug_101_AnimController.controller";

    private const string ControllersFolder = "Assets/Controllers";
    private const string DstBug201Controller = ControllersFolder + "/Game_Bug_201.controller";
    private const string DstBug104Controller = ControllersFolder + "/Game_Bug_104.controller";
    private const string DstBug302Controller = ControllersFolder + "/Game_Bug_302.controller";
    private const string DstBug101Controller = ControllersFolder + "/Game_Bug_101.controller";

    [MenuItem("Tools/Animators/Analyze all 4 bug controllers (unconditional Idle/Walk)")]
    public static void Analyze()
    {
        AnalyzeController(SrcBug201Controller, "Bug_201 (Enemy)");
        AnalyzeController(SrcBug104Controller, "Bug_104 (Armored)");
        AnalyzeController(SrcBug302Controller, "Bug_302 (Ranged)");
        AnalyzeController(SrcBug101Controller, "Bug_101 (Swarm)");
    }

    // Safe path: duplicate vendor controllers (keeps your manual default-state edits) and assign to prefabs. No graph changes.
    [MenuItem("Tools/Animators/Duplicate + Reassign (no graph changes) for all 4 bugs")]
    public static void DuplicateAndReassignNoChanges()
    {
        EnsureFolder(ControllersFolder);
        // Copy
        CopyAssetIfPossible(SrcBug201Controller, DstBug201Controller);
        CopyAssetIfPossible(SrcBug104Controller, DstBug104Controller);
        CopyAssetIfPossible(SrcBug302Controller, DstBug302Controller);
        CopyAssetIfPossible(SrcBug101Controller, DstBug101Controller);
        // Load
        var bug201 = AssetDatabase.LoadAssetAtPath<AnimatorController>(DstBug201Controller);
        var bug104 = AssetDatabase.LoadAssetAtPath<AnimatorController>(DstBug104Controller);
        var bug302 = AssetDatabase.LoadAssetAtPath<AnimatorController>(DstBug302Controller);
        var bug101 = AssetDatabase.LoadAssetAtPath<AnimatorController>(DstBug101Controller);
        // Assign
        if (bug201 != null) AssignControllerToPrefab(EnemyPrefabPath, bug201);
        if (bug104 != null) AssignControllerToPrefab(ArmoredPrefabPath, bug104);
        if (bug302 != null) AssignControllerToPrefab(RangedPrefabPath, bug302);
        if (bug101 != null) AssignControllerToPrefab(SwarmPrefabPath, bug101);
        Debug.Log("AnimatorCrossfadeFixer: Duplicated vendor controllers and re-assigned to prefabs (no graph changes). Controllers live under Assets/Controllers and will be tracked by git.");
    }

    // Optional: duplicate, prune unconditional transitions (Idle/Walk), then assign to prefabs.
    [MenuItem("Tools/Animators/Duplicate + Fix (prune unconditional Idle/Walk) for all 4 bugs")]
    public static void DuplicateAndFix()
    {
        EnsureFolder(ControllersFolder);
        // Copy
        CopyAssetIfPossible(SrcBug201Controller, DstBug201Controller);
        CopyAssetIfPossible(SrcBug104Controller, DstBug104Controller);
        CopyAssetIfPossible(SrcBug302Controller, DstBug302Controller);
        CopyAssetIfPossible(SrcBug101Controller, DstBug101Controller);
        // Load
        var bug201 = AssetDatabase.LoadAssetAtPath<AnimatorController>(DstBug201Controller);
        var bug104 = AssetDatabase.LoadAssetAtPath<AnimatorController>(DstBug104Controller);
        var bug302 = AssetDatabase.LoadAssetAtPath<AnimatorController>(DstBug302Controller);
        var bug101 = AssetDatabase.LoadAssetAtPath<AnimatorController>(DstBug101Controller);
        // Prune
        if (bug201 != null) { PruneForCrossfade(bug201); EditorUtility.SetDirty(bug201); }
        if (bug104 != null) { PruneForCrossfade(bug104); EditorUtility.SetDirty(bug104); }
        if (bug302 != null) { PruneForCrossfade(bug302); EditorUtility.SetDirty(bug302); }
        if (bug101 != null) { PruneForCrossfade(bug101); EditorUtility.SetDirty(bug101); }
        AssetDatabase.SaveAssets();
        // Assign
        if (bug201 != null) AssignControllerToPrefab(EnemyPrefabPath, bug201);
        if (bug104 != null) AssignControllerToPrefab(ArmoredPrefabPath, bug104);
        if (bug302 != null) AssignControllerToPrefab(RangedPrefabPath, bug302);
        if (bug101 != null) AssignControllerToPrefab(SwarmPrefabPath, bug101);
        Debug.Log("AnimatorCrossfadeFixer: Duplicated vendor controllers, pruned unconditional Idle/Walk transitions, and assigned to prefabs. Controllers under Assets/Controllers.");
    }

    private static void AnalyzeController(string path, string label)
    {
        var ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (ac == null)
        {
            Debug.LogWarning($"AnimatorCrossfadeFixer: Controller not found at {path} ({label}).");
            return;
        }
        int anyToIdle = 0, anyToWalk = 0, idleToWalk = 0, walkToIdle = 0;
        foreach (var layer in ac.layers)
        {
            var sm = layer.stateMachine;
            foreach (var t in sm.anyStateTransitions)
            {
                if (t == null) continue;
                bool unconditional = t.conditions == null || t.conditions.Length == 0;
                var dest = t.destinationState != null ? t.destinationState.name : (t.destinationStateMachine != null ? t.destinationStateMachine.name : "<SM>");
                if (string.Equals(dest, "Idle", System.StringComparison.OrdinalIgnoreCase)) { if (unconditional) anyToIdle++; }
                if (string.Equals(dest, "Walk", System.StringComparison.OrdinalIgnoreCase)) { if (unconditional) anyToWalk++; }
            }
            foreach (var cs in sm.states)
            {
                var s = cs.state; if (s == null) continue;
                foreach (var tr in s.transitions)
                {
                    var dest = tr.destinationState != null ? tr.destinationState.name : (tr.destinationStateMachine != null ? tr.destinationStateMachine.name : "<SM>");
                    if (string.Equals(s.name, "Idle", System.StringComparison.OrdinalIgnoreCase) && string.Equals(dest, "Walk", System.StringComparison.OrdinalIgnoreCase)) idleToWalk++;
                    if (string.Equals(s.name, "Walk", System.StringComparison.OrdinalIgnoreCase) && string.Equals(dest, "Idle", System.StringComparison.OrdinalIgnoreCase)) walkToIdle++;
                }
            }
        }
        Debug.Log($"AnimatorCrossfadeFixer Analysis [{label}]: Any->Idle(uncond)={anyToIdle}, Any->Walk(uncond)={anyToWalk}, Idle->Walk={idleToWalk}, Walk->Idle={walkToIdle}");
    }

    private static void PruneForCrossfade(AnimatorController ac)
    {
        int removedAnyToIdle = 0, removedAnyToWalk = 0, removedIdleWalk = 0, removedWalkIdle = 0;
        foreach (var layer in ac.layers)
        {
            var sm = layer.stateMachine;
            // Remove Any State -> Idle/Walk unconditional transitions
            var any = sm.anyStateTransitions.ToList();
            for (int i = any.Count - 1; i >= 0; i--)
            {
                var t = any[i]; if (t == null) continue;
                bool unconditional = t.conditions == null || t.conditions.Length == 0;
                var destName = t.destinationState != null ? t.destinationState.name : (t.destinationStateMachine != null ? t.destinationStateMachine.name : "");
                if (!unconditional || string.IsNullOrEmpty(destName)) continue;
                if (EqualsIgnoreCase(destName, "Idle")) { sm.RemoveAnyStateTransition(t); removedAnyToIdle++; }
                else if (EqualsIgnoreCase(destName, "Walk")) { sm.RemoveAnyStateTransition(t); removedAnyToWalk++; }
            }
            // Remove explicit Idle<->Walk transitions (we'll drive with CrossFade)
            AnimatorState idle = null, walk = null;
            foreach (var cs in sm.states)
            {
                var s = cs.state; if (s == null) continue;
                if (idle == null && EqualsIgnoreCase(s.name, "Idle")) idle = s;
                if (walk == null && EqualsIgnoreCase(s.name, "Walk")) walk = s;
            }
            if (idle != null)
            {
                var trs = idle.transitions.ToList();
                for (int i = trs.Count - 1; i >= 0; i--)
                {
                    var tr = trs[i]; if (tr != null && tr.destinationState == walk) idle.RemoveTransition(tr); removedIdleWalk++;
                }
            }
            if (walk != null)
            {
                var trs = walk.transitions.ToList();
                for (int i = trs.Count - 1; i >= 0; i--)
                {
                    var tr = trs[i]; if (tr != null && tr.destinationState == idle) walk.RemoveTransition(tr); removedWalkIdle++;
                }
            }
        }
        Debug.Log($"AnimatorCrossfadeFixer: Pruned {removedAnyToIdle} Any->Idle, {removedAnyToWalk} Any->Walk, {removedIdleWalk} Idle->Walk, {removedWalkIdle} Walk->Idle in {ac.name}.");
    }

    private static void AssignControllerToPrefab(string prefabPath, RuntimeAnimatorController controller)
    {
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root == null) { Debug.LogError("AnimatorCrossfadeFixer: Failed to open prefab: " + prefabPath); return; }
        var anim = root.GetComponentInChildren<Animator>();
        if (anim == null) Debug.LogWarning("AnimatorCrossfadeFixer: No Animator found in prefab: " + prefabPath);
        else anim.runtimeAnimatorController = controller;
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        string parent = "Assets";
        foreach (var part in folder.Split('/'))
        {
            if (part == "Assets" || string.IsNullOrEmpty(part)) continue;
            string current = parent + "/" + part;
            if (!AssetDatabase.IsValidFolder(current)) AssetDatabase.CreateFolder(parent, part);
            parent = current;
        }
    }

    private static void CopyAssetIfPossible(string src, string dst)
    {
        if (!File.Exists(src)) { Debug.LogWarning("AnimatorCrossfadeFixer: Source controller missing: " + src); return; }
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(dst) != null)
        {
            AssetDatabase.DeleteAsset(dst); // overwrite to keep latest vendor edits
        }
        var ok = AssetDatabase.CopyAsset(src, dst);
        if (!ok) Debug.LogError("AnimatorCrossfadeFixer: Failed to copy to " + dst);
    }

    private static bool EqualsIgnoreCase(string a, string b) => string.Equals(a, b, System.StringComparison.OrdinalIgnoreCase);
}
#endif

