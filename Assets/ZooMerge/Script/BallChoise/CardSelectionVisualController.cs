using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardSelectionVisualController : MonoBehaviour
{
    private static readonly int GrayscaleId =
        Shader.PropertyToID("_Grayscale");

    private static readonly int BrightnessId =
        Shader.PropertyToID("_Brightness");

    private static readonly int ContrastId =
        Shader.PropertyToID("_Contrast");

    [Header("Material")]
    [Tooltip("Material using the UI/Grayscale Faded shader.")]
    [SerializeField] private Material grayscaleMaterial;

    [Header("Unselected Appearance")]
    [SerializeField, Range(0f, 1f)]
    private float unselectedGrayscale = 1f;

    [SerializeField, Range(0f, 1f)]
    private float unselectedBrightness = 0.45f;

    [SerializeField, Range(0f, 2f)]
    private float unselectedContrast = 0.85f;

    [Header("Selected Appearance")]
    [SerializeField, Range(0f, 1f)]
    private float selectedGrayscale = 0f;

    [SerializeField, Range(0f, 1f)]
    private float selectedBrightness = 1f;

    [SerializeField, Range(0f, 2f)]
    private float selectedContrast = 1f;

    [Header("Transition")]
    [SerializeField, Min(0f)]
    private float transitionDuration = 0.2f;

    [SerializeField]
    private AnimationCurve transitionCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Selection Pulse")]
    [SerializeField]
    private bool animateScale = true;

    [SerializeField, Min(1f)]
    private float selectedPulseScale = 1.06f;

    [SerializeField, Min(0f)]
    private float pulseDuration = 0.18f;

    [SerializeField]
    private AnimationCurve pulseCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.45f, 1f),
        new Keyframe(1f, 0f)
    );

    [Header("Targets")]
    [Tooltip("When enabled, all child Images are collected automatically.")]
    [SerializeField] private bool findChildImagesAutomatically = true;

    [Tooltip("Optional manual list when automatic collection is disabled.")]
    [SerializeField] private List<Image> targetImages = new();

    [Tooltip("Images that should keep their normal material.")]
    [SerializeField] private List<Image> excludedImages = new();

    private Material runtimeMaterial;
    private Coroutine transitionRoutine;
    private Coroutine pulseRoutine;

    private Vector3 originalScale;
    private bool isSelected;

    public bool IsSelected => isSelected;

    private void Awake()
    {
        originalScale = transform.localScale;

        CollectImages();
        CreateRuntimeMaterial();

        // Opening state should be immediate, not animated.
        SetSelectedImmediate(false);
    }

    private void OnDisable()
    {
        StopRunningAnimations();
        transform.localScale = originalScale;
    }

    private void OnDestroy()
    {
        StopRunningAnimations();

        if (runtimeMaterial != null)
            Destroy(runtimeMaterial);
    }

    private void CollectImages()
    {
        if (!findChildImagesAutomatically)
            return;

        targetImages.Clear();

        Image[] childImages =
            GetComponentsInChildren<Image>(true);

        foreach (Image image in childImages)
        {
            if (image == null)
                continue;

            if (excludedImages.Contains(image))
                continue;

            targetImages.Add(image);
        }
    }

    private void CreateRuntimeMaterial()
    {
        if (grayscaleMaterial == null)
        {
            Debug.LogError(
                $"[{nameof(CardSelectionVisualController)}] " +
                $"No grayscale material assigned on {gameObject.name}."
            );

            return;
        }

        runtimeMaterial = new Material(grayscaleMaterial)
        {
            name = $"{grayscaleMaterial.name}_{gameObject.name}_Runtime"
        };

        foreach (Image image in targetImages)
        {
            if (image != null)
                image.material = runtimeMaterial;
        }
    }

    public void SetSelected(bool selected)
    {
        bool stateChanged = isSelected != selected;
        isSelected = selected;

        if (runtimeMaterial == null)
            return;

        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        transitionRoutine = StartCoroutine(
            AnimateMaterialState(selected)
        );

        if (stateChanged && animateScale)
        {
            if (pulseRoutine != null)
                StopCoroutine(pulseRoutine);

            pulseRoutine = StartCoroutine(
                AnimateSelectionPulse(selected)
            );
        }
    }

    public void SetSelectedImmediate(bool selected)
    {
        isSelected = selected;

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
            pulseRoutine = null;
        }

        transform.localScale = originalScale;

        ApplyMaterialValues(
            selected ? selectedGrayscale : unselectedGrayscale,
            selected ? selectedBrightness : unselectedBrightness,
            selected ? selectedContrast : unselectedContrast
        );
    }

    private IEnumerator AnimateMaterialState(bool selected)
    {
        float startGrayscale =
            runtimeMaterial.GetFloat(GrayscaleId);

        float startBrightness =
            runtimeMaterial.GetFloat(BrightnessId);

        float startContrast =
            runtimeMaterial.GetFloat(ContrastId);

        float targetGrayscale = selected
            ? selectedGrayscale
            : unselectedGrayscale;

        float targetBrightness = selected
            ? selectedBrightness
            : unselectedBrightness;

        float targetContrast = selected
            ? selectedContrast
            : unselectedContrast;

        if (transitionDuration <= 0f)
        {
            ApplyMaterialValues(
                targetGrayscale,
                targetBrightness,
                targetContrast
            );

            transitionRoutine = null;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float normalizedTime =
                Mathf.Clamp01(elapsed / transitionDuration);

            float easedTime =
                transitionCurve.Evaluate(normalizedTime);

            ApplyMaterialValues(
                Mathf.Lerp(
                    startGrayscale,
                    targetGrayscale,
                    easedTime
                ),
                Mathf.Lerp(
                    startBrightness,
                    targetBrightness,
                    easedTime
                ),
                Mathf.Lerp(
                    startContrast,
                    targetContrast,
                    easedTime
                )
            );

            yield return null;
        }

        ApplyMaterialValues(
            targetGrayscale,
            targetBrightness,
            targetContrast
        );

        transitionRoutine = null;
    }

    private IEnumerator AnimateSelectionPulse(bool selected)
    {
        if (pulseDuration <= 0f)
        {
            transform.localScale = originalScale;
            pulseRoutine = null;
            yield break;
        }

        float elapsed = 0f;

        // Selected cards grow slightly.
        // Deselected cards shrink slightly before returning.
        float direction = selected ? 1f : -1f;
        float scaleDifference = selectedPulseScale - 1f;

        while (elapsed < pulseDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float normalizedTime =
                Mathf.Clamp01(elapsed / pulseDuration);

            float curveValue =
                pulseCurve.Evaluate(normalizedTime);

            float scaleMultiplier =
                1f + scaleDifference * curveValue * direction;

            transform.localScale =
                originalScale * scaleMultiplier;

            yield return null;
        }

        transform.localScale = originalScale;
        pulseRoutine = null;
    }

    private void ApplyMaterialValues(
        float grayscale,
        float brightness,
        float contrast)
    {
        if (runtimeMaterial == null)
            return;

        runtimeMaterial.SetFloat(
            GrayscaleId,
            grayscale
        );

        runtimeMaterial.SetFloat(
            BrightnessId,
            brightness
        );

        runtimeMaterial.SetFloat(
            ContrastId,
            contrast
        );
    }

    private void StopRunningAnimations()
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
            pulseRoutine = null;
        }
    }
}