using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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

    private Coroutine countReductionRoutine;

    protected Camera canvasCam;

    public RectTransform FlyTarget => flyTarget != null ? flyTarget : (RectTransform)transform;

    protected virtual void OnDisable()
    {
        if (countReductionRoutine != null)
        {
            StopCoroutine(countReductionRoutine);
            countReductionRoutine = null;
        }
    }

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

    public void AnimateCountReduction(
    int fromValue,
    int toValue,
    float delay,
    float duration)
    {
        if (countReductionRoutine != null)
        {
            StopCoroutine(countReductionRoutine);
            countReductionRoutine = null;
        }

        countReductionRoutine = StartCoroutine(
            CountReductionRoutine(
                fromValue,
                toValue,
                delay,
                duration
            )
        );
    }

    private IEnumerator CountReductionRoutine(
    int fromValue,
    int toValue,
    float delay,
    float duration)
    {
        fromValue = Mathf.Max(0, fromValue);
        toValue = Mathf.Max(0, toValue);

        // Start from the value that existed before the purchase.
        count = fromValue;
        pendingCount = fromValue;
        UpdateCountText();

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (duration <= 0f ||
            fromValue == toValue)
        {
            count = toValue;
            pendingCount = toValue;
            UpdateCountText();

            countReductionRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        int lastDisplayedValue = fromValue;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(elapsed / duration);

            int displayedValue =
                Mathf.RoundToInt(
                    Mathf.Lerp(
                        fromValue,
                        toValue,
                        progress
                    )
                );

            if (displayedValue != lastDisplayedValue)
            {
                lastDisplayedValue = displayedValue;
                count = displayedValue;
                pendingCount = displayedValue;
                UpdateCountText();
            }

            yield return null;
        }

        count = toValue;
        pendingCount = toValue;
        UpdateCountText();

        countReductionRoutine = null;
    }
}
