using System.Collections.Generic;
using UnityEngine;

public class EnemyIconController
{
    private readonly List<EnemyIconNode> _icons = new();
    public List<EnemyIconNode> GetRawList() => _icons;

    public IReadOnlyList<EnemyIconNode> Icons => _icons;

    public void Clear() => _icons.Clear();

    public void Add(EnemyIconNode icon)
    {
        if (icon != null)
            _icons.Add(icon);
    }

    public void TriggerRestartAll()
    {
        foreach (var icon in _icons)
            icon?.TriggerRestart();
    }

    public void MarkEnemyDone(int index)
    {
        if (index >= 0 && index < _icons.Count)
            _icons[index]?.TriggerGrey(true);
    }

    public void UpgradePreviousToDone(int currentGreyIndex)
    {
        for (int i = 0; i < currentGreyIndex; i++)
        {
            _icons[i]?.TriggerDone();
        }
    }

    public void MarkRangeDoneInclusive(int from, int to)
    {
        int a = Mathf.Clamp(from, 0, _icons.Count - 1);
        int b = Mathf.Clamp(to, 0, _icons.Count - 1);
        for (int i = a; i <= b; i++)
            _icons[i]?.TriggerDone();
    }

    public void SyncToIndex(int currentIndex, bool includeCurrent = false)
    {
        int lastToGrey = includeCurrent ? currentIndex : currentIndex - 1;
        for (int i = 0; i <= lastToGrey; i++)
        {
            if (i >= 0 && i < _icons.Count)
                _icons[i]?.TriggerGrey(false);
        }
    }

    // ⭐ Added: Sort icons by X position (left → right)
    public void SortByPosition()
    {
        _icons.Sort((a, b) =>
            a.RectTransform.anchoredPosition.x.CompareTo(b.RectTransform.anchoredPosition.x)
        );
    }
}
