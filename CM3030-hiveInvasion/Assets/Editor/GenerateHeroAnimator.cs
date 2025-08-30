#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations;

// Creates a simple AnimatorController for the Hero and assigns it to the rig root under the Hero.
// It scans the project for AnimationClips (preferably near the Strider model) and wires Idle/Walk/Run
// into a 1D blend tree by Speed, plus optional Attack/Shoot/AOE/Die triggers.
public static class GenerateHeroAnimator
{
    private const string DefaultControllerPath = "Assets/Controllers/Hero.controller";

    [MenuItem("Tools/Hero/Generate & Assign Hero Animator (scan clips)")]
    public static void Generate()
    {
        var hero = Object.FindObjectOfType<HeroController>();
        if (hero == null)
        {
            Debug.LogError("GenerateHeroAnimator: No HeroController found in the open scene.");
            return;
        }

        // Find the rig root (the child that will own the Animator)
        var anim = hero.GetComponentInChildren<Animator>();
        if (anim == null)
        {
            Debug.LogError("GenerateHeroAnimator: No Animator found under Hero. Add Animator to the Strider rig root.");
            return;
        }

        // Pick a search folder near the model if possible
        string modelFolder = FindModelFolder(anim.gameObject);
        string[] searchIn = string.IsNullOrEmpty(modelFolder) ? new[] { "Assets" } : new[] { modelFolder, "Assets/PolygonSciFiWorlds" };

        // Find clips by keywords
        var idle = FindClip(searchIn, new[] { "idle", "stand" });
        var walk = FindClip(searchIn, new[] { "walk" });
        var run  = FindClip(searchIn, new[] { "run" });
        var attack = FindClip(searchIn, new[] { "attack", "melee", "slash", "punch" });
        var shoot  = FindClip(searchIn, new[] { "shoot", "rifle", "pistol", "fire" });
        var aoe    = FindClip(searchIn, new[] { "cast", "spell", "ability", "charge" });
        var die    = FindClip(searchIn, new[] { "die", "death" });

        // Create controller folder
        EnsureFolder("Assets/Controllers");
        // (Re)create controller
        if (File.Exists(DefaultControllerPath)) AssetDatabase.DeleteAsset(DefaultControllerPath);
        var ac = AnimatorController.CreateAnimatorControllerAtPath(DefaultControllerPath);

        // Parameters
        ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ac.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        ac.AddParameter("Shoot", AnimatorControllerParameterType.Trigger);
        ac.AddParameter("AOE", AnimatorControllerParameterType.Trigger);
        ac.AddParameter("Die", AnimatorControllerParameterType.Trigger);

        var layer = ac.layers[0];
        var sm = layer.stateMachine;

        // Idle state
        var idleState = sm.AddState("Idle");
        idleState.motion = idle != null ? idle : null;
        sm.defaultState = idleState;

        // Locomotion blend tree
        Motion locomotionMotion = null;
        {
            var tree = new BlendTree();
            tree.name = "Locomotion";
            tree.blendType = BlendTreeType.Simple1D;
            tree.useAutomaticThresholds = false;
            tree.blendParameter = "Speed";
            AssetDatabase.AddObjectToAsset(tree, ac);

            // Populate based on available clips
            if (walk != null && run != null)
            {
                tree.AddChild(walk, 0.2f);
                tree.AddChild(run, 0.8f);
            }
            else if (walk != null)
            {
                tree.AddChild(walk, 0.5f);
            }
            else if (run != null)
            {
                tree.AddChild(run, 0.8f);
            }
            else if (idle != null)
            {
                // Fallback so animation system still blends
                tree.AddChild(idle, 0.0f);
            }

            locomotionMotion = tree;
        }

        var locoState = sm.AddState("Locomotion");
        locoState.motion = locomotionMotion;

        // AnyState → action triggers
        if (attack != null)
        {
            var a = sm.AddState("Attack"); a.motion = attack;
            var anyToA = sm.AddAnyStateTransition(a); anyToA.AddCondition(AnimatorConditionMode.If, 0, "Attack"); anyToA.hasExitTime = false; anyToA.duration = 0;
            var aToIdle = a.AddTransition(idleState); aToIdle.hasExitTime = true; aToIdle.exitTime = 0.9f; aToIdle.duration = 0.1f;
        }
        if (shoot != null)
        {
            var s = sm.AddState("Shoot"); s.motion = shoot;
            var anyToS = sm.AddAnyStateTransition(s); anyToS.AddCondition(AnimatorConditionMode.If, 0, "Shoot"); anyToS.hasExitTime = false; anyToS.duration = 0;
            var sToIdle = s.AddTransition(idleState); sToIdle.hasExitTime = true; sToIdle.exitTime = 0.9f; sToIdle.duration = 0.1f;
        }
        if (aoe != null)
        {
            var q = sm.AddState("AOE"); q.motion = aoe;
            var anyToQ = sm.AddAnyStateTransition(q); anyToQ.AddCondition(AnimatorConditionMode.If, 0, "AOE"); anyToQ.hasExitTime = false; anyToQ.duration = 0;
            var qToIdle = q.AddTransition(idleState); qToIdle.hasExitTime = true; qToIdle.exitTime = 0.9f; qToIdle.duration = 0.1f;
        }
        if (die != null)
        {
            var d = sm.AddState("Die"); d.motion = die;
            var anyToD = sm.AddAnyStateTransition(d); anyToD.AddCondition(AnimatorConditionMode.If, 0, "Die"); anyToD.hasExitTime = false; anyToD.duration = 0;
        }

        // Locomotion transitions
        var idleToLoco = idleState.AddTransition(locoState);
        idleToLoco.hasExitTime = false; idleToLoco.duration = 0.1f;
        idleToLoco.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        var locoToIdle = locoState.AddTransition(idleState);
        locoToIdle.hasExitTime = false; locoToIdle.duration = 0.1f;
        locoToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "Speed"); // Speed <= 0 triggers back (approx)

        EditorUtility.SetDirty(ac);
        AssetDatabase.SaveAssets();

        // Assign to Animator
        anim.runtimeAnimatorController = ac;
        EditorUtility.SetDirty(anim);

        Debug.Log($"GenerateHeroAnimator: Created {DefaultControllerPath} and assigned to {anim.gameObject.name}. Clips -> Idle:{(idle? idle.name: "-")}, Walk:{(walk? walk.name: "-")}, Run:{(run? run.name: "-")}, Attack:{(attack? attack.name: "-")}, Shoot:{(shoot? shoot.name: "-")}, AOE:{(aoe? aoe.name: "-")}, Die:{(die? die.name: "-")}");
    }

    private static string FindModelFolder(GameObject rigRoot)
    {
        // Try to locate the prefab asset path for the rig root to infer folder
        var prefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(rigRoot);
        if (prefab == null) return null;
        var path = AssetDatabase.GetAssetPath(prefab);
        if (string.IsNullOrEmpty(path)) return null;
        return Path.GetDirectoryName(path).Replace('\\', '/');
    }

    private static AnimationClip FindClip(string[] searchFolders, string[] keywords)
    {
        string filter = "t:AnimationClip";
        var guids = AssetDatabase.FindAssets(filter, searchFolders);
        AnimationClip best = null; int bestScore = -1;
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) continue;
            string lower = clip.name.ToLowerInvariant();
            int score = 0;
            foreach (var k in keywords)
            {
                if (!string.IsNullOrEmpty(k) && lower.Contains(k.ToLowerInvariant())) score++;
            }
            if (score > bestScore)
            {
                best = clip; bestScore = score;
            }
        }
        return best;
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
}
#endif
