using System.Collections;
using UnityEngine;

public class SpaceshipHoverMotion : MonoBehaviour
{
    [Header("Position")]
    [SerializeField] private float horizontalRange = 3f;
    [SerializeField] private float verticalRange = 8f;

    [Header("Rotation")]
    [SerializeField] private float rotationRange = 2f;

    [Header("Motion")]
    [SerializeField] private float noiseSpeed = 0.25f;
    [SerializeField] private float positionSmoothTime = 0.6f;
    [SerializeField] private float rotationSmoothTime = 0.8f;

    [Header("Optional Scale Breathing")]
    [SerializeField] private bool useScaleBreathing = true;
    [SerializeField] private float scaleAmount = 0.015f;
    [SerializeField] private float scaleSmoothTime = 0.8f;

    private RectTransform rect;

    private Vector2 basePosition;
    private Vector3 baseScale;
    private float baseRotation;

    private Vector2 positionVelocity;
    private float rotationVelocity;
    private float scaleVelocity;

    private float noiseSeedX;
    private float noiseSeedY;
    private float noiseSeedRot;
    private float noiseSeedScale;

    private Coroutine hoverRoutine;

    private void Awake()
    {
        rect = transform as RectTransform;

        if (rect != null)
            basePosition = rect.anchoredPosition;

        baseRotation = transform.localEulerAngles.z;
        baseScale = transform.localScale;

        noiseSeedX = Random.Range(0f, 1000f);
        noiseSeedY = Random.Range(0f, 1000f);
        noiseSeedRot = Random.Range(0f, 1000f);
        noiseSeedScale = Random.Range(0f, 1000f);
    }

    private void OnEnable()
    {
        if (hoverRoutine != null)
            StopCoroutine(hoverRoutine);

        hoverRoutine = StartCoroutine(
            HoverRoutine()
        );
    }

    private void OnDisable()
    {
        if (hoverRoutine != null)
        {
            StopCoroutine(hoverRoutine);
            hoverRoutine = null;
        }
    }

    private IEnumerator HoverRoutine()
    {
        while (true)
        {
            float time =
                Time.unscaledTime *
                noiseSpeed;

            // --------------------------------
            // RANDOM SMOOTH POSITION TARGET
            // --------------------------------

            float noiseX =
                Mathf.PerlinNoise(
                    noiseSeedX,
                    time
                ) * 2f - 1f;

            float noiseY =
                Mathf.PerlinNoise(
                    noiseSeedY,
                    time
                ) * 2f - 1f;

            Vector2 targetPosition =
                basePosition +
                new Vector2(
                    noiseX * horizontalRange,
                    noiseY * verticalRange
                );

            if (rect != null)
            {
                rect.anchoredPosition =
                    Vector2.SmoothDamp(
                        rect.anchoredPosition,
                        targetPosition,
                        ref positionVelocity,
                        positionSmoothTime,
                        Mathf.Infinity,
                        Time.unscaledDeltaTime
                    );
            }

            // --------------------------------
            // RANDOM SMOOTH ROTATION
            // --------------------------------

            float rotationNoise =
                Mathf.PerlinNoise(
                    noiseSeedRot,
                    time * 0.7f
                ) * 2f - 1f;

            float targetRotation =
                baseRotation +
                rotationNoise *
                rotationRange;

            float currentRotation =
                transform.localEulerAngles.z;

            float rotation =
                Mathf.SmoothDampAngle(
                    currentRotation,
                    targetRotation,
                    ref rotationVelocity,
                    rotationSmoothTime,
                    Mathf.Infinity,
                    Time.unscaledDeltaTime
                );

            transform.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    rotation
                );

            // --------------------------------
            // OPTIONAL SCALE BREATHING
            // --------------------------------

            if (useScaleBreathing)
            {
                float scaleNoise =
                    Mathf.PerlinNoise(
                        noiseSeedScale,
                        time * 0.5f
                    ) * 2f - 1f;

                float targetScale =
                    1f +
                    scaleNoise *
                    scaleAmount;

                float currentScale =
                    transform.localScale.x /
                    baseScale.x;

                float smoothedScale =
                    Mathf.SmoothDamp(
                        currentScale,
                        targetScale,
                        ref scaleVelocity,
                        scaleSmoothTime,
                        Mathf.Infinity,
                        Time.unscaledDeltaTime
                    );

                transform.localScale =
                    baseScale *
                    smoothedScale;
            }

            yield return null;
        }
    }
}