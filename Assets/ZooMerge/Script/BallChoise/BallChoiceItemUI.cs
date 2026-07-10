using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BallChoiceItemUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI animalTypeText;
    [SerializeField] private TextMeshProUGUI mergeCountText;
    [SerializeField] private Image profileImage;

    public BallType Type { get; private set; }

    public void Initialize(BallType type, Sprite profileSprite)
    {
        Type = type;

        SetProfileSprite(profileSprite);
        Refresh();
    }

    public void Refresh()
    {
        SetAnimalTypeText();
        SetMergeCountText();
    }

    private void SetAnimalTypeText()
    {
        if (animalTypeText == null)
            return;

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