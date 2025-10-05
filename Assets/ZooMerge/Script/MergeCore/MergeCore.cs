using UnityEngine;

/// <summary>
/// Pure merge logic: only merges when SAME TYPE and SAME LEVEL,
/// despawns both, and spawns the next level (same type) at the given position.
/// </summary>
public class MergeCore
{
    private readonly IBallFactory factory;
    public MergeCore(IBallFactory factory) => this.factory = factory;

    // Midpoint helper
    public bool TryMerge(BallInfo a, BallInfo b)
    {
        if (!CanMerge(a, b)) return false;

        var mid = (a.transform.position + b.transform.position) * 0.5f;
        return TryMergeAt(a, b, new Vector3(mid.x, mid.y, mid.z));
    }

    // Explicit spawn position (e.g., collision contact)
    public bool TryMergeAt(BallInfo a, BallInfo b, Vector3 spawnPos)
    {
        if (!CanMerge(a, b)) return false;

        a.BeginMerge();
        b.BeginMerge();

        var rootA = GetInstanceRoot(a);
        var rootB = GetInstanceRoot(b);

        var type = a.Type;
        var nextLevel = a.Level + 1;

        factory.Despawn(rootA);
        factory.Despawn(rootB);

        var merged = factory.SpawnLevel(type, nextLevel, spawnPos);

        if (merged != null)
        {
            var cdc = merged.GetComponentInChildren<CircleDropController>(true);
            if (cdc != null) cdc.PlayIntroMerged();

            // 🔹 Raise the merge event for game over logic
            var ballInfo = merged.GetComponentInChildren<BallInfo>(true);
            if (ballInfo != null)
            {
                BallEventManager.RaiseBallMerged(ballInfo);
            }
        }

        return true;
    }

    // --- Helpers ---

    private static bool CanMerge(BallInfo a, BallInfo b)
    {
        if (a == null || b == null || a == b) return false;
        if (!a.IsMergeReady || !b.IsMergeReady) return false;
        if (a.IsMerging || b.IsMerging) return false;
        if (a.Level != b.Level) return false;
        if (a.Type != b.Type) return false;   // <-- type gate
        return true;
    }

    private static GameObject GetInstanceRoot(BallInfo info)
    {
        var cdc = info.GetComponentInParent<CircleDropController>(true);
        return cdc != null ? cdc.gameObject : info.gameObject; // fallback
    }
}
