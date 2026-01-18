using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TopBarItemUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI countText;

    [SerializeField] private Animator animator;
    [SerializeField] private string addAnimationName = "TopBarItemAnimation_Add"; // 🔁 name-based

    [Header("Fly Target")]
    [SerializeField] private RectTransform flyTarget;

    public RectTransform FlyTarget => flyTarget != null ? flyTarget : (RectTransform)transform;

    public BallType Type { get; private set; }
    private int count;

    // 👉 When the new value is received (from inventory), we store it and update later via animation
    private int pendingCount;

    // injected (no hierarchy search)
    private Camera canvasCam;

    // Inject camera
    public void InjectUICamera(Camera uiCam)
    {
        canvasCam = uiCam;
    }

    public void Initialize(BallType type, Sprite icon, int startCount)
    {
        Type = type;
        count = startCount;
        pendingCount = startCount;

        if (iconImage != null) iconImage.sprite = icon;
        UpdateCountText(); // initial display
    }

    /// <summary>
    /// Called from TopBarMenu when inventory changes
    /// </summary>
    public void SetCount(int value)
    {
        pendingCount = value;

        // ✅ Play the animation when value changes
        if (animator != null && !string.IsNullOrEmpty(addAnimationName))
        {
            animator.Play(addAnimationName, 0, 0f); // play from start
        }
        else
        {
            // If no animation, fallback to immediate update
            ApplyPendingCount();
        }
    }

    /// <summary>
    /// Called from Animation Event — applies the new count value to the text.
    /// </summary>
    public void ApplyPendingCount()
    {
        count = pendingCount;
        UpdateCountText();
    }

    private void UpdateCountText()
    {
        if (countText != null)
            countText.text = count.ToString();
    }

    public void AddOne()
    {
        SetCount(count + 1);
    }

    public Vector2 GetFlyTargetScreenPoint()
    {
        return RectTransformUtility.WorldToScreenPoint(canvasCam, FlyTarget.position);
    }
}
