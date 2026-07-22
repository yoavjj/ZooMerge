using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
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

    [Header("Completed Animator")]
    [SerializeField] private Animator requirementAnimator;
    [SerializeField] private string doneTrigger = "Done";

    [Header("Completed Shader")]
    [Tooltip("Graphic that receives the grayscale/color material.")]
    [SerializeField] private Graphic targetGraphic;

    [Tooltip("Material using the same shader as GalaxyColorAnimator.")]
    [SerializeField] private Material sourceMaterial;

    [SerializeField] private bool overrideMaterial = true;

    [Tooltip("Shader float property controlled by the animation.")]
    [SerializeField] private string blendProperty = "_Blend";

    [Header("Animated Shader Value")]
    [Tooltip("Keyframe this value in the Done animation.")]
    [Range(0f, 1f)]
    [SerializeField] private float blend = 1f;

    [Header("Completion Timing")]
    [SerializeField, Range(0f, 1f)]
    private float doneTriggerAtProgress = 0.88f;

    private Coroutine fillRoutine;
    private Material runtimeMaterial;
    private Material originalMaterial;

    private int cachedVisibleCurrent;
    private int cachedRequiredAmount;
    private float cachedTargetProgress;
    private bool cachedRequirementComplete;
    private bool donePlayed;

    private void Reset()
    {
        if (targetGraphic == null)
            targetGraphic = GetComponent<Graphic>();

        if (requirementAnimator == null)
            requirementAnimator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        EnsureMaterialInstance();
        ApplyShaderValue();
    }

    private void OnValidate()
    {
        EnsureMaterialInstance();
        ApplyShaderValue();
    }

    private void Update()
    {
        // Allows previewing the Blend value in Edit Mode.
        if (!Application.isPlaying)
            ApplyShaderValue();
    }

    private void OnDidApplyAnimationProperties()
    {
        // Called when the Animator keyframes the serialized blend field.
        ApplyShaderValue();
    }

    public void Initialize(
        string requirementName,
        Sprite icon,
        int currentAmount,
        int requiredAmount)
    {
        currentAmount = Mathf.Max(0, currentAmount);
        requiredAmount = Mathf.Max(0, requiredAmount);

        donePlayed = false;

        if (fillRoutine != null)
        {
            StopCoroutine(fillRoutine);
            fillRoutine = null;
        }

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        cachedRequiredAmount = requiredAmount;

        cachedVisibleCurrent = requiredAmount > 0
            ? Mathf.Min(currentAmount, requiredAmount)
            : currentAmount;

        cachedTargetProgress = requiredAmount > 0
            ? Mathf.Clamp01(
                (float)currentAmount / requiredAmount
            )
            : 1f;

        cachedRequirementComplete =
            requiredAmount <= 0 ||
            currentAmount >= requiredAmount;

        if (progressSlider != null)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.wholeNumbers = false;
            progressSlider.interactable = false;

            // Wait for the popup animation event.
            progressSlider.SetValueWithoutNotify(0f);
        }

        if (requirementText != null)
        {
            requirementText.text =
                $"0/{cachedRequiredAmount}";
        }

        EnsureMaterialInstance();
        ApplyShaderValue();
    }

    public void PlayFillAnimation()
    {
        if (!Application.isPlaying)
            return;

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

        if (cachedRequirementComplete)
            PlayDone();
    }

    private IEnumerator AnimateProgressRoutine()
    {
        float startProgress = progressSlider != null
            ? progressSlider.value
            : 0f;

        int startCount = 0;
        bool doneTriggeredDuringFill = false;

        if (fillDuration <= 0f)
        {
            if (progressSlider != null)
            {
                progressSlider.SetValueWithoutNotify(
                    cachedTargetProgress
                );
            }

            UpdateProgressText(cachedVisibleCurrent);

            if (cachedRequirementComplete)
                PlayDone();

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

            if (cachedRequirementComplete &&
                !doneTriggeredDuringFill &&
                normalizedTime >= doneTriggerAtProgress)
            {
                doneTriggeredDuringFill = true;
                PlayDone();
            }

            yield return null;
        }

        if (progressSlider != null)
        {
            progressSlider.SetValueWithoutNotify(
                cachedTargetProgress
            );
        }

        UpdateProgressText(cachedVisibleCurrent);

        // Safety in case the threshold was never reached.
        if (cachedRequirementComplete &&
            !doneTriggeredDuringFill)
        {
            PlayDone();
        }

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

    public void PlayDone()
    {
        if (donePlayed)
            return;

        donePlayed = true;

        if (requirementAnimator == null ||
            string.IsNullOrEmpty(doneTrigger))
        {
            return;
        }

        requirementAnimator.ResetTrigger(doneTrigger);
        requirementAnimator.SetTrigger(doneTrigger);
    }

    public void SetBlend(float value)
    {
        blend = Mathf.Clamp01(value);

        EnsureMaterialInstance();
        ApplyShaderValue();
    }

    private void EnsureMaterialInstance()
    {
        if (targetGraphic == null)
            return;

        if (originalMaterial == null)
            originalMaterial = targetGraphic.material;

        Material baseMaterial =
            overrideMaterial && sourceMaterial != null
                ? sourceMaterial
                : targetGraphic.material;

        if (baseMaterial == null)
            return;

        if (runtimeMaterial != null &&
            targetGraphic.material == runtimeMaterial)
        {
            return;
        }

        // Avoid cloning our own runtime instance repeatedly.
        if (runtimeMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(runtimeMaterial);
            else
                DestroyImmediate(runtimeMaterial);

            runtimeMaterial = null;
        }

        runtimeMaterial = Instantiate(baseMaterial);
        runtimeMaterial.name =
            $"{baseMaterial.name}_{gameObject.name}_Runtime";

        targetGraphic.material = runtimeMaterial;
    }

    private void ApplyShaderValue()
    {
        if (targetGraphic == null)
            return;

        Material material = targetGraphic.material;

        if (material == null)
            return;

        if (material.HasProperty(blendProperty))
        {
            material.SetFloat(
                blendProperty,
                blend
            );

            targetGraphic.SetMaterialDirty();
        }
    }

    private void OnDisable()
    {
        if (fillRoutine != null)
        {
            StopCoroutine(fillRoutine);
            fillRoutine = null;
        }

        CleanupRuntimeMaterial();
    }

    private void OnDestroy()
    {
        CleanupRuntimeMaterial();
    }

    private void CleanupRuntimeMaterial()
    {
        if (runtimeMaterial == null)
            return;

        if (targetGraphic != null &&
            targetGraphic.material == runtimeMaterial &&
            originalMaterial != null)
        {
            targetGraphic.material = originalMaterial;
        }

        if (Application.isPlaying)
            Destroy(runtimeMaterial);
        else
            DestroyImmediate(runtimeMaterial);

        runtimeMaterial = null;
    }
}