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
    public AudioClip winSFX; // Added back
    public AudioClip gameOverSFX; // Added back
    public AudioClip enemyDeathSFX;
    public AudioClip playerDeathSFX;
    public AudioClip attackUsableSFX;
    public AudioClip attackChargedSFX;
    public AudioClip attackSuperchargedSFX;
    public AudioClip idleSFX;
    public AudioClip dashSFX;
    public AudioClip dashStopSFX;
    // public AudioClip deathSFX; // Removed generic deathSFX
    public AudioClip defenseSFX;
    public AudioClip trapThrowSFX;
    public AudioClip entranceSFX;
    public AudioClip stuckSFX;
    public AudioClip damageSFX;
    public AudioClip startSFX;
    // public AudioClip matchSFX; // Replaced by specific match SFXs
    public AudioClip match3SFX;
    public AudioClip match4SFX;
    public AudioClip match5PlusSFX;
    public AudioClip usePowerUpSFX;
    public AudioClip powerUpChargeSFX;
    public AudioClip powerUpNextLevelReachedSFX;

    private AudioSource musicSource;
    // private AudioSource sfxSource; // Removed: SFX will use temporary AudioSources

    public enum ActionType
    {
        AttackUsable,
        AttackCharged,
        AttackSupercharged,
        Idle,
        Dash,
        DashStop,
        PlayerDeath,
        EnemyDeath,
        Win,         // Added
        GameOver,    // Added
        Defense,
        TrapThrow,
        Entrance,
        Stuck,
        Damage,
        Start,
        // Match, // Replaced by specific match SFXs
        Match3,
        Match4,
        Match5Plus,
        UsePowerUp,
        PowerUpCharge,
        PowerUpNextLevelReached
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
            case ActionType.Idle:
                clipToPlay = idleSFX;
                break;
            case ActionType.Dash:
                clipToPlay = dashSFX;
                break;
            case ActionType.DashStop:
                clipToPlay = dashStopSFX;
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
            case ActionType.Entrance:
                clipToPlay = entranceSFX;
                break;
            case ActionType.Stuck:
                clipToPlay = stuckSFX;
                break;
            case ActionType.Damage:
                clipToPlay = damageSFX;
                break;
            case ActionType.Start:
                clipToPlay = startSFX;
                break;
            // case ActionType.Match: // Replaced by specific match SFXs
            //     clipToPlay = matchSFX;
            //     break;
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