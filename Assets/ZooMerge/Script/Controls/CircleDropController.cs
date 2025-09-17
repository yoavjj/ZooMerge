using System.Collections;
using UnityEngine;

public class CircleDropController : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float settleDuration = 3f; // seconds to fully settle

    [SerializeField] private Animator animator;
    [SerializeField] private CircleCollider2D circle; // the collider to grow/shrink
    [SerializeField, Min(0.0001f)] private float introTinyRadius = 0.02f; // collider radius during intro
    private float savedRadius;

    [SerializeField, Min(0.01f)] private float introGrowDuration = 0.25f;
    [SerializeField] private AnimationCurve introGrowCurve = null; // set to EaseInOut in Inspector
    private Coroutine introRoutine;

    // (keep the serialized ref if you want, but we won't read from it anymore)
    [SerializeField] private BallInfo ballInfo;

    [SerializeField] private PhysicsMaterial2D noFrictionMat2D; // assign in Inspector
    private PhysicsMaterial2D originalMat2D;
    private int wallContacts = 0;

    private float finalLinearDamping;
    private float finalAngularDamping;

    private bool isDragging = true;
    private Coroutine settleRoutine;

    private float settleElapsed = 0f;
    private float settleSpeedMultiplier = 1f;
    private bool isSettling = false;

    // ✅ Only cache components here
    private void Awake()
    {
        if (rb == null) rb = GetComponentInChildren<Rigidbody2D>(true);
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (circle == null) circle = GetComponentInChildren<CircleCollider2D>(true);
        if (circle != null) savedRadius = circle.radius;
        if (circle != null) originalMat2D = circle.sharedMaterial;

        PrepareForDrag();
    }

    // ✅ No data pull here; values are pushed via SetDamping() before use
    void Start()
    {
        rb.WakeUp();
    }

    // Calls by BallInfo.Setup()
    public void SetDamping(float linear, float angular)
    {
        finalLinearDamping = linear;
        finalAngularDamping = angular;
    }

    public void PrepareForDrag()
    {
        isDragging = true;
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero; // or rb.velocity in older Unity
            rb.angularVelocity = 0f;
        }
    }

    private void OnDisable()
    {
        if (CircleDragInput.Instance != null)
            CircleDragInput.Instance.ClearActiveBall(this);

        if (introRoutine != null) { StopCoroutine(introRoutine); introRoutine = null; }
    }

    public bool IsDraggable() => isDragging;

    public void Drop()
    {
        if (!isDragging) return;
        isDragging = false;
        rb.bodyType = RigidbodyType2D.Dynamic;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Wall"))
        {
            wallContacts++;
            if (circle != null && noFrictionMat2D != null)
                circle.sharedMaterial = noFrictionMat2D; // zero friction only while touching walls
            return; // block-only; skip settle bump for walls
        }

        if (collision.collider.CompareTag("Enclosure"))
        {
            if (settleRoutine == null)
                settleRoutine = StartCoroutine(SettleAfterTime(settleDuration));
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Wall"))
        {
            wallContacts = Mathf.Max(0, wallContacts - 1);
            if (wallContacts == 0 && circle != null)
                circle.sharedMaterial = originalMat2D; // restore normal friction for non-wall contacts
        }
    }

    private IEnumerator SettleAfterTime(float duration)
    {
        isSettling = true;
        settleElapsed = 0f;
        settleSpeedMultiplier = 1f;

        rb.linearDamping = 0f;
        rb.angularDamping = 0f;

        // Safety: if somehow not set yet (shouldn’t happen), you could keep a tiny fallback:
        if (finalLinearDamping == 0f && finalAngularDamping == 0f) { finalLinearDamping = 5f; finalAngularDamping = 5f; }

        while (settleElapsed < duration)
        {
            settleElapsed += Time.fixedDeltaTime * settleSpeedMultiplier;
            float t = Mathf.Clamp01(settleElapsed / duration);
            rb.linearDamping = Mathf.Lerp(0f, finalLinearDamping, t);
            rb.angularDamping = Mathf.Lerp(0f, finalAngularDamping, t);
            yield return new WaitForFixedUpdate();
        }

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

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

    public void PlayIntroNew()
    {
        if (animator != null) animator.SetTrigger("New");
    }

    public void PlayIntroMerged()
    {
        if (animator != null) animator.SetTrigger("Merged");
    }

    // Animation Event at the beginning of the intro clip
    public void IntroBegin()
    {
        PrepareForDrag(); // kinematic + zero vels

        if (circle == null) return;
        if (savedRadius <= 0f) savedRadius = circle.radius;

        if (introRoutine != null) StopCoroutine(introRoutine);

        circle.isTrigger = true;           // avoid impulses while growing
        circle.radius = introTinyRadius;

        introRoutine = StartCoroutine(GrowColliderRoutine(savedRadius));
    }

    private System.Collections.IEnumerator GrowColliderRoutine(float targetRadius)
    {
        float t = 0f;
        float start = circle.radius;
        var curve = introGrowCurve != null ? introGrowCurve : AnimationCurve.EaseInOut(0, 0, 1, 1);

        while (t < 1f)
        {
            t += Time.deltaTime / introGrowDuration;
            float k = curve.Evaluate(Mathf.Clamp01(t));
            circle.radius = Mathf.Lerp(start, targetRadius, k);
            yield return null;
        }

        circle.radius = targetRadius;
        circle.isTrigger = false;  // re-enable collisions
        Drop();                    // go live

        introRoutine = null;
    }
}
