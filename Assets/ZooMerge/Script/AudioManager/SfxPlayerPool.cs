using System.Collections.Generic;
using UnityEngine;

public class SfxPlayerPool
{
    private const string KEY_VOLUME = "Audio_SfxVolume";
    private const string KEY_MUTED = "Audio_SfxMuted";

    private readonly List<AudioSource> pool = new();

    private float volume;
    private bool muted;

    public bool IsMuted => muted;
    public bool IsEnabled => !muted;

    public SfxPlayerPool(
        Transform owner,
        int poolSize,
        float defaultVolume)
    {
        volume = PlayerPrefs.GetFloat(KEY_VOLUME, defaultVolume);
        muted = PlayerPrefs.GetInt(KEY_MUTED, 0) == 1;

        poolSize = Mathf.Max(1, poolSize);

        for (int i = 0; i < poolSize; i++)
        {
            GameObject sourceObject = new GameObject($"SFX_Source_{i}");
            sourceObject.transform.SetParent(owner, false);

            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;

            pool.Add(source);
        }
    }

    public void Play(AudioClip clip, float volumeMultiplier = 1f)
    {
        Play(clip, volumeMultiplier, 1f);
    }

    public void Play(AudioClip clip, float volumeMultiplier, float pitch)
    {
        if (clip == null || muted)
            return;

        AudioSource source = GetFreeSource();
        if (source == null)
            return;

        source.pitch = Mathf.Clamp(pitch, 0.5f, 2f);
        source.volume = volume * Mathf.Clamp01(volumeMultiplier);
        source.PlayOneShot(clip);
    }

    private AudioSource GetFreeSource()
    {
        for (int i = pool.Count - 1; i >= 0; i--)
        {
            AudioSource source = pool[i];

            if (source == null)
            {
                pool.RemoveAt(i);
                continue;
            }

            if (!source.isPlaying)
                return source;
        }

        return pool.Count > 0 ? pool[0] : null;
    }

    public void SetVolume(float value)
    {
        volume = Mathf.Clamp01(value);

        PlayerPrefs.SetFloat(KEY_VOLUME, volume);
        PlayerPrefs.Save();
    }

    public void SetMuted(bool value)
    {
        muted = value;

        PlayerPrefs.SetInt(KEY_MUTED, muted ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetEnabled(bool enabled)
    {
        SetMuted(!enabled);
    }

    public void ToggleMuted()
    {
        SetMuted(!muted);
    }
}