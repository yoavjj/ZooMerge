using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BallUnlockRequirementItemUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI requirementText;
    [SerializeField] private Slider progressSlider;

    [Header("Progress Animation")]
    [SerializeField, Min(0f)]
    private float fillDuration = 0.5f;

    [SerializeField]
    private AnimationCurve fillCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Coroutine fillRoutine;

    private int cachedVisibleCurrent;
    private int cachedRequiredAmount;
    private float cachedTargetProgress;

    public void Initialize(
        string requirementName,
        Sprite icon,
        int currentAmount,
        int requiredAmount)
    {
        currentAmount = Mathf.Max(0, currentAmount);
        requiredAmount = Mathf.Max(0, requiredAmount);

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        cachedRequiredAmount = requiredAmount;

        cachedVisibleCurrent = requiredAmount > 0
            ? Mathf.Min(currentAmount, requiredAmount)
            : currentAmount;

        cachedTargetProgress =
            requiredAmount > 0
                ? Mathf.Clamp01(
                    (float)currentAmount / requiredAmount
                )
                : 1f;

        if (progressSlider != null)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.wholeNumbers = false;
            progressSlider.interactable = false;

            progressSlider.SetValueWithoutNotify(0f);
        }

        if (requirementText != null)
        {
            requirementText.text =
                $"0/{cachedRequiredAmount}";
        }
    }

    public void PlayFillAnimation()
    {
        if (fillRoutine != null)
            StopCoroutine(fillRoutine);

        fillRoutine = StartCoroutine(
            AnimateProgressRoutine()
        );
    }

    public void SetProgressImmediate()
    {
        if (fillRoutine != null)
        {
            StopCoroutine(fillRoutine);
            fillRoutine = null;
        }

        if (progressSlider != null)
        {
            progressSlider.SetValueWithoutNotify(
                cachedTargetProgress
            );
        }

        UpdateProgressText(cachedVisibleCurrent);
    }

    private IEnumerator AnimateProgressRoutine()
    {
        float startProgress = progressSlider != null
            ? progressSlider.value
            : 0f;

        int startCount = 0;

        if (fillDuration <= 0f)
        {
            if (progressSlider != null)
            {
                progressSlider.SetValueWithoutNotify(
                    cachedTargetProgress
                );
            }

            UpdateProgressText(cachedVisibleCurrent);

            fillRoutine = null;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < fillDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float normalizedTime =
                Mathf.Clamp01(elapsed / fillDuration);

            float curvedTime = fillCurve != null
                ? fillCurve.Evaluate(normalizedTime)
                : normalizedTime;

            if (progressSlider != null)
            {
                float sliderValue = Mathf.Lerp(
                    startProgress,
                    cachedTargetProgress,
                    curvedTime
                );

                progressSlider.SetValueWithoutNotify(
                    sliderValue
                );
            }

            int visibleCount = Mathf.RoundToInt(
                Mathf.Lerp(
                    startCount,
                    cachedVisibleCurrent,
                    curvedTime
                )
            );

            UpdateProgressText(visibleCount);

            yield return null;
        }

        if (progressSlider != null)
        {
            progressSlider.SetValueWithoutNotify(
                cachedTargetProgress
            );
        }

        UpdateProgressText(cachedVisibleCurrent);

        fillRoutine = null;
    }

    private void UpdateProgressText(int currentValue)
    {
        if (requirementText == null)
            return;

        currentValue = Mathf.Clamp(
            currentValue,
            0,
            cachedVisibleCurrent
        );

        requirementText.text =
            $"{currentValue}/{cachedRequiredAmount}";
    }

    private void OnDisable()
    {
        if (fillRoutine != null)
        {
            StopCoroutine(fillRoutine);
            fillRoutine = null;
        }
    }
}