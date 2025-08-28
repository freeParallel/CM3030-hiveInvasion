using UnityEngine;
using UnityEngine.SceneManagement;

// Ensures MusicManager is created/destroyed appropriately on every scene load.
// - No BGM in MainMenu
// - BGM auto-creates in any other scene
public static class MusicBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu")
        {
            if (MusicManager.Instance != null)
            {
                Object.Destroy(MusicManager.Instance.gameObject);
            }
            return;
        }

        if (MusicManager.Instance == null)
        {
            var go = new GameObject("MusicManager");
            go.AddComponent<MusicManager>();
        }
        else
        {
            // Reset volume in case previous scene faded it to zero
            MusicManager.Instance.ResetVolumeToDefault();
        }
    }
}

