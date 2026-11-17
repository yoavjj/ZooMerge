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
    [SerializeField] private Animator topMergePanleAnimator;
    [SerializeField] private Animator uiEnemyAnimator;
    [SerializeField] private Animator bottomUIAnimator;
    [SerializeField] private Animator pauseUIAnimator;

    [Header("Pause Button Art")]
    [SerializeField] private Animator pauseButtonArtAnimator;

    private static readonly int TR_PauseButtonIn = Animator.StringToHash("In");
    private static readonly int TR_PauseButtonOut = Animator.StringToHash("Out");
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

    [Header("Bottom Sidebar Animator")]
    [SerializeField] private Animator bottomSideBarAnimator;
    [SerializeField] private string menuInTriggerName = "Menu_In";
    [SerializeField] private string menuOutTriggerName = "Menu_Out";

    private bool isSessionUIActive = false;
    private bool bottomUIStartedEarlyForReward = false;
    private bool dieFxTriggeredThisEnemy = false;
    private bool tutorialMode;
    private bool tutorialEnemyUIEnabled;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        tutorialMode =
            TutorialController.IsActive;

        tutorialEnemyUIEnabled =
            TutorialController.TutorialEnemyEnabled;

        if (overlayRaycaster != null)
            overlayRaycaster.enabled = false;
    }

    private IEnumerator Start()
    {
        yield return null;

        if (tutorialMode)
            yield break;

        if (!isSessionUIActive)
            PlayBottomSideBarMenuIn();
    }
    private void OnEnable()
    {
        TutorialController.TutorialStateChanged += HandleTutorialStateChanged;

        OnSessionStarted += TriggerSessionStart;
        OnSessionPaused += TriggerSessionPause;
        OnSessionResumed += TriggerSessionResume;

        OnSessionStarted += HandleSessionStartedForMerges;
        BallEventManager.OnEnemySessionEnded += HandleSessionEndedForMerges;

        BallEventManager.OnEnemySessionEnded += OnEnemySessionEnded;
        BallEventManager.OnEnemyDeathSpineEvent += OnEnemyDeathSpineEvent;

        BallEventManager.OnReturnToMainMenu += HandleReturnToMainMenu;

        BallEventManager.OnBallTouchedGameOverLine += HandleBallTouchedGameOverLine;

        BallEventManager.OnMainMenuPopupOpened += HandleMainMenuPopupOpened;
        BallEventManager.OnMainMenuPopupClosed += HandleMainMenuPopupClosed;
    }

    private void OnDisable()
    {
        TutorialController.TutorialStateChanged -= HandleTutorialStateChanged;

        OnSessionStarted -= TriggerSessionStart;
        OnSessionPaused -= TriggerSessionPause;
        OnSessionResumed -= TriggerSessionResume;

        OnSessionStarted -= HandleSessionStartedForMerges;
        BallEventManager.OnEnemySessionEnded -= HandleSessionEndedForMerges;

        BallEventManager.OnEnemySessionEnded -= OnEnemySessionEnded;
        BallEventManager.OnEnemyDeathSpineEvent -= OnEnemyDeathSpineEvent;

        BallEventManager.OnReturnToMainMenu -= HandleReturnToMainMenu;
        BallEventManager.OnBallTouchedGameOverLine -= HandleBallTouchedGameOverLine;

        BallEventManager.OnMainMenuPopupOpened -= HandleMainMenuPopupOpened;
        BallEventManager.OnMainMenuPopupClosed -= HandleMainMenuPopupClosed;
    }

    private void HandleTutorialStateChanged(bool active)
    {
        tutorialMode = active;

        if (tutorialMode)
        {
            Debug.Log(
                "[SessionManager] Switched to tutorial UI mode."
            );

            return;
        }

        Debug.Log(
            "[SessionManager] Tutorial finished. Normal UI mode restored."
        );
    }

    private void HandleMainMenuPopupOpened()
    {
        if (tutorialMode)
            return;

        PlayBottomSideBarMenuIn();
    }

    private void HandleMainMenuPopupClosed()
    {
        if (tutorialMode)
            return;

        PlayBottomSideBarMenuOut();
    }

    private void PlayBottomSideBarMenuIn()
    {
        if (tutorialMode)
            return;

        if (bottomSideBarAnimator == null)
            return;

        bottomSideBarAnimator.ResetTrigger(menuOutTriggerName);
        bottomSideBarAnimator.SetTrigger(menuInTriggerName);
    }

    private void PlayBottomSideBarMenuOut()
    {
        if (tutorialMode)
            return;
            
        if (bottomSideBarAnimator == null)
            return;

        bottomSideBarAnimator.ResetTrigger(menuInTriggerName);
        bottomSideBarAnimator.SetTrigger(menuOutTriggerName);
    }

    private void HandleBallTouchedGameOverLine(BallInfo info)
    {
        TriggerSessionEnd();
    }

    private void HandleReturnToMainMenu()
    {
        // Treat quitting like an end
        TriggerSessionEnd();

        // Safety: main menu always forces Pause UI to its Session_End state.
        pauseUIAnimator?.ResetTrigger(TR_SessionStart);
        pauseUIAnimator?.SetTrigger(TR_SessionEnd);

        // Make sure the bottom sidebar menu is in the "in" state
        PlayBottomSideBarMenuIn();

        // Also make sure gameplay interaction is locked
        BallEventManager.SetMergesBlocked(true);

        if (mergeUnblockRoutine != null)
        {
            StopCoroutine(mergeUnblockRoutine);
            mergeUnblockRoutine = null;
        }
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

        bottomUIStartedEarlyForReward = false;

        AudioManager.Instance?.StopSessionMusic();

        if (enemyDieAnimator != null && !string.IsNullOrEmpty(enemyEndTriggerName))
        {
            enemyDieAnimator.ResetTrigger(enemyEndTriggerName);
            enemyDieAnimator.SetTrigger(enemyEndTriggerName);
        }

        topMergePanleAnimator?.ResetTrigger(TR_SessionStart);
        uiEnemyAnimator?.ResetTrigger(TR_SessionStart);
        bottomUIAnimator?.ResetTrigger(TR_SessionStart);
        pauseUIAnimator?.ResetTrigger(TR_SessionStart);

        topMergePanleAnimator?.SetTrigger(TR_SessionEnd);
        uiEnemyAnimator?.SetTrigger(TR_SessionEnd);
        bottomUIAnimator?.SetTrigger(TR_SessionEnd);
        pauseUIAnimator?.SetTrigger(TR_SessionEnd);

        if (overlayRaycaster != null)
            overlayRaycaster.enabled = false;
    }

    public void TriggerSessionStart()
    {
        if (isSessionUIActive)
            return;

        isSessionUIActive = true;

        AudioManager.Instance?.PlaySessionMusic();

        dieFxTriggeredThisEnemy = false;

        // --------------------------------
        // TUTORIAL
        // --------------------------------

        if (tutorialMode)
        {
            if (tutorialEnemyUIEnabled)
            {
                uiEnemyAnimator?.ResetTrigger(
                    TR_SessionEnd
                );

                uiEnemyAnimator?.SetTrigger(
                    TR_SessionStart
                );
            }

            bottomUIStartedEarlyForReward = false;

            if (overlayRaycaster != null)
                overlayRaycaster.enabled = true;

            Debug.Log(
                tutorialEnemyUIEnabled
                    ? "[SessionManager] Tutorial started with Enemy UI."
                    : "[SessionManager] Tutorial started without Enemy UI."
            );

            return;
        }

        // --------------------------------
        // NORMAL GAME
        // --------------------------------

        topMergePanleAnimator?.ResetTrigger(TR_SessionEnd);
        topMergePanleAnimator?.SetTrigger(TR_SessionStart);

        uiEnemyAnimator?.ResetTrigger(TR_SessionEnd);
        uiEnemyAnimator?.SetTrigger(TR_SessionStart);

        if (!bottomUIStartedEarlyForReward)
        {
            bottomUIAnimator?.ResetTrigger(TR_SessionEnd);
            bottomUIAnimator?.SetTrigger(TR_SessionStart);
        }

        pauseUIAnimator?.ResetTrigger(TR_SessionEnd);
        pauseUIAnimator?.SetTrigger(TR_SessionStart);

        bottomUIStartedEarlyForReward = false;

        if (overlayRaycaster != null)
            overlayRaycaster.enabled = true;
    }

    private void TriggerSessionPause()
    {
        if (!isSessionUIActive)
            return;

        if (!tutorialMode || tutorialEnemyUIEnabled)
        {
            uiEnemyAnimator?.ResetTrigger(
                TR_SessionResume
            );

            uiEnemyAnimator?.SetTrigger(
                TR_SessionPause
            );
        }

        // Merge panel is not active during tutorial.
        if (!tutorialMode)
        {
            topMergePanleAnimator?.ResetTrigger(TR_SessionResume);
            topMergePanleAnimator?.SetTrigger(TR_SessionPause);

            bottomUIAnimator?.ResetTrigger(TR_SessionResume);
            bottomUIAnimator?.SetTrigger(TR_SessionPause);
        }

        if (overlayRaycaster != null)
            overlayRaycaster.enabled = false;
    }

    private void TriggerSessionResume()
    {
        if (!isSessionUIActive)
            return;

        if (!tutorialMode || tutorialEnemyUIEnabled)
        {
            uiEnemyAnimator?.ResetTrigger(
                TR_SessionPause
            );

            uiEnemyAnimator?.SetTrigger(
                TR_SessionResume
            );
        }

        // Merge panel is not active during tutorial.
        if (!tutorialMode)
        {
            topMergePanleAnimator?.ResetTrigger(TR_SessionPause);
            topMergePanleAnimator?.SetTrigger(TR_SessionResume);

            bottomUIAnimator?.ResetTrigger(TR_SessionPause);
            bottomUIAnimator?.SetTrigger(TR_SessionResume);
        }

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

    private void OnApplicationPause(bool pause)
    {
        AnalyticsEvents.OnAppPaused(pause);
        CloudSaveManager.OnAppPaused(pause);
    }

    private void OnApplicationQuit()
    {
        AnalyticsEvents.OnAppQuit();
        CloudSaveManager.OnAppQuit();
    }

    public void PrepareBottomUIForReward()
    {
        if (bottomUIAnimator == null)
            return;

        bottomUIStartedEarlyForReward = true;

        bottomUIAnimator.ResetTrigger(TR_SessionEnd);
        bottomUIAnimator.SetTrigger(TR_SessionStart);
    }

    public void ShowPauseButtonArt()
    {
        if (pauseButtonArtAnimator == null)
            return;

        pauseButtonArtAnimator.ResetTrigger(TR_PauseButtonOut);
        pauseButtonArtAnimator.SetTrigger(TR_PauseButtonIn);
    }

    public void HidePauseButtonArt()
    {
        if (pauseButtonArtAnimator == null)
            return;

        pauseButtonArtAnimator.ResetTrigger(TR_PauseButtonIn);
        pauseButtonArtAnimator.SetTrigger(TR_PauseButtonOut);
    }
}
