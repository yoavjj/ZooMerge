using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GalaxyProgressSlider : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider slider;

    [Header("Optional Progress Text")]
    [SerializeField] private TextMeshProUGUI progressText; // shows "1/10"
    [SerializeField] private TextMeshProUGUI levelNameText;

    [Header("Animation")]
    [SerializeField, Min(0f)] private float animDuration = 0.25f;
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Fill Mapping")]
    [Tooltip("If true: 1/10 -> 0.1 fill. If false: Level 1 -> 0 fill, Level N -> 1 fill.")]
    [SerializeField] private bool useLevelOverTotal = true;
    [SerializeField] private bool treatCurrentAsCompleted = true;

    [Header("Callback Timing")]
    [SerializeField, Range(0f, 1f)] private float triggerPoint = 1f;

    private bool hasTriggered;

    private Coroutine anim;
    public System.Action OnAnimationComplete;

    // Keep your old API working
    public void SetInstant(float normalized01)
    {
        if (slider == null) return;
        if (anim != null) StopCoroutine(anim);
        slider.value = Mathf.Clamp01(normalized01);
    }

    public void AnimateTo(float normalized01)
    {
        if (slider == null) return;
        normalized01 = Mathf.Clamp01(normalized01);

        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(AnimateRoutine(slider.value, normalized01));
    }

    /// <summary>
    /// New: set both the numeric text and the fill using current/total.
    /// Example: current=1 total=10 -> text "1/10" and fill 0.1 (if useLevelOverTotal is true)
    /// </summary>
    public void SetLevelIndexProgress(int levelIndex1Based, int total, bool animate = true, bool updateTexts = true)
    {
        int safeTotal = Mathf.Max(1, total);
        int completed = Mathf.Clamp(levelIndex1Based - 1, 0, safeTotal);
        ApplyProgress(completed, safeTotal, animate, updateTexts);
    }

    public void SetCompletedProgress(int completed, int total, bool animate = true, bool updateTexts = true)
    {
        int safeTotal = Mathf.Max(1, total);
        int safeCompleted = Mathf.Clamp(completed, 0, safeTotal);
        ApplyProgress(safeCompleted, safeTotal, animate, updateTexts);
    }

    private void ApplyProgress(int completed, int total, bool animate, bool updateTexts)
    {
        if (updateTexts)
        {

            if (levelNameText != null)
                levelNameText.text = MergeLevelManager.CurrentGalaxyName;
        }

        if (progressText != null)
            progressText.text = $"{completed}/{total}";

        float normalized = (float)completed / total;

        if (animate) AnimateTo(normalized);
        else SetInstant(normalized);
    }

    private IEnumerator AnimateRoutine(float from, float to)
    {
        if (animDuration <= 0f)
        {
            slider.value = to;
            anim = null;
            OnAnimationComplete?.Invoke();
            yield break;
        }

        float t = 0f;
        hasTriggered = false;

        while (t < 1f)
        {
            t += Time.deltaTime / animDuration;
            float normalizedT = Mathf.Clamp01(t);

            float k = curve != null ? curve.Evaluate(normalizedT) : normalizedT;
            slider.value = Mathf.Lerp(from, to, k);

            // ✅ Trigger at custom point
            if (!hasTriggered && normalizedT >= triggerPoint)
            {
                hasTriggered = true;
                OnAnimationComplete?.Invoke();
            }

            yield return null;
        }

        slider.value = to;
        anim = null;

        // Safety: ensure it fires if triggerPoint == 1 and missed due to precision
        if (!hasTriggered)
            OnAnimationComplete?.Invoke();
    }
}