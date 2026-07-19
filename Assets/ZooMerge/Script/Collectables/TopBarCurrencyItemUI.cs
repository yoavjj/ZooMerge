using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class TopBarCurrencyItemUI : SfxBehaviourTirgger
{
    [SerializeField] protected Image iconImage;
    [SerializeField] protected TextMeshProUGUI countText;

    [SerializeField] protected Animator animator;
    [SerializeField] protected string addAnimationName = "TopBarItemAnimation_Add";

    [Header("Fly Target")]
    [SerializeField] protected RectTransform flyTarget;

    protected int count;
    protected int pendingCount;
    protected Camera canvasCam;

    public RectTransform FlyTarget => flyTarget != null ? flyTarget : (RectTransform)transform;

    public void InjectUICamera(Camera uiCam)
    {
        canvasCam = uiCam;
    }

    public virtual void Initialize(Sprite icon, int startCount)
    {
        count = startCount;
        pendingCount = startCount;

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            Debug.Log($"✅ Icon set on {gameObject.name}: {icon.name}");
        }

        UpdateCountText();
    }

    public virtual void SetCount(int value)
    {
        pendingCount = value;

        // ✅ Only play "Add" animation if value is increasing
        bool isIncrease = pendingCount > count;

        if (isIncrease && animator != null && !string.IsNullOrEmpty(addAnimationName))
        {
            animator.Play(addAnimationName, 0, 0f);
        }
        else
        {
            ApplyPendingCount(); // decrease or same value -> update immediately
        }
    }

    public void SetCountImmediate(int value)
    {
        pendingCount = value;
        ApplyPendingCount();
    }

    public virtual void ApplyPendingCount()
    {
        count = pendingCount;
        UpdateCountText();
    }

    protected virtual void UpdateCountText()
    {
        if (countText != null)
            countText.text = count.ToString();
    }

    public Vector2 GetFlyTargetScreenPoint()
    {
        return RectTransformUtility.WorldToScreenPoint(canvasCam, FlyTarget.position);
    }

    public Sprite GetIcon()
    {
        if (iconImage == null)
        {
            Debug.LogWarning($"⚠️ iconImage is null on {gameObject.name}");
            return null;
        }

        if (iconImage.sprite == null)
        {
            Debug.LogWarning($"⚠️ iconImage.sprite is null on {gameObject.name}");
            return null;
        }

        return iconImage.sprite;
    }
}
