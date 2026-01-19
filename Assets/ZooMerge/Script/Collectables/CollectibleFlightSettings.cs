using UnityEngine;

[CreateAssetMenu(
    fileName = "CollectibleFlightSettings",
    menuName = "Collectibles/Flight Settings",
    order = 0)]
public class CollectibleFlightSettings : ScriptableObject
{
    [Header("Flight Durations")]
    [Min(0.1f)] public float shortFlyDuration = 1f;
    [Min(0.1f)] public float longFlyDuration = 1.55f;

    [Header("Offsets & Motion")]
    [Min(0f)] public float holdDuration = 0.3f;
    public float arcHeight = 100f;

    [Header("Staggering")]
    [Tooltip("Delay between each collectible’s arrival callback (seconds)")]
    public float arrivalStaggerDelay = 0.35f;

    [Header("Easing")]
    public AnimationCurve easeInCurve;
    public AnimationCurve easeOutCurve;
}
