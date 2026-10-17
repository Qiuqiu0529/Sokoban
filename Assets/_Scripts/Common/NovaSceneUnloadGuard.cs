using Nova;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Deactivates Nova list views in the outgoing scene before scene transitions.
/// This lets Nova unregister its virtualized UI hierarchy while the scene is
/// still intact, instead of during Unity's scene-destruction pass.
/// </summary>
public static class NovaSceneUnloadGuard
{
    public static void PrepareActiveSceneForUnload()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isLoaded)
            return;

        ListView[] listViews = Object.FindObjectsOfType<ListView>();
        foreach (ListView listView in listViews)
        {
            if (listView == null || listView.gameObject.scene != activeScene)
                continue;

            // Setting the view root inactive invokes Nova's normal disable
            // lifecycle before its UIBlocks are destroyed with the scene.
            listView.gameObject.SetActive(false);
        }
    }
}
