using System.Collections;
using UnityEngine;
using Spine;
using Spine.Unity;

public class EnemyUnit : MonoBehaviour
{
    [Header("Animation (optional legacy)")]
    [SerializeField] private Animator animator;

    [Header("Spine UI Animation")]
    [SerializeField] private SkeletonGraphic spineGraphic;

    [Header("Spine Clip Names")]
    [SpineAnimation] public string enterAnimation = "Enemy_enter";
    [SpineAnimation] public string idleAnimation = "Enemy_idle";
    [SpineAnimation] public string hitAnimation = "Enemy_hit";
    [SpineAnimation] public string dieAnimation = "Enemy_die";

    [Header("Death Timing")]
    [SerializeField, Min(0f)] private float dieDelay = 0.25f;

    [Header("Spine Death Event")]
    [SerializeField] private string deathEndEventName = "AnimEnd"; // ✅ Spine event name

    [Header("Death Fade")]
    [SerializeField, Min(0f)] private float deathFadeDuration = 0.08f; // 0 = instant
    private Coroutine fadeRoutine;

    private bool isDying;
    private bool deathCleanupTriggered; // ✅ prevents double-cleanup
    private GameObject root;
    private Coroutine dieRoutine;

    void Awake()
    {
        root = transform.root != null ? transform.root.gameObject : gameObject;
    }

    void OnEnable()
    {
        BallEventManager.OnEnemyHit += HandleHit;
        BallEventManager.OnEnemySessionEnded += HandleSessionEnd;
    }

    void OnDisable()
    {
        BallEventManager.OnEnemyHit -= HandleHit;
        BallEventManager.OnEnemySessionEnded -= HandleSessionEnd;

        if (dieRoutine != null)
        {
            StopCoroutine(dieRoutine);
            dieRoutine = null;
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }
    }

    private void HandleHit(GameObject hitObject)
    {
        if (isDying) return;

        ParticleEvents.Request("EnemyImpact", transform.position);

        if (UseSpine())
        {
            spineGraphic.AnimationState.SetAnimation(0, SafeClip(hitAnimation), false);
            spineGraphic.AnimationState.AddAnimation(0, SafeClip(idleAnimation), true, 0f);
        }
        else if (animator != null)
        {
            animator.SetTrigger("Hit");
        }
    }

    private void HandleSessionEnd()
    {
        if (isDying) return;
        isDying = true;
        deathCleanupTriggered = false;

        if (dieRoutine != null) StopCoroutine(dieRoutine);
        dieRoutine = StartCoroutine(DieAfterDelay());
    }

    private IEnumerator DieAfterDelay()
    {
        if (dieDelay > 0f)
            yield return new WaitForSeconds(dieDelay);

        // Spine die
        if (UseSpine() && HasClip(dieAnimation))
        {
            var entry = spineGraphic.AnimationState.SetAnimation(0, dieAnimation, false);

            // ✅ Listen only to THIS die entry's events
            entry.Event += OnDieEntryEvent;

            // ✅ Safety fallback: if AnimEnd is missing, still cleanup on complete
            entry.Complete += _ =>
            {
                if (!deathCleanupTriggered)
                    CleanupAfterDeath();
            };
        }
        else
        {
            // No die clip -> just cleanup
            CleanupAfterDeath();
        }

        dieRoutine = null;
    }

    private void OnDieEntryEvent(TrackEntry entry, Spine.Event e)
    {
        if (deathCleanupTriggered) return;

        if (e != null && e.Data != null && e.Data.Name == deathEndEventName)
        {
            deathCleanupTriggered = true;
        
            // Unsubscribe immediately to avoid multiple triggers if the event fires more than once
            entry.Event -= OnDieEntryEvent;

            FadeOutNow();          // fade opacity to zero at AnimEnd
            CleanupAfterDeath();
        }
    }

    private void CleanupAfterDeath()
    {
        if (deathCleanupTriggered == false)
            deathCleanupTriggered = true;

        BallEventManager.RaiseEnemyDeathSpineEvent(root);

        EnemySessionTracker.Unregister(root);
        EnemySpawner.Instance?.NotifyEnemyDestroyed(root);
    }

    public void PlayEnter()
    {
        animator?.SetTrigger("Enter");

        var state = spineGraphic?.AnimationState;
        if (state == null) return;

        if (HasClip(enterAnimation))
        {
            state.SetAnimation(0, enterAnimation, false);
            state.AddAnimation(0, SafeClip(idleAnimation), true, 0f);
        }
        else
        {
            state.SetAnimation(0, SafeClip(idleAnimation), true);
        }
    }

    private bool UseSpine() => spineGraphic != null && spineGraphic.AnimationState != null;

    private bool HasClip(string clip)
    {
        if (string.IsNullOrEmpty(clip) || spineGraphic == null || spineGraphic.Skeleton == null) return false;
        var data = spineGraphic.Skeleton.Data;
        return data != null && data.FindAnimation(clip) != null;
    }

    private string SafeClip(string clip)
    {
        if (HasClip(clip)) return clip;
        return HasClip(idleAnimation) ? idleAnimation : string.Empty;
    }

    private void FadeOutNow()
    {
        if (spineGraphic == null) return;

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);

        if (deathFadeDuration <= 0f)
        {
            var c = spineGraphic.color;
            c.a = 0f;
            spineGraphic.color = c;
            return;
        }

        fadeRoutine = StartCoroutine(FadeSpineGraphicAlpha(0f, deathFadeDuration));
    }

    private IEnumerator FadeSpineGraphicAlpha(float targetAlpha, float duration)
    {
        var c = spineGraphic.color;
        float startAlpha = c.a;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, duration);
            float a = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(t));

            c = spineGraphic.color;
            c.a = a;
            spineGraphic.color = c;

            yield return null;
        }

        c = spineGraphic.color;
        c.a = targetAlpha;
        spineGraphic.color = c;

        fadeRoutine = null;
    }
}
