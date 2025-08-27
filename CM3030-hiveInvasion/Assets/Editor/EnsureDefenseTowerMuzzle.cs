#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class EnsureDefenseTowerMuzzle
{
    private const string PrefabPath = "Assets/Prefabs/DefesiveTowers/DefenseTower.prefab";
    private const string SessionKey = "HI_EnsureDefenseTowerMuzzle_Done";

    [InitializeOnLoadMethod]
    private static void AutoEnsureOnLoad()
    {
        // Run once per editor session to avoid repeated asset writes
        if (SessionState.GetBool(SessionKey, false)) return;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null) return;
        TryEnsureMuzzle(logToConsole: false);
        SessionState.SetBool(SessionKey, true);
    }

    [MenuItem("Tools/Setup/Ensure DefenseTower Muzzle")]
    public static void MenuEnsureMuzzle()
    {
        TryEnsureMuzzle(logToConsole: true);
    }

    private static void TryEnsureMuzzle(bool logToConsole)
    {
        var prefabGo = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefabGo == null)
        {
            if (logToConsole) Debug.LogWarning($"Prefab not found at {PrefabPath}");
            return;
        }

        // Open prefab contents for editing
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (root == null)
        {
            if (logToConsole) Debug.LogWarning("Failed to load prefab contents");
            return;
        }

        bool changed = false;
        var tc = root.GetComponentInChildren<TowerCombat>(true);
        if (tc == null)
        {
            if (logToConsole) Debug.LogWarning("TowerCombat not found in DefenseTower prefab.");
        }
        else
        {
            // Ensure a child named "Muzzle" exists under the TowerCombat GameObject
            Transform parent = tc.transform;
            Transform muzzle = parent.Find("Muzzle");
            if (muzzle == null)
            {
                GameObject m = new GameObject("Muzzle");
                muzzle = m.transform;
                muzzle.SetParent(parent, worldPositionStays: false);
                // Place at computed top-center
                Vector3 worldTop = ComputeTopCenterWorld(parent);
                muzzle.position = worldTop;
                changed = true;
            }
            else
            {
                // If it exists but looks uninitialized at origin, place it at top center
                if (muzzle.position == Vector3.zero)
                {
                    muzzle.position = ComputeTopCenterWorld(parent);
                    changed = true;
                }
            }

            // Assign to TowerCombat if not set
            if (tc.muzzleTransform != muzzle)
            {
                tc.muzzleTransform = muzzle;
                changed = true;
            }

            // Ensure projectiles are enabled by default on the prefab
            if (!tc.useProjectiles)
            {
                tc.useProjectiles = true;
                changed = true;
            }
        }

        if (changed)
        {
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            if (logToConsole) Debug.Log("DefenseTower prefab updated: added/assigned Muzzle and enabled projectiles.");
        }

        PrefabUtility.UnloadPrefabContents(root);
    }

    private static Vector3 ComputeTopCenterWorld(Transform root)
    {
        // Combine renderer bounds for best visual accuracy
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers != null && renderers.Length > 0)
        {
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                b.Encapsulate(renderers[i].bounds);
            }
            return new Vector3(b.center.x, b.max.y + 0.1f, b.center.z);
        }
        // Fallback to collider bounds
        var col = root.GetComponentInChildren<Collider>(true);
        if (col != null)
        {
            Bounds b = col.bounds;
            return new Vector3(b.center.x, b.max.y + 0.1f, b.center.z);
        }
        // Final fallback: above root position
        return root.position + Vector3.up * 1f;
    }
}
#endif

