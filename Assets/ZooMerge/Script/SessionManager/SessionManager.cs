using UnityEngine;
using UnityEngine.UI;
using static BallEventManager;

public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }

    [Header("UI Animators")]
    [SerializeField] private Animator topUIAnimator;
    [SerializeField] private Animator bottomUIAnimator;

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

        OnSessionPaused += TriggerSessionEnd;

        OnEnemyDeathSpineEvent += (enemy) => TriggerEnemyDieFx();

        // ✅ This is the important one:
        // When enemy session ends, EnemyUnit starts Enemy_die and we also fire our extra animator trigger.
        OnEnemySessionEnded += () => HandleGameOver(default, default);


        OnSessionResumed += TriggerSessionStart;
    }

    private void OnDisable()
    {
        OnSessionStarted -= TriggerSessionStart;

        OnSessionPaused -= TriggerSessionEnd;

        OnEnemySessionEnded -= () => HandleGameOver(default, default);

        OnSessionResumed -= TriggerSessionStart;
        OnEnemyDeathSpineEvent -= (enemy) => TriggerEnemyDieFx();
    }

    private void HandleGameOver(BallInfo info, GameOverReason reason)
    {
        // Prevent double triggers if RaiseEnemySessionEnded happens twice by mistake
        if (dieFxTriggeredThisEnemy) return;
        dieFxTriggeredThisEnemy = true;

        if (enemyDieAnimator != null && !string.IsNullOrEmpty(enemyDieTriggerName))
        {
            enemyDieAnimator.ResetTrigger(enemyDieTriggerName);
            enemyDieAnimator.SetTrigger(enemyDieTriggerName);
        }
    }

    private void TriggerEnemyDieFx()
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

    public void HandleEnemyDieAnimationEvent()
    {
        BallEventManager.RaiseEnemyDefeatedMidLevel();

        PopupManager.Instance?.ShowEnemyDefeatedMessage();
    }
}
