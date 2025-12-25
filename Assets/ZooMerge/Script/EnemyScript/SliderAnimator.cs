using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SliderAnimator
{
    private readonly Slider _slider;
    private MonoBehaviour _host;
    private Coroutine _routine;

    public SliderAnimator(Slider slider, MonoBehaviour host)
    {
        _slider = slider;
        _host = host;
    }

    public void AnimateTo(float target, float duration, AnimationCurve curve = null)
    {
        if (_slider == null || _host == null) return;
        if (_routine != null) _host.StopCoroutine(_routine);
        _routine = _host.StartCoroutine(AnimateRoutine(target, duration, curve));
    }

    public void SetInstant(float value)
    {
        if (_slider == null) return;
        _slider.value = Mathf.Clamp(value, _slider.minValue, _slider.maxValue);
    }

    public void SetPreviousIndex()
    {
        int total = MergeLevelManager.TotalEnemiesInLevel;
        int nextIndex = Mathf.Clamp(MergeLevelManager.CurrentEnemyIndex, 0, total);
        float startIndex = Mathf.Clamp(nextIndex - 1f, _slider.minValue, _slider.maxValue);
        _slider.value = startIndex;
    }

    private IEnumerator AnimateRoutine(float target, float duration, AnimationCurve curve)
    {
        float start = _slider.value;
        if (duration <= 0f)
        {
            _slider.value = target;
            _routine = null;
            yield break;
        }

        var ease = curve ?? AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float k = ease.Evaluate(Mathf.Clamp01(t));
            _slider.value = Mathf.Lerp(start, target, k);
            yield return null;
        }

        _slider.value = target;
        _routine = null;
    }
}
