#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Custom inspector button + menu utilities to auto-detect animation state names
[CustomEditor(typeof(EnemyAnimationController))]
public class EnemyAnimationControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var ctrl = (EnemyAnimationController)target;
        if (GUILayout.Button("Auto-Detect State Names from Child Animator"))
        {
            if (TryAutoDetect(ctrl, out string walk, out string attack, out string die))
            {
                Undo.RecordObject(ctrl, "Auto-Detect Enemy Anim States");
                if (!string.IsNullOrEmpty(walk)) ctrl.walkState = walk;
                if (!string.IsNullOrEmpty(attack)) ctrl.attackState = attack;
                if (!string.IsNullOrEmpty(die)) ctrl.dieState = die;
                EditorUtility.SetDirty(ctrl);
                Debug.Log($"EnemyAnimationController: Detected states -> Walk='{ctrl.walkState}', Attack='{ctrl.attackState}', Die='{ctrl.dieState}' on {ctrl.name}");
            }
            else
            {
                Debug.LogWarning("EnemyAnimationController: Could not detect states (no AnimatorController or states found)");
            }
        }
    }

    public static bool TryAutoDetect(EnemyAnimationController bridge, out string walk, out string attack, out string die)
    {
        walk = attack = die = null;
        if (bridge == null) return false;
        var animator = bridge.animator != null ? bridge.animator : bridge.GetComponentInChildren<Animator>();
        if (animator == null || animator.runtimeAnimatorController == null) return false;

        RuntimeAnimatorController rac = animator.runtimeAnimatorController;
        AnimatorController ac = rac as AnimatorController;
        if (ac == null && rac is AnimatorOverrideController aoc)
        {
            ac = aoc.runtimeAnimatorController as AnimatorController;
        }
        if (ac == null) return false;

        var names = CollectAllStateNames(ac);
        if (names.Count == 0) return false;

        walk = FindFirst(names, new[] { "walk" });
        attack = FindFirst(names, new[] { "attack", "atk", "bite", "strike", "hit", "shoot", "fire" });
        die = FindFirst(names, new[] { "die_1", "die", "death", "dead" });
        return true;
    }

    private static string FindFirst(List<string> names, string[] keywords)
    {
        foreach (var key in keywords)
        {
            for (int i = 0; i < names.Count; i++)
            {
                if (names[i].ToLower().Contains(key.ToLower())) return names[i];
            }
        }
        return null;
    }

    private static List<string> CollectAllStateNames(AnimatorController ac)
    {
        var list = new List<string>();
        if (ac == null) return list;
        foreach (var layer in ac.layers)
        {
            CollectFromStateMachine(layer.stateMachine, list);
        }
        return list;
    }

    private static void CollectFromStateMachine(AnimatorStateMachine sm, List<string> list)
    {
        if (sm == null) return;
        foreach (var cs in sm.states)
        {
            if (cs.state != null && !string.IsNullOrEmpty(cs.state.name))
            {
                if (!list.Contains(cs.state.name)) list.Add(cs.state.name);
            }
        }
        foreach (var ssm in sm.stateMachines)
        {
            CollectFromStateMachine(ssm.stateMachine, list);
        }
    }
}

public static class EnemyAnimStateAutoDetectMenu
{
    private const string SwarmPrefabPath = "Assets/Prefabs/Enemies/SwarmEnemy.prefab";

    [MenuItem("Tools/Setup/Auto-Detect Enemy Anim States (SwarmEnemy)")]
    public static void AutoDetectForSwarm()
    {
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(SwarmPrefabPath);
        if (go == null) { Debug.LogError("SwarmEnemy prefab not found"); return; }
        var root = PrefabUtility.LoadPrefabContents(SwarmPrefabPath);
        if (root == null) { Debug.LogError("Failed to open SwarmEnemy prefab"); return; }

        var bridge = root.GetComponent<EnemyAnimationController>();
        if (bridge == null)
        {
            bridge = root.AddComponent<EnemyAnimationController>();
        }

        if (EnemyAnimationControllerEditor.TryAutoDetect(bridge, out string walk, out string attack, out string die))
        {
            Undo.RecordObject(bridge, "Auto-Detect Enemy Anim States (Swarm)");
            if (!string.IsNullOrEmpty(walk)) bridge.walkState = walk;
            if (!string.IsNullOrEmpty(attack)) bridge.attackState = attack;
            if (!string.IsNullOrEmpty(die)) bridge.dieState = die;
            EditorUtility.SetDirty(bridge);
            PrefabUtility.SaveAsPrefabAsset(root, SwarmPrefabPath);
            Debug.Log($"SwarmEnemy updated with detected states -> Walk='{bridge.walkState}', Attack='{bridge.attackState}', Die='{bridge.dieState}'");
        }
        else
        {
            Debug.LogWarning("Could not detect states for SwarmEnemy (no AnimatorController or no states found)");
        }

        PrefabUtility.UnloadPrefabContents(root);
    }
}
#endif
