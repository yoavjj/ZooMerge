using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static BallEventManager;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }

    [Header ("Session Control")]
    [SerializeField, Min(0f)] private float mergeUnblockDelay = 0.35f;
    private Coroutine mergeUnblockRoutine;

    [Header("UI Animators")]
    [SerializeField] private Animator topUIAnimator;
    [SerializeField] private Animator bottomUIAnimator;

    private static readonly int TR_SessionStart = Animator.StringToHash("Session_Start");
    private static readonly int TR_SessionEnd = Animator.StringToHash("Session_End");
    private static readonly int TR_SessionPause = Animator.StringToHash("Session_Pause");
    private static readonly int TR_SessionResume = Animator.StringToHash("Session_Resume");

    [Header("Enemy Die FX Animator")]
    [SerializeField] private Animator enemyDieAnimator;
    [SerializeField] private string enemyDieTriggerName = "Die";
    [SerializeField] private string enemyEndTriggerName = "End";

    [Header("Raycast Control")]
    [SerializeField] private GraphicRaycaster overlayRaycaster;

    private bool isSessionUIActive = false;
    private bool dieFxTriggeredThisEnemy = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (overlayRaycaster != null)
            overlayRaycaster.enabled = false;
    }

    private void OnEnable()
    {
        OnSessionStarted += TriggerSessionStart;
        OnSessionPaused += TriggerSessionPause;
        OnSessionResumed += TriggerSessionResume;

        OnSessionStarted += HandleSessionStartedForMerges;   // add this
        BallEventManager.OnEnemySessionEnded += HandleSessionEndedForMerges;

        BallEventManager.OnEnemySessionEnded += OnEnemySessionEnded;
        BallEventManager.OnEnemyDeathSpineEvent += OnEnemyDeathSpineEvent;
    }

    private void OnDisable()
    {
        OnSessionStarted -= TriggerSessionStart;
        OnSessionPaused -= TriggerSessionPause;
        OnSessionResumed -= TriggerSessionResume;

        OnSessionStarted -= HandleSessionStartedForMerges;   // add this
        BallEventManager.OnEnemySessionEnded -= HandleSessionEndedForMerges;

        BallEventManager.OnEnemySessionEnded -= OnEnemySessionEnded;
        BallEventManager.OnEnemyDeathSpineEvent -= OnEnemyDeathSpineEvent;
    }

    private void HandleSessionStartedForMerges()
    {
        BallEventManager.SetMergesBlocked(true);

        if (mergeUnblockRoutine != null) StopCoroutine(mergeUnblockRoutine);
        mergeUnblockRoutine = StartCoroutine(UnblockMergesAfterDelay());
    }

    
    private void HandleSessionEndedForMerges()
    {
        BallEventManager.SetMergesBlocked(true);

        if (mergeUnblockRoutine != null)
        {
            StopCoroutine(mergeUnblockRoutine);
            mergeUnblockRoutine = null;
        }
    }

    
    private IEnumerator UnblockMergesAfterDelay()
    {
        if (mergeUnblockDelay > 0f)
            yield return new WaitForSeconds(mergeUnblockDelay);

        BallEventManager.SetMergesBlocked(false);
        mergeUnblockRoutine = null;
    }

    private void OnEnemySessionEnded()
    {
        // Start the "Die" FX when the death sequence begins
        if (!dieFxTriggeredThisEnemy)
        {
            dieFxTriggeredThisEnemy = true;

            if (enemyDieAnimator != null && !string.IsNullOrEmpty(enemyDieTriggerName))
            {
                enemyDieAnimator.ResetTrigger(enemyDieTriggerName);
                enemyDieAnimator.SetTrigger(enemyDieTriggerName);
            }
        }
    }

    private void OnEnemyDeathSpineEvent(GameObject enemyRoot)
    {
        TriggerSessionEnd();
    }

    public void TriggerSessionEnd()
    {
        if (!isSessionUIActive) return;
        isSessionUIActive = false;

        if (enemyDieAnimator != null && !string.IsNullOrEmpty(enemyEndTriggerName))
        {
            enemyDieAnimator.ResetTrigger(enemyEndTriggerName);
            enemyDieAnimator.SetTrigger(enemyEndTriggerName);
        }

        topUIAnimator?.ResetTrigger("Session_Start");
        bottomUIAnimator?.ResetTrigger("Session_Start");

        topUIAnimator?.SetTrigger("Session_End");
        bottomUIAnimator?.SetTrigger("Session_End");

        if (overlayRaycaster != null)
            overlayRaycaster.enabled = false;
    }

    public void TriggerSessionStart()
    {
        if (isSessionUIActive) return;
        isSessionUIActive = true;

        // New enemy/session -> allow die FX again
        dieFxTriggeredThisEnemy = false;

        topUIAnimator?.ResetTrigger("Session_End");
        bottomUIAnimator?.ResetTrigger("Session_End");

        topUIAnimator?.SetTrigger("Session_Start");
        bottomUIAnimator?.SetTrigger("Session_Start");

        if (overlayRaycaster != null)
            overlayRaycaster.enabled = true;
    }

    private void TriggerSessionPause()
    {
        // Only if session UI is currently active
        if (!isSessionUIActive) return;

        // Don’t touch enemyEndTriggerName here. Pause is not an end.
        topUIAnimator?.ResetTrigger(TR_SessionResume);
        bottomUIAnimator?.ResetTrigger(TR_SessionResume);

        topUIAnimator?.SetTrigger(TR_SessionPause);
        bottomUIAnimator?.SetTrigger(TR_SessionPause);

        // Usually keep overlay raycaster OFF while paused
        if (overlayRaycaster != null)
            overlayRaycaster.enabled = false;
    }

    private void TriggerSessionResume()
    {
        // Only if session UI is currently active
        if (!isSessionUIActive) return;

        topUIAnimator?.ResetTrigger(TR_SessionPause);
        bottomUIAnimator?.ResetTrigger(TR_SessionPause);

        topUIAnimator?.SetTrigger(TR_SessionResume);
        bottomUIAnimator?.SetTrigger(TR_SessionResume);

        // Re-enable interactions
        if (overlayRaycaster != null)
            overlayRaycaster.enabled = true;
    }

    public void HandleEnemyDieAnimationEvent()
    {
        // End-of-level case (final enemy)
        if (MergeLevelManager.LevelCompletePending)
        {
            MergeLevelManager.ClearLevelCompletePending();

            // ✅ now we end the run for real
            BallEventManager.RaiseGameOver(null, GameOverReason.Won);

            // now safe to clear
            MergeAttemptTracker.ClearAll();
            BallRegistry.Clear();

            PopupManager.Instance?.ShowEndLvlPopup(GameOverReason.Won);
            return;
        }

        // Mid-level enemy defeated case
        BallEventManager.RaiseEnemyDefeatedMidLevel();
        PopupManager.Instance?.ShowEnemyDefeatedMessage();
    }
}
