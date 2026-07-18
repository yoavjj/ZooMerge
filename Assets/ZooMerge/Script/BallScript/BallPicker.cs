using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class BallPicker : MonoBehaviour
{
    [Header("Catalog")]
    [SerializeField] private BallSet ballSet;

    [Header("Random Range (Levels)")]
    [SerializeField] private int minLevel = 0;
    [SerializeField] private int maxLevel = 1;

    /// <summary>
    /// Returns true when this entry is currently allowed to spawn.
    /// </summary>
    public bool IsEntryAllowed(BallSet.Entry entry)
    {
        if (entry == null)
            return false;

        if (entry.prefab == null)
            return false;

        if (!entry.includeInRandom)
            return false;

        if (entry.level < minLevel || entry.level > maxLevel)
            return false;

        BallUnlockManager unlockManager =
            BallUnlockManager.Instance;

        if (unlockManager == null)
        {
            Debug.LogWarning(
                "[BallPicker] BallUnlockManager.Instance is null."
            );

            return false;
        }

        if (!unlockManager.IsUnlocked(entry.type))
            return false;

        BallSelectionManager selectionManager =
            BallSelectionManager.Instance;

        if (selectionManager == null)
        {
            Debug.LogWarning(
                "[BallPicker] BallSelectionManager.Instance is null."
            );

            return false;
        }

        return selectionManager.IsSelected(entry.type);
    }

    public bool TryPickRandomEntry(
        out BallSet.Entry entry,
        out string reason)
    {
        entry = null;
        reason = string.Empty;

        if (ballSet == null)
        {
            reason = "BallSet is not assigned.";
            return false;
        }

        if (ballSet.entries == null || ballSet.entries.Count == 0)
        {
            reason = "BallSet contains no entries.";
            return false;
        }

        BallSelectionManager selectionManager =
            BallSelectionManager.Instance;

        if (selectionManager == null)
        {
            reason = "BallSelectionManager.Instance is null.";
            return false;
        }

        if (!selectionManager.HasRequiredSelection)
        {
            reason =
                $"Required selection is incomplete. " +
                $"Selected {selectionManager.SelectedCount}/" +
                $"{selectionManager.RequiredSelectionCount}.";

            return false;
        }

        List<BallSet.Entry> pool = BuildValidPool();

        if (pool.Count == 0)
        {
            reason =
                $"No spawnable entries for the selected ball types " +
                $"in level range [{minLevel}..{maxLevel}].";

            return false;
        }

        entry = pool[Random.Range(0, pool.Count)];
        return true;
    }

    public bool TryPickRandom(
        out AssetReferenceGameObject prefab,
        out string reason)
    {
        prefab = null;

        if (!TryPickRandomEntry(out BallSet.Entry entry, out reason))
            return false;

        prefab = entry.prefab;
        return prefab != null;
    }

    public AssetReferenceGameObject PickRandom()
    {
        return TryPickRandom(
            out AssetReferenceGameObject prefab,
            out _
        )
            ? prefab
            : null;
    }

    private List<BallSet.Entry> BuildValidPool()
    {
        var pool = new List<BallSet.Entry>();

        if (ballSet == null || ballSet.entries == null)
            return pool;

        foreach (BallSet.Entry entry in ballSet.entries)
        {
            if (IsEntryAllowed(entry))
                pool.Add(entry);
        }

        return pool;
    }

    public float GetScaleForEntry(BallSet.Entry entry)
    {
        if (entry == null || ballSet == null)
        {
            Debug.LogWarning(
                "[BallPicker] GetScaleForEntry: entry or BallSet is null."
            );

            return 1f;
        }

        BallSet.BallPhysicsData data =
            ballSet.GetPhysicsFor(entry);

        if (data == null)
        {
            Debug.LogWarning(
                $"[BallPicker] No physics data for " +
                $"{entry.type}, level {entry.level}."
            );

            return 1f;
        }

        return data.uniformScale > 0f
            ? data.uniformScale
            : 1f;
    }
}