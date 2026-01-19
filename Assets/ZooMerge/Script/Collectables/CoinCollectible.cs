using UnityEngine;
using UnityEngine.UI;

public class CoinCollectible : BaseFlyingCollectible
{
    [SerializeField] private Image iconImage;

    private RectTransform rect;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    public override RectTransform Rect => rect;

    public override void SetIcon(Sprite sprite)
    {
        if (iconImage != null)
            iconImage.sprite = sprite;
    }

    public override void OnArrive(int count)
    {

    }
}
