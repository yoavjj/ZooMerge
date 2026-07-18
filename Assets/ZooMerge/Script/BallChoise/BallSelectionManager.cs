using System;
using System.Collections.Generic;
using UnityEngine;

public class BallSelectionManager : MonoBehaviour
{
    public static BallSelectionManager Instance { get; private set; }

    [SerializeField, Min(1)]
    private int requiredSelectionCount = 3;

    private readonly HashSet<BallType> selectedTypes = new();
    private readonly List<BallType> selectionOrder = new();

    public event Action OnSelectionChanged;

    public int RequiredSelectionCount => requiredSelectionCount;
    public int SelectedCount => selectedTypes.Count;

    public bool HasRequiredSelection =>
        selectedTypes.Count == requiredSelectionCount;

    public IReadOnlyCollection<BallType> SelectedTypes =>
        selectedTypes;

    public IReadOnlyList<BallType> SelectionOrder =>
        selectionOrder;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
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

    /// <summary>
    /// Returns the visible selection number: 1, 2, 3.
    /// Returns 0 when the type is not selected.
    /// </summary>
    public int GetSelectionNumber(BallType type)
    {
        int index = selectionOrder.IndexOf(type);

        return index >= 0
            ? index + 1
            : 0;
    }

    public bool TrySelect(BallType type)
    {
        if (selectedTypes.Contains(type))
            return true;

        BallUnlockManager unlockManager =
            BallUnlockManager.Instance;

        if (unlockManager == null)
        {
            Debug.LogWarning(
                "[BallSelectionManager] " +
                "BallUnlockManager.Instance is null."
            );

            return false;
        }

        if (!unlockManager.IsUnlocked(type))
        {
            Debug.Log(
                $"[BallSelectionManager] Cannot select locked type: {type}."
            );

            return false;
        }

        if (selectedTypes.Count >= requiredSelectionCount)
            return false;

        selectedTypes.Add(type);
        selectionOrder.Add(type);

        OnSelectionChanged?.Invoke();
        return true;
    }

    public bool Deselect(BallType type)
    {
        if (!selectedTypes.Remove(type))
            return false;

        selectionOrder.Remove(type);

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
        selectionOrder.Clear();

        OnSelectionChanged?.Invoke();
    }
}