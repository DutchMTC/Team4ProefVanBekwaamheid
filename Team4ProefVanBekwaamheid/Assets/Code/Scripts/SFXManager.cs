using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    [Header("Music")]
    public AudioClip mainMenuMusic;
    public AudioClip gameMusic;

    [Header("SFX")]
    public AudioClip attackUsableSFX;
    public AudioClip attackChargedSFX;
    public AudioClip attackSuperchargedSFX;
    public AudioClip idleSFX;
    public AudioClip dashSFX;
    public AudioClip dashStopSFX;
    public AudioClip deathSFX;
    public AudioClip defenseSFX;
    public AudioClip trapThrowSFX;
    public AudioClip entranceSFX;
    public AudioClip stuckSFX;
    public AudioClip damageSFX;
    public AudioClip startSFX;
    public AudioClip matchSFX;
    public AudioClip usePowerUpSFX;
    public AudioClip powerUpChargeSFX;
    public AudioClip powerUpNextLevelReachedSFX;

    private AudioSource musicSource;
    private AudioSource sfxSource;

    public enum ActionType
    {
        AttackUsable,
        AttackCharged,
        AttackSupercharged,
        Idle,
        Dash,
        DashStop,
        Death,
        Defense,
        TrapThrow,
        Entrance,
        Stuck,
        Damage,
        Start,
        Match,
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
            sfxSource = gameObject.AddComponent<AudioSource>();

            musicSource.loop = true;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayMainMenuMusic()
    {
        PlayMusic(mainMenuMusic);
    }

    public void PlayGameMusic()
    {
        PlayMusic(gameMusic);
    }

    private void PlayMusic(AudioClip clip)
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
            case ActionType.Death:
                clipToPlay = deathSFX;
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
            case ActionType.Match:
                clipToPlay = matchSFX;
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
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }
}