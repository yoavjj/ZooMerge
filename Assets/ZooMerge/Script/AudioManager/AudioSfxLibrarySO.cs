using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    menuName = "ZooMerge/Audio/SFX Library",
    fileName = "AudioSfxLibrary"
)]
public class AudioSfxLibrarySO : ScriptableObject
{
    [Serializable]
    public class SfxEntry
    {
        public SfxCue cue;
        public AudioClip clip;

        [Range(0f, 1f)]
        public float volume = 1f;

        [Range(0.5f, 2f)]
        public float pitch = 1f;

        [Range(0f, 0.5f)]
        public float randomPitchRange;
    }

    [Serializable]
    public class MergeSfxEntry
    {
        public SfxMerge cue;
        public AudioClip clip;

        [Range(0f, 1f)]
        public float volume = 1f;

        [Range(0.5f, 2f)]
        public float pitch = 1f;

        [Range(0f, 0.5f)]
        public float randomPitchRange;
    }

    [Serializable]
    public class MergeBlockedSfxEntry
    {
        public SfxMergeBlocked cue;
        public AudioClip clip;

        [Range(0f, 1f)]
        public float volume = 1f;

        [Range(0.5f, 2f)]
        public float pitch = 1f;

        [Range(0f, 0.5f)]
        public float randomPitchRange;
    }

    [Serializable]
    public class EnemyHitSfxEntry
    {
        public SfxEnemyHit cue;
        public AudioClip clip;

        [Range(0f, 1f)]
        public float volume = 1f;

        [Range(0.5f, 2f)]
        public float pitch = 1f;

        [Range(0f, 0.5f)]
        public float randomPitchRange;
    }

    [Serializable]
    public class PopCollectSfxEntry
    {
        public SfxPopCollect cue;
        public AudioClip clip;

        [Range(0f, 1f)]
        public float volume = 1f;

        [Range(0.5f, 2f)]
        public float pitch = 1f;

        [Range(0f, 0.5f)]
        public float randomPitchRange;
    }

    [Serializable]
    public class WooshSfxEntry
    {
        public SfxWoosh cue;
        public AudioClip clip;

        [Range(0f, 1f)]
        public float volume = 1f;

        [Range(0.5f, 2f)]
        public float pitch = 1f;

        [Range(0f, 0.5f)]
        public float randomPitchRange;
    }

    [Header("Normal Sound Effects")]
    [SerializeField] private List<SfxEntry> entries = new();

    [Header("Random Merge Sound Effects")]
    [SerializeField] private List<MergeSfxEntry> mergeEntries = new();

    private Dictionary<SfxCue, SfxEntry> lookup;
    private readonly List<MergeSfxEntry> validMergeEntries = new();

    public IReadOnlyList<SfxEntry> Entries => entries;
    public IReadOnlyList<MergeSfxEntry> MergeEntries => mergeEntries;

    [Header("Random Merge Blocked Sound Effects")]
    [SerializeField]
    private List<MergeBlockedSfxEntry> mergeBlockedEntries = new();

    private readonly List<MergeBlockedSfxEntry> validMergeBlockedEntries = new();
    private bool mergeBlockedCacheBuilt;

    public IReadOnlyList<MergeBlockedSfxEntry> MergeBlockedEntries =>
    mergeBlockedEntries;

    [Header("Random Enemy Hit Sound Effects")]
    [SerializeField]
    private List<EnemyHitSfxEntry> enemyHitEntries = new();

    private readonly List<EnemyHitSfxEntry> validEnemyHitEntries = new();
    private bool enemyHitCacheBuilt;

    public IReadOnlyList<EnemyHitSfxEntry> EnemyHitEntries =>
        enemyHitEntries;

    [Header("Random Pop Collect Sound Effects")]
    [SerializeField]
    private List<PopCollectSfxEntry> popCollectEntries = new();

    private readonly List<PopCollectSfxEntry> validPopCollectEntries = new();
    private bool popCollectCacheBuilt;

    public IReadOnlyList<PopCollectSfxEntry> PopCollectEntries =>
        popCollectEntries;

    
    [Header("Random Woosh Sound Effects")]
    [SerializeField]
    private List<WooshSfxEntry> wooshEntries = new();

    private readonly List<WooshSfxEntry> validWooshEntries = new();
    private bool wooshCacheBuilt;

    public IReadOnlyList<WooshSfxEntry> WooshEntries =>
        wooshEntries;


    public void Warmup()
    {
        BuildLookupIfNeeded();
        BuildMergeLookupIfNeeded();
        BuildMergeBlockedCacheIfNeeded();
        BuildEnemyHitCacheIfNeeded();
        BuildPopCollectCacheIfNeeded();
        BuildWooshCacheIfNeeded();
    }

    public bool TryGetRandomWoosh(
    out WooshSfxEntry entry)
    {
        BuildWooshCacheIfNeeded();

        if (validWooshEntries.Count == 0)
        {
            entry = null;
            return false;
        }

        int index = UnityEngine.Random.Range(
            0,
            validWooshEntries.Count
        );

        entry = validWooshEntries[index];
        return true;
    }

    private void BuildWooshCacheIfNeeded()
    {
        if (wooshCacheBuilt)
            return;

        wooshCacheBuilt = true;
        validWooshEntries.Clear();

        foreach (var entry in wooshEntries)
        {
            if (entry == null || entry.clip == null)
                continue;

            validWooshEntries.Add(entry);
        }
    }

    public void Preload(SfxCue cue)
    {
        if (!TryGet(cue, out SfxEntry entry))
            return;

        if (entry.clip == null)
            return;

        entry.clip.LoadAudioData();
    }

    public bool TryGetRandomMergeBlocked(
    out MergeBlockedSfxEntry entry)
    {
        BuildMergeBlockedCacheIfNeeded();

        if (validMergeBlockedEntries.Count == 0)
        {
            entry = null;
            return false;
        }

        int index = UnityEngine.Random.Range(
            0,
            validMergeBlockedEntries.Count
        );

        entry = validMergeBlockedEntries[index];
        return true;
    }

    private void BuildMergeBlockedCacheIfNeeded()
    {
        if (mergeBlockedCacheBuilt)
            return;

        mergeBlockedCacheBuilt = true;
        validMergeBlockedEntries.Clear();

        foreach (var entry in mergeBlockedEntries)
        {
            if (entry == null || entry.clip == null)
                continue;

            validMergeBlockedEntries.Add(entry);
        }
    }

    public bool TryGet(SfxCue cue, out SfxEntry entry)
    {
        BuildLookupIfNeeded();
        return lookup.TryGetValue(cue, out entry);
    }

    public bool TryGetRandomMerge(out MergeSfxEntry entry)
    {
        BuildMergeLookupIfNeeded();

        if (validMergeEntries.Count == 0)
        {
            entry = null;
            return false;
        }

        int index = UnityEngine.Random.Range(0, validMergeEntries.Count);
        entry = validMergeEntries[index];
        return true;
    }

    public bool TryGetRandomEnemyHit(out EnemyHitSfxEntry entry)
    {
        BuildEnemyHitCacheIfNeeded();

        if (validEnemyHitEntries.Count == 0)
        {
            entry = null;
            return false;
        }

        int index = UnityEngine.Random.Range(
            0,
            validEnemyHitEntries.Count
        );

        entry = validEnemyHitEntries[index];
        return true;
    }

    public bool TryGetRandomPopCollect(
    out PopCollectSfxEntry entry)
    {
        BuildPopCollectCacheIfNeeded();

        if (validPopCollectEntries.Count == 0)
        {
            entry = null;
            return false;
        }

        int index = UnityEngine.Random.Range(
            0,
            validPopCollectEntries.Count
        );

        entry = validPopCollectEntries[index];
        return true;
    }

    private void BuildPopCollectCacheIfNeeded()
    {
        if (popCollectCacheBuilt)
            return;

        popCollectCacheBuilt = true;
        validPopCollectEntries.Clear();

        foreach (var entry in popCollectEntries)
        {
            if (entry == null || entry.clip == null)
                continue;

            validPopCollectEntries.Add(entry);
        }
    }

    private void BuildLookupIfNeeded()
    {
        if (lookup != null)
            return;

        lookup = new Dictionary<SfxCue, SfxEntry>();

        foreach (var entry in entries)
        {
            if (entry == null || entry.clip == null)
                continue;

            lookup[entry.cue] = entry;
        }
    }

    private void BuildMergeLookupIfNeeded()
    {
        if (validMergeEntries.Count > 0)
            return;

        foreach (var entry in mergeEntries)
        {
            if (entry == null || entry.clip == null)
                continue;

            validMergeEntries.Add(entry);
        }
    }

    private void BuildEnemyHitCacheIfNeeded()
    {
        if (enemyHitCacheBuilt)
            return;

        enemyHitCacheBuilt = true;
        validEnemyHitEntries.Clear();

        foreach (var entry in enemyHitEntries)
        {
            if (entry == null || entry.clip == null)
                continue;

            validEnemyHitEntries.Add(entry);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        lookup = null;

        validMergeEntries.Clear();

        validMergeBlockedEntries.Clear();
        mergeBlockedCacheBuilt = false;

        validEnemyHitEntries.Clear();
        enemyHitCacheBuilt = false;

        validPopCollectEntries.Clear();
        popCollectCacheBuilt = false;

        validWooshEntries.Clear();
        wooshCacheBuilt = false;
    }
#endif
}