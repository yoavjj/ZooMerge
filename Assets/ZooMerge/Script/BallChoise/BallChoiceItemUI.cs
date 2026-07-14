using System;
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

    public BallType Type { get; private set; }
    public bool IsSelected { get; private set; }

    public event Action<BallChoiceItemUI> Clicked;

    private void Awake()
    {
        if (selectionButton != null)
            selectionButton.onClick.AddListener(HandleButtonPressed);
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
        SetSelectionState(false, false);
    }

    public void Refresh()
    {
        SetAnimalTypeText();
        SetMergeCountText();
    }

    public void SetSelectionState(
        bool isSelected,
        bool selectionLimitReached)
    {
        IsSelected = isSelected;

        if (selectionVisualController != null)
            selectionVisualController.SetSelected(isSelected);

        bool unavailable =
            selectionLimitReached &&
            !isSelected;

        if (selectionButton != null)
            selectionButton.interactable = true;
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

        int mergeCount = GameInventory.Instance.Get(Type);
        mergeCountText.text = mergeCount.ToString();
    }

    private void SetProfileSprite(Sprite sprite)
    {
        if (profileImage == null)
            return;

        profileImage.sprite = sprite;
        profileImage.enabled = sprite != null;
    }
}