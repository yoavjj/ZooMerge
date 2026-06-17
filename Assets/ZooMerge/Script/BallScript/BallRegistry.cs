using System.Collections.Generic;
using UnityEngine;

public static class BallRegistry
{
    private static readonly HashSet<BallInfo> activeBalls = new();
    private static readonly List<BallSnapshot> savedBalls = new();

    public static IReadOnlyCollection<BallInfo> ActiveBalls => activeBalls;
    public static int SavedBallCount => savedBalls.Count;

    public static void Register(BallInfo ball)
    {
        if (ball != null)
            activeBalls.Add(ball);
    }

    public static void Unregister(BallInfo ball)
    {
        if (ball != null)
        {
            activeBalls.Remove(ball);
            MergeAttemptTracker.ClearForBall(ball); // 🧼 Clear related merge attempts
        }
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

            var dc = ball.DropController;

            // Don’t save balls that are currently touching the gameOver area
            if (dc != null && dc.IsTouchingGameOver)
                continue;

            var transform = ball.transform;

            int sOrder = dc != null ? dc.GetAssignedOrder() : 0;

            savedBalls.Add(new BallSnapshot
            {
                position = transform.position,
                rotation = transform.rotation,
                level = ball.Level,
                type = ball.Type,
                scale = transform.localScale.x,
                sortingOrder = sOrder
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

                // ✅ Apply the saved sorting order back to BOTH:
                // 1) the controller field (so next SaveState is correct)
                // 2) the renderer (so visuals are correct right now)
                if (info.DropController != null)
                {
                    info.DropController.SetAssignedOrder(snapshot.sortingOrder);

                    // Optional: tell your sorting manager this order is in use so new balls don't overlap it
                    SpineSortingOrderManager.ClaimOrder(snapshot.sortingOrder);
                }

                BallRegistry.Register(info);
                info.Controller?.PlayIntroNewMidLevel();
                info.MarkAsMergeReady(true);
                info.DropController?.SetDraggable(false);
                info.DropController?.EnableGameOverCheckImmediate();
            }
            else
            {
                Debug.LogError($"❌ Failed to spawn ball: Level={snapshot.level}, Type={snapshot.type}");
            }
        }

        Debug.Log($"✅ Restored {savedBalls.Count} balls with original sorting orders.");
    }

    public static bool IsActive(BallInfo ball)
    {
        return ball != null && activeBalls.Contains(ball);
    }
}