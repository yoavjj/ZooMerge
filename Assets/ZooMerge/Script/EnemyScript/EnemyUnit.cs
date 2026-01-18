using UnityEngine;
using Spine;
using Spine.Unity; // SkeletonGraphic

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

    private bool isDying;
    private GameObject root; // cached top-level object

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
    }

    private void HandleHit(GameObject hitObject)
    {
        if (isDying || hitObject != gameObject) return;

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

        if (UseSpine() && HasClip(dieAnimation))
        {
            var entry = spineGraphic.AnimationState.SetAnimation(0, dieAnimation, false);
            entry.Complete += _ =>
            {
                BallEventManager.RaiseEnemyDefeatedMidLevel(); // 🆕 Trigger session end

                EnemySessionTracker.Unregister(root);
                EnemySpawner.Instance?.NotifyEnemyDestroyed(root);
                Destroy(this.gameObject);
            };
        }
        else
        {
            BallEventManager.RaiseEnemyDefeatedMidLevel(); // 🆕 Trigger session end

            EnemySessionTracker.Unregister(root);
            EnemySpawner.Instance?.NotifyEnemyDestroyed(root);
            Destroy(this.gameObject);
        }
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
}
