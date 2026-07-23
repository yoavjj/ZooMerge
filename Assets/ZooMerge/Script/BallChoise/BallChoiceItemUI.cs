using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BallChoiceItemUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI animalTypeText;
    [SerializeField] private TextMeshProUGUI mergeCountText;
    [SerializeField] private Image profileImage;
    [SerializeField] private Button selectionButton;

    [Header("Selection Appearance")]
    [SerializeField]
    private CardSelectionVisualController selectionVisualController;

    [Header("Selection Number")]
    [SerializeField] private CanvasGroup selectedCanvasGroup;
    [SerializeField] private TextMeshProUGUI selectedNumberText;

    [SerializeField, Min(0f)]
    private float selectedFadeDuration = 0.2f;

    [SerializeField]
    private AnimationCurve selectedFadeCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Locked Appearance")]
    [SerializeField] private CanvasGroup lockedCanvasGroup;

    [SerializeField, Min(0f)]
    private float lockedFadeDuration = 0.2f;

    [Header("Unlock Reveal")]
    [SerializeField]
    private BallCardRevealAnimator revealAnimator;

    [SerializeField]
    private AnimationCurve lockedFadeCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Coroutine lockedFadeRoutine;
    private bool isLocked;
    private bool forceFullColor;

    public BallType Type { get; private set; }
    public bool IsSelected { get; private set; }

    public event Action<BallChoiceItemUI> Clicked;
    public event Action RevealFinished;

    private Coroutine selectedFadeRoutine;

    private void Awake()
    {
        if (selectionButton != null)
            selectionButton.onClick.AddListener(HandleButtonPressed);

        if (revealAnimator != null)
            revealAnimator.RevealFinished += HandleRevealFinished;

        SetSelectionNumberImmediate(false, 0);
        SetLockedStateImmediate(false);
    }

    private void OnDisable()
    {
        if (selectedFadeRoutine != null)
        {
            StopCoroutine(selectedFadeRoutine);
            selectedFadeRoutine = null;
        }

        if (lockedFadeRoutine != null)
        {
            StopCoroutine(lockedFadeRoutine);
            lockedFadeRoutine = null;
        }
    }

    private void OnDestroy()
    {
        if (selectionButton != null)
            selectionButton.onClick.RemoveListener(HandleButtonPressed);

        if (revealAnimator != null)
            revealAnimator.RevealFinished -= HandleRevealFinished;
    }

    private void HandleRevealFinished()
    {
        RevealFinished?.Invoke();
    }

    public void Initialize(
        BallType type,
        Sprite profileSprite,
        bool locked = false)
    {
        Type = type;
        forceFullColor = false;

        SetProfileSprite(profileSprite);
        Refresh();

        SetSelectionStateImmediate(
            false,
            false,
            0
        );

        SetLockedState(
            locked,
            immediate: true
        );
    }

    public void SetLockedState(
        bool locked,
        bool immediate = false)
    {
        bool stateChanged = isLocked != locked;
        isLocked = locked;

        float targetAlpha = locked ? 1f : 0f;

        if (immediate || !stateChanged)
        {
            SetLockedCanvasImmediate(targetAlpha);
        }
        else
        {
            AnimateLockedCanvas(targetAlpha);
        }

        RefreshCardVisual(immediate);
    }

    private void SetLockedStateImmediate(bool locked)
    {
        isLocked = locked;

        SetLockedCanvasImmediate(
            locked ? 1f : 0f
        );

        RefreshCardVisual(immediate: true);
    }

    private void SetLockedCanvasImmediate(float alpha)
    {
        if (lockedFadeRoutine != null)
        {
            StopCoroutine(lockedFadeRoutine);
            lockedFadeRoutine = null;
        }

        if (lockedCanvasGroup == null)
            return;

        lockedCanvasGroup.alpha = alpha;

        // The main selection button should continue receiving clicks
        // so it can open the unlock popup.
        lockedCanvasGroup.interactable = false;
        lockedCanvasGroup.blocksRaycasts = false;
    }

    private void AnimateLockedCanvas(float targetAlpha)
    {
        if (lockedCanvasGroup == null)
            return;

        if (lockedFadeRoutine != null)
            StopCoroutine(lockedFadeRoutine);

        lockedFadeRoutine = StartCoroutine(
            AnimateLockedCanvasRoutine(targetAlpha)
        );
    }

    private IEnumerator AnimateLockedCanvasRoutine(
    float targetAlpha)
    {
        float startAlpha = lockedCanvasGroup.alpha;

        if (lockedFadeDuration <= 0f)
        {
            lockedCanvasGroup.alpha = targetAlpha;
            lockedFadeRoutine = null;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < lockedFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float normalizedTime = Mathf.Clamp01(
                elapsed / lockedFadeDuration
            );

            float curvedTime = lockedFadeCurve != null
                ? lockedFadeCurve.Evaluate(normalizedTime)
                : normalizedTime;

            lockedCanvasGroup.alpha = Mathf.Lerp(
                startAlpha,
                targetAlpha,
                curvedTime
            );

            yield return null;
        }

        lockedCanvasGroup.alpha = targetAlpha;
        lockedFadeRoutine = null;
    }

    public void SetDisplayOnly(
        bool showFullColor = true,
        bool showLockedOverlay = false)
    {
        if (selectionButton != null)
            selectionButton.interactable = false;

        forceFullColor = showFullColor;
        isLocked = showLockedOverlay;

        SetLockedCanvasImmediate(
            showLockedOverlay ? 1f : 0f
        );

        RefreshCardVisual(immediate: true);

        if (selectedCanvasGroup != null)
        {
            selectedCanvasGroup.alpha = 0f;
            selectedCanvasGroup.interactable = false;
            selectedCanvasGroup.blocksRaycasts = false;
        }

        if (selectedNumberText != null)
            selectedNumberText.text = string.Empty;
    }

    public void PlayUnlockReveal()
    {
        if (revealAnimator == null)
        {
            Debug.LogWarning(
                $"[{nameof(BallChoiceItemUI)}] " +
                $"Reveal animator is not assigned on {gameObject.name}."
            );

            return;
        }

        revealAnimator.PlayReveal();
    }

    public void Refresh()
    {
        SetAnimalTypeText();
        SetMergeCountText();
    }

    public void SetSelectionState(
        bool isSelected,
        bool selectionLimitReached,
        int selectionNumber)
    {
        bool stateChanged = IsSelected != isSelected;

        IsSelected = isSelected;

        RefreshCardVisual(immediate: false);

        if (selectedNumberText != null)
        {
            selectedNumberText.text = isSelected
                ? selectionNumber.ToString()
                : string.Empty;
        }

        if (stateChanged)
        {
            AnimateSelectedCanvas(
                isSelected ? 1f : 0f
            );
        }
        else if (selectedCanvasGroup != null)
        {
            // Keep alpha synchronized if the order changes
            // without changing this item's selected state.
            selectedCanvasGroup.alpha =
                isSelected ? 1f : 0f;
        }

        bool unavailable =
            selectionLimitReached &&
            !isSelected;

        if (selectionButton != null)
            selectionButton.interactable = true;
    }

    public void SetSelectionStateImmediate(
        bool isSelected,
        bool selectionLimitReached,
        int selectionNumber)
    {
        IsSelected = isSelected;

        RefreshCardVisual(immediate: true);

        SetSelectionNumberImmediate(
            isSelected,
            selectionNumber
        );

        if (selectionButton != null)
            selectionButton.interactable = true;
    }

    private void AnimateSelectedCanvas(float targetAlpha)
    {
        if (selectedCanvasGroup == null)
            return;

        if (selectedFadeRoutine != null)
            StopCoroutine(selectedFadeRoutine);

        selectedFadeRoutine = StartCoroutine(
            AnimateSelectedCanvasRoutine(targetAlpha)
        );
    }

    private IEnumerator AnimateSelectedCanvasRoutine(
        float targetAlpha)
    {
        float startAlpha = selectedCanvasGroup.alpha;

        if (selectedFadeDuration <= 0f)
        {
            selectedCanvasGroup.alpha = targetAlpha;
            selectedFadeRoutine = null;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < selectedFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float normalizedTime = Mathf.Clamp01(
                elapsed / selectedFadeDuration
            );

            float curvedTime = selectedFadeCurve != null
                ? selectedFadeCurve.Evaluate(normalizedTime)
                : normalizedTime;

            selectedCanvasGroup.alpha = Mathf.Lerp(
                startAlpha,
                targetAlpha,
                curvedTime
            );

            yield return null;
        }

        selectedCanvasGroup.alpha = targetAlpha;
        selectedFadeRoutine = null;
    }

    private void SetSelectionNumberImmediate(
        bool selected,
        int selectionNumber)
    {
        if (selectedCanvasGroup != null)
        {
            selectedCanvasGroup.alpha =
                selected ? 1f : 0f;

            selectedCanvasGroup.interactable = false;
            selectedCanvasGroup.blocksRaycasts = false;
        }

        if (selectedNumberText != null)
        {
            selectedNumberText.text = selected
                ? selectionNumber.ToString()
                : string.Empty;
        }
    }

    private void HandleButtonPressed()
    {
        Clicked?.Invoke(this);
    }

    private void SetAnimalTypeText()
    {
        if (animalTypeText != null)
            animalTypeText.text = Type.ToString();
    }

    private void SetMergeCountText()
    {
        if (mergeCountText == null)
            return;

        int mergeCount =
            GameInventory.Instance.Get(Type);

        mergeCountText.text =
            mergeCount.ToString();
    }

    private void SetProfileSprite(Sprite sprite)
    {
        if (profileImage == null)
            return;

        profileImage.sprite = sprite;
        profileImage.enabled = sprite != null;
    }

    private void RefreshCardVisual(bool immediate)
    {
        if (selectionVisualController == null)
            return;

        bool showFullColor =
            forceFullColor ||
            isLocked ||
            IsSelected;

        if (immediate)
        {
            selectionVisualController.SetSelectedImmediate(
                showFullColor
            );
        }
        else
        {
            selectionVisualController.SetSelected(
                showFullColor
            );
        }
    }

    public void AE_HideLockedOverlay()
    {
        SetLockedState(
            false,
            immediate: false
        );
    }
}