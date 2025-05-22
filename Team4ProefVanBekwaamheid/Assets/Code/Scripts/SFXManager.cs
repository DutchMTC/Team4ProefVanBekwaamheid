using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    [Header("Music")]
    public AudioClip mainMenuMusic;
    public AudioClip matchPhaseMusic;
    public AudioClip battlePhaseMusic;

    [Header("SFX")]
    public AudioClip winSFX;
    public AudioClip gameOverSFX; 
    public AudioClip enemyDeathSFX;
    public AudioClip playerDeathSFX;
    public AudioClip attackUsableSFX;
    public AudioClip attackChargedSFX;
    public AudioClip attackSuperchargedSFX;
    public AudioClip dashSFX;
    public AudioClip defenseSFX;
    public AudioClip trapThrowSFX;
    public AudioClip stuckSFX;
    public AudioClip damageSFX;
    public AudioClip match3SFX;
    public AudioClip match4SFX;
    public AudioClip match5PlusSFX;
    public AudioClip usePowerUpSFX;
    public AudioClip powerUpChargeSFX;
    public AudioClip powerUpNextLevelReachedSFX;
    public AudioClip playGameSFX; 
    public AudioClip pickupArmorSFX; 
    public AudioClip pickupHealSFX;

    private AudioSource musicSource;

    public enum ActionType
    {
        AttackUsable,
        AttackCharged,
        AttackSupercharged,
        Idle,
        Dash,
        PlayerDeath,
        EnemyDeath,
        Win,        
        GameOver,    
        Defense,
        TrapThrow,
        Stuck,
        Damage,
        Match3,
        Match4,
        Match5Plus,
        UsePowerUp,
        PowerUpCharge,
        PowerUpNextLevelReached,
        PlayGame, 
        PickupArmor, 
        PickupHeal 
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            musicSource = gameObject.AddComponent<AudioSource>();
            // sfxSource = gameObject.AddComponent<AudioSource>(); // Removed

            musicSource.loop = true;

            // Music based on scene name for main menu
            string currentSceneName = SceneManager.GetActiveScene().name;
            if (currentSceneName == "TitleScreenScene")
            {
                PlayMusic(mainMenuMusic); // Changed from PlayMainMenuMusic()
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Removed PlayMainMenuMusic, PlayMatchPhaseMusic, PlayBattlePhaseMusic
    // Removed PlayEnemyDeathAudio, PlayPlayerDeathAudio (will be handled by PlayActionSFX)

    public void PlayMusic(AudioClip clip) // Made public (was private, but effectively public via wrappers)
    {
        if (musicSource.clip == clip) return;
        musicSource.clip = clip;
        if (clip != null)
        {
            musicSource.Play();
        }
        else
        {
            musicSource.Stop();
        }
    }

    public IEnumerator FadeMusicVolume(float targetVolume, float duration)
    {
        float startVolume = musicSource.volume;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, targetVolume, Mathf.Clamp01(elapsedTime / duration));
            yield return null;
        }
        musicSource.volume = targetVolume;
    }

    public void PlayActionSFX(ActionType actionType)
    {
        AudioClip clipToPlay = null;
        switch (actionType)
        {
            case ActionType.AttackUsable:
                clipToPlay = attackUsableSFX;
                break;
            case ActionType.AttackCharged:
                clipToPlay = attackChargedSFX;
                break;
            case ActionType.AttackSupercharged:
                clipToPlay = attackSuperchargedSFX;
                break;
            case ActionType.Dash:
                clipToPlay = dashSFX;
                break;
            case ActionType.PlayerDeath:
                clipToPlay = playerDeathSFX;
                break;
            case ActionType.EnemyDeath:
                clipToPlay = enemyDeathSFX;
                break;
            case ActionType.Win:
                clipToPlay = winSFX;
                break;
            case ActionType.GameOver:
                clipToPlay = gameOverSFX;
                break;
            case ActionType.Defense:
                clipToPlay = defenseSFX;
                break;
            case ActionType.TrapThrow:
                clipToPlay = trapThrowSFX;
                break;
            case ActionType.Stuck:
                clipToPlay = stuckSFX;
                break;
            case ActionType.Damage:
                clipToPlay = damageSFX;
                break;
            case ActionType.Match3:
                clipToPlay = match3SFX;
                break;
            case ActionType.Match4:
                clipToPlay = match4SFX;
                break;
            case ActionType.Match5Plus:
                clipToPlay = match5PlusSFX;
                break;
            case ActionType.UsePowerUp:
                clipToPlay = usePowerUpSFX;
                break;
            case ActionType.PowerUpCharge:
                clipToPlay = powerUpChargeSFX;
                break;
            case ActionType.PowerUpNextLevelReached:
                clipToPlay = powerUpNextLevelReachedSFX;
                break;
            case ActionType.PlayGame:
                clipToPlay = playGameSFX;
                break;
            case ActionType.PickupArmor:
                clipToPlay = pickupArmorSFX;
                break;
            case ActionType.PickupHeal:
                clipToPlay = pickupHealSFX;
                break;
        }
        PlaySFX(clipToPlay);
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;

        GameObject sfxHost = new GameObject("SFX_" + clip.name);
        sfxHost.transform.SetParent(transform);
        AudioSource audioSource = sfxHost.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.Play();
        Destroy(sfxHost, clip.length + 0.1f);
    }
}