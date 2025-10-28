using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class HealthTween
{
    private MonoBehaviour host;

    public HealthTween(MonoBehaviour host)
    {
        this.host = host;
    }

    public void AnimateSliderAndText(
        Slider slider,
        TextMeshProUGUI text,
        float from,
        float to,
        int intFrom,
        int intTo,
        AnimationCurve curve,
        float duration = 0.35f,
        Action onComplete = null)
    {
        host.StartCoroutine(SmoothTransition(slider, text, from, to, intFrom, intTo, curve, duration, onComplete));
    }

    private IEnumerator SmoothTransition(
        Slider slider,
        TextMeshProUGUI text,
        float from,
        float to,
        int intFrom,
        int intTo,
        AnimationCurve curve,
        float duration,
        Action onComplete)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / duration);
            float eased = curve.Evaluate(progress); // ✅ Apply easing

            float currentVal = Mathf.Lerp(from, to, eased);
            int currentInt = Mathf.RoundToInt(Mathf.Lerp(intFrom, intTo, eased));

            if (slider != null) slider.value = currentVal;
            if (text != null) text.text = currentInt.ToString();

            yield return null;
        }

        // Final assignment for accuracy
        if (slider != null) slider.value = to;
        if (text != null) text.text = intTo.ToString();

        onComplete?.Invoke();
    }
}
