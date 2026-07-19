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

    public override void LaunchToLocalPoint(Vector2 endPosition, float duration, System.Action onComplete = null, float delayTime = 0, float startScale = 1f, float endScale = 1f, AnimationCurve positionCurve = null, AnimationCurve scaleCurve = null)
    {
        
    }
}
