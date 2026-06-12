using UnityEngine;

public interface IFlyTargetUI
{
    Sprite GetIcon();
    Vector2 GetFlyTargetScreenPoint();
    void OnArrive(int amount);
}

public interface IFlyingCollectible
{
    RectTransform Rect { get; }
    void SetIcon(Sprite icon);

    void LaunchToLocalPoint(
        Vector2 targetLocalPosition,
        float totalDuration,
        System.Action onArrive,
        float delay,
        float arcHeight,
        float holdDuration,
        AnimationCurve easeInCurve,
        AnimationCurve easeOutCurve
    );
}
