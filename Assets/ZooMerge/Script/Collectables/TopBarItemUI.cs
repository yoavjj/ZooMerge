using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TopBarItemUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI countText;

    [Header("Fly Target")]
    [SerializeField] private RectTransform flyTarget;

    public RectTransform FlyTarget => flyTarget != null ? flyTarget : (RectTransform)transform;

    public BallType Type { get; private set; }
    private int count;

    // injected (no hierarchy search)
    private Camera canvasCam;

    // Call this once right after Instantiate
    public void InjectUICamera(Camera uiCam)
    {
        canvasCam = uiCam; // can be null for ScreenSpaceOverlay (that's OK)
    }

    public void Initialize(BallType type, Sprite icon, int startCount)
    {
        Type = type;
        count = startCount;

        if (iconImage != null) iconImage.sprite = icon;
        SetCount(startCount);
    }

    public void SetCount(int value)
    {
        count = value;
        if (countText != null) countText.text = count.ToString();
    }

    public void AddOne() => SetCount(count + 1);

    public Vector2 GetFlyTargetScreenPoint()
    {
        // if overlay, camera should be null
        return RectTransformUtility.WorldToScreenPoint(canvasCam, FlyTarget.position);
    }
}
