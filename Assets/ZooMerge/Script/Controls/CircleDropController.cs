using System.Collections;
using UnityEngine;

public class CircleDropController : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float settleDuration = 3f; // seconds to fully settle

    [Header("Intro Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private CircleCollider2D circle;            // collider to pop
    [SerializeField, Min(0.0001f)] private float introTinyRadius = 0.01f; // <-- start at 0.01
    private float savedRadius;

    // Explosive POP settings for MERGED balls only
    [Header("Explosive (Merged Only)")]
    [SerializeField, Min(1f)] private float mergeOvershootFactor = 1.25f;
    [SerializeField, Min(0.01f)] private float mergeExplodeUpDuration = 0.08f;
    [SerializeField, Min(0.01f)] private float mergeSettleDownDuration = 0.06f;
    [SerializeField] private AnimationCurve mergeUpCurve = null;   // suggest EaseOut
    [SerializeField] private AnimationCurve mergeDownCurve = null; // suggest EaseIn

    private Coroutine introRoutine;
    private bool introIsMerged = false;

    [Header("Info")]
    [SerializeField] private BallInfo ballInfo; // values pushed via SetDamping()

    [Header("Walls")]
    [SerializeField] private PhysicsMaterial2D noFrictionMat2D; // assign in Inspector
    private PhysicsMaterial2D originalMat2D;
    private int wallContacts = 0;

    // Settling
    private float finalLinearDamping;
    private float finalAngularDamping;
    private bool isDragging = true;
    private Coroutine settleRoutine;
    private float settleElapsed = 0f;
    private float settleSpeedMultiplier = 1f;
    private bool isSettling = false;
    private bool hasAppliedInstantPhysics;

    private float targetGravityScale;
    private float startGravityScale;
    private float prefabGravityScale;

    // ---- Lifecycle ----
    private void Awake()
    {
        if (rb == null) rb = GetComponentInChildren<Rigidbody2D>(true);
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (circle == null) circle = GetComponentInChildren<CircleCollider2D>(true);
        if (circle != null)
        {
            savedRadius = circle.radius;       // cache original collider radius
            originalMat2D = circle.sharedMaterial;
        }

        prefabGravityScale = rb != null ? rb.gravityScale : 0.45f;

        PrepareForDrag(); // lock immediately to avoid gravity ticks
    }

    private void Start()
    {
        rb.WakeUp();
    }

    private void OnDisable()
    {
        if (CircleDragInput.Instance != null)
            CircleDragInput.Instance.ClearActiveBall(this);

        if (introRoutine != null) { StopCoroutine(introRoutine); introRoutine = null; }
    }

    // ---- Data push from BallInfo ----
    public void SetPhysics(float linear, float angular, float gravityStart, float gravityEnd)
    {
        finalLinearDamping = linear;
        finalAngularDamping = angular;
        startGravityScale = gravityStart;
        targetGravityScale = gravityEnd;
    }

    // ---- Drag / Drop ----
    public bool IsDraggable() => isDragging;

    public void PrepareForDrag()
    {
        isDragging = true;
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero; // or rb.velocity in older Unity
            rb.angularVelocity = 0f;
            rb.useFullKinematicContacts = true;
        }
    }

    public void Drop()
    {
        if (!isDragging) return;
        isDragging = false;
        rb.bodyType = RigidbodyType2D.Dynamic;
    }

    // ---- Collisions ----
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Wall"))
        {
            wallContacts++;
            if (circle != null && noFrictionMat2D != null)
                circle.sharedMaterial = noFrictionMat2D; // zero friction only while touching walls
            return; // skip settle for walls
        }

        if (collision.collider.CompareTag("Enclosure"))
        {
            if (isDragging) return;
            if (hasAppliedInstantPhysics) return;
            ApplyFinalPhysicsImmediately();
            //if (settleRoutine == null)
            //settleRoutine = StartCoroutine(SettleAfterTime(settleDuration));
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Wall"))
        {
            wallContacts = Mathf.Max(0, wallContacts - 1);
            if (wallContacts == 0 && circle != null)
                circle.sharedMaterial = originalMat2D; // restore on exit
        }
    }

    private IEnumerator SettleAfterTime(float duration)
    {
        isSettling = true;
        settleElapsed = 0f;
        settleSpeedMultiplier = 1f;

        rb.linearDamping = 0f;
        rb.angularDamping = 0f;

        // Use configured gravity range (fallbacks if unset)
        float g0 = startGravityScale != 0f || targetGravityScale != 0f
                   ? startGravityScale
                   : prefabGravityScale;
        float g1 = targetGravityScale != 0f ? targetGravityScale : prefabGravityScale;

        // start at the chosen starting gravity (NOT zero)
        rb.gravityScale = g0;

        if (finalLinearDamping == 0f && finalAngularDamping == 0f)
        {
            finalLinearDamping = 5f;
            finalAngularDamping = 5f;
        }

        while (settleElapsed < duration)
        {
            settleElapsed += Time.fixedDeltaTime * settleSpeedMultiplier;
            float t = Mathf.Clamp01(settleElapsed / duration);

            rb.linearDamping = Mathf.Lerp(0f, finalLinearDamping, t);
            rb.angularDamping = Mathf.Lerp(0f, finalAngularDamping, t);
            rb.gravityScale = Mathf.Lerp(g0, g1, t);

            yield return new WaitForFixedUpdate();
        }

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.gravityScale = g1;

        isSettling = false;
        settleRoutine = null;
    }

    public void AccelerateSettle(float multiplier)
    {
        if (isSettling)
        {
            settleSpeedMultiplier += multiplier;
        }
    }

    public void ApplyFinalPhysicsImmediately()
    {
        if (rb == null) return;

        // stop any gradual settle
        if (settleRoutine != null) { StopCoroutine(settleRoutine); settleRoutine = null; }

        // ensure we're live
        if (isDragging) Drop();

        // apply final values now
        rb.linearDamping = finalLinearDamping;
        rb.angularDamping = finalAngularDamping;

        float g = (targetGravityScale != 0f) ? targetGravityScale : prefabGravityScale;
        rb.gravityScale = g;

        hasAppliedInstantPhysics = true;
    }

    public void StartSettleIfNeeded()
    {
        if (!isDragging && settleRoutine == null)
            settleRoutine = StartCoroutine(SettleAfterTime(settleDuration));
    }

    // ---- Intros / Animation ----
    public void PlayIntroNew()
    {
        introIsMerged = false;
        if (animator != null) animator.SetTrigger("New");
    }

    public void PlayIntroMerged()
    {
        introIsMerged = true;
        if (animator != null) animator.SetTrigger("Merged");
    }

    public void IntroPrep()
    {
        circle.radius = 0.01f;
    }

    // Animation Event at the beginning of the intro clip
    public void IntroBegin()
    {
        PrepareForDrag(); // kinematic + zero vels

        if (circle == null) return;
        if (savedRadius <= 0f) savedRadius = circle.radius; // ensure valid

        if (introRoutine != null) StopCoroutine(introRoutine);

        if (introIsMerged)
        {
            // MERGED: explosive pop from 0.01 -> overshoot -> savedRadius
            circle.isTrigger = false;       // avoid impulses while popping
            circle.radius = introTinyRadius; // 0.01 start

            introRoutine = StartCoroutine(ExplodeColliderRoutine(
                savedRadius,
                Mathf.Max(1f, mergeOvershootFactor),
                Mathf.Max(0.0001f, mergeExplodeUpDuration),
                Mathf.Max(0.0001f, mergeSettleDownDuration),
                mergeUpCurve,
                mergeDownCurve
            ));
        }
        else
        {
            // NEW: no pop — enable collider at real size and drop immediately
            circle.radius = savedRadius;
            circle.isTrigger = false;
        }
    }

    private IEnumerator ExplodeColliderRoutine(
        float targetRadius,
        float overshootFactor,
        float upDuration,
        float downDuration,
        AnimationCurve upCurve,
        AnimationCurve downCurve)
    {
        // Start at introTinyRadius (already set), peak > targetRadius, settle to targetRadius
        float start = circle.radius;                         // 0.01
        float peak = targetRadius * overshootFactor;        // overshoot

        var up = upCurve != null ? upCurve : AnimationCurve.EaseInOut(0, 0, 1, 1);
        var down = downCurve != null ? downCurve : AnimationCurve.EaseInOut(0, 0, 1, 1);

        // explode up
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / upDuration;
            float k = up.Evaluate(Mathf.Clamp01(t));
            circle.radius = Mathf.Lerp(start, peak, k);
            yield return null;
        }

        // settle down
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / downDuration;
            float k = down.Evaluate(Mathf.Clamp01(t));
            circle.radius = Mathf.Lerp(peak, targetRadius, k);
            yield return null;
        }

        circle.radius = targetRadius; // savedRadius
        circle.isTrigger = false;        // re-enable collisions
        Drop();                          // go live

        introRoutine = null;
    }
}
