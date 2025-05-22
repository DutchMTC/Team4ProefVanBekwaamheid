using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndScreen : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image _victoryImage;
    [SerializeField] private Image _gameOverImage;

    private SceneFader sceneFader;

    void Start()
    {
        // Attempt to find the SceneFader instance in the scene
        sceneFader = FindObjectOfType<SceneFader>();
        if (sceneFader == null)
        {
            Debug.LogError("EndScreen: SceneFader not found in the scene. Please ensure a SceneFader object exists and is active.");
        }

        // Ensure images are assigned
        if (_victoryImage == null) Debug.LogError("EndScreen: Victory Image not assigned in Inspector.");
        if (_gameOverImage == null) Debug.LogError("EndScreen: Game Over Image not assigned in Inspector.");
    }

    public void ShowVictoryScreen()
    {
        if (_victoryImage != null) _victoryImage.gameObject.SetActive(true);
        if (_gameOverImage != null) _gameOverImage.gameObject.SetActive(false);
    }

    public void ShowGameOverScreen()
    {
        if (_victoryImage != null) _victoryImage.gameObject.SetActive(false);
        if (_gameOverImage != null) _gameOverImage.gameObject.SetActive(true);
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