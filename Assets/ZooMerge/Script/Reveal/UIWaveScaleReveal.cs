using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIWaveScaleReveal : MonoBehaviour
{
    [Header("Objects In Reveal Order")]
    [SerializeField]
    private List<Transform> targets =
        new List<Transform>();

    [Header("Scale")]
    [SerializeField, Range(0f, 1f)]
    private float minScale = 0f;

    [SerializeField, Range(0f, 1.5f)]
    private float maxScale = 1f;

    [Header("Timing")]
    [SerializeField, Min(0.01f)]
    private float itemDuration = 0.25f;

    [SerializeField, Min(0f)]
    private float delayBetweenItems = 0.08f;

    [Header("Feel")]
    [SerializeField]
    private AnimationCurve scaleCurve =
        AnimationCurve.EaseInOut(
            0f,
            0f,
            1f,
            1f
        );

    [Header("Reset")]
    [SerializeField]
    private bool resetOnEnable = true;

    private readonly List<Vector3> baseScales =
        new List<Vector3>();

    private Coroutine waveRoutine;

    private void Awake()
    {
        CacheBaseScales();
    }

    private void OnEnable()
    {
        if (baseScales.Count != targets.Count)
            CacheBaseScales();

        if (resetOnEnable)
            SetAllToMinScale();
    }

    private void OnDisable()
    {
        if (waveRoutine != null)
        {
            StopCoroutine(waveRoutine);
            waveRoutine = null;
        }
    }

    private void CacheBaseScales()
    {
        baseScales.Clear();

        foreach (Transform target in targets)
        {
            baseScales.Add(
                target != null
                    ? target.localScale
                    : Vector3.one
            );
        }
    }

    private void SetAllToMinScale()
    {
        for (int i = 0; i < targets.Count; i++)
        {
            Transform target = targets[i];

            if (target == null)
                continue;

            Vector3 baseScale =
                i < baseScales.Count
                    ? baseScales[i]
                    : Vector3.one;

            target.localScale =
                baseScale * minScale;
        }
    }

    // Animation Event calls this.
    public void AE_PlayWaveReveal()
    {
        if (waveRoutine != null)
            StopCoroutine(waveRoutine);

        SetAllToMinScale();

        waveRoutine =
            StartCoroutine(
                PlayWaveRoutine()
            );
    }

    private IEnumerator PlayWaveRoutine()
    {
        for (int i = 0; i < targets.Count; i++)
        {
            Transform target = targets[i];

            if (target == null)
                continue;

            Vector3 baseScale =
                i < baseScales.Count
                    ? baseScales[i]
                    : Vector3.one;

            // Start this item's animation.
            StartCoroutine(
                ScaleTargetRoutine(
                    target,
                    baseScale
                )
            );

            // Then begin the next item slightly later.
            if (delayBetweenItems > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    delayBetweenItems
                );
            }
        }

        // Wait enough for the final item to finish.
        if (itemDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                itemDuration
            );
        }

        waveRoutine = null;
    }

    private IEnumerator ScaleTargetRoutine(
        Transform target,
        Vector3 baseScale)
    {
        float elapsed = 0f;

        Vector3 startScale =
            baseScale * minScale;

        Vector3 endScale =
            baseScale * maxScale;

        target.localScale =
            startScale;

        while (elapsed < itemDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float normalized =
                Mathf.Clamp01(
                    elapsed / itemDuration
                );

            float curved =
                scaleCurve != null
                    ? scaleCurve.Evaluate(normalized)
                    : normalized;

            target.localScale =
                Vector3.LerpUnclamped(
                    startScale,
                    endScale,
                    curved
                );

            yield return null;
        }

        target.localScale = endScale;
    }
}