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
    }

    public void MainMenu()
    {
        if (sceneFader != null)
        {
            sceneFader.FadeToScene("TitleScreenScene");
        }
    }
}