using UnityEngine;
using UnityEngine.SceneManagement;

public class ResetSceneButton : MonoBehaviour
{
    /// <summary>
    /// Call this from your UI Button's OnClick to reload the active scene.
    /// </summary>
    public void ReloadScene()
    {
        // Get the currently‐active scene…
        Scene active = SceneManager.GetActiveScene();
        // …and reload it by name (or buildIndex).
        SceneManager.LoadScene(active.name);
    }
}
