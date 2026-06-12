using UnityEngine;
using UnityEngine.UI;

public class CollectibleFlyTarget : MonoBehaviour, IFlyTargetUI
{
    [Header("Target Point")]
    [SerializeField] private RectTransform targetRect;

    [Header("Spawn (optional override)")]
    [SerializeField] private RectTransform spawnContainerOverride;

    [Header("Icon (optional, used for spawned collectible icon)")]
    [SerializeField] private Image iconImage;

    [Header("On Arrive Target (assign a handler script here)")]
    [SerializeField] private MonoBehaviour onArriveTarget; // must implement ICollectibleArriveHandler

    [Header("Arrive UI FX")]
    [SerializeField] private Animator arriveAnimator;     // assign in inspector (NO GetComponentInChildren)
    [SerializeField] private string arriveTrigger = "Add";

    private ICollectibleArriveHandler handler;

    public int LastArriveAmount { get; private set; } = 0;
    private bool hasPendingArrive;

    public RectTransform GetSpawnContainerOverride() => spawnContainerOverride;

    private void Reset()
    {
        targetRect = GetComponent<RectTransform>();
        iconImage = GetComponent<Image>();
    }

    private void Awake() => CacheHandler();

    private void OnEnable()
    {
        CacheHandler();
    }

    private void OnValidate()
    {
        CacheHandler();
        if (targetRect == null) targetRect = GetComponent<RectTransform>();
        if (iconImage == null) iconImage = GetComponent<Image>();
    }

    private void CacheHandler()
    {
        handler = onArriveTarget as ICollectibleArriveHandler;

        if (onArriveTarget != null && handler == null)
        {
            Debug.LogError(
                $"[{nameof(CollectibleFlyTarget)}] On Arrive Target is '{onArriveTarget.GetType().FullName}' " +
                $"but it does NOT implement ICollectibleArriveHandler.",
                this);
        }
    }

    public Sprite GetIcon() => iconImage != null ? iconImage.sprite : null;

    public Vector2 GetFlyTargetScreenPoint()
    {
        var canvas = GetComponentInParent<Canvas>();
        var cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

        Vector3 world = targetRect.TransformPoint(targetRect.rect.center);
        return RectTransformUtility.WorldToScreenPoint(cam, world);
    }


    public void OnArrive(int amount)
    {
        LastArriveAmount = amount;
        hasPendingArrive = true;

        if (arriveAnimator != null && !string.IsNullOrEmpty(arriveTrigger))
        {
            arriveAnimator.ResetTrigger(arriveTrigger);
            arriveAnimator.SetTrigger(arriveTrigger);
        }
    }

    public void CommitPendingArrive()
    {
        if (!hasPendingArrive)
            return;

        hasPendingArrive = false;

        if (handler == null && onArriveTarget != null)
            CacheHandler();

        if (handler == null)
        {
            Debug.LogError(
                $"[{nameof(CollectibleFlyTarget)}] No arrive handler assigned on '{name}'. " +
                $"Assign a component like AddRetriesOnArrive to 'On Arrive Target'.",
                this);
            return;
        }

        handler.HandleArrive(LastArriveAmount, gameObject.name, gameObject);
    }
}