using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialFingerHint : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform movementRoot;
    [SerializeField] private RectTransform fingerRoot;
    [SerializeField] private Image fingerImage;
    [SerializeField] private Canvas canvas;

    [Header("Finger Art")]
    [SerializeField] private List<Sprite> fingerArts = new();
    [SerializeField, Min(0)] private int defaultFingerIndex;

    [Header("Horizontal Movement")]
    [SerializeField, Range(0f, 0.45f)]
    private float horizontalInset = 0.12f;

    [SerializeField, Min(0.1f)]
    private float oneWayDuration = 1.1f;

    [SerializeField]
    private AnimationCurve movementCurve =
        AnimationCurve.EaseInOut(
            0f,
            0f,
            1f,
            1f
        );

    [Header("Scale In / Out")]
    [SerializeField, Min(0f)]
    private float scaleInDuration = 0.25f;

    [SerializeField, Min(0f)]
    private float scaleOutDuration = 0.18f;

    [SerializeField]
    private AnimationCurve scaleCurve =
        AnimationCurve.EaseInOut(
            0f,
            0f,
            1f,
            1f
        );

    private CircleDragInput input;

    private Coroutine movementRoutine;
    private Coroutine hideRoutine;

    private Vector3 baseScale;
    private Vector3 baseLocalPosition;

    private float leftX;
    private float rightX;

    private bool isShowing;

    private void Awake()
    {
        if (fingerRoot == null)
            return;

        baseScale = fingerRoot.localScale;
        baseLocalPosition = fingerRoot.localPosition;

        fingerRoot.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        UnsubscribeFromInput();
    }

    public void Play()
    {
        Play(
            CircleDragInput.Instance,
            defaultFingerIndex
        );
    }

    public void Play(
        CircleDragInput dragInput,
        int fingerIndex)
    {
        if (fingerRoot == null ||
            fingerImage == null ||
            movementRoot == null ||
            dragInput == null)
        {
            Debug.LogWarning(
                "[TutorialFingerHint] Missing required reference."
            );

            return;
        }

        StopRoutines();
        UnsubscribeFromInput();

        input = dragInput;

        ApplyFingerArt(
            fingerIndex
        );

        if (!CalculateMovementBounds())
        {
            Debug.LogWarning(
                "[TutorialFingerHint] Could not calculate movement bounds."
            );

            return;
        }

        input.ValidPointerDown +=
            HandleValidPointerDown;

        isShowing = true;

        fingerRoot.gameObject.SetActive(true);

        Vector3 position =
            baseLocalPosition;

        position.x = leftX;

        fingerRoot.localPosition =
            position;

        fingerRoot.localScale =
            Vector3.zero;

        movementRoutine =
            StartCoroutine(
                MovementRoutine()
            );
    }

    public void HideImmediate()
    {
        isShowing = false;

        StopRoutines();
        UnsubscribeFromInput();

        if (fingerRoot != null)
        {
            fingerRoot.localScale =
                Vector3.zero;

            fingerRoot.gameObject.SetActive(false);
        }
    }

    private void HandleValidPointerDown()
    {
        if (!isShowing)
            return;

        isShowing = false;

        UnsubscribeFromInput();

        if (movementRoutine != null)
        {
            StopCoroutine(
                movementRoutine
            );

            movementRoutine = null;
        }

        hideRoutine =
            StartCoroutine(
                HideRoutine()
            );
    }

    private IEnumerator MovementRoutine()
    {
        // --------------------------------
        // SCALE IN
        // --------------------------------

        float elapsed = 0f;

        while (elapsed < scaleInDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float normalized =
                scaleInDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(
                        elapsed /
                        scaleInDuration
                    );

            float t =
                scaleCurve != null
                    ? scaleCurve.Evaluate(
                        normalized
                    )
                    : normalized;

            fingerRoot.localScale =
                Vector3.LerpUnclamped(
                    Vector3.zero,
                    baseScale,
                    t
                );

            yield return null;
        }

        fingerRoot.localScale =
            baseScale;

        // --------------------------------
        // LEFT <-> RIGHT LOOP
        // --------------------------------

        elapsed = 0f;

        while (isShowing)
        {
            elapsed +=
                Time.unscaledDeltaTime;

            float normalized =
                Mathf.PingPong(
                    elapsed /
                    Mathf.Max(
                        0.01f,
                        oneWayDuration
                    ),
                    1f
                );

            float t =
                movementCurve != null
                    ? movementCurve.Evaluate(
                        normalized
                    )
                    : normalized;

            Vector3 position =
                baseLocalPosition;

            position.x =
                Mathf.LerpUnclamped(
                    leftX,
                    rightX,
                    t
                );

            fingerRoot.localPosition =
                position;

            yield return null;
        }

        movementRoutine = null;
    }

    private IEnumerator HideRoutine()
    {
        Vector3 startScale =
            fingerRoot.localScale;

        float elapsed = 0f;

        while (elapsed < scaleOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float normalized =
                scaleOutDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(
                        elapsed /
                        scaleOutDuration
                    );

            float t =
                scaleCurve != null
                    ? scaleCurve.Evaluate(
                        normalized
                    )
                    : normalized;

            fingerRoot.localScale =
                Vector3.LerpUnclamped(
                    startScale,
                    Vector3.zero,
                    t
                );

            yield return null;
        }

        fingerRoot.localScale =
            Vector3.zero;

        fingerRoot.gameObject.SetActive(
            false
        );

        hideRoutine = null;
    }

    private bool CalculateMovementBounds()
    {
        if (input == null)
            return false;

        if (!input.TryGetHorizontalScreenBounds(
                out float leftScreenX,
                out float rightScreenX))
        {
            return false;
        }

        float width =
            rightScreenX -
            leftScreenX;

        float inset =
            width *
            horizontalInset;

        leftScreenX += inset;
        rightScreenX -= inset;

        if (!TryScreenXToLocalX(
                leftScreenX,
                out leftX))
        {
            return false;
        }

        if (!TryScreenXToLocalX(
                rightScreenX,
                out rightX))
        {
            return false;
        }

        return true;
    }

    private bool TryScreenXToLocalX(
        float screenX,
        out float localX)
    {
        localX = 0f;

        Camera uiCamera = null;

        if (canvas != null &&
            canvas.renderMode !=
            RenderMode.ScreenSpaceOverlay)
        {
            uiCamera =
                canvas.worldCamera;
        }

        bool success =
            RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    movementRoot,
                    new Vector2(
                        screenX,
                        Screen.height * 0.5f
                    ),
                    uiCamera,
                    out Vector2 localPoint
                );

        if (!success)
            return false;

        localX = localPoint.x;

        return true;
    }

    private void ApplyFingerArt(
        int fingerIndex)
    {
        if (fingerArts == null ||
            fingerArts.Count == 0)
        {
            return;
        }

        int index =
            Mathf.Clamp(
                fingerIndex,
                0,
                fingerArts.Count - 1
            );

        Sprite sprite =
            fingerArts[index];

        if (sprite != null)
            fingerImage.sprite = sprite;
    }

    private void StopRoutines()
    {
        if (movementRoutine != null)
        {
            StopCoroutine(
                movementRoutine
            );

            movementRoutine = null;
        }

        if (hideRoutine != null)
        {
            StopCoroutine(
                hideRoutine
            );

            hideRoutine = null;
        }
    }

    private void UnsubscribeFromInput()
    {
        if (input == null)
            return;

        input.ValidPointerDown -=
            HandleValidPointerDown;

        input = null;
    }
}