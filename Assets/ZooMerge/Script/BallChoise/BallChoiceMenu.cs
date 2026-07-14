using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BallChoiceMenu : MonoBehaviour
{
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

        if (clearSelectionOnBuild)
            manager.ClearSelection();

        foreach (BallType type in Enum.GetValues(typeof(BallType)))
            CreateItem(type);

        RefreshSelectionVisuals();
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

        item.Initialize(type, profileSprite);
        item.Clicked += HandleItemClicked;

        itemsByType[type] = item;
    }

    private void HandleItemClicked(BallChoiceItemUI item)
    {
        if (item == null)
            return;

        BallSelectionManager manager = SelectionManager;

        if (manager == null)
        {
            Debug.LogError(
                "[BallChoiceMenu] BallSelectionManager.Instance is null."
            );

            return;
        }

        manager.Toggle(item.Type);
    }

    public void RefreshAll()
    {
        foreach (BallChoiceItemUI item in itemsByType.Values)
        {
            if (item != null)
                item.Refresh();
        }

        RefreshSelectionVisuals();
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

            pair.Value.SetSelectionState(
                manager.IsSelected(pair.Key),
                limitReached
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
}