using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Music")]
    [SerializeField] private AudioSource musicSourceA;
    [SerializeField] private AudioSource musicSourceB;

    [SerializeField] private AudioClip introMusic;
    [SerializeField] private AudioClip sessionMusic;

    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.6f;

    [Header("Intro Music Feel")]
    [SerializeField] private bool playIntroMusicOnStart = true;
    [SerializeField, Min(0f)] private float introFadeInDuration = 2f;
    

    [Header("Scene Volume")]
    [SerializeField, Range(0f, 1f)] private float mainSceneIntroVolumeMultiplier = 0.75f;
    [SerializeField, Min(0f)] private float sceneVolumeFadeDuration = 1f;

    [Header("Session Music")]
    [SerializeField, Min(0f)] private float sessionCrossfadeDuration = 1.25f;
    [SerializeField, Min(0f)] private float sessionFadeOutDuration = 1f;

    [Header("Music Settings")]
    [SerializeField, Min(0f)]
    private float musicSettingsFadeDuration = 0.3f;

    [SerializeField, Min(0f)]
    private float enemyDefeatFadeOutDuration = 0.25f;

    [Header("SFX")]
    [SerializeField, Min(1)] private int sfxPoolSize = 8;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

    [Header("SFX Library")]
    [SerializeField] private AudioSfxLibrarySO sfxLibrary;
    
    private MusicPlayer musicPlayer;
    private SfxPlayerPool sfxPlayer;

    private Coroutine musicRoutine;
    private bool hasEnteredMainScene;

    public bool IsMusicEnabled =>
    musicPlayer != null &&
    musicPlayer.IsEnabled;

    public bool IsSfxEnabled =>
    sfxPlayer != null && sfxPlayer.IsEnabled;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        IOSAudioSession.EnablePlaybackInSilentMode();

        musicPlayer = new MusicPlayer(
            owner: gameObject,
            sourceA: musicSourceA,
            sourceB: musicSourceB,
            defaultVolume: musicVolume
        );

        sfxPlayer = new SfxPlayerPool(
            owner: transform,
            poolSize: sfxPoolSize,
            defaultVolume: sfxVolume
        );

        if (playIntroMusicOnStart && introMusic != null)
        {
            StartMusicRoutine(musicPlayer.FadeIn(
                clip: introMusic,
                duration: introFadeInDuration,
                loop: true
            ));
        }

        sfxLibrary?.Warmup();
        sfxLibrary?.Preload(SfxCue.ButtonClick);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (hasEnteredMainScene)
            return;

        hasEnteredMainScene = true;

        // When Splash -> Main finishes, soften the intro music volume a bit.
        StartMusicRoutine(musicPlayer.FadeVolumeMultiplier(
            targetMultiplier: mainSceneIntroVolumeMultiplier,
            duration: sceneVolumeFadeDuration
        ));
    }

    public void PlaySessionMusic()
    {
        if (sessionMusic == null)
            return;

        StartMusicRoutine(musicPlayer.CrossfadeTo(
            clip: sessionMusic,
            duration: sessionCrossfadeDuration,
            loop: true,
            volumeMultiplier: 1f
        ));
    }

    public void StopSessionMusic()
    {
        StartMusicRoutine(musicPlayer.FadeOutAll(sessionFadeOutDuration));
    }

    public void PlayIntroMusic()
    {
        if (introMusic == null)
            return;

        StartMusicRoutine(musicPlayer.CrossfadeTo(
            clip: introMusic,
            duration: sessionCrossfadeDuration,
            loop: true,
            volumeMultiplier: mainSceneIntroVolumeMultiplier
        ));
    }

    public void SetMusicEnabled(bool enabled)
    {
        if (musicPlayer == null)
            return;

        StartMusicRoutine(
            musicPlayer.SetEnabled(
                enabled,
                musicSettingsFadeDuration
            )
        );
    }

    private void StartMusicRoutine(IEnumerator routine)
    {
        if (musicRoutine != null)
            StopCoroutine(musicRoutine);

        musicRoutine = StartCoroutine(routine);
    }

    public void SetMusicVolume(float value)
    {
        musicPlayer?.SetVolume(value);
    }

    public void SetMusicMuted(bool muted)
    {
        musicPlayer?.SetMuted(muted);
    }

    public void ToggleMusic()
    {
        musicPlayer?.ToggleMuted();
    }

    public void PlaySfxClip(
        AudioClip clip,
        float volumeMultiplier = 1f)
    {
        sfxPlayer?.Play(clip, volumeMultiplier);
    }

    public void PlaySfxClip(
        AudioClip clip,
        float volumeMultiplier,
        float pitch)
    {
        sfxPlayer?.Play(
            clip,
            volumeMultiplier,
            pitch
        );
    }

    public void PlayRandomEnemyHitSfx()
    {
        if (sfxLibrary == null)
        {
            Debug.LogWarning(
                "[AudioManager] Missing SFX library."
            );

            return;
        }

        if (!sfxLibrary.TryGetRandomEnemyHit(out var entry))
        {
            Debug.LogWarning(
                "[AudioManager] No valid enemy hit SFX found."
            );

            return;
        }

        float pitch = entry.pitch;

        if (entry.randomPitchRange > 0f)
        {
            pitch += Random.Range(
                -entry.randomPitchRange,
                entry.randomPitchRange
            );
        }

        pitch = Mathf.Clamp(pitch, 0.5f, 2f);

        sfxPlayer?.Play(
            entry.clip,
            entry.volume,
            pitch
        );
    }

    public void PlaySfx(
        SfxCue cue,
        float? pitchOverride = null)
    {
        if (sfxLibrary == null)
        {
            Debug.LogWarning(
                "[AudioManager] Missing SFX library."
            );

            return;
        }

        if (!sfxLibrary.TryGet(cue, out var entry))
        {
            Debug.LogWarning(
                $"[AudioManager] SFX cue not found: {cue}"
            );

            return;
        }

        float pitch = pitchOverride ?? entry.pitch;

        if (!pitchOverride.HasValue &&
            entry.randomPitchRange > 0f)
        {
            pitch += Random.Range(
                -entry.randomPitchRange,
                entry.randomPitchRange
            );
        }

        pitch = Mathf.Clamp(pitch, 0.5f, 2f);

        sfxPlayer?.Play(
            entry.clip,
            entry.volume,
            pitch
        );
    }

    public void PlayRandomMergeSfx()
    {
        if (sfxLibrary == null)
        {
            Debug.LogWarning("[AudioManager] Missing SFX library.");
            return;
        }

        if (!sfxLibrary.TryGetRandomMerge(out var entry))
        {
            Debug.LogWarning("[AudioManager] No valid merge SFX found.");
            return;
        }

        float pitch = entry.pitch;

        if (entry.randomPitchRange > 0f)
        {
            pitch += Random.Range(
                -entry.randomPitchRange,
                entry.randomPitchRange
            );
        }

        pitch = Mathf.Clamp(pitch, 0.5f, 2f);

        sfxPlayer?.Play(
            entry.clip,
            entry.volume,
            pitch
        );
    }

    public void PlayRandomMergeBlockedSfx(float impactVolume = 1f)
    {
        if (sfxLibrary == null)
        {
            Debug.LogWarning("[AudioManager] Missing SFX library.");
            return;
        }

        if (!sfxLibrary.TryGetRandomMergeBlocked(out var entry))
        {
            Debug.LogWarning("[AudioManager] No valid blocked merge SFX found.");
            return;
        }

        float pitch = entry.pitch;

        if (entry.randomPitchRange > 0f)
        {
            pitch += Random.Range(
                -entry.randomPitchRange,
                entry.randomPitchRange
            );
        }

        pitch = Mathf.Clamp(pitch, 0.5f, 2f);

        float finalVolume =
            entry.volume * Mathf.Clamp01(impactVolume);

        sfxPlayer?.Play(
            entry.clip,
            finalVolume,
            pitch
        );
    }

    public void PlayEnemyDefeatSequence()
    {
        StartMusicRoutine(EnemyDefeatRoutine());
    }

    private IEnumerator EnemyDefeatRoutine()
    {
        StartCoroutine(
            musicPlayer.FadeOutAll(enemyDefeatFadeOutDuration)
        );

        yield return new WaitForSecondsRealtime(
            enemyDefeatFadeOutDuration * 0.6f
        );

        PlaySfx(
            SfxCue.Enemy_Defeat,
            pitchOverride: 1f
        );
    }

    public void PlayRandomPopCollectSfx()
    {
        if (sfxLibrary == null)
        {
            Debug.LogWarning(
                "[AudioManager] Missing SFX library."
            );

            return;
        }

        if (!sfxLibrary.TryGetRandomPopCollect(out var entry))
        {
            Debug.LogWarning(
                "[AudioManager] No valid pop collect SFX found."
            );

            return;
        }

        float pitch = entry.pitch;

        if (entry.randomPitchRange > 0f)
        {
            pitch += Random.Range(
                -entry.randomPitchRange,
                entry.randomPitchRange
            );
        }

        pitch = Mathf.Clamp(pitch, 0.5f, 2f);

        sfxPlayer?.Play(
            entry.clip,
            entry.volume,
            pitch
        );
    }

    public void PlayRandomWooshSfx()
    {
        if (sfxLibrary == null)
        {
            Debug.LogWarning(
                "[AudioManager] Missing SFX library."
            );

            return;
        }

        if (!sfxLibrary.TryGetRandomWoosh(out var entry))
        {
            Debug.LogWarning(
                "[AudioManager] No valid woosh SFX found."
            );

            return;
        }

        float pitch = entry.pitch;

        if (entry.randomPitchRange > 0f)
        {
            pitch += Random.Range(
                -entry.randomPitchRange,
                entry.randomPitchRange
            );
        }

        pitch = Mathf.Clamp(
            pitch,
            0.5f,
            2f
        );

        sfxPlayer?.Play(
            entry.clip,
            entry.volume,
            pitch
        );
    }

    public void SetSfxVolume(float value)
    {
        sfxPlayer?.SetVolume(value);
    }

    public void SetSfxEnabled(bool enabled)
    {
        sfxPlayer?.SetEnabled(enabled);
    }

    public void SetSfxMuted(bool muted)
    {
        sfxPlayer?.SetMuted(muted);
    }

    public void ToggleSfx()
    {
        sfxPlayer?.ToggleMuted();
    }
}