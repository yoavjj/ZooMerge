using System.Collections.Generic;

public static class MergeAttemptTracker
{
    private static readonly HashSet<string> failedAttempts = new();
    private static readonly HashSet<string> loggedFailures = new();
    private static readonly HashSet<string> loggedSkips = new();
    private static readonly HashSet<string> skippedUnregisteredPairs = new();

    public static void MarkFailed(BallInfo a, BallInfo b)
    {
        string key = GetKey(a, b);
        failedAttempts.Add(key);
    }

    public static bool HasAlreadyFailed(BallInfo a, BallInfo b)
    {
        return failedAttempts.Contains(GetKey(a, b));
    }

    public static bool TryLogOnce(BallInfo a, BallInfo b)
    {
        string key = GetKey(a, b);
        if (loggedFailures.Contains(key)) return false;

        loggedFailures.Add(key);
        return true;
    }

    public static bool TryLogSkipOnce(BallInfo a, BallInfo b)
    {
        string key = GetKey(a, b);
        if (loggedSkips.Contains(key)) return false;

        loggedSkips.Add(key);
        return true;
    }

    public static void ClearAll()
    {
        failedAttempts.Clear();
        loggedFailures.Clear();
        loggedSkips.Clear();
        skippedUnregisteredPairs.Clear();
    }

    public static void ClearForBall(BallInfo ball)
    {
        int id = ball.GetInstanceID();
        failedAttempts.RemoveWhere(key => key.StartsWith($"{id}-") || key.Contains($"-{id}-"));
        loggedFailures.RemoveWhere(key => key.StartsWith($"{id}-") || key.Contains($"-{id}-"));
        loggedSkips.RemoveWhere(key => key.StartsWith($"{id}-") || key.Contains($"-{id}-"));
    }

    private static string GetKey(BallInfo a, BallInfo b)
    {
        if (a == null || b == null) return "null-pair";

        int idA = a.GetInstanceID();
        int idB = b.GetInstanceID();

        if (idA > idB) (idA, idB) = (idB, idA);

        return $"{idA}-{a?.Level ?? 0}-{idB}-{b?.Level ?? 0}";
    }

    public static bool HasAlreadySkippedUnregistered(BallInfo a, BallInfo b)
    {
        return skippedUnregisteredPairs.Contains(GetKey(a, b));
    }

    public static void MarkSkippedUnregistered(BallInfo a, BallInfo b)
    {
        skippedUnregisteredPairs.Add(GetKey(a, b));
    }
}
