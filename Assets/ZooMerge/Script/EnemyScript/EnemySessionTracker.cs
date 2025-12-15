using System.Collections.Generic;
using UnityEngine;

public static class EnemySessionTracker
{
    private static readonly HashSet<GameObject> activeEnemies = new();

    public static void Register(GameObject enemy)
    {
        if (enemy != null)
            activeEnemies.Add(enemy);
    }

    public static void Unregister(GameObject enemy)
    {
        if (enemy != null)
            activeEnemies.Remove(enemy);
    }

    public static bool IsTracked(GameObject enemy)
    {
        return enemy != null && activeEnemies.Contains(enemy);
    }

    public static void Clear()
    {
        activeEnemies.Clear();
    }
}
