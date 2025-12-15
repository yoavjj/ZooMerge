using UnityEngine;

/// <summary>
/// Pure merge logic: only merges when SAME TYPE and SAME LEVEL,
/// despawns both, and spawns the next level (same type) at the given position.
/// </summary>
public class MergeCore
{
    private readonly IBallFactory factory;
    public MergeCore(IBallFactory factory) => this.factory = factory;

    public bool TryMerge(BallInfo a, BallInfo b)
    {
        if (!CanMerge(a, b, out string reason))
        {
            //Debug.LogWarning($"❌ Merge failed: {reason}\n→ A: {FormatBall(a)}\n→ B: {FormatBall(b)}");
            return false;
        }

        var mid = (a.transform.position + b.transform.position) * 0.5f;
        return TryMergeAt(a, b, new Vector3(mid.x, mid.y, mid.z));
    }

    public bool TryMergeAt(BallInfo a, BallInfo b, Vector3 spawnPos)
    {
        if (MergeAttemptTracker.HasAlreadyFailed(a, b))
            return false;

        if (!CanMerge(a, b, out string reason))
        {
            MergeAttemptTracker.MarkFailed(a, b);
            return false;
        }

        a.BeginMerge();
        b.BeginMerge();

        var rootA = GetInstanceRoot(a);
        var rootB = GetInstanceRoot(b);

        var type = a.Type;
        var nextLevel = a.Level + 1;

        factory.Despawn(rootA);
        factory.Despawn(rootB);

        var spawned = BallFactoryAddressables.Instance.SpawnLevelWithRefs(type, nextLevel, spawnPos);
        var merged = spawned.info;

        if (merged != null)
        {
            var anim = BallFactoryAddressables.Instance.BallSet.GetAnimationForLevel(nextLevel);

            bool hasSpine = merged.Controller != null &&
                            merged.Controller.Spine != null &&
                            anim != null &&
                            !string.IsNullOrEmpty(anim.mergeAnimation);

            if (hasSpine)
            {
                // STEP A: Play intro / pop effect if you have one.
                merged.Controller.PlayIntroMerged();

                // STEP B: Play the merge animation (non‑looping).
                merged.Controller.PlaySpine(anim.mergeAnimation, false);

                // STEP C: Subscribe to completion to then play idle.
                merged.Controller.Spine.AnimationState.Complete += OnComplete;

                void OnComplete(Spine.TrackEntry entry)
                {
                    if (entry.Animation != null && entry.Animation.Name == anim.mergeAnimation)
                    {
                        // Play idle with loop = true
                        merged.Controller.PlaySpine(anim.idleAnimation, true);

                        // Unsubscribe
                        merged.Controller.Spine.AnimationState.Complete -= OnComplete;
                    }
                }
            }
            else
            {
                // Fallback path if no spine animation exists.
                merged.Controller?.PlayIntroMerged();
            }

            // Raise events, scoring, etc
            BallEventManager.RaiseBallMerged(merged);

            int score = MergeLevelManager.GetCurrentLevel().scores
                            ?.Find(s => s.level == a.Level)?.score
                         ?? FirebaseInitializer.BaseMergeScore;

            BallEventManager.RaiseMergeScore(spawnPos, score, level: a.Level);
            ParticleEvents.Request("merge", spawnPos);
        }

        return true;
    }


    private static bool CanMerge(BallInfo a, BallInfo b, out string reason)
    {
        reason = "";

        if (a == null || b == null)
        {
            reason = "One or both balls are null";
            return false;
        }

        if (a == b)
        {
            reason = "Attempting to merge a ball with itself";
            return false;
        }

        if (a.IsMerging || b.IsMerging)
        {
            reason = "One or both balls are already merging";
            return false;
        }

        if (!a.IsMergeReady || !b.IsMergeReady)
        {
            reason = "One or both balls are not merge-ready";
            return false;
        }

        if (a.Level != b.Level)
        {
            reason = $"Levels don't match (A: {a.Level}, B: {b.Level})";
            return false;
        }

        if (a.Type != b.Type)
        {
            reason = $"Types don't match (A: {a.Type}, B: {b.Type})";
            return false;
        }

        return true;
    }

    private static GameObject GetInstanceRoot(BallInfo info)
    {
        return info.Controller != null ? info.Controller.gameObject : info.gameObject;
    }

    private static string FormatBall(BallInfo b)
    {
        if (b == null) return "null";

        return $"[{b.Type} | Lvl {b.Level} | Pos {b.transform.position} | Merging: {b.IsMerging} | Ready: {b.IsMergeReady}]";
    }
}
