using System.Collections;
using UnityEngine;
using static BallEventManager;

public class CircleDropController : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private Rigidbody2D rb;
    public Rigidbody2D Rigidbody => rb;
    [SerializeField] private float settleDuration = 3f; // seconds to fully settle
    [SerializeField] private Transform motionTarget;

    [SerializeField] private GameObject reflectionGO;
    [SerializeField] private float reflectionActivateDelay = 1.5f;

    [SerializeField] private float gameOverDelay = 1.5f;
    private bool gameOverCheckEnabled = false;

    [Header("Intro Animation")]
    [SerializeField] public Animator animator;
    [SerializeField] public CircleCollider2D circle;
    public CircleCollider2D Collider => circle;
    [SerializeField, Min(0.0001f)] private float introTinyRadius = 0.01f; // <-- start at 0.01
    private float savedRadius;

    [SerializeField] private Spine.Unity.SkeletonAnimation spineAnimation;
    public Spine.Unity.SkeletonAnimation Spine => spineAnimation;

    [SerializeField] private MeshRenderer spineRenderer;

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
    [SerializeField] public BallInfo ballInfo; // values pushed via SetDamping()

    [Header("Walls")]
    [SerializeField] private PhysicsMaterial2D noFrictionMat2D; // assign in Inspector
    private PhysicsMaterial2D originalMat2D;
    private int wallContacts = 0;

    [Header("Game Over Trigger Settings")]
    [SerializeField] private float requiredGameOverContactTime = 3f;
    private Coroutine gameOverTouchRoutine;

    // Settling
    private float finalLinearDamping;
    private float finalAngularDamping;
    private bool isDragging = true;
    private Coroutine settleRoutine;
    private float settleElapsed = 0f;
    private float settleSpeedMultiplier = 1f;
    private bool isSettling = false;
    private bool hasAppliedInstantPhysics;
    private bool hasLanded = false;
    private bool pauseBlockActive = false;

    private float targetGravityScale;
    private float startGravityScale;
    private float prefabGravityScale;
    private int assignedSortingOrder = 5;

    public int GetAssignedOrder() => assignedSortingOrder;

    // --- Cached animation names (filled once) ---
    private string animIdle;
    private string animFalling;
    private string animLand;
    private string animTouching;

    private bool animNamesCached = false;

    // ---- Lifecycle ----
    private void Awake()
    {
        if (rb == null && motionTarget != null) rb = motionTarget.GetComponentInChildren<Rigidbody2D>(true);
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
        CacheAnimNamesIfNeeded();
    }

    private void OnEnable()
    {
        BallEventManager.OnGameOverAnimation += HandleGameOverAnimation;
        BallEventManager.OnSessionWonAnimation += HandleSessionWonAnimation;
        BallEventManager.OnReturnToMainMenu += HandleReturnToMainMenu;
    }

    private void OnDisable()
    {
        BallEventManager.OnGameOverAnimation -= HandleGameOverAnimation;
        BallEventManager.OnSessionWonAnimation -= HandleSessionWonAnimation;
        BallEventManager.OnReturnToMainMenu -= HandleReturnToMainMenu;

        if (CircleDragInput.Instance != null)
            CircleDragInput.Instance.ClearActiveBall(this);

        if (introRoutine != null) { StopCoroutine(introRoutine); introRoutine = null; }

        ReleasePauseBlockIfActive();
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
        if (reflectionGO != null)
            reflectionGO.SetActive(false);

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

        if (!pauseBlockActive)
        {
            pauseBlockActive = true;
            BallEventManager.PushPauseBlock();
        }

        // ✅ Register the ball to BallRegistry when it goes live
        if (ballInfo != null)
            BallRegistry.Register(ballInfo);

        animator.SetTrigger("Dropped");
        rb.bodyType = RigidbodyType2D.Dynamic;

        if (reflectionGO != null)
            StartCoroutine(ActivateReflectionAfterDelay(reflectionActivateDelay));

        StartCoroutine(EnableGameOverCheckAfterDelay());

        // ✅ Assign unique Spine sorting order
        if (spineRenderer != null)
        {
            assignedSortingOrder = SpineSortingOrderManager.GetNextOrder();
            spineRenderer.sortingOrder = assignedSortingOrder;
        }

        // ✅ Play Falling Animation
        if (!introIsMerged && spineAnimation != null && ballInfo != null)
        {
            CacheAnimNamesIfNeeded();
            if (!string.IsNullOrEmpty(animFalling) && SpineHasAnimation(animFalling))
            {
                PlaySpine(animFalling, true);
            }
        }
    }

    public void SetDraggable(bool value)
    {
        isDragging = value;

        if (rb != null)
        {
            rb.bodyType = value ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;
            rb.useFullKinematicContacts = value;
        }

        if (!value)
        {
            rb.WakeUp(); // wake up physics system if going live
        }

        Debug.Log($"🎮 SetDraggable({value}) called on {name}");
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

        if (isDragging) return;

        if (hasLanded) return; // ✅ Already landed — skip
        
        ReleasePauseBlockIfActive();

        hasLanded = true; // ✅ Mark as landed on ANY collision
        CacheAnimNamesIfNeeded();

        if (spineAnimation != null)
        {
            if (!string.IsNullOrEmpty(animLand) && SpineHasAnimation(animLand))
            {
                var entry = PlaySpine(animLand, false); // play landing once

                // Queue idle after landing
                if (!string.IsNullOrEmpty(animIdle) && SpineHasAnimation(animIdle))
                {
                    entry.Complete += delegate { PlaySpine(animIdle, true); };
                }
            }
            else if (!string.IsNullOrEmpty(animIdle) && SpineHasAnimation(animIdle))
            {
                // Fallback straight to idle if no land anim
                PlaySpine(animIdle, true);
            }
        }
    }

    public void ReleasePauseBlockIfActive()
    {
        if (!pauseBlockActive) return;
        pauseBlockActive = false;
        BallEventManager.PopPauseBlock();
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Wall"))
        {
            wallContacts = Mathf.Max(0, wallContacts - 1);
            if (wallContacts == 0 && circle != null)
                circle.sharedMaterial = originalMat2D; // restore on exit
        }


        if (!hasLanded) return; // ✅ do not force idle until landed

        CacheAnimNamesIfNeeded();
        if (spineAnimation != null && !string.IsNullOrEmpty(animIdle) && SpineHasAnimation(animIdle))
        {
            PlaySpine(animIdle, true);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("GameOver"))
        {
            gameOverTouchRoutine = StartCoroutine(WaitToTriggerGameOver());

            if (animator != null)
                animator.SetBool("IsSaved", false);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("GameOver")) return;

        if (gameOverTouchRoutine != null)
        {
            if (animator != null)
                animator.SetBool("IsSaved", true);

            StopCoroutine(gameOverTouchRoutine);
            gameOverTouchRoutine = null;

            if (spineAnimation == null) return;

            // Use cached names, don't re-fetch
            CacheAnimNamesIfNeeded();

            // Only restore if we were actually playing the touching loop.
            var cur = spineAnimation.AnimationState.GetCurrent(0);
            var curName = cur != null ? cur.Animation.Name : null;

            // If we were in "touching", restore the correct loop for our state.
            if (curName == animTouching || string.IsNullOrEmpty(curName))
            {
                if (!hasLanded)
                {
                    if (!string.IsNullOrEmpty(animFalling) && SpineHasAnimation(animFalling))
                        PlaySpine(animFalling, true);   // keep falling loop
                }
                else
                {
                    if (!string.IsNullOrEmpty(animIdle) && SpineHasAnimation(animIdle))
                        PlaySpine(animIdle, true);      // back to idle loop
                }
            }
            // else: landing (or something else) is playing — let it finish; completion handler goes to idle.
        }
    }

    private IEnumerator WaitToTriggerGameOver()
    {
        yield return new WaitForSeconds(gameOverDelay);
        // ✅ Animator: Trigger "Touching"
        if (animator != null) animator.SetTrigger("Touching");

        // 🔹 Play Touching animation via Spine
        CacheAnimNamesIfNeeded();
        if (spineAnimation != null && !string.IsNullOrEmpty(animTouching) && SpineHasAnimation(animTouching))
            PlaySpine(animTouching, true);

        yield return new WaitForSeconds(requiredGameOverContactTime);
        BallEventManager.RaiseBallTouchedGameOverLine(ballInfo, GameOverReason.Lost);
        gameOverTouchRoutine = null;
    }


    private IEnumerator EnableGameOverCheckAfterDelay()
    {
        yield return new WaitForSeconds(gameOverDelay);
        gameOverCheckEnabled = true;
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

    private IEnumerator ActivateReflectionAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (reflectionGO != null)
            reflectionGO.SetActive(true);
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

        // Play existing Unity Animator intro
        if (animator != null)
            animator.SetTrigger("New");

        // Play Spine idle animation automatically
        CacheAnimNamesIfNeeded();
        if (spineAnimation != null && !string.IsNullOrEmpty(animIdle) && SpineHasAnimation(animIdle))
            PlaySpine(animIdle, true);

    }


    public void PlayIntroMerged()
    {
        introIsMerged = true;

        if (reflectionGO != null)
            reflectionGO.SetActive(true);

        if (animator != null) animator.SetTrigger("Merged");
    }

    public Spine.TrackEntry PlaySpine(string anim, bool loop = false)
    {
        if (spineAnimation == null) return null;
        return spineAnimation.AnimationState.SetAnimation(0, anim, loop);
    }

    private bool SpineHasAnimation(string anim)
    {
        if (spineAnimation == null || spineAnimation.Skeleton == null)
            return false;

        return spineAnimation.Skeleton.Data.FindAnimation(anim) != null;
    }

    public void PlayIntroNewMidLevel()
    {
        if (reflectionGO != null)
            reflectionGO.SetActive(true);

        introIsMerged = false;

        if (animator != null)
        {
            animator.SetTrigger("New");
            animator.SetTrigger("MidLevel");
        }

        CacheAnimNamesIfNeeded();
        if (spineAnimation != null && !string.IsNullOrEmpty(animIdle) && SpineHasAnimation(animIdle))
            PlaySpine(animIdle, true);
    }

    public void IntroPrep()
    {
        circle.radius = 0.01f;
    }

    // Animation Event at the beginning of the intro clip
    public void IntroBegin()
    {
        if (!introIsMerged)
            PrepareForDrag(); // Only for fresh balls

        if (circle == null) return;
        if (savedRadius <= 0f) savedRadius = circle.radius;

        if (introRoutine != null) StopCoroutine(introRoutine);

        if (introIsMerged)
        {
            Drop();                    // ✅ Make it dynamic *before* expansion
            circle.isTrigger = false;  // ✅ Enable collisions immediately
            circle.radius = introTinyRadius;

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
        Drop();                          // go live

        introRoutine = null;
    }

    private void HandleGameOverAnimation()
    {
        float delay = Random.Range(0.05f, 0.35f);
        StartCoroutine(PlayOutAnimationAfterDelay(delay));
    }

    private void HandleSessionWonAnimation()
    {
        StartCoroutine(PlayOutAnimationAfterDelay(gameOverDelay)); // ✅ delay 0 as requested
    }

    public IEnumerator PlayOutAnimationAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (animator != null)
            animator.Play("IntroBallAnimation_Out");

        Destroy(gameObject, 2f); // cleanup after animation
    }

    private void HandleReturnToMainMenu()
    {
        Destroy(gameObject); // Clean up this ball
    }

    private void CacheAnimNamesIfNeeded()
    {
        if (animNamesCached) return;
        if (ballInfo == null || BallFactoryAddressables.Instance == null) return;

        var data = BallFactoryAddressables.Instance.BallSet.GetAnimationForLevel(ballInfo.Level);
        if (data != null)
        {
            animIdle = data.idleAnimation;
            animFalling = data.fallingAnimation;
            animLand = data.landAnimation;
            animTouching = data.TouchingAnimation; // if your struct uses a different name, keep it
        }

        animNamesCached = true;
    }
}
