using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupNavigationSlider : SfxBehaviourTirgger
{
    [Serializable]
    private class PopupTab
    {
        public string id;
        public RectTransform button;
        public string prefabId;

        [Header("Availability")]
        public bool isAvailable = true;

        [TextArea]
        public string unavailableMessage = "Coming Soon!";
    }

    [Header("Moving Selector")]
    [SerializeField] private RectTransform selector;

    [Header("Popup Setup")]
    [SerializeField] private List<PopupTab> tabs = new();

    [Header("Popup Slide")]
    [SerializeField, Min(1f)]
    private float slideDistanceMultiplier = 1.05f;

    private float cachedSlideDistance;

    [SerializeField, Min(0f)] private float popupMoveDuration = 0.25f;
    [SerializeField] private bool useContainerWidthAsSlideDistance = true;
    [SerializeField, Min(0f)] private float customSlideDistance = 900f;

    [Header("Selector Animation")]
    [SerializeField, Min(0f)] private float selectorMoveDuration = 0.15f;

    [SerializeField]
    private AnimationCurve moveCurve = AnimationCurve.EaseInOut(
        0f,
        0f,
        1f,
        1f
    );

    [Header("Popup Message")]
    [SerializeField] private Animator popupMessageAnimator;
    [SerializeField] private TextMeshProUGUI popupMessageText;
    [SerializeField] private string showMessageTrigger = "Show_Message";

    [Header("SFX")]
    [SerializeField] private SfxCue clickSfx = SfxCue.ButtonClick;

    private readonly Dictionary<int, RectTransform> spawnedPopups = new();

    private Coroutine selectorRoutine;
    private Coroutine popupRoutine;

    private int currentIndex = -1;
    private bool isSwitching;

    private const string MAIN_MENU_POPUP_ID = "MainMenuPopup";
    private const string GALAXY_ROADMAP_POPUP_ID = "GalaxyRoadmapPopup_Menu";

    private Popup_GalaxyRoadmap currentRoadmap;

    public static PopupNavigationSlider Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private IEnumerator Start()
    {
        // Wait for Canvas Scaler and responsive UI layout.
        yield return null;

        Canvas.ForceUpdateCanvases();

        CacheSlideDistance();

        SelectPopupIndexInstant(2);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (currentRoadmap != null)
        {
            currentRoadmap.OnClosedRoadmap -=
                HandleRoadmapClosed;
        }
    }

    private void CacheSlideDistance()
    {
        RectTransform popupParent = null;

        if (PopupManager.Instance != null)
        {
            popupParent =
                PopupManager.Instance.transform as RectTransform;
        }

        if (popupParent != null)
        {
            cachedSlideDistance =
                popupParent.rect.width * slideDistanceMultiplier;
        }
        else
        {
            cachedSlideDistance =
                customSlideDistance * slideDistanceMultiplier;
        }
    }

    public void SelectPopupIndex(int index)
    {
        if (isSwitching)
            return;

        if (!IsValidIndex(index))
            return;

        if (index == currentIndex)
            return;

        PlayUiSfx(clickSfx);

        PopupTab tab = tabs[index];

        if (!tab.isAvailable)
        {
            ShowPopupMessage(tab.unavailableMessage);
            return;
        }

        MoveSelectorToIndex(index);

        if (popupRoutine != null)
            StopCoroutine(popupRoutine);

        popupRoutine = StartCoroutine(
            SwitchPopupRoutine(index)
        );
    }

    private void ShowPopupMessage(string message)
    {
        if (popupMessageText != null)
        {
            popupMessageText.text = message;
        }

        if (popupMessageAnimator == null)
            return;

        popupMessageAnimator.ResetTrigger(showMessageTrigger);
        popupMessageAnimator.SetTrigger(showMessageTrigger);
    }

    private void SelectPopupIndexInstant(int index)
    {
        if (!IsValidIndex(index))
            return;

        RectTransform popup = GetOrCreatePopup(index);

        if (popup == null)
            return;

        popup.gameObject.SetActive(true);
        popup.anchoredPosition = Vector2.zero;

        RefreshPopupUI(popup);
        PreparePopupForDisplay(index, popup);

        currentIndex = index;

        if (selector != null && tabs[index].button != null)
        {
            selector.position = new Vector3(
                tabs[index].button.position.x,
                selector.position.y,
                selector.position.z
            );
        }

        NotifyPopupOpened(index);
    }

    private IEnumerator SwitchPopupRoutine(int nextIndex)
    {
        isSwitching = true;

        int previousIndex = currentIndex;

        RectTransform nextPopup = GetOrCreatePopup(nextIndex);
        RectTransform previousPopup = previousIndex >= 0
            ? GetOrCreatePopup(previousIndex)
            : null;

        if (nextPopup == null)
        {
            isSwitching = false;
            yield break;
        }

        float slideDistance = GetSlideDistance();

        int direction = previousIndex < 0 || nextIndex > previousIndex
            ? 1
            : -1;

        Vector2 previousStart = Vector2.zero;
        Vector2 previousTarget = new Vector2(-direction * slideDistance, 0f);

        Vector2 nextStart = new Vector2(direction * slideDistance, 0f);
        Vector2 nextTarget = Vector2.zero;

        nextPopup.gameObject.SetActive(true);
        nextPopup.anchoredPosition = nextStart;

        if (previousPopup != null)
            previousPopup.gameObject.SetActive(true);

        float time = 0f;

        while (time < popupMoveDuration)
        {
            time += Time.unscaledDeltaTime;

            float normalizedTime = popupMoveDuration <= 0f
                ? 1f
                : Mathf.Clamp01(time / popupMoveDuration);

            float curvedTime = moveCurve != null
                ? moveCurve.Evaluate(normalizedTime)
                : normalizedTime;

            if (previousPopup != null)
            {
                previousPopup.anchoredPosition = Vector2.LerpUnclamped(
                    previousStart,
                    previousTarget,
                    curvedTime
                );
            }

            nextPopup.anchoredPosition = Vector2.LerpUnclamped(
                nextStart,
                nextTarget,
                curvedTime
            );

            yield return null;
        }

        if (previousPopup != null)
        {
            previousPopup.anchoredPosition = previousTarget;
            previousPopup.gameObject.SetActive(true);
        }

        nextPopup.anchoredPosition = Vector2.zero;

        // The popup is now at its final visible position.
        RefreshPopupUI(nextPopup);

        // Run popup-specific opening logic.
        // For the roadmap, this calls Initialize() and PlayIntro(false).
        PreparePopupForDisplay(nextIndex, nextPopup);

        nextPopup.anchoredPosition = Vector2.zero;

        RefreshPopupUI(nextPopup);
        PreparePopupForDisplay(nextIndex, nextPopup);

        currentIndex = nextIndex;

        NotifyPopupOpened(nextIndex);

        popupRoutine = null;
        isSwitching = false;
    }

    private RectTransform GetOrCreatePopup(int index)
    {
        if (!IsValidIndex(index))
            return null;

        if (spawnedPopups.TryGetValue(
                index,
                out RectTransform existing) &&
            existing != null)
        {
            return existing;
        }

        if (PopupManager.Instance == null)
            return null;

        RectTransform popup =
            PopupManager.Instance.GetOrCreateNavigationPopup(
                tabs[index].prefabId
            );

        if (popup == null)
            return null;

        spawnedPopups[index] = popup;

        return popup;
    }

    private void MoveSelectorToIndex(int index)
    {
        if (selector == null || tabs[index].button == null)
            return;

        if (selectorRoutine != null)
            StopCoroutine(selectorRoutine);

        selectorRoutine = StartCoroutine(
            MoveSelectorToButton(tabs[index].button)
        );
    }

    private IEnumerator MoveSelectorToButton(RectTransform targetButton)
    {
        Vector3 startWorldPosition = selector.position;

        Vector3 targetWorldPosition = new Vector3(
            targetButton.position.x,
            selector.position.y,
            selector.position.z
        );

        float time = 0f;

        while (time < selectorMoveDuration)
        {
            time += Time.unscaledDeltaTime;

            float normalizedTime = selectorMoveDuration <= 0f
                ? 1f
                : Mathf.Clamp01(time / selectorMoveDuration);

            float curvedTime = moveCurve != null
                ? moveCurve.Evaluate(normalizedTime)
                : normalizedTime;

            selector.position = Vector3.LerpUnclamped(
                startWorldPosition,
                targetWorldPosition,
                curvedTime
            );

            yield return null;
        }

        selector.position = targetWorldPosition;
        selectorRoutine = null;
    }

    private float GetSlideDistance()
    {
        if (useContainerWidthAsSlideDistance &&
            cachedSlideDistance > 0f)
        {
            return cachedSlideDistance;
        }

        return customSlideDistance;
    }

    private bool IsValidIndex(int index)
    {
        return tabs != null &&
               index >= 0 &&
               index < tabs.Count &&
               tabs[index] != null;
    }

    private void NotifyPopupOpened(int index)
    {
        if (!IsValidIndex(index))
            return;

        if (tabs[index].prefabId == MAIN_MENU_POPUP_ID)
        {
            BallEventManager.RaiseMainMenuPopupOpened();
        }
    }

    public void DestroyOtherTabPopups()
    {
        if (popupRoutine != null)
        {
            StopCoroutine(popupRoutine);
            popupRoutine = null;
        }

        if (selectorRoutine != null)
        {
            StopCoroutine(selectorRoutine);
            selectorRoutine = null;
        }

        PopupManager.Instance?.DestroyNavigationPopupsExcept(
            MAIN_MENU_POPUP_ID
        );

        List<int> indexesToRemove = new();

        foreach (var pair in spawnedPopups)
        {
            int index = pair.Key;

            if (!IsValidIndex(index))
            {
                indexesToRemove.Add(index);
                continue;
            }

            if (tabs[index].prefabId != MAIN_MENU_POPUP_ID)
            {
                indexesToRemove.Add(index);
            }
        }

        foreach (int index in indexesToRemove)
        {
            spawnedPopups.Remove(index);
        }

        isSwitching = false;
    }

    private static void RefreshPopupUI(RectTransform popup)
    {
        if (popup == null)
            return;

        Canvas.ForceUpdateCanvases();

        LayoutRebuilder.ForceRebuildLayoutImmediate(popup);

        Graphic[] graphics =
            popup.GetComponentsInChildren<Graphic>(true);

        foreach (Graphic graphic in graphics)
        {
            graphic.SetVerticesDirty();
            graphic.SetMaterialDirty();
        }

        Canvas.ForceUpdateCanvases();
    }

    private void PreparePopupForDisplay(
    int index,
    RectTransform popup)
    {
        if (!IsValidIndex(index) || popup == null)
            return;

        string prefabId = tabs[index].prefabId;

        if (prefabId == GALAXY_ROADMAP_POPUP_ID)
        {
            PrepareGalaxyRoadmap(popup);
        }
    }

    private void PrepareGalaxyRoadmap(RectTransform popup)
    {
        if (popup == null)
            return;

        Popup_GalaxyRoadmap roadmap =
            popup.GetComponent<Popup_GalaxyRoadmap>();

        if (roadmap == null)
        {
            Debug.LogError(
                $"[PopupNavigationSlider] Popup '{GALAXY_ROADMAP_POPUP_ID}' " +
                $"does not contain a {nameof(Popup_GalaxyRoadmap)} component."
            );

            return;
        }

        if (currentRoadmap != null && currentRoadmap != roadmap)
        {
            currentRoadmap.OnClosedRoadmap -=
                HandleRoadmapClosed;
        }

        currentRoadmap = roadmap;

        // Prevent duplicate subscriptions when reopening the same cached popup.
        currentRoadmap.OnClosedRoadmap -=
            HandleRoadmapClosed;

        currentRoadmap.OnClosedRoadmap +=
            HandleRoadmapClosed;

        ResetPopupTransform(popup);

        AnalyticsEvents.LogRoadmapView(
            true,
            MergeLevelManager.CurrentGalaxyId.ToString(),
            MergeLevelManager.CurrentLevelNumber
        );

        currentRoadmap.OpenFromNavigation();
    }

    private void HandleRoadmapClosed()
    {
        int mainMenuIndex =
            FindTabIndexByPrefabId(MAIN_MENU_POPUP_ID);

        if (mainMenuIndex < 0)
        {
            Debug.LogWarning(
                "[PopupNavigationSlider] Main-menu tab was not found."
            );

            return;
        }

        SelectPopupIndex(mainMenuIndex);
    }

    private int FindTabIndexByPrefabId(string prefabId)
    {
        if (tabs == null || string.IsNullOrEmpty(prefabId))
            return -1;

        for (int i = 0; i < tabs.Count; i++)
        {
            PopupTab tab = tabs[i];

            if (tab != null && tab.prefabId == prefabId)
                return i;
        }

        return -1;
    }

    private static void ResetPopupTransform(RectTransform popup)
    {
        if (popup == null)
            return;

        popup.localRotation = Quaternion.identity;
        popup.localScale = Vector3.one;
    }
}