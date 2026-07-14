using System;
using System.Collections.Generic;
using UnityEngine;

public class BallSelectionManager : MonoBehaviour
{
    public static BallSelectionManager Instance { get; private set; }

    [SerializeField, Min(1)] private int requiredSelectionCount = 3;

    private readonly HashSet<BallType> selectedTypes = new();

    public event Action OnSelectionChanged;

    public int RequiredSelectionCount => requiredSelectionCount;
    public int SelectedCount => selectedTypes.Count;

    public bool HasRequiredSelection =>
        selectedTypes.Count == requiredSelectionCount;

    public IReadOnlyCollection<BallType> SelectedTypes =>
        selectedTypes;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // This GameObject must be a root object.
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool IsSelected(BallType type)
    {
        return selectedTypes.Contains(type);
    }

    public bool TrySelect(BallType type)
    {
        if (selectedTypes.Contains(type))
            return true;

        if (selectedTypes.Count >= requiredSelectionCount)
            return false;

        selectedTypes.Add(type);
        OnSelectionChanged?.Invoke();

        return true;
    }

    public bool Deselect(BallType type)
    {
        if (!selectedTypes.Remove(type))
            return false;

        OnSelectionChanged?.Invoke();
        return true;
    }

    public bool Toggle(BallType type)
    {
        if (IsSelected(type))
            return Deselect(type);

        return TrySelect(type);
    }

    public void ClearSelection()
    {
        if (selectedTypes.Count == 0)
            return;

        selectedTypes.Clear();
        OnSelectionChanged?.Invoke();
    }
}