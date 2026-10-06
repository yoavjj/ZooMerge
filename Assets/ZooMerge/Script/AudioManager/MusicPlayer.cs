using System.Collections;
using UnityEngine;

public class MusicPlayer
{
    private const string KEY_VOLUME = "Audio_MusicVolume";
    private const string KEY_MUTED = "Audio_MusicMuted";

    private readonly AudioSource sourceA;
    private readonly AudioSource sourceB;

    private AudioSource activeSource;
    private AudioSource inactiveSource;

    private float volume;
    private float volumeMultiplier = 1f;
    private float muteMultiplier;
    private bool muted;
    public bool IsEnabled => !muted;

    private AudioClip prewarmedClip;
    private AudioSource prewarmedSource;

    public IEnumerator PrewarmClip(AudioClip clip)
    {
        if (clip == null)
            yield break;

        // Ask Unity to load the audio data before gameplay.
        if (clip.loadState == AudioDataLoadState.Unloaded)
            clip.LoadAudioData();

        while (clip.loadState == AudioDataLoadState.Loading)
            yield return null;

        if (clip.loadState != AudioDataLoadState.Loaded)
        {
            Debug.LogWarning(
                $"[MusicPlayer] Failed to preload audio clip '{clip.name}'."
            );

            yield break;
        }

        // sourceA is currently playing the intro music.
        // sourceB / inactiveSource is free, so use it to force FMOD
        // to create/open the sound NOW, while still on Splash.
        AudioSource source = inactiveSource;

        source.clip = clip;
        source.loop = true;
        source.volume = 0f;
        source.mute = true;
        source.time = 0f;

        Debug.Log(
            $"[MusicPlayer] Priming audio clip: {clip.name}"
        );

        // This is currently the expensive call.
        // We intentionally make it happen during Splash.
        source.Play();

        // Give Unity/FMOD one update with the sound initialized.
        yield return null;

        // Keep the initialized source around instead of Stop(),
        // so session start can simply resume it.
        source.Pause();

        source.timeSamples = 0;
        source.mute = false;
        source.volume = 0f;

        prewarmedClip = clip;
        prewarmedSource = source;

        Debug.Log(
            $"[MusicPlayer] Audio clip prewarmed: {clip.name}"
        );
    }

    public MusicPlayer(
        GameObject owner,
        AudioSource sourceA,
        AudioSource sourceB,
        float defaultVolume)
    {
        this.sourceA = sourceA != null ? sourceA : owner.AddComponent<AudioSource>();

        // ✅ Music Source B must be different from Source A.
        // If both inspector fields accidentally reference the same AudioSource,
        // create a second one automatically.
        if (sourceB != null && sourceB != this.sourceA)
        {
            this.sourceB = sourceB;
        }
        else
        {
            this.sourceB = owner.AddComponent<AudioSource>();
            Debug.LogWarning("[MusicPlayer] Music Source B was missing or same as Source A. Created a second AudioSource automatically.");
        }

        volume = PlayerPrefs.GetFloat(KEY_VOLUME, defaultVolume);
        muted = PlayerPrefs.GetInt(KEY_MUTED, 0) == 1;

        muteMultiplier = muted ? 0f : 1f;

        SetupSource(this.sourceA);
        SetupSource(this.sourceB);

        activeSource = this.sourceA;
        inactiveSource = this.sourceB;
    }

    private void SetupSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = 0f;
    }

    public IEnumerator FadeIn(AudioClip clip, float duration, bool loop)
    {
        if (clip == null)
            yield break;

        activeSource.clip = clip;
        activeSource.loop = loop;
        activeSource.volume = 0f;
        activeSource.Play();

        float t = 0f;
        float targetVolume = CurrentTargetVolume();

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);

            activeSource.volume = Mathf.Lerp(0f, targetVolume, p);

            yield return null;
        }

        activeSource.volume = targetVolume;
    }

    public IEnumerator CrossfadeTo(
        AudioClip clip,
        float duration,
        bool loop,
        float volumeMultiplier = 1f)
    {
        if (clip == null)
            yield break;

        if (activeSource.clip == clip && activeSource.isPlaying)
            yield break;

        this.volumeMultiplier =
            Mathf.Clamp01(volumeMultiplier);

        bool usePrewarmedSource =
            prewarmedClip == clip &&
            prewarmedSource == inactiveSource;

        if (usePrewarmedSource)
        {
            inactiveSource.loop = loop;
            inactiveSource.volume = 0f;
            inactiveSource.mute = false;

            inactiveSource.UnPause();

            prewarmedClip = null;
            prewarmedSource = null;
        }
        else
        {
            inactiveSource.clip = clip;
            inactiveSource.loop = loop;
            inactiveSource.volume = 0f;

            inactiveSource.Play();
        }

        float fromStartVolume =
            activeSource.volume;

        float toTargetVolume =
            CurrentTargetVolume();

        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;

            float p = duration <= 0f
                ? 1f
                : Mathf.Clamp01(t / duration);

            activeSource.volume =
                Mathf.Lerp(
                    fromStartVolume,
                    0f,
                    p
                );

            inactiveSource.volume =
                Mathf.Lerp(
                    0f,
                    toTargetVolume,
                    p
                );

            yield return null;
        }

        activeSource.Stop();
        activeSource.volume = 0f;

        inactiveSource.volume =
            toTargetVolume;

        SwapSources();
    }

    public IEnumerator FadeVolumeMultiplier(float targetMultiplier, float duration)
    {
        targetMultiplier = Mathf.Clamp01(targetMultiplier);

        float startMultiplier = volumeMultiplier;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);

            volumeMultiplier = Mathf.Lerp(startMultiplier, targetMultiplier, p);
            ApplyVolume();

            yield return null;
        }

        volumeMultiplier = targetMultiplier;
        ApplyVolume();
    }

    private void SwapSources()
    {
        AudioSource oldActive = activeSource;
        activeSource = inactiveSource;
        inactiveSource = oldActive;
    }

    private float CurrentTargetVolume()
    {
        return volume *
               volumeMultiplier *
               muteMultiplier;
    }

    private void ApplyVolume()
    {
        if (activeSource != null && activeSource.isPlaying)
            activeSource.volume = CurrentTargetVolume();
    }

    public void SetVolume(float value)
    {
        volume = Mathf.Clamp01(value);

        PlayerPrefs.SetFloat(KEY_VOLUME, volume);
        PlayerPrefs.Save();

        ApplyVolume();
    }

    public void SetMuted(bool value)
    {
        muted = value;
        muteMultiplier = muted ? 0f : 1f;

        PlayerPrefs.SetInt(
            KEY_MUTED,
            muted ? 1 : 0
        );

        PlayerPrefs.Save();

        ApplyVolume();
    }

    public void ToggleMuted()
    {
        SetMuted(!muted);
    }

    public IEnumerator FadeOutAll(float duration)
    {
        float startVolumeA = sourceA != null ? sourceA.volume : 0f;
        float startVolumeB = sourceB != null ? sourceB.volume : 0f;

        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);

            if (sourceA != null)
                sourceA.volume = Mathf.Lerp(startVolumeA, 0f, p);

            if (sourceB != null)
                sourceB.volume = Mathf.Lerp(startVolumeB, 0f, p);

            yield return null;
        }

        if (sourceA != null)
        {
            sourceA.Stop();
            sourceA.volume = 0f;
        }

        if (sourceB != null)
        {
            sourceB.Stop();
            sourceB.volume = 0f;
        }

        activeSource = sourceA;
        inactiveSource = sourceB;
    }

    public IEnumerator SetEnabled(
    bool enabled,
    float duration)
    {
        bool newMutedState = !enabled;

        muted = newMutedState;

        PlayerPrefs.SetInt(
            KEY_MUTED,
            muted ? 1 : 0
        );

        PlayerPrefs.Save();

        float startMultiplier = muteMultiplier;
        float targetMultiplier = enabled ? 1f : 0f;

        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;

            float p = duration <= 0f
                ? 1f
                : Mathf.Clamp01(t / duration);

            muteMultiplier = Mathf.Lerp(
                startMultiplier,
                targetMultiplier,
                p
            );

            ApplyVolume();

            yield return null;
        }

        muteMultiplier = targetMultiplier;

        ApplyVolume();
    }
}