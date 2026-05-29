using UnityEngine;

public sealed class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("References (assign in Inspector)")]
    [SerializeField] private AudioSource musicSource;

    [Header("Music")]
    [SerializeField] private AudioClip splashAndMenuMusic;
    [SerializeField, Range(0f, 1f)] private float volume = 0.7f;
    [SerializeField] private bool playOnAwake = true;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Validate inspector wiring
        if (musicSource == null)
        {
            Debug.LogError("[MusicManager] musicSource is not assigned. Add an AudioSource and drag it in.");
            enabled = false;
            return;
        }

        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.volume = volume;

        if (playOnAwake)
            Play(splashAndMenuMusic);
    }

    public void Play(AudioClip clip)
    {
        if (clip == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.Play();
    }

    public void Stop() => musicSource.Stop();

    public void SetVolume(float v)
    {
        volume = Mathf.Clamp01(v);
        musicSource.volume = volume;
    }
}