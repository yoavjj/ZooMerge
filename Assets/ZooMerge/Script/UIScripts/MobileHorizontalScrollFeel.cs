using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MobileHorizontalScrollFeel : MonoBehaviour
{
    [Header("Scroll")]
    [SerializeField] private ScrollRect scrollRect;

    [Header("Touch")]
    [SerializeField, Min(1)]
    private int dragThreshold = 4;

    [Header("Motion")]
    [SerializeField, Range(0f, 1f)]
    private float decelerationRate = 0.25f;

    [SerializeField, Range(0f, 1f)]
    private float elasticity = 0.1f;

    private void Awake()
    {
        if (scrollRect == null)
            scrollRect = GetComponent<ScrollRect>();

        if (scrollRect != null)
        {
            scrollRect.horizontal = true;
            scrollRect.vertical = false;

            scrollRect.movementType =
                ScrollRect.MovementType.Elastic;

            scrollRect.inertia = true;

            scrollRect.decelerationRate =
                decelerationRate;

            scrollRect.elasticity =
                elasticity;
        }

        if (EventSystem.current != null)
        {
            EventSystem.current.pixelDragThreshold =
                dragThreshold;
        }
    }
}