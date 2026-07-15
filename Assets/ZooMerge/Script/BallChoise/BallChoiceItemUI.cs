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

    public BallType Type { get; private set; }
    public bool IsSelected { get; private set; }

    public event Action<BallChoiceItemUI> Clicked;

    private Coroutine selectedFadeRoutine;

    private void Awake()
    {
        if (selectionButton != null)
            selectionButton.onClick.AddListener(HandleButtonPressed);

        SetSelectionNumberImmediate(false, 0);
    }

    private void OnDisable()
    {
        if (selectedFadeRoutine != null)
        {
            StopCoroutine(selectedFadeRoutine);
            selectedFadeRoutine = null;
        }
    }

    private void OnDestroy()
    {
        if (selectionButton != null)
            selectionButton.onClick.RemoveListener(HandleButtonPressed);
    }

    public void Initialize(BallType type, Sprite profileSprite)
    {
        Type = type;

        SetProfileSprite(profileSprite);
        Refresh();

        SetSelectionStateImmediate(
            false,
            false,
            0
        );
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

        if (selectionVisualController != null)
            selectionVisualController.SetSelected(isSelected);

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

        if (selectionVisualController != null)
        {
            selectionVisualController.SetSelectedImmediate(
                isSelected
            );
        }

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
}