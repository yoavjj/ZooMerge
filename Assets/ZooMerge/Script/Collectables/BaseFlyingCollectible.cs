using System;
using UnityEngine;

public abstract class BaseFlyingCollectible : MonoBehaviour, IFlyingCollectible
{
    public abstract RectTransform Rect { get; }
    public abstract void SetIcon(Sprite sprite);

    /// <summary>
    /// Override this if a collectible needs a special effect when arriving.
    /// </summary>
    public virtual void OnArrive(int count) { }

    // Only flying collectibles must implement this:
    public abstract void LaunchToLocalPoint(
        Vector2 targetLocalPosition,
        float totalDuration,
        Action onArrive,
        float delay,
        float arcHeight,
        float holdDuration,
        AnimationCurve easeInCurve,
        AnimationCurve easeOutCurve
    );
}
