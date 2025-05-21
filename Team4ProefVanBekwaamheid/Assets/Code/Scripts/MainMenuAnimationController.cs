using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuAnimationController : MonoBehaviour
{
    [Header("Animators")]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private Animator enemyAnimator;
    [SerializeField] private Animator buttonAnimator;
    [SerializeField] private Animator cameraAnimator;
    
    [Header("Scene Fading")]
    [SerializeField] private SceneFader sceneFader; // Assign this in the Inspector


    // Start is called before the first frame update
    void Start()
    {
        // Fallback if not assigned in Inspector - ensure a SceneFader is in this scene
        if (sceneFader == null)
        {
            sceneFader = FindObjectOfType<SceneFader>();
            if (sceneFader == null)
            {
                Debug.LogError("MainMenuAnimationController: SceneFader not found in the scene. Please add a SceneFader component to a GameObject in this scene and assign it, or ensure one is present.");
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void PlayerStart()
    {
        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger("Start");
        }
        else
        {
            Debug.LogWarning("Player Animator is not assigned in MainMenuAnimationController.");
        }
    }

    public void EnemyStart()
    {
        if (enemyAnimator != null)
        {
            enemyAnimator.SetTrigger("Start");
        }
        else
        {
            Debug.LogWarning("Enemy Animator is not assigned in MainMenuAnimationController.");
        }
    }

    public void ButtonStart()
    {
        if (buttonAnimator != null)
        {
            buttonAnimator.SetTrigger("Start");
        }
        else
        {
            Debug.LogWarning("Button Animator is not assigned in MainMenuAnimationController.");
        }
    }

    public void StartGame()
    {
        Debug.Log("registerd"); // Reverted to original, including typo if it was there
        if (cameraAnimator != null)
        {
            cameraAnimator.SetTrigger("Start");
        }
        else
        {
            Debug.LogWarning("Camera Animator is not assigned in MainMenuAnimationController.");
        }
    }

    public void SwitchToGameScene()
    {
        // Ensure you have a scene named "Game" in your Build Settings
        if (sceneFader != null)
        {
            sceneFader.FadeToScene("Game");
        }
        else
        {
            Debug.LogError("MainMenuAnimationController: Cannot switch scene, SceneFader reference is not set.");
            // As a fallback, you might want to load directly if fader is missing:
            // UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
        }
    }
}
