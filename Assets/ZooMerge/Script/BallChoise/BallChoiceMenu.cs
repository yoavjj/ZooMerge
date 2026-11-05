using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BallChoiceMenu : SfxBehaviourTirgger
{
    public event Action<BallType> UnlockPopupRequested;
    private BallUnlockManager subscribedUnlockManager;

    [Header("Data")]
    [SerializeField] private BallSet ballSet;

    [Header("UI")]
    [SerializeField] private Transform container;
    [SerializeField] private BallChoiceItemUI itemPrefab;

    [Header("Message Panel")]
    [SerializeField] private Animator messagePanelAnimator;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private string messageTrigger = "In";

    [SerializeField, Min(0f)]
    private float messageCooldown = 0.75f;

    private bool messageLocked;
    private Coroutine messageCooldownRoutine;

    [SerializeField, TextArea]
    private string incompleteSelectionMessage =
        "Choose exactly 3 animals before playing.";

    [Header("Settings")]
    [SerializeField] private bool clearSelectionOnBuild = true;

    private readonly Dictionary<BallType, BallChoiceItemUI> itemsByType = new();

    private BallSelectionManager subscribedManager;

    private BallSelectionManager SelectionManager =>
        BallSelectionManager.Instance;

    private void OnDisable()
    {
        UnsubscribeFromSelectionManager();
        UnsubscribeFromUnlockManager();

        if (messageCooldownRoutine != null)
        {
            StopCoroutine(messageCooldownRoutine);
            messageCooldownRoutine = null;
        }

        messageLocked = false;
    }

    public void Build()
    {
        Clear();

        BallSelectionManager manager = SelectionManager;

        if (manager == null)
        {
            Debug.LogError(
                "[BallChoiceMenu] BallSelectionManager.Instance is null. " +
                "Place the manager on a persistent scene root."
            );

            return;
        }

        SubscribeToSelectionManager(manager);
        SubscribeToUnlockManager();

        BallUnlockManager unlockManager = BallUnlockManager.Instance;

        bool catUnlocked =
            unlockManager != null &&
            unlockManager.IsUnlocked(BallType.Cat);

        // Before Cat is unlocked, always rebuild the automatic
        // starter selection from scratch.
        //
        // After Cat is unlocked, return to the normal behavior.
        if (!catUnlocked || clearSelectionOnBuild)
            manager.ClearSelection();

        foreach (BallType type in Enum.GetValues(typeof(BallType)))
            CreateItem(type);

        // Before Cat is unlocked, automatically choose
        // the first 3 currently unlocked animals.
        if (!catUnlocked)
            AutoSelectStarterHeroes(manager);

        RefreshSelectionVisuals();
        RefreshLockedVisuals(immediate: true);
    }

    private void AutoSelectStarterHeroes(BallSelectionManager manager)
    {
        BallUnlockManager unlockManager = BallUnlockManager.Instance;

        if (manager == null || unlockManager == null)
            return;

        foreach (BallType type in Enum.GetValues(typeof(BallType)))
        {
            if (manager.SelectedCount >= manager.RequiredSelectionCount)
                break;

            if (!unlockManager.IsUnlocked(type))
                continue;

            if (manager.IsSelected(type))
                continue;

            manager.TrySelect(type);
        }

        Debug.Log(
            $"[BallChoiceMenu] Starter heroes auto-selected: " +
            $"{manager.SelectedCount}/{manager.RequiredSelectionCount}"
        );
    }

    private void SubscribeToUnlockManager()
    {
        BallUnlockManager manager =
            BallUnlockManager.Instance;

        if (manager == null)
            return;

        if (subscribedUnlockManager == manager)
            return;

        UnsubscribeFromUnlockManager();

        subscribedUnlockManager = manager;

        subscribedUnlockManager.OnBallUnlockStateChanged +=
            HandleUnlockStateChanged;
    }

    private void UnsubscribeFromUnlockManager()
    {
        if (subscribedUnlockManager == null)
            return;

        subscribedUnlockManager.OnBallUnlockStateChanged -=
            HandleUnlockStateChanged;

        subscribedUnlockManager = null;
    }

    private void HandleUnlockStateChanged(BallType type)
    {
        BallUnlockManager unlockManager = BallUnlockManager.Instance;

        if (unlockManager == null)
            return;

        bool isLocked = !unlockManager.IsUnlocked(type);

        if (itemsByType.TryGetValue(type, out BallChoiceItemUI item) && item != null)
        {
            item.SetLockedState(
                isLocked,
                immediate: false
            );
        }

        BallSelectionManager selectionManager = SelectionManager;

        if (selectionManager == null)
            return;

        // A locked animal must not remain selected.
        if (isLocked)
        {
            if (selectionManager.IsSelected(type))
                selectionManager.Deselect(type);

            return;
        }

        // --------------------------------
        // CAT UNLOCKED
        // --------------------------------

        // Cat is the point where manual hero selection begins.
        // Remove the automatic starter selection so the player
        // now has to choose their own 3 heroes.
        if (type == BallType.Cat)
        {
            selectionManager.ClearSelection();

            Debug.Log(
                "[BallChoiceMenu] Cat unlocked. " +
                "Automatic starter selection ended. " +
                "Player must now choose 3 heroes."
            );
        }
    }

    private void SubscribeToSelectionManager(
        BallSelectionManager manager)
    {
        if (subscribedManager == manager)
            return;

        UnsubscribeFromSelectionManager();

        subscribedManager = manager;
        subscribedManager.OnSelectionChanged +=
            RefreshSelectionVisuals;
    }

    private void UnsubscribeFromSelectionManager()
    {
        if (subscribedManager == null)
            return;

        subscribedManager.OnSelectionChanged -=
            RefreshSelectionVisuals;

        subscribedManager = null;
    }

    private void CreateItem(BallType type)
    {
        if (itemPrefab == null || container == null)
        {
            Debug.LogError(
                "[BallChoiceMenu] Missing item prefab or container."
            );

            return;
        }

        BallChoiceItemUI item = Instantiate(
            itemPrefab,
            container
        );

        Sprite profileSprite = ballSet != null
            ? ballSet.GetProfileSprite(type)
            : null;

        BallUnlockManager unlockManager =
            BallUnlockManager.Instance;

        bool isLocked =
            unlockManager != null &&
            !unlockManager.IsUnlocked(type);

        item.Initialize(
            type,
            profileSprite,
            isLocked
        );

        item.Clicked += HandleItemClicked;

        itemsByType[type] = item;
    }

    private void HandleItemClicked(BallChoiceItemUI item)
    {
        if (item == null)
            return;

        BallSelectionManager selectionManager = SelectionManager;

        if (selectionManager == null)
        {
            Debug.LogError("[BallChoiceMenu] BallSelectionManager.Instance is null.");
            return;
        }

        BallUnlockManager unlockManager = BallUnlockManager.Instance;

        if (unlockManager == null)
        {
            Debug.LogError("[BallChoiceMenu] BallUnlockManager.Instance is null.");
            return;
        }

        bool itemUnlocked = unlockManager.IsUnlocked(item.Type);

        // --------------------------------
        // LOCKED ANIMAL
        // --------------------------------

        if (!itemUnlocked)
        {
            // Ball Unlock feature has not been introduced yet.
            if (!BallUnlockFeatureProgress.IsUnlocked)
            {
                int unlockLevel = MergeLevelManager.GetBallUnlockFeatureUnlockLevelNumber();

                if (unlockLevel > 0)
                    ShowMessage($"Unlocked at Level {unlockLevel}");
                else
                    ShowMessage("Animal unlocks are not available yet.");

                PlayUiSfx(SfxCue.ButtonClickNegative);

                return;
            }

            // Ball Unlock feature is available.
            Debug.Log($"[BallChoiceMenu] Requesting unlock popup for {item.Type}.");

            UnlockPopupRequested?.Invoke(item.Type);
            return;
        }

        // --------------------------------
        // STARTER AUTO-SELECTION MODE
        // --------------------------------

        // Until Cat is unlocked, the original 3 heroes stay
        // automatically selected and cannot be manually changed.
        if (!unlockManager.IsUnlocked(BallType.Cat))
        {
            Debug.Log("[BallChoiceMenu] Manual hero selection unlocks after Cat is unlocked.");
            return;
        }

        // --------------------------------
        // NORMAL MANUAL SELECTION MODE
        // --------------------------------

        // Cat has been unlocked, so selection now works
        // exactly like the existing system.
        if (selectionManager.IsSelected(item.Type))
        {
            selectionManager.Deselect(item.Type);
            return;
        }

        bool selected = selectionManager.TrySelect(item.Type);

        if (!selected)
        {
            ShowMessage(
                $"Pick only {selectionManager.RequiredSelectionCount}!"
            );
        }
    }

    public void RefreshAll()
    {
        foreach (
            BallChoiceItemUI item
            in itemsByType.Values)
        {
            if (item != null)
                item.Refresh();
        }

        RefreshSelectionVisuals();

        // Animated transition:
        // locked alpha 1 -> unlocked alpha 0.
        RefreshLockedVisuals(immediate: false);
    }

    public void PlayBallUnlockFeatureReveal()
    {
        if (!itemsByType.TryGetValue(
                BallType.Cat,
                out BallChoiceItemUI catItem))
        {
            return;
        }

        if (catItem == null)
            return;

        catItem.PlayBallUnlockFeatureReveal();

        Debug.Log(
            "[BallChoiceMenu] Ball Unlock feature reveal started on Cat card."
        );
    }

    private void RefreshSelectionVisuals()
    {
        BallSelectionManager manager = SelectionManager;

        if (manager == null)
            return;

        bool limitReached =
            manager.SelectedCount >=
            manager.RequiredSelectionCount;

        foreach (var pair in itemsByType)
        {
            if (pair.Value == null)
                continue;

            bool isSelected =
                manager.IsSelected(pair.Key);

            int selectionNumber =
                manager.GetSelectionNumber(pair.Key);

            pair.Value.SetSelectionState(
                isSelected,
                limitReached,
                selectionNumber
            );
        }
    }

    private void Clear()
    {
        foreach (BallChoiceItemUI item in itemsByType.Values)
        {
            if (item == null)
                continue;

            item.Clicked -= HandleItemClicked;
            Destroy(item.gameObject);
        }

        itemsByType.Clear();
    }

    public void ShowIncompleteSelectionMessage()
    {
        BallSelectionManager manager = SelectionManager;

        int selectedCount = manager != null
            ? manager.SelectedCount
            : 0;

        int requiredCount = manager != null
            ? manager.RequiredSelectionCount
            : 3;

        ShowMessage(
            incompleteSelectionMessage
        );
    }

    public void ShowMessage(string message)
    {
        if (messageLocked)
            return;

        messageLocked = true;

        if (messageText != null)
            messageText.text = message;

        if (messagePanelAnimator != null &&
            !string.IsNullOrEmpty(messageTrigger))
        {
            messagePanelAnimator.ResetTrigger(messageTrigger);
            messagePanelAnimator.SetTrigger(messageTrigger);
        }

        if (messageCooldownRoutine != null)
            StopCoroutine(messageCooldownRoutine);

        messageCooldownRoutine =
            StartCoroutine(MessageCooldownRoutine());
    }

    private IEnumerator MessageCooldownRoutine()
    {
        yield return new WaitForSecondsRealtime(messageCooldown);

        messageLocked = false;
        messageCooldownRoutine = null;
    }

    private void RefreshLockedVisuals(
    bool immediate = false)
    {
        BallUnlockManager unlockManager =
            BallUnlockManager.Instance;

        if (unlockManager == null)
        {
            Debug.LogWarning(
                "[BallChoiceMenu] BallUnlockManager.Instance is null."
            );

            return;
        }

        foreach (
            KeyValuePair<BallType, BallChoiceItemUI> pair
            in itemsByType)
        {
            BallChoiceItemUI item = pair.Value;

            if (item == null)
                continue;

            bool isLocked =
                !unlockManager.IsUnlocked(pair.Key);

            item.SetLockedState(
                isLocked,
                immediate
            );
        }
    }

    public void SetClearSelectionOnBuild(bool value)
    {
        clearSelectionOnBuild = value;
    }

    public bool TryGetFirstLockedBall(out BallType type)
    {
        BallUnlockManager unlockManager = BallUnlockManager.Instance;

        if (unlockManager == null)
        {
            type = default;
            return false;
        }

        foreach (BallType candidate in Enum.GetValues(typeof(BallType)))
        {
            if (!unlockManager.IsUnlocked(candidate))
            {
                type = candidate;
                return true;
            }
        }

        type = default;
        return false;
    }
}