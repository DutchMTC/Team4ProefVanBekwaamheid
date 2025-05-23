using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuAnimationController : MonoBehaviour
{
    [Header("Animators")]
    [SerializeField] private Animator _playerAnimator;
    [SerializeField] private Animator _enemyAnimator;
    [SerializeField] private Animator _buttonAnimator;
    [SerializeField] private Animator _cameraAnimator;
    
    [Header("Scene Fading")]
    [SerializeField] private SceneFader _sceneFader; // Assign this in the Inspector


    // Start is called before the first frame update
    void Start()
    {
        // Fallback if not assigned in Inspector - ensure a SceneFader is in this scene
        if (_sceneFader == null)
        {
            _sceneFader = FindObjectOfType<SceneFader>();
        }
    }

    public void PlayerStart()
    {
        if (_playerAnimator != null)
        {
            _playerAnimator.SetTrigger("Start");
        }
    }

    public void EnemyStart()
    {
        if (_enemyAnimator != null)
        {
            _enemyAnimator.SetTrigger("Start");
        }
    }

    public void ButtonStart()
    {
        if (_buttonAnimator != null)
        {
            _buttonAnimator.SetTrigger("Start");
        }
    }

    public void StartGame()
    {
        if (SFXManager.Instance != null)
        {
            SFXManager.Instance.PlayActionSFX(SFXManager.ActionType.PlayGame);
        }
        if (_cameraAnimator != null)
        {
            _cameraAnimator.SetTrigger("Start");
        }
    }

    public void SwitchToGameScene()
    {
        // Ensure you have a scene named "Game" in your Build Settings
        if (_sceneFader != null)
        {
            _sceneFader.FadeToScene("Game");
        }
    }
}
