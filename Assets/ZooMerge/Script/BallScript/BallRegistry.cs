using System.Collections.Generic;
using UnityEngine;

public static class BallRegistry
{
    private static readonly HashSet<BallInfo> activeBalls = new();
    private static readonly List<BallSnapshot> savedBalls = new();


    public static IReadOnlyCollection<BallInfo> ActiveBalls => activeBalls;

    public static void Register(BallInfo ball)
    {
        if (ball != null)
            activeBalls.Add(ball);
        //Debug.Log($"Ball registered: {ball.name}. Total active balls: {activeBalls.Count}");
    }

    public static void Unregister(BallInfo ball)
    {
        if (ball != null)
        {
            activeBalls.Remove(ball);
            MergeAttemptTracker.ClearForBall(ball); // 🧼 Clear related merge attempts
        }
        //Debug.Log($"Ball unregistered: {ball.name}. Total active balls: {activeBalls.Count}");
    }

    public static void Clear()
    {
        activeBalls.Clear();
        SpineSortingOrderManager.ResetAll();
    }

    public static void SaveState(IEnumerable<BallInfo> balls)
    {
        savedBalls.Clear();

        foreach (var ball in balls)
        {
            if (ball == null) continue;

            var transform = ball.transform;
            savedBalls.Add(new BallSnapshot
            {
                position = transform.position,
                rotation = transform.rotation,
                level = ball.Level,
                type = ball.Type,
                scale = transform.localScale.x
            });
        }

        Debug.Log($"💾 Saved {savedBalls.Count} active balls from BallRegistry.");
    }

    public static void RestoreState(Transform parent)
    {
        foreach (var snapshot in savedBalls)
        {
            var spawned = BallFactoryAddressables.Instance.SpawnLevelWithRefs(snapshot.type, snapshot.level, snapshot.position, parent);
            var info = spawned.info;
            if (info != null)
            {
                info.transform.rotation = snapshot.rotation;
                info.transform.localScale = Vector3.one * snapshot.scale;

                // 🟢 Reapply level, type, and physics config
                var physics = BallFactoryAddressables.Instance.GetPhysicsFor(snapshot.type, snapshot.level);
                if (physics != null)
                {
                    info.Setup(
                        snapshot.level,
                        snapshot.type,
                        physics.finalLinearDamping,
                        physics.finalAngularDamping,
                        physics.gravityStart,
                        physics.gravityEnd,
                        snapshot.scale
                    );
                }
                else
                {
                    Debug.LogWarning($"[RestoreState] No physics found for type {snapshot.type} level {snapshot.level}");
                }

                BallRegistry.Register(info);
                info.Controller?.PlayIntroNewMidLevel();
                info.MarkAsMergeReady(true);
                info.DropController?.SetDraggable(false);

                // ✅ Log restored ball info
                //Debug.Log($"🟠 Restored ball: Level={snapshot.level}, Type={snapshot.type}, Pos={snapshot.position}");
            }
            else
            {
                Debug.LogError($"❌ Failed to spawn ball: Level={snapshot.level}, Type={snapshot.type}");
            }
        }

        Debug.Log($"✅ Restored {savedBalls.Count} balls.");
    }

}
