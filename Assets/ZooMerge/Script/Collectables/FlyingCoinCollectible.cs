using System.Collections;
using UnityEngine;

public class FlyingCoinCollectible : BaseFlyingCollectible
{
    [SerializeField] private FlyingCollectible visualLogic;

    public override RectTransform Rect => visualLogic.Rect;

    public override void SetIcon(Sprite sprite)
    {
        visualLogic.SetIcon(sprite);
    }

    public override void LaunchToLocalPoint(
        Vector2 targetLocalPosition,
        float totalDuration,
        System.Action onArrive,
        float delay = 0f,
        float arcHeight = 100f,
        float holdDuration = 0.2f,
        AnimationCurve easeInCurve = null,
        AnimationCurve easeOutCurve = null)
    {
        if (visualLogic == null)
        {
            Debug.LogError("❌ visualLogic not set on FlyingCoinCollectible!");
            return;
        }

        visualLogic.LaunchToLocalPoint(
            targetLocalPosition,
            totalDuration,
            () =>
            {
                onArrive?.Invoke();
                //OnArrive(1); // trigger sparkles / logs etc
                Destroy(gameObject);
            },
            delay,
            arcHeight,
            holdDuration,
            easeInCurve,
            easeOutCurve
        );
    }

    public override void OnArrive(int count)
    {
        // Optional sparkles or sound on coin collect
        // e.g., play VFX, trigger UI animation, etc.
    }
}
