using System.Collections.Generic;

public static class GameInventory
{
    public static Inventory Instance { get; } = new Inventory();
}

public sealed class Inventory
{
    private readonly Dictionary<BallType, int> values = new();

    public int Get(BallType type)
        => values.TryGetValue(type, out var v) ? v : 0;

    public void Add(BallType type, int amount)
    {
        if (amount <= 0) return;

        if (!values.ContainsKey(type))
            values[type] = 0;

        values[type] += amount;
    }

    public IReadOnlyDictionary<BallType, int> Snapshot()
        => values;
}
