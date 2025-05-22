using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class EndScreen : MonoBehaviour
{
    public Image[] buttons;
    private SceneFader sceneFader;

    void Start()
    {
        // Attempt to find the SceneFader instance in the scene
        sceneFader = FindObjectOfType<SceneFader>();
        if (sceneFader == null)
        {
            Debug.LogError("EndScreen: SceneFader not found in the scene. Please ensure a SceneFader object exists and is active.");
        }

        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].alphaHitTestMinimumThreshold = 1f;
        }
    }

    public void PlayAgain()
    {
        if (sceneFader != null)
        {
            // Get the current active scene's build index to reload it
            sceneFader.FadeToScene(SceneManager.GetActiveScene().buildIndex);
        }
        else
        {
            Debug.LogError("EndScreen: SceneFader reference is missing. Cannot reload scene.");
        }
    }

    public void MainMenu()
    {
        if (sceneFader != null)
        {
            sceneFader.FadeToScene("TitleScreenScene");
        }
        else
        {
            Debug.LogError("EndScreen: SceneFader reference is missing. Cannot load Main Menu.");
        }
    }
}